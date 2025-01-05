using System;
using JHelper.Common.ProcessInterop.API;

namespace JHelper.Common.ProcessInterop;

public partial class ProcessMemory : IDisposable
{
    /// <summary>
    /// Allocates memory in the target process's address space.
    /// The newly-allocated memory region will be flagged for full read, write and execute permissions.
    /// </summary>
    /// <param name="minSize">The minimum size of memory to allocate, in bytes.</param>
    /// <returns>The address of the allocated memory block if successful; otherwise, IntPtr.Zero.</returns>
    public IntPtr Allocate(int minSize)
    {
        WinAPI.AllocateMemory(pHandle, minSize, out IntPtr address);
        return address;
    }

    /// <summary>
    /// Deallocates previously allocated memory in the target process's address space.
    /// </summary>
    /// <param name="address">The address of the memory block to deallocate.</param>
    /// <returns>True if memory was successfully deallocated; otherwise, false.</returns>
    public bool Deallocate(IntPtr address)
    {
        return WinAPI.DeallocateMemory(pHandle, address);
    }
}