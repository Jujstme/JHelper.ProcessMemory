using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace JHelper.Common.MemoryUtils;

/// <summary>
/// The <see cref="Endian"/> class provides methods for switching the byte order (endianness) of unmanaged types.
/// </summary>
public static class Endian
{
    /// <summary>
    /// Converts the byte order (endianness) of a 16-bit signed integer based on the desired endianness.
    /// </summary>
    /// <param name="value">The value whose byte order will be converted.</param>
    /// <param name="endian">The desired endianness (<see cref="Endianness.Little"/> or <see cref="Endianness.Big"/>).</param>
    /// <returns>The converted value matching the target endianness.</returns>
    [SkipLocalsInit]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static short FromEndian(this short value, Endianness endian) => BitConverter.IsLittleEndian == (endian == Endianness.Little) ? value : BinaryPrimitives.ReverseEndianness(value);
    
    /// <summary>
    /// Converts the byte order (endianness) of a 16-bit unsigned integer based on the desired endianness.
    /// </summary>
    /// <param name="value">The value whose byte order will be converted.</param>
    /// <param name="endian">The desired endianness (<see cref="Endianness.Little"/> or <see cref="Endianness.Big"/>).</param>
    /// <returns>The converted value matching the target endianness.</returns>
    [SkipLocalsInit]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort FromEndian(this ushort value, Endianness endian) => BitConverter.IsLittleEndian == (endian == Endianness.Little) ? value : BinaryPrimitives.ReverseEndianness(value);
    
    /// <summary>
    /// Converts the byte order (endianness) of a 32-bit signed integer based on the desired endianness.
    /// </summary>
    /// <param name="value">The value whose byte order will be converted.</param>
    /// <param name="endian">The desired endianness (<see cref="Endianness.Little"/> or <see cref="Endianness.Big"/>).</param>
    /// <returns>The converted value matching the target endianness.</returns>
    [SkipLocalsInit]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int FromEndian(this int value, Endianness endian) => BitConverter.IsLittleEndian == (endian == Endianness.Little) ? value : BinaryPrimitives.ReverseEndianness(value);
    
    /// <summary>
    /// Converts the byte order (endianness) of a 32-bit unsigned integer based on the desired endianness.
    /// </summary>
    /// <param name="value">The value whose byte order will be converted.</param>
    /// <param name="endian">The desired endianness (<see cref="Endianness.Little"/> or <see cref="Endianness.Big"/>).</param>
    /// <returns>The converted value matching the target endianness.</returns>
    [SkipLocalsInit]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint FromEndian(this uint value, Endianness endian) => BitConverter.IsLittleEndian == (endian == Endianness.Little) ? value : BinaryPrimitives.ReverseEndianness(value);
    
    /// <summary>
    /// Converts the byte order (endianness) of a 64-bit signed integer based on the desired endianness.
    /// </summary>
    /// <param name="value">The value whose byte order will be converted.</param>
    /// <param name="endian">The desired endianness (<see cref="Endianness.Little"/> or <see cref="Endianness.Big"/>).</param>
    /// <returns>The converted value matching the target endianness.</returns>
    [SkipLocalsInit]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long FromEndian(this long value, Endianness endian) => BitConverter.IsLittleEndian == (endian == Endianness.Little) ? value : BinaryPrimitives.ReverseEndianness(value);
    
    /// <summary>
    /// Converts the byte order (endianness) of a 64-bit unsigned integer based on the desired endianness.
    /// </summary>
    /// <param name="value">The value whose byte order will be converted.</param>
    /// <param name="endian">The desired endianness (<see cref="Endianness.Little"/> or <see cref="Endianness.Big"/>).</param>
    /// <returns>The converted value matching the target endianness.</returns>
    [SkipLocalsInit]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong FromEndian(this ulong value, Endianness endian) => BitConverter.IsLittleEndian == (endian == Endianness.Little) ? value : BinaryPrimitives.ReverseEndianness(value);
    
    /// <summary>
    /// Converts the byte order (endianness) of a single-precision floating-point number based on the desired endianness.
    /// </summary>
    /// <param name="value">The value whose byte order will be converted.</param>
    /// <param name="endian">The desired endianness (<see cref="Endianness.Little"/> or <see cref="Endianness.Big"/>).</param>
    /// <returns>The converted value matching the target endianness.</returns>
    [SkipLocalsInit]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float FromEndian(this float value, Endianness endian)
    {
        if (BitConverter.IsLittleEndian == (endian == Endianness.Little))
            return value; 

        uint bits = Unsafe.As<float, uint>(ref value);
        bits = BinaryPrimitives.ReverseEndianness(bits);
        return Unsafe.As<uint, float>(ref bits);   
    }

    /// <summary>
    /// Converts the byte order (endianness) of a double-precision floating-point number based on the desired endianness.
    /// </summary>
    /// <param name="value">The value whose byte order will be converted.</param>
    /// <param name="endian">The desired endianness (<see cref="Endianness.Little"/> or <see cref="Endianness.Big"/>).</param>
    /// <returns>The converted value matching the target endianness.</returns>
    [SkipLocalsInit]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double FromEndian(this double value, Endianness endian)
    {
        if (BitConverter.IsLittleEndian == (endian == Endianness.Little))
            return value; 

        ulong bits = Unsafe.As<double, ulong>(ref value);
        bits = BinaryPrimitives.ReverseEndianness(bits);
        return Unsafe.As<ulong, double>(ref bits);   
    }  

    /// <summary>
    /// Converts the byte order (endianness) of each element in a span of unmanaged types based on the desired endianness.
    /// </summary>
    /// <typeparam name="T">The unmanaged type contained within the span.</typeparam>
    /// <param name="value">The span of values whose byte order will be converted in-place.</param>
    /// <param name="endian">The desired endianness (<see cref="Endianness.Little"/> or <see cref="Endianness.Big"/>).</param>
    /// <exception cref="InvalidOperationException">Thrown when <typeparamref name="T"/> is not a supported primitive size (2, 4, or 8 bytes).</exception>
    public static void FromEndian<T>(this Span<T> value, Endianness endian) where T : unmanaged
    {
        if (BitConverter.IsLittleEndian == (endian == Endianness.Little))
            return;

        Type type = typeof(T);

        if (type == typeof(byte) || type == typeof(sbyte))
            return;

        // 2-byte types (short, ushort)
        if (type == typeof(ushort) || type == typeof(short))
        {
            var casted = MemoryMarshal.Cast<T, ushort>(value);
            for (int i = 0; i < casted.Length; i++)
                casted[i] = BinaryPrimitives.ReverseEndianness(casted[i]);
        }

        // 4-byte types (int, uint, float)
        else if (type == typeof(uint) || type == typeof(int) || type == typeof(float))
        {
            var casted = MemoryMarshal.Cast<T, uint>(value);
            for (int i = 0; i < casted.Length; i++)
                casted[i] = BinaryPrimitives.ReverseEndianness(casted[i]);
        }

        // 8-byte types (long, ulong, double)
        else if (type == typeof(long) || type == typeof(ulong) || type == typeof(double))
        {
            var casted = MemoryMarshal.Cast<T, ulong>(value);
            for (int i = 0; i < casted.Length; i++)
                casted[i] = BinaryPrimitives.ReverseEndianness(casted[i]);
        }

        // Restrict usage exclusively to supported primitives to protect against accidental custom struct corruption.
        else
            throw new InvalidOperationException("You can switch endianness of Spans only with primitive types.");
    }
}

/// <summary>
/// Defines the possible endianness types: Little-endian or Big-endian.
/// </summary>
public enum Endianness
{
    /// <summary>
    /// Little-endian byte order (least significant byte first).
    /// </summary>
    Little,

    /// <summary>
    /// Big-endian byte order (most significant byte first).
    /// </summary>
    Big,
}