using System;
using JHelper.Common.ProcessInterop.API;

namespace JHelper.Common.ProcessInterop;

public partial class ProcessMemory : IDisposable
{
    /// <summary>
    /// Reads a value of type <typeparamref name="T"/> from a given memory address with optional pointer dereferencing using offsets.
    /// </summary>
    /// <typeparam name="T">The unmanaged type of the value to read (e.g., int, float, IntPtr).</typeparam>
    /// <param name="address">The base memory address to read from.</param>
    /// <param name="offsets">Optional offsets for pointer dereferencing (to navigate through pointer chains).</param>
    /// <returns>The value of type <typeparamref name="T"/> if read successfully, otherwise the default value of <typeparamref name="T"/>.</returns>
    public T ReadBigEndian<T>(IntPtr address, params int[] offsets) where T : unmanaged
    {
        if (!DerefOffsetsBigEndian(address, out IntPtr endAddress, offsets))
            return default;

        return ReadBigEndian<T>(endAddress, out T value)
            ? value
            : default;
    }

    /// <summary>
    /// Reads a pointer (IntPtr) from the specified memory address.
    /// </summary>
    /// <param name="address">The memory address to read from.</param>
    /// <returns>The pointer read from the address, or IntPtr.Zero if the read fails.</returns>
    public IntPtr ReadPointerBigEndian(IntPtr address)
    {
        return ReadBigEndian<IntPtr>(address);
    }

    /// <summary>
    /// Reads a pointer (IntPtr) from the specified memory address and outputs it through the <paramref name="value"/> parameter.
    /// </summary>
    /// <param name="address">The memory address to read from.</param>
    /// <param name="value">Outputs the read pointer (IntPtr) if successful.</param>
    /// <returns>True if the read was successful, otherwise false.</returns>
    public bool ReadPointerBigEndian(IntPtr address, out IntPtr value)
    {
        return ReadBigEndian(address, out value);
    }

    /// <summary>
    /// Reads a value of type <typeparamref name="T"/> from the specified memory address and outputs it through the <paramref name="value"/> parameter.
    /// </summary>
    /// <typeparam name="T">The unmanaged type of the value to read (e.g., int, float, IntPtr).</typeparam>
    /// <param name="address">The memory address to read from.</param>
    /// <param name="value">Outputs the value of type <typeparamref name="T"/> if the read is successful.</param>
    /// <returns>True if the read was successful, otherwise false.</returns>
    public bool ReadBigEndian<T>(IntPtr address, out T value) where T : unmanaged
    {
        if (!IsNativePtr<T>())
            return WinAPI.ReadProcessMemoryBigEndian(pHandle, address, out value);

        // Handle pointer reading in an unsafe block for unmanaged memory access.
        value = default;
        unsafe
        {
            fixed (void* ptr = &value)
            {
                Span<byte> bytes = new(ptr, PointerSize);
                if (!ReadArray(address, bytes))
                    return false;
                bytes.Reverse();
                return true;
            }
        }
    }

    /// <summary>
    /// Reads an array of type <typeparamref name="T"/> from the specified memory address.
    /// </summary>
    /// <typeparam name="T">The unmanaged type of elements in the array.</typeparam>
    /// <param name="arrayLength">The number of elements to read.</param>
    /// <param name="address">The memory address to read from.</param>
    /// <param name="offsets">Optional offsets for pointer dereferencing.</param>
    /// <returns>The array of type <typeparamref name="T"/> if read successfully, otherwise an empty array.</returns>
    public T[] ReadArrayBigEndian<T>(int arrayLength, IntPtr address, params int[] offsets) where T : unmanaged
    {
        T[] value = new T[arrayLength];

        if (DerefOffsetsBigEndian(address, out IntPtr endAddress, offsets))
            ReadArrayBigEndian<T>(endAddress, value);

        return value;
    }

    /// <summary>
    /// Reads an array of type <typeparamref name="T"/> from the specified memory address and outputs it.
    /// </summary>
    /// <typeparam name="T">The unmanaged type of elements in the array.</typeparam>
    /// <param name="arrayLength">The number of elements to read.</param>
    /// <param name="address">The memory address to read from.</param>
    /// <param name="array">Outputs the read array.</param>
    /// <returns>True if the array was read successfully, otherwise false.</returns>
    public bool ReadArrayBigEndian<T>(int arrayLength, IntPtr address, out T[] array) where T : unmanaged
    {
        array = new T[arrayLength];
        return ReadArrayBigEndian<T>(address, array);
    }

    /// <summary>
    /// Reads an array of type <typeparamref name="T"/> from the specified memory address into the given <see cref="Span{T}"/>.
    /// </summary>
    /// <typeparam name="T">The unmanaged type of elements in the array.</typeparam>
    /// <param name="address">The memory address to read from.</param>
    /// <param name="array">The span that will hold the read values.</param>
    /// <returns>True if the array was read successfully, otherwise false.</returns>
    public bool ReadArrayBigEndian<T>(IntPtr address, Span<T> array) where T : unmanaged
    {
        if (_disposed)
            throw new InvalidOperationException($"Cannot invoke ReadProcessMemory method on a disposed {nameof(ProcessMemory)} instance");

        return WinAPI.ReadProcessMemoryBigEndian<T>(pHandle, address, array);
    }

    /// <summary>
    /// Dereferences a series of pointer offsets starting from a base memory address.
    /// </summary>
    /// <param name="address">The base memory address to start from.</param>
    /// <param name="offsets">An array of integer offsets to dereference through.</param>
    /// <returns>The final memory address after all dereferencing if successful, otherwise <see cref="IntPtr.Zero"/>.</returns>
    public IntPtr DerefOffsetsBigEndian(IntPtr address, params int[] offsets)
    {
        return DerefOffsetsBigEndian(address, out IntPtr endAddress, offsets)
            ? endAddress
            : default;
    }

    /// <summary>
    /// Dereferences through a series of pointer offsets starting from a base address.
    /// </summary>
    /// <param name="baseAddress">The starting memory address for pointer dereferencing.</param>
    /// <param name="finalAddress">Outputs the final address after dereferencing through all offsets.</param>
    /// <param name="offsets">An array of integer offsets to traverse pointer chains.</param>
    /// <returns>True if the dereferencing was successful, otherwise false.</returns>
    public bool DerefOffsetsBigEndian(IntPtr address, out IntPtr value, params int[] offsets)
    {
        value = address;
        foreach (int offset in offsets)
        {
            if (!ReadPointerBigEndian(value, out value) || value == IntPtr.Zero)
            {
                value = IntPtr.Zero;
                return false;
            }
            value += offset;
        }
        return true;
    }
}
