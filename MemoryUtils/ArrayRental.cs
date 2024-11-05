using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace JHelper.Common.MemoryUtils;

/// <summary>
/// A utility struct for renting arrays from a shared pool.
/// Uses <see cref="ArrayPool{T}"/> to efficiently manage temporary arrays,
/// reducing memory allocation overhead and garbage collection pressure.
/// </summary>
/// <typeparam name="T">The type of elements in the array. Must be unmanaged.</typeparam>
[SkipLocalsInit]
public readonly struct ArrayRental<T> : IDisposable where T : unmanaged
{
    // The rented array from the shared pool
    private readonly T[] _array;

    // The requested size of the array
    private readonly int _size;

    // Tracks whether the struct has been disposed
    private readonly bool _disposed = false;

    /// <summary>
    /// Provides a span over the rented array up to the requested size.
    /// Allows safe, bounds-checked access to the array without copying data.
    /// </summary>
    public Span<T> Span => _array.AsSpan(0, _size);

    /// <summary>
    /// Rents an array from the shared pool with the specified size.
    /// </summary>
    /// <param name="size">The number of elements to rent.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the size is less than 1.</exception>
    [SkipLocalsInit]
    public ArrayRental(int size)
    {
        if (size < 1)
            throw new ArgumentOutOfRangeException(nameof(size));

        // Rent an array from the shared pool
        _array = ArrayPool<T>.Shared.Rent(size);

        // Set the requested size (may be less than actual rented array length)
        _size = size;
    }

    /// <summary>
    /// Releases the rented array back to the shared pool.
    /// </summary>
    public void Dispose()
    {
        // Only return the array if it hasn't already been disposed
        if (!_disposed)
        {
            ArrayPool<T>.Shared.Return(_array);

            // Unsafe workaround to modify `_disposed` in a readonly struct
            Unsafe.AsRef(_disposed) = true;
        }
    }
}