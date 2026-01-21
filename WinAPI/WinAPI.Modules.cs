using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Net;
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
    public static IEnumerable<ProcessModule> EnumProcessModules(IntPtr pHandle)
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
            int handleArraySize = initialHandleArraySize;
            uint bufferSize = (uint)(handleArraySize * IntPtr.Size); // Size of the moduleHandles array in bytes

            // Allocate an array to store module names
            moduleNameBuffer = ArrayPool<char>.Shared.Rent(allocSize);

            // Call the EnumProcessModulesEx function to get the module handles
            if (!EnumProcessModulesEx(pHandle, moduleHandles, bufferSize, out uint bytesNeeded, LIST_MODULES_ALL))
                yield break;

            // Calculate the number of modules found
            int moduleCount = (int)(bytesNeeded / IntPtr.Size);

            // If the module count exceeds the initial array size, resize the array
            if (moduleCount > handleArraySize)
            {
                const int maxTries = 3;
                int retryCount= 0;

                while (retryCount < maxTries)
                {
                    ArrayPool<IntPtr>.Shared.Return(moduleHandles); // Return the old array to the pool
                    moduleHandles = ArrayPool<IntPtr>.Shared.Rent(moduleCount); // Rent a new larger array
                    handleArraySize = moduleCount;
                    bufferSize = (uint)(handleArraySize * IntPtr.Size); // Update the buffer size

                    // Call the enumeration function again with the resized array
                    if (!EnumProcessModulesEx(pHandle, moduleHandles, bufferSize, out bytesNeeded, LIST_MODULES_ALL))
                        yield break;

                    moduleCount = (int)(bytesNeeded / IntPtr.Size);

                    if (moduleCount <= handleArraySize)
                        break;
                    
                    retryCount++;
                }

                if (retryCount == maxTries)
                    yield break; // Failed to get modules within the retry limit
            }

            int sizeofModuleInfo;
            unsafe
            {
                sizeofModuleInfo = sizeof(MODULEINFO);
            }

            // Iterate over the module handles
            for (int i = 0; i < moduleCount; i++)
            {
                // Get the current module handle and skip the iteration if it's invalid
                IntPtr moduleHandle = moduleHandles[i];
                if (moduleHandle == IntPtr.Zero)
                    continue;

                // Retrieve information about the module and skip if the information could not be retrieved
                if (!GetModuleInformation(pHandle, moduleHandle, out MODULEINFO moduleInfo, sizeofModuleInfo))
                    continue;

                // Get the module's file name
                uint fileLength = GetModuleFileNameExW(pHandle, moduleHandle, moduleNameBuffer, allocSize);
                if (fileLength == 0 || fileLength >= allocSize) // Error or buffer too small
                    continue;
                string moduleFileName = new(moduleNameBuffer, 0, (int)fileLength);

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
