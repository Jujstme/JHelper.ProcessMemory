using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using JHelper.Common.MemoryUtils;

namespace JHelper.Common.ProcessInterop.API;

internal static partial class WinAPI
{
    /// <summary>
    /// Enumerates function entries in the specified process module.
    /// </summary>
    /// <returns>An enumerable collection of Symbol records containing function names and addresses.</returns>
    [SkipLocalsInit]
    internal static IEnumerable<Symbol> EnumerateFunctions(IntPtr pHandle, IntPtr moduleBaseAddress)
    {
        if (pHandle == IntPtr.Zero)
            throw new InvalidOperationException("Invalid process handle.");

        // Read the DOS header to find the location of the PE header (offset at 0x3C is e_lfanew).
        if (!ReadProcessMemory<int>(pHandle, moduleBaseAddress + 0x3C, out int e_lfanew))
            yield break;

        // Calculate the address of the PE header using the e_lfanew offset.
        IntPtr peHeaderAddress = moduleBaseAddress + e_lfanew;

        if (!ReadProcessMemory<ushort>(pHandle, peHeaderAddress + 24, out ushort flag))
            yield break;

        // The flag 0x20B signifies a 64-bit PE file; anything else is assumed 32-bit.
        // As a 64-bit process doesn't necessarily imply that every module is 64-bit
        // as well (especially for WOW64 modules) we will evaluate this directly from
        // the PE header.
        bool is64Bit = flag == 0x20B;
        int optionalHeaderOffset = is64Bit ? 0x88 : 0x78; // Offset to the export directory depending on 32-bit or 64-bit

        // Read the address of the export directory (RVA) from the optional header.
        if (!ReadProcessMemory<int>(pHandle, peHeaderAddress + optionalHeaderOffset, out int exportDirectoryRVA))
            yield break;

        // Calculate the address of the export directory by adding the RVA to the base address.
        IntPtr exportDirectory = moduleBaseAddress + exportDirectoryRVA;

        // Read and parse the export directory data structure.
        if (!ReadExportDirectory(pHandle, exportDirectory, out ExportDirectoryData exportData))
            yield break;

        // Proceed only if export data was successfully retrieved.
        int numberOfFunctions = exportData.NumberOfFunctions;
        int numberOfNames = exportData.NumberOfNames;

        // Allocate arrays to store function addresses and function name addresses.
        int[] functionAddresses = ArrayPool<int>.Shared.Rent(numberOfFunctions);
        int[] nameAddresses = ArrayPool<int>.Shared.Rent(numberOfNames);
        ushort[] nameOrdinals = ArrayPool<ushort>.Shared.Rent(numberOfNames);

        try
        {
            // As ArrayPool is nto guaranteed to give us exactly the size we want for the array,
            // we manually specify it in a Span
            Span<int> sFunctionAddresses = functionAddresses.AsSpan(0, numberOfFunctions);
            Span<int> sNameAddresses = nameAddresses.AsSpan(0, numberOfNames);
            Span<ushort> sNameOrdinals = nameOrdinals.AsSpan(0, numberOfNames);

            // Read the arrays of function pointers and function name RVAs from the export directory.
            if (!ReadProcessMemory<int>(pHandle, moduleBaseAddress + exportData.AddressOfFunctionsRVA, sFunctionAddresses)
                || !ReadProcessMemory<int>(pHandle, moduleBaseAddress + exportData.AddressOfNamesRVA, sNameAddresses)
                || !ReadProcessMemory(pHandle, moduleBaseAddress + exportData.AddressOfNameOrdinalsRVA, sNameOrdinals))
                yield break;

            // Iterate over each function name
            for (int i = 0; i < numberOfNames; i++)
            {
                // Read the function name from memory using the name address.
                string? functionName = GetFunctionName(pHandle, moduleBaseAddress + nameAddresses[i]);
                if (functionName is not null)
                {
                    int ordinal = nameOrdinals[i];

                    if (ordinal < numberOfFunctions)
                    {
                        // If a valid function name was retrieved, yield it along with the function address.
                        yield return new Symbol(functionName, moduleBaseAddress + functionAddresses[ordinal]);
                    }
                }
            }
        }
        finally
        {
            ArrayPool<int>.Shared.Return(functionAddresses);
            ArrayPool<int>.Shared.Return(nameAddresses);
            ArrayPool<ushort>.Shared.Return(nameOrdinals);
        }
      
        /// <summary>
        /// Reads the export directory structure from the given memory address in the external process.
        /// </summary>
        /// <param name="processHandle">Handle to the external process.</param>
        /// <param name="exportDirectory">Address of the export directory in the external process.</param>
        /// <returns>An ExportDirectoryData object containing relevant details of the export directory, or null if reading fails.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [SkipLocalsInit]
        static bool ReadExportDirectory(IntPtr processHandle, IntPtr exportDirectory, out ExportDirectoryData exportData)
        {
            using (ArrayRental<int> exportDirBuffer = new(stackalloc int[10]))
            {
                if (!ReadProcessMemory<int>(processHandle, exportDirectory, exportDirBuffer.Span))
                {
                    exportData = default;
                    return false;
                }

                exportData = new ExportDirectoryData
                (
                    exportDirBuffer.Span[5], // Number of functions in the export table
                    exportDirBuffer.Span[6], // Number of names in the export table
                    exportDirBuffer.Span[7], // RVA to the function addresses
                    exportDirBuffer.Span[8], // RVA to the name addresses
                    exportDirBuffer.Span[9]  // RVA to the name ordinals
                );
            }

            return true;
        }

        /// <summary>
        /// Extracts a function name from the external process memory based on its address.
        /// </summary>
        /// <param name="processHandle">Handle to the external process.</param>
        /// <param name="nameAddress">Address where the function name is stored in memory.</param>
        /// <returns>A string containing the function name, or null if reading fails.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [SkipLocalsInit]
        static string? GetFunctionName(IntPtr processHandle, IntPtr nameAddress)
        {
            using (ArrayRental<sbyte> nameBytes = new(stackalloc sbyte[255]))
            {
                Span<sbyte> span = nameBytes.Span;

                if (!ReadProcessMemory<sbyte>(processHandle, nameAddress, span))
                    return null;

                int nullTerminatorIndex = span.IndexOf((sbyte)0);

                int length = nullTerminatorIndex == -1
                    ? span.Length
                    : nullTerminatorIndex;

                unsafe
                {
                    fixed (sbyte* ptr = nameBytes.Span)
                        return new string(ptr, 0, length);
                }
            }
        }
    }

    /// <summary>
    /// Structure that holds parsed information from the export directory.
    /// </summary>
    [SkipLocalsInit]
    private readonly ref struct ExportDirectoryData(int numberOfFunctions, int numberOfNames, int addressOfFunctionsRVA, int addressOfNamesRVA, int addressOfNameOrdinalsRVA)
    {
        public readonly int NumberOfFunctions = numberOfFunctions; // Number of functions in the export table
        public readonly int NumberOfNames = numberOfNames; // Number of names in the export table
        public readonly int AddressOfFunctionsRVA = addressOfFunctionsRVA; // RVA of the functions array
        public readonly int AddressOfNamesRVA = addressOfNamesRVA; // RVA of the names array
        public readonly int AddressOfNameOrdinalsRVA = addressOfNameOrdinalsRVA; // RVA of the name ordinals array
    }
}
