﻿using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using JHelper.Common.ProcessInterop.API.Definitions;

namespace JHelper.Common.ProcessInterop.API;

internal static partial class WinAPI
{
    /// <summary>
    /// Enumerates the modules of the specified process.
    /// </summary>
    /// <param name="pHandle">Handle to the process whose modules are to be enumerated.</param>
    /// <param name="firstModuleOnly">Indicates whether to return only the first module.</param>
    /// <returns>An enumerable collection of <see cref="ProcessModule"/> objects representing the modules.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the process handle is invalid.</exception>
    [SkipLocalsInit]
    public static IEnumerable<ProcessModule> EnumProcessModules(IntPtr pHandle, bool firstModuleOnly)
    {
        if (pHandle == IntPtr.Zero)
            throw new InvalidOperationException("Invalid process handle.");

        // Define constants for module enumeration
        const uint LIST_MODULES_ALL = 0x03; // Flag to list all modules
        const int initialHandleArraySize = 1024;    // Initial size for the module handles array
        const int allocSize = 512;

        char[]? moduleNameBuffer = null;
        IntPtr[]? moduleHandles = null;

        try
        {
            // Allocate an array to store module handles, using a shared array pool
            moduleHandles = ArrayPool<IntPtr>.Shared.Rent(initialHandleArraySize);
            int handleArraySize = moduleHandles.Length;

            // Allocate an array to store module names
            moduleNameBuffer = ArrayPool<char>.Shared.Rent(allocSize);

            uint bufferSize = (uint)(handleArraySize * IntPtr.Size); // Size of the moduleHandles array in bytes

            // Call the EnumProcessModulesEx function to get the module handles
            bool enumSuccess = EnumProcessModulesEx(pHandle, moduleHandles, bufferSize, out uint bytesNeeded, LIST_MODULES_ALL);

            if (enumSuccess)
            {
                // Calculate the number of modules found
                int moduleCount = (int)(bytesNeeded / IntPtr.Size);

                // If the module count exceeds the initial array size, resize the array
                while (enumSuccess && moduleCount > handleArraySize)
                {
                    ArrayPool<IntPtr>.Shared.Return(moduleHandles); // Return the old array to the pool
                    moduleHandles = ArrayPool<IntPtr>.Shared.Rent(moduleCount); // Rent a new larger array
                    handleArraySize = moduleHandles.Length;

                    bufferSize = (uint)(handleArraySize * IntPtr.Size); // Update the buffer size

                    // Call the enumeration function again with the resized array
                    enumSuccess = EnumProcessModulesEx(pHandle, moduleHandles, bufferSize, out bytesNeeded, LIST_MODULES_ALL);
                    moduleCount = (int)(bytesNeeded / IntPtr.Size);
                }

                if (enumSuccess)
                {
                    int sizeofModuleInfo;
                    unsafe
                    {
                        sizeofModuleInfo = sizeof(MODULEINFO);
                    }

                    // Iterate over the module handles
                    for (int i = 0; i < moduleCount; i++)
                    {
                        // If we only want the first module, exit after the first iteration
                        if (i > 0 && firstModuleOnly)
                            break;

                        // Get the current module handle and skip the iteration if it's invalid
                        IntPtr moduleHandle = moduleHandles[i];
                        if (moduleHandle == IntPtr.Zero)
                            continue;

                        // Retrieve information about the module and skip if the information could not be retrieved
                        if (!GetModuleInformation(pHandle, moduleHandle, out MODULEINFO moduleInfo, sizeofModuleInfo))
                            continue;

                        // Get the module's file name
                        GetModuleFileNameExW(pHandle, moduleHandle, moduleNameBuffer, allocSize);

                        // Convert the unmanaged module name buffer to a managed string
                        int stringLength = moduleNameBuffer.AsSpan().IndexOf('\0');
                        if (stringLength == -1)
                            stringLength = moduleNameBuffer.Length;
                        string moduleFileName = new(moduleNameBuffer, 0, stringLength);

                        // Yield a new Module object containing information about the current module
                        yield return new ProcessModule(
                            pHandle,
                            moduleInfo.lpBaseOfDll,             // Base address of the module
                            moduleInfo.EntryPoint,              // Entry point of the module
                            moduleFileName,                     // Name of the module
                            (int)moduleInfo.SizeOfImage,        // Size of the module image
                            Path.GetFileName(moduleFileName)    // Extract just the file name from the full path
                        );
                    }
                }
            }
        }
        finally
        {
            // Clean up: Free the allocated memory for the module name buffer
            if (moduleNameBuffer is not null)
                ArrayPool<char>.Shared.Return(moduleNameBuffer);

            // Return the rented array of module handles back to the pool
            if (moduleHandles is not null)
                ArrayPool<IntPtr>.Shared.Return(moduleHandles); // Return the rented array back to the pool
        }

        // External methods imported from the Windows API
        [DllImport(Libs.Psapi)]
        [SuppressUnmanagedCodeSecurity]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool EnumProcessModulesEx(IntPtr hProcess, [In, Out] IntPtr[] lphModule, uint cb, [Out] out uint lpcbNeeded, uint dwFilterFlag);

        [DllImport(Libs.Psapi, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        [return: MarshalAs(UnmanagedType.U4)]
        static extern uint GetModuleFileNameExW(IntPtr hProcess, IntPtr hModule, [In, Out] char[] lpBaseName, uint nSize);

        [DllImport(Libs.Psapi)]
        [SuppressUnmanagedCodeSecurity]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetModuleInformation(IntPtr hProcess, IntPtr hModule, [Out] out MODULEINFO lpmodinfo, int nSize);
    }

    [StructLayout(LayoutKind.Sequential)]
    [SkipLocalsInit]
    private readonly struct MODULEINFO
    {
        public readonly IntPtr lpBaseOfDll;
        public readonly uint SizeOfImage;
        public readonly IntPtr EntryPoint;
    }
}
