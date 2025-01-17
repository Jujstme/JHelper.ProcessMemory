using System;
using System.Runtime.InteropServices;
using JHelper.Common.MemoryUtils;

namespace JHelper.Common.ProcessInterop.API;

internal static partial class WinAPI
{
    /// <summary>
    /// Reads a value of type T from an external process's memory at a given address, assuming the memory layout is big endian.
    /// </summary>
    /// <typeparam name="T">The type of value to read (must be unmanaged).</typeparam>
    /// <param name="processHandle">Handle to the external process.</param>
    /// <param name="address">The memory address to read from in the external process.</param>
    /// <param name="value">The read value output.</param>
    /// <returns>True if the value is successfully read, false otherwise.</returns>
    internal unsafe static bool ReadProcessMemoryBigEndian<T>(IntPtr processHandle, IntPtr address, out T value) where T: unmanaged
    {
        fixed (void* valuePtr = &value)
        {
            Span<byte> valueBuffer = new(valuePtr, sizeof(T));
            if (!ReadProcessMemory(processHandle, address, valueBuffer))
                return false;

            if (sizeof(T) != sizeof(byte))
                valueBuffer.Reverse();

            return true;
        }
    }

    /// <summary>
    /// Reads memory from an external process into a provided buffer of type T, assuming the memory layout is big endian.
    /// </summary>
    /// <typeparam name="T">The type of elements in the buffer (must be unmanaged).</typeparam>
    /// <param name="processHandle">Handle to the external process.</param>
    /// <param name="address">The memory address to read from in the external process.</param>
    /// <param name="buffer">The buffer where the memory will be written.</param>
    /// <returns>True if the memory is successfully read, false otherwise.</returns>
    /// <exception cref="ArgumentException">Thrown when the buffer is empty.</exception>
    internal static bool ReadProcessMemoryBigEndian<T>(IntPtr processHandle, IntPtr address, Span<T> buffer) where T: unmanaged
    {
        Span<byte> byteBuffer = MemoryMarshal.Cast<T, byte>(buffer);
        if (!ReadProcessMemory(processHandle, address, byteBuffer))
            return false;

        int size;
        unsafe
        {
            size = sizeof(T);
        }

        if (size == sizeof(byte))
            return true;

        for (int i = 0; i < buffer.Length; i++)
            byteBuffer.Slice(i * size, size).Reverse();

        return true;
    }

    /// <summary>
    /// Writes a value of type T into an external process's memory at a given address, assuming the memory layout is big endian.
    /// </summary>
    /// <typeparam name="T">The type of value to write (must be unmanaged).</typeparam>
    /// <param name="processHandle">Handle to the external process.</param>
    /// <param name="address">The memory address to write to in the external process.</param>
    /// <param name="value">The value to write.</param>
    /// <returns>True if the value is successfully written, false otherwise.</returns>
    internal static unsafe bool WriteProcessMemoryBigEndian<T>(IntPtr processHandle, IntPtr address, T value) where T : unmanaged
    {
        T tempValue = value;
        Span<byte> valueBuffer = new(&tempValue, sizeof(T));
        
        if (sizeof (T) != sizeof(byte))
            valueBuffer.Reverse();

        return WriteProcessMemory(processHandle, address, valueBuffer);
    }

    /// <summary>
    /// Writes a buffer of type T into an external process's memory at a given address, assuming the memory layout is big endian.
    /// </summary>
    /// <typeparam name="T">The type of elements in the buffer (must be unmanaged).</typeparam>
    /// <param name="processHandle">Handle to the external process.</param>
    /// <param name="address">The memory address to write to in the external process.</param>
    /// <param name="buffer">The buffer to write from.</param>
    /// <returns>True if the buffer is successfully written, false otherwise.</returns>
    /// <exception cref="ArgumentException">Thrown when the buffer is empty.</exception>
    internal static bool WriteProcessMemoryBigEndian<T>(IntPtr processHandle, IntPtr address, ReadOnlySpan<T> buffer) where T : unmanaged
    {
        ReadOnlySpan<byte> byteBuffer = MemoryMarshal.Cast<T, byte>(buffer);

        int size = byteBuffer.Length;

        using (ArrayRental<byte> buf = size <= 1024 ? new(stackalloc byte[size]) : new(size))
        {
            Span<byte> span = buf.Span;
            byteBuffer.CopyTo(span);

            if (size != sizeof(byte))
            {
                for (int i = 0; i < buffer.Length; i++)
                    span.Slice(i * size, size).Reverse();
            }

            return WriteProcessMemory(processHandle, address, span);
        }
    }
}