using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace JHelper.Common.MemoryUtils;

/// <summary>
/// A utility struct for renting arrays from a shared pool.
/// Uses <see cref="ArrayPool{T}"/> to efficiently manage temporary arrays,
/// reducing memory allocation overhead and garbage collection pressure.
/// </summary>
/// <remarks>
/// This struct should be instantiated with a `using` declaration to ensure it is
/// properly disposed once it goes out of scope. This struct, in particular, implements 
/// a `Dispose` method, which is compatible with `using` in C# 8 and later versions.
/// Using `using` ensures that the array is returned to the pool, avoiding memory
/// leaks or unnecessary memory usage.
/// </remarks>
/// <typeparam name="T">The type of elements in the array. Must be unmanaged.</typeparam>
[SkipLocalsInit]
public readonly ref struct ArrayRental<T> where T : unmanaged
{
    // The rented array from the shared pool
    private readonly T[]? _array;

    // Tracks whether the struct has been disposed.
    // Note: Since `ArrayRental<T>` is a readonly struct, we can't directly set `_disposed`
    // within `Dispose`. We use `Unsafe.AsRef` to bypass this limitation, allowing `_disposed`
    // to be set even in a readonly struct.
    private readonly bool _disposed = false;

    /// <summary>
    /// Provides a span over the rented array up to the requested size.
    /// Allows safe, bounds-checked access to the array without copying data.
    /// </summary>
    public Span<T> Span
    {
        get
        {
            if (_disposed)
                throw new ObjectDisposedException("You can't access the Span in a disposed instance of ArrayRental");
            return _span;
        }
    }

    private readonly Span<T> _span;

    /// <summary>
    /// Initializes the ArrayRental with an existing Span, without renting a new array from the pool.
    /// Useful when the caller already has a Span that needs temporary management.
    /// </summary>
    [SkipLocalsInit]
    public ArrayRental(Span<T> span)
    {
        _array = null;
        _span = span;
    }

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

        // Rent an array from the shared ArrayPool to avoid a new allocation
        _array = ArrayPool<T>.Shared.Rent(size);
        _span = _array.AsSpan(0, size);
    }

    /// <summary>
    /// Releases the rented array back to the shared pool if it hasn't been disposed yet.
    /// Ensures safe cleanup and prevents memory leaks or multiple returns to the pool.
    /// </summary>
    public void Dispose()
    {
        // Only return the array if it hasn't already been disposed
        if (!_disposed)
        {
            if (_array is not null)
                ArrayPool<T>.Shared.Return(_array);

            // Mark as disposed using unsafe workaround (necessary in readonly struct)
            unsafe
            {
                fixed (bool* ptr = &_disposed)
                    *ptr = true;
            }
        }
    }
}