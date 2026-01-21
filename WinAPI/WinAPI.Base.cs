using System;
using System.Runtime.InteropServices;
using System.Security;
using JHelper.Common.ProcessInterop.API.Definitions;
using System.Runtime.CompilerServices;
using JHelper.Common.MemoryUtils;

namespace JHelper.Common.ProcessInterop.API;

internal static partial class WinAPI
{
    /// <summary>
    /// Opens a handle to a process by its name.
    /// </summary>
    /// <param name="name">The name of the process to search for. The extension (eg: .exe) must be included.</param>
    /// <param name="processId">The ID of the process if found; otherwise, 0.</param>
    /// <param name="pHandle">The handle to the process if found; otherwise, IntPtr.Zero.</param>
    /// <returns>True if the process is found and the handle is opened; otherwise, false.</returns>
    [SkipLocalsInit]
    internal static bool OpenProcessHandleByName(string name, out int processId, out IntPtr pHandle)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Process name cannot be null or empty.", nameof(name));

        // set default values in case of early exit
        processId = 0;
        pHandle = IntPtr.Zero;

        // Initial estimation of the number of processes. 
        // Most Windows systems have 150-300, so 512 provides a safe starting buffer.
        int processIdsArraySize = 512;

        while (true)
        {
            using (ArrayRental<int> processIds = new(processIdsArraySize))
            {
                int bytesNeeded;

                unsafe
                {
                    fixed (int* pProcessIds = processIds.Span)
                    {
                        if (!EnumProcesses(pProcessIds, processIdsArraySize * sizeof(int), out bytesNeeded))
                            return false;

                    }
                }

                // Calculate how many process IDs were actually returned by the API
                int numProcesses = bytesNeeded / sizeof(int);

                // Per MSDN: If bytesNeeded == (arraySize * sizeof(int)), the buffer might have been too small.
                // We only proceed to search if the number of processes returned is strictly less than 
                // our buffer size, ensuring we haven't truncated the process list.
                if (numProcesses < processIdsArraySize)
                {
                    // Iterate through each process ID to find a matching process name
                    for (int i = 0; i < numProcesses; i++)
                    {
                        int localProcessId = processIds.Span[i];

                        // Skip the System Idle Process and System process
                        if (localProcessId == 0 || localProcessId == 4)
                            continue;

                        if (!OpenProcess(localProcessId, out IntPtr localHandle))
                            continue;   // If unable to open, skip to the next process

                        try
                        {
                            // Check if the process matches the requested name (case-insensitive)
                            // If the process is not running, GetProcessName may return an empty string, allowing the loop to continue.
                            if (GetProcessName(localHandle).Equals(name, StringComparison.OrdinalIgnoreCase))
                            {
                                // If a match is found, set the outputs and return true
                                processId = localProcessId;
                                pHandle = localHandle;
                                return true;
                            }
                        }
                        finally
                        {
                            // If we didn't find the right process, close the handle to avoid leaks
                            if (pHandle != localHandle)
                                CloseProcessHandle(localHandle);
                        }
                    }

                    // If the loop finishes without returning, the process was not found.
                    break;
                }

                // If we reached here, it means the buffer was potentially too small (numProcesses == arraySize).
                // We double the size and re-run the EnumProcesses call.
                processIdsArraySize *= 2; // Double the buffer size and try again
            }
        }

        // If no matching process is found, return false
        return false;
        
        // Importing necessary methods from the Windows API
        [DllImport(Libs.Psapi)]
        [SuppressUnmanagedCodeSecurity]
        static unsafe extern bool EnumProcesses(int* lpidProcess, int cb, [Out] out int lpcbNeeded);
    }

    /// <summary>
    /// Retrieves the name of the process associated with the specified handle.
    /// </summary>
    /// <param name="pHandle">A handle to the process whose name is to be retrieved.</param>
    /// <returns>
    /// The name of the process, or an empty string if the name cannot be retrieved.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the process handle is invalid (i.e., IntPtr.Zero).
    /// </exception>
    [SkipLocalsInit]
    internal static string GetProcessName(IntPtr pHandle)
    {
        if (pHandle == IntPtr.Zero)
            throw new InvalidOperationException("Invalid process handle.");
        
        // We assume a max length of 255 for process names. It's a reasonable limit.
        const int BUFFER_LENGTH = 255;

        using (ArrayRental<char> nameBuffer = new(stackalloc char[BUFFER_LENGTH]))
        {
            unsafe
            {
                fixed (char* pNameBuffer = nameBuffer.Span)
                {
                    // Get the base module name (executable name) of the process
                    uint size = GetModuleBaseNameW(pHandle, IntPtr.Zero, pNameBuffer, BUFFER_LENGTH);

                    // If size is 0, we assume the call failed. We can then return an empty string
                    if (size == 0)
                        return string.Empty;

                    return new string(pNameBuffer, 0, (int)size);
                }
            }
        }

        // Import GetModuleBaseNameW from psapi.dll to retrieve the base module name
        [DllImport(Libs.Psapi, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        static unsafe extern uint GetModuleBaseNameW(IntPtr hProcess, IntPtr hModule, char* lpBaseName, int nSize);
    }

    /// <summary>
    /// Opens a handle to the specified process id.
    /// </summary>
    /// <param name="processId">The unique id of the process to hook to.</param>
    /// <returns>A handle to the target process if the function succeeds; otherwise, IntPtr.Zero</returns>
    internal static bool OpenProcess(int processId, out IntPtr processHandle)
    {
        // Constants that define the process access flags for reading and querying the process.
        const uint PROCESS_VM_READ = 0x0010;             // Grants read access to the process's memory
        const uint PROCESS_VM_WRITE = 0x0020;            // Grants write access to the process's memory
        const uint PROCESS_VM_OPERATION = 0x0008;        // Required to perform an operation on the address space of a process (including WriteProcessMemory)
        const uint PROCESS_QUERY_INFORMATION = 0x0400;   // Grants the ability to query information about the process
        const uint SYNCHRONIZE = 0x00100000;             // Allows to use wait functions, eg. WaitForSingleObject

        // Open the process with the required permissions. The function fails if the returned handle is zero.
        processHandle = OpenProcess(PROCESS_VM_READ | PROCESS_VM_WRITE | PROCESS_VM_OPERATION | PROCESS_QUERY_INFORMATION | SYNCHRONIZE, 0, processId);
        return processHandle != IntPtr.Zero;

        // The OpenProcess function is imported from kernel32.dll.
        // It is used to open a handle to the external process with the specified access rights.
        [DllImport(Libs.Kernel32)]
        [SuppressUnmanagedCodeSecurity]
        static extern IntPtr OpenProcess(uint dwDesiredAccess, int bInheritHandle, int dwProcessId);
    }

    /// <summary>
    /// Closes a specified process handle, freeing the related unmanaged resources.
    /// </summary>
    /// <param name="processHandle">The handle to close.</param>
    /// <returns>True if the handle was successfully closed; otherwise, false.</returns>
    internal static bool CloseProcessHandle(IntPtr processHandle)
    {
        return CloseHandle(processHandle);

        // The CloseHandle function is imported from kernel32.dll.
        // It is used to close the handle to the external process when we're done with it.
        // As the handle is an unmanaged resource, it is important to close the handle in order to prevent leaking of resources.
        [DllImport(Libs.Kernel32)]
        [SuppressUnmanagedCodeSecurity]
        static extern bool CloseHandle(IntPtr hObject);
    }

    /// <summary>
    /// Checks if the specified process is still running.
    /// </summary>
    /// <param name="handle">Handle to the process.</param>
    /// <returns>True if the process is still running, false if it has exited.</returns>
    /// <remarks>Note: for this to work, the handle passed in (pHandle) must have been opened using the SYNCHRONIZE access right.</remarks>
    internal static bool IsOpen(IntPtr handle)
    {
        if (handle == IntPtr.Zero)
            throw new InvalidOperationException("Invalid process handle.");

        // Constants used by WaitForSingleObject
        const uint WAIT_TIMEOUT = 0x00000102;

        // Pass 0ms timeout to check immediately without blocking.
        // Process handles become signaled when the process exits,
        // so WAIT_TIMEOUT means the process is still running.
        return WaitForSingleObject(handle, 0) == WAIT_TIMEOUT;

        // Importing WaitForSingleObject from kernel32.dll
        [DllImport(Libs.Kernel32)]
        [SuppressUnmanagedCodeSecurity]
        static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);
    }

    /// <summary>
    /// Determines if the given process is 64-bit.
    /// </summary>
    /// <param name="hProcess">The handle to the process.</param>
    /// <returns>True if the process is 64-bit, false if it is 32-bit.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the handle is invalid.</exception>
    internal static bool Is64Bit(IntPtr hProcess)
    {
        if (hProcess == IntPtr.Zero)
            throw new InvalidOperationException("Invalid process handle.");

        // No processes are 64-bit on a 32-bit OS
        if (!Environment.Is64BitOperatingSystem)
            return false;

        Version osVersion = Environment.OSVersion.Version;

        // IsWow64Process2 is supported starting from Windows 10 1511+ (build 10586)
        if (osVersion.Major > 10 || (osVersion.Major == 10 && osVersion.Build >= 10586))
        {
            const ushort IMAGE_FILE_MACHINE_AMD64 = 0x8664;  // x64
            const ushort IMAGE_FILE_MACHINE_ARM64 = 0xAA64;  // ARM64
            const ushort IMAGE_FILE_MACHINE_UNKNOWN = 0;

            return IsWow64Process2(hProcess, out ushort processMachine, out _)
                && (processMachine == IMAGE_FILE_MACHINE_AMD64 || processMachine == IMAGE_FILE_MACHINE_ARM64 || processMachine == IMAGE_FILE_MACHINE_UNKNOWN);

            [DllImport(Libs.Kernel32)]
            [SuppressUnmanagedCodeSecurity]
            static extern bool IsWow64Process2(IntPtr hProcess, out ushort pProcessMachine, [Out] out ushort pNativeMachine);
        }
        else
        {
            // Use legacy API for older Windows versions
            // WOW64 = Windows 32-bit on Windows 64-bit
            // If not running under WOW64 on a 64-bit OS, the process must be 64-bit
            return IsWow64Process(hProcess, out bool iswow64) && !iswow64;

            [DllImport(Libs.Kernel32)]
            [SuppressUnmanagedCodeSecurity]
            static extern bool IsWow64Process(IntPtr hProcess, [Out] out bool wow64Process);
        }
    }
}
