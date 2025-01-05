using System;
using System.Runtime.InteropServices;
using System.Security;
using JHelper.Common.ProcessInterop.API.Definitions;

namespace JHelper.Common.ProcessInterop.API;

internal static partial class WinAPI
{
    /// <summary>
    /// Allocates memory in the address space of the specified process.
    /// </summary>
    /// <param name="processHandle">A handle to the target process.</param>
    /// <param name="size">The minimum size of the memory block to allocate, in bytes.</param>
    /// <param name="address">The allocated memory address (out parameter).</param>
    /// <returns>True if memory allocation was successful; otherwise, false.</returns>
    internal static bool AllocateMemory(IntPtr processHandle, int size, out IntPtr address)
    {
        if (processHandle == IntPtr.Zero)
            throw new InvalidOperationException("Invalid process handle.");

        // Constants for memory allocation types and protection options
        const uint MEM_COMMIT = 0x1000;             // Commit memory pages
        const uint MEM_RESERVE = 0x2000;            // Reserve memory pages
        const uint PAGE_EXECUTE_READWRITE = 0x40;   // Memory protection for execute, read, and write

        // Allocate memory in the address space of the target process.
        // Use VirtualAllocEx to reserve and commit the memory with read/write/execute protection.
        address = VirtualAllocEx(processHandle, IntPtr.Zero, size, MEM_COMMIT | MEM_RESERVE, PAGE_EXECUTE_READWRITE);

        // Return true if the allocation was successful (according to Microsoft specifications, the address will be IntPtr.Zero if it fails).
        return address != IntPtr.Zero;

        // Import the VirtualAllocEx function from kernel32.dll
        // It is used to allocate memory in a external process' address space.
        [DllImport(Libs.Kernel32)]
        [SuppressUnmanagedCodeSecurity]
        [return: MarshalAs(UnmanagedType.SysInt)]
        static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, nint dwSize, uint flAllocationType, uint flProtect);
    }

    /// <summary>
    /// Deallocates memory in the address space of the specified process.
    /// </summary>
    /// <param name="processHandle">A handle to the target process.</param>
    /// <param name="address">The base address of the memory region to deallocate.</param>
    /// <returns>True if memory deallocation was successful; otherwise, false.</returns>
    internal static bool DeallocateMemory(IntPtr processHandle, IntPtr address)
    {
        if (processHandle == IntPtr.Zero)
            throw new InvalidOperationException("Invalid process handle.");

        const uint MEM_RELEASE = 0x8000;    // Flag used to signal the need to release previously allocated memory

        return VirtualFreeEx(processHandle, address, 0, MEM_RELEASE);

        // Import the VirtualFreeEx function from kernel32.dll
        // It is used to free allocated memory in a process' address space.
        [DllImport(Libs.Kernel32)]
        [SuppressUnmanagedCodeSecurity]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool VirtualFreeEx(IntPtr hProcess, IntPtr lpAddress, nint dwSize, uint dwFreeType);
    }
}
