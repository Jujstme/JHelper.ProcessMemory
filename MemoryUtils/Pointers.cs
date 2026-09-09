using JHelper.Common.ProcessInterop;
using System;
using System.Runtime.InteropServices;

namespace JHelper.Common.MemoryUtils;

/// <summary>
/// Represents a strongly typed 64-bit memory pointer.
/// </summary>
/// <typeparam name="T">The unmanaged structure or value type located at the pointer target address.</typeparam>
[StructLayout(LayoutKind.Sequential)]
public readonly struct Pointer64<T> where T : unmanaged
{
    private readonly long _value;

    /// <summary>
    /// Initializes a new instance of the <see cref="Pointer64{T}"/> struct with a 64-bit memory address.
    /// </summary>
    /// <param name="value">The 64-bit integer representing the raw memory address.</param>
    public Pointer64(long value) => _value = value;

    /// <summary>
    /// Gets the raw memory address.
    /// </summary>
    public IntPtr Value => (IntPtr)_value;

    /// <summary>
    /// Attempts to read and dereference the value of type <typeparamref name="T"/> from the specified process memory.
    /// </summary>
    /// <param name="process">The process memory reader used to access target memory.</param>
    /// <param name="value">When this method returns, contains the value read from memory if successful; otherwise, default.</param>
    /// <returns><c>true</c> if the memory read operation succeeded; otherwise, <c>false</c>.</returns>
    public bool Deref(ProcessMemory process, out T value) => process.Read(Value, out value);

    /// <summary>
    /// Reads and dereferences the value of type <typeparamref name="T"/> from the specified process memory.
    /// </summary>
    /// <param name="process">The process memory reader used to access target memory.</param>
    /// <returns>The value of type <typeparamref name="T"/> read from memory.</returns>
    public T Deref(ProcessMemory process) => process.Read<T>(Value);

    /// <summary>
    /// Reinterprets the pointer address as pointing to a different unmanaged target type.
    /// </summary>
    /// <typeparam name="U">The new unmanaged type to cast the pointer address to.</typeparam>
    /// <returns>A new <see cref="Pointer64{U}"/> targeting the same 64-bit memory address.</returns>
    public Pointer64<U> Cast<U>() where U : unmanaged => new Pointer64<U>(_value);
}

/// <summary>
/// Represents a strongly typed 32-bit memory pointer.
/// </summary>
/// <typeparam name="T">The unmanaged structure or value type located at the pointer target address.</typeparam>
[StructLayout(LayoutKind.Sequential)]
public readonly struct Pointer32<T> where T : unmanaged
{
    private readonly int _value;

    /// <summary>
    /// Initializes a new instance of the <see cref="Pointer32{T}"/> struct with a 32-bit memory address.
    /// </summary>
    /// <param name="value">The 32-bit integer representing the raw memory address.</param>
    public Pointer32(int value) => _value = value;

    /// <summary>
    /// Gets the raw memory address.
    /// </summary>
    public IntPtr Value => (IntPtr)(long)(uint)_value;

    /// <summary>
    /// Attempts to read and dereference the value of type <typeparamref name="T"/> from the specified process memory.
    /// </summary>
    /// <param name="process">The process memory reader used to access target memory.</param>
    /// <param name="value">When this method returns, contains the value read from memory if successful; otherwise, default.</param>
    /// <returns><c>true</c> if the memory read operation succeeded; otherwise, <c>false</c>.</returns>
    public bool Deref(ProcessMemory process, out T value) => process.Read(Value, out value);

    /// <summary>
    /// Reads and dereferences the value of type <typeparamref name="T"/> from the specified process memory.
    /// </summary>
    /// <param name="process">The process memory reader used to access target memory.</param>
    /// <returns>The value of type <typeparamref name="T"/> read from memory.</returns>
    public T Deref(ProcessMemory process) => process.Read<T>(Value);

    /// <summary>
    /// Reinterprets the pointer address as pointing to a different unmanaged target type.
    /// </summary>
    /// <typeparam name="U">The new unmanaged type to cast the pointer address to.</typeparam>
    /// <returns>A new <see cref="Pointer32{U}"/> targeting the same 32-bit memory address.</returns>
    public Pointer32<U> Cast<U>() where U : unmanaged => new Pointer32<U>(_value);
}