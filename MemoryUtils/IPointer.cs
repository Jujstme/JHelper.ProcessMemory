using System;
using System.Runtime.InteropServices;

namespace JHelper.ProcessMemory.MemoryUtils;

/// <summary>
/// Interface that represents a generic pointer to an unmanaged memory location of type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The type of data stored at the pointer location, which must be unmanaged.</typeparam>
public interface IPointer<T> where T : unmanaged
{
    /// <summary>
    /// Gets the address of the memory location pointed to by this pointer.
    /// </summary>
    public IntPtr Address { get; }

    /// <summary>
    /// Reads the value of type <typeparamref name="T"/> from the memory location specified by <see cref="Address"/>.
    /// </summary>
    /// <param name="process">The process whose memory will be accessed.</param>
    /// <param name="value">When this method returns, contains the read value if the operation succeeds; otherwise, default value.</param>
    /// <returns>True if the read operation succeeds; otherwise, false.</returns>
    public bool Read(Common.ProcessInterop.ProcessMemory process, out T value);
}

/// <summary>
/// Represents a 32-bit memory pointer to an unmanaged type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The type of data stored at the pointer location, which must be unmanaged.</typeparam>
[StructLayout(LayoutKind.Explicit, Size = 0x4)]
public readonly struct Pointer32<T> : IPointer<T> where T : unmanaged
{
    [FieldOffset(0)] private readonly int _address;

    public IntPtr Address => (IntPtr)_address;

    public Pointer32(IntPtr address)
    {
        _address = (int)address;
    }

    public bool Read(Common.ProcessInterop.ProcessMemory process, out T value)
    {
        return process.Read(Address, out value);
    }
}

/// <summary>
/// Represents a 64-bit memory pointer to an unmanaged type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The type of data stored at the pointer location, which must be unmanaged.</typeparam>
[StructLayout(LayoutKind.Explicit, Size = 0x8)]
public readonly struct Pointer64<T> : IPointer<T> where T : unmanaged
{
    [FieldOffset(0)] private readonly long _address;

    public IntPtr Address => (IntPtr) _address;

    public Pointer64(IntPtr address)
    {
        _address = (long) address;
    }

    public bool Read(Common.ProcessInterop.ProcessMemory process, out T value)
    {
        return process.Read(Address, out value);
    }
}
