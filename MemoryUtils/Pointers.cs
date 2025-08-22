using System;
using System.Runtime.CompilerServices;

namespace JHelper.Common.MemoryUtils;

/// <summary>
/// Represents a typed pointer abstraction for reading process memory.
/// <typeparam name="TValue">The type of the value being pointed to.</typeparam>
/// <typeparam name="TPointer">The type of the pointer/address (must be <see cref="int"/> or <see cref="long"/>).</typeparam>
/// </summary>
[SkipLocalsInit]
public readonly struct Ptr<TValue, TPointer>
    where TValue : unmanaged
    where TPointer : unmanaged
{
    /// <summary>
    /// The underlying value representing the pointer. Its interpretation depends on <typeparamref name="S"/>.
    /// </summary>
    private readonly TPointer _value;

    /// <summary>
    /// Initializes a new instance of the <see cref="Ptr{T, S}"/> struct with the specified value.
    /// </summary>
    /// <param name="value">The value representing the pointer.</param>
    public Ptr(TPointer value) => _value = value;

    /// <summary>
    /// Gets the pointer as an <see cref="IntPtr"/>, interpreting it according to the size of <typeparamref name="S"/>.
    /// Supports 32-bit (<see cref="int"/>) and 64-bit (<see cref="long"/>) pointers.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if <typeparamref name="S"/> is not 4 or 8 bytes in size.</exception>
    public IntPtr Value
    {
        get
        {
            unsafe
            {
                fixed (void* ptr = &_value)
                {
                    return sizeof(TPointer) switch
                    {
                        4 => (IntPtr)(*(int*)ptr),
                        8 => (IntPtr)(*(long*)ptr),
                        _ => throw new InvalidOperationException(),
                    };
                }
            }
        }
    }

    /// <summary>
    /// Static constructor ensures that only supported pointer types are used.
    /// </summary>
    /// <exception cref="NotSupportedException">
    /// Thrown if <typeparamref name="S"/> is not <see cref="int"/> or <see cref="long"/>.
    /// </exception>
    static Ptr()
    {
        if (typeof(TPointer) != typeof(int) && typeof(TPointer) != typeof(long))
            throw new NotSupportedException($"An instance of Ptr<T, S> can only be instantiated as Ptr<T, int> or Ptr<T, long>");
    }

    /// <summary>
    /// Reads the value of type <typeparamref name="T"/> from the specified process memory at the address of this pointer.
    /// </summary>
    /// <param name="process">The process memory wrapper to read from.</param>
    /// <param name="value">Outputs the value read from memory.</param>
    /// <returns>True if the read was successful; otherwise, false.</returns>
    public bool Deref(Common.ProcessInterop.ProcessMemory process, out TValue value) => process.Read(Value, out value);

    /// <summary>
    /// Reads and returns the value of type <typeparamref name="T"/> from the specified process memory at the address of this pointer.
    /// </summary>
    /// <param name="process">The process memory wrapper to read from.</param>
    /// <returns>The value read from memory.</returns>
    public TValue Deref(Common.ProcessInterop.ProcessMemory process) => process.Read<TValue>(Value);

    /// <summary>
    /// Casts a pointer from the same address to a different value type.
    /// This is useful when you want to reinterpret the pointed-to memory as a different type.
    /// </summary>
    /// <typeparam name="RValue">The new type of the value being pointed to. Must be an unmanaged type.</typeparam>
    /// <returns>
    /// A new <see cref="Ptr{RValue, TPointer}"/> instance with the same pointer address
    /// but pointing to a value of type <typeparamref name="RValue"/>.
    /// </returns>
    public Ptr<RValue, TPointer> Cast<RValue>() where RValue : unmanaged
    {
        return new Ptr<RValue, TPointer>(_value);
    }
}