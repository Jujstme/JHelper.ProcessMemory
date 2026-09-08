using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;

namespace JHelper.Common.MemoryUtils;

/// <summary>
/// The <see cref="Endian"/> class provides methods for switching the byte order (endianness) of unmanaged types.
/// </summary>
public static class Endian
{
    /// <summary>
    /// Converts the byte order (endianness) of an unmanaged type based on the desired endianness.
    /// </summary>
    /// <typeparam name="T">The unmanaged type to convert.</typeparam>
    /// <param name="value">The value whose byte order will be converted.</param>
    /// <param name="endian">The desired endianness (either <see cref="Endianness.Little"/> or <see cref="Endianness.Big"/>).</param>
    /// <returns>The value with its byte order converted to the specified endianness.</returns>
    [SkipLocalsInit]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T FromEndian<T>(T value, Endianness endian) where T : unmanaged
    {
        // Return the value as-is if the system's current endianness matches the desired target endianness.
        if (BitConverter.IsLittleEndian == (endian == Endianness.Little))
            return value;

        var type = typeof(T);

        // 1-byte types require no swapping.
        if (type == typeof(byte) || type == typeof(sbyte))
            return value;
        
        // 2-byte types (short, ushort)
        if (type == typeof(ushort) || type == typeof(short))
        {
            ushort temp = Unsafe.As<T, ushort>(ref value);
            temp = BinaryPrimitives.ReverseEndianness(temp);
            return Unsafe.As<ushort, T>(ref temp);
        }
        
        // 4-byte types (int, uint, float)
        if (type == typeof(uint) || type == typeof(int) || type == typeof(float))
        {
            uint temp = Unsafe.As<T, uint>(ref value);
            temp = BinaryPrimitives.ReverseEndianness(temp);
            return Unsafe.As<uint, T>(ref temp);
        }
        
        // 8-byte types (long, ulong, double)
        if (type == typeof(long) || type == typeof(ulong) || type == typeof(double))
        {
            ulong temp = Unsafe.As<T, ulong>(ref value);
            temp = BinaryPrimitives.ReverseEndianness(temp);
            return Unsafe.As<ulong, T>(ref temp);
        }

        // Restrict usage exclusively to supported primitives to protect against accidental custom struct corruption.
        throw new InvalidOperationException("FromEndian<T> is supported only primitive types.");
    }

    // Common unmanaged types
    [SkipLocalsInit]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static short FromEndian(this short value, Endianness endian) => BitConverter.IsLittleEndian == (endian == Endianness.Little) ? value : BinaryPrimitives.ReverseEndianness(value);
    
    [SkipLocalsInit]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort FromEndian(this ushort value, Endianness endian) => BitConverter.IsLittleEndian == (endian == Endianness.Little) ? value : BinaryPrimitives.ReverseEndianness(value);
    
    [SkipLocalsInit]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int FromEndian(this int value, Endianness endian) => BitConverter.IsLittleEndian == (endian == Endianness.Little) ? value : BinaryPrimitives.ReverseEndianness(value);
    
    [SkipLocalsInit]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint FromEndian(this uint value, Endianness endian) => BitConverter.IsLittleEndian == (endian == Endianness.Little) ? value : BinaryPrimitives.ReverseEndianness(value);
    
    [SkipLocalsInit]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long FromEndian(this long value, Endianness endian) => BitConverter.IsLittleEndian == (endian == Endianness.Little) ? value : BinaryPrimitives.ReverseEndianness(value);
    
    [SkipLocalsInit]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong FromEndian(this ulong value, Endianness endian) => BitConverter.IsLittleEndian == (endian == Endianness.Little) ? value : BinaryPrimitives.ReverseEndianness(value);
    
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
    /// <param name="values">The span of values whose byte order will be converted.</param>
    /// <param name="endian">The desired endianness (either <see cref="Endianness.Little"/> or <see cref="Endianness.Big"/>).</param>
    public static void FromEndian<T>(Span<T> value, Endianness endian) where T : unmanaged
    {
        if (BitConverter.IsLittleEndian == (endian == Endianness.Little))
            return;

        // Swap the byte order of each element in the span.
        for (int i = 0; i < value.Length; i++)
            value[i] = FromEndian(value[i], endian);
    }
}

/// <summary>
/// Defines the possible endianness types: Little-endian or Big-endian.
/// </summary>
public enum Endianness
{
    Little,
    Big,
}