﻿using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using JHelper.Common.ProcessInterop.API;

namespace JHelper.Common.ProcessInterop;

/// <summary>
/// Provides functionality for interacting with the memory of an external process.
/// </summary>
public partial class ProcessMemory : IDisposable
{
    /// <summary>
    /// Gets the process ID of the attached process.
    /// </summary>
    public int Id { get; }

    /// <summary>
    /// Internal handle to the external process.
    /// </summary>
    private readonly IntPtr pHandle;

    /// <summary>
    /// The name of the hooked process
    /// </summary>
    public string ProcessName
    {
        get
        {
            processName ??= WinAPI.GetProcessName(pHandle);
            return processName;
        }
    }
    private string? processName = null;

    /// <summary>
    /// Indicates whether the attached process is 64-bit.
    /// </summary>
    public bool Is64Bit
    {
        get
        {
            is64Bit ??= WinAPI.Is64Bit(pHandle);
            return is64Bit.Value;
        }
    }
    private bool? is64Bit = null;

    /// <summary>
    /// Gets the size of pointers for the process (8 bytes for 64-bit, 4 bytes for 32-bit).
    /// </summary>
    public byte PointerSize
    {
        get
        {
            pointerSize ??= (byte)(Is64Bit ? 8 : 4);
            return pointerSize.Value;
        }
    }
    private byte? pointerSize = null;

    /// <summary>
    /// Checks if the process handle is still valid and the process is open.
    /// </summary>
    public bool IsOpen => WinAPI.IsOpen(pHandle);

    /// <summary>
    /// The main module (executable) of the attached process.
    /// </summary>
    public ProcessModule MainModule
    {
        get
        {
            mainModule ??= new ProcessModuleCollection(pHandle, true).First();
            return mainModule.Value;
        }
    }
    private ProcessModule? mainModule = null;

    /// <summary>
    /// Gets the collection of loaded modules (DLLs or shared libraries) in the process.
    /// </summary>
    public ProcessModuleCollection Modules
    {
        get
        {
            modules ??= new ProcessModuleCollection(pHandle, false);
            return modules.Value;
        }
    }
    private ProcessModuleCollection? modules = null;

    /// <summary>
    /// Gets a collection of the memory pages in the process.
    /// </summary>
    public MemoryPagesSnapshot MemoryPages
    {
        get
        {
            memoryPages ??= new MemoryPagesSnapshot(pHandle, true);
            return memoryPages.Value;
        }
    }
    private MemoryPagesSnapshot? memoryPages = null;


    private ProcessMemory(int processId, IntPtr handle)
    {
        Id = processId;
        pHandle = handle;
    }

    /// <summary>
    /// Attaches to an external process by its process ID.
    /// </summary>
    /// <param name="processId">The ID of the process to hook into.</param>
    /// <returns>A ProcessMemory instance if successful, otherwise null.</returns>
    public static ProcessMemory? HookProcess(int processId) => WinAPI.OpenProcess(processId, out IntPtr handle) ? new(processId, handle) : null;

    /// <summary>
    /// Attaches to an external process using a Process object.
    /// </summary>
    /// <param name="process">The Process object representing the target process.</param>
    /// <returns>A ProcessMemory instance if successful, otherwise null.</returns>
    public static ProcessMemory? HookProcess(Process process) => WinAPI.OpenProcess(process.Id, out IntPtr handle) ? new(process.Id, handle) : null;

    /// <summary>
    /// Attaches to an external process by its name.
    /// </summary>
    /// <param name="processName">The name of the process to hook into.</param>
    /// <returns>A ProcessMemory instance if successful, otherwise null.</returns>
    public static ProcessMemory? HookProcess(string processName)
    {
        if (!WinAPI.OpenProcessHandleByName(processName, out int pId, out IntPtr pHandle))
            return null;
        
        return new ProcessMemory(pId, pHandle);
    }

    /// <summary>
    /// Releases resources used by the ProcessMemory instance and closes the process handle.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            if (pHandle != IntPtr.Zero)
                WinAPI.CloseProcessHandle(pHandle);
        }
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    private bool _disposed = false;

    /// <summary>
    /// Determines if the type <typeparamref name="T"/> is a native pointer type (e.g., IntPtr or UIntPtr).
    /// </summary>
    /// <typeparam name="T">The type to check.</typeparam>
    /// <returns>True if the type is a native pointer type, otherwise false.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IsNativePtr<T>()
    {
        Type type = typeof(T);
        return type == typeof(IntPtr) || type == typeof(UIntPtr);
    }
}
