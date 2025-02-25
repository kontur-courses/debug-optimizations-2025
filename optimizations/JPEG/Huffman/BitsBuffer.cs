using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace JPEG.Huffman;

internal class BitsBuffer(int maxLenght)
{
    private byte[] buffer = new byte[maxLenght];
    private int bufferLength;
    private int unfinishedBitsBits;
    private int unfinishedBitsBitsCount;
    
    public Span<byte> ToArray(out long bitsCount)
    {
        bitsCount = bufferLength * 8L + unfinishedBitsBitsCount;
        if (unfinishedBitsBitsCount > 0)
            buffer[bufferLength] = (byte)(unfinishedBitsBits << (8 - unfinishedBitsBitsCount));
        return buffer.AsSpan().Slice(0, bufferLength);
    }

    public void Fill(Span<byte> data, BitsWithLength[] encodeTable)
    {
        var bitBuffer = (ulong)unfinishedBitsBits;
        var bitsInBuffer = unfinishedBitsBitsCount;

        ref var pBufferRef = ref MemoryMarshal.GetArrayDataReference(buffer);
        ref var tableRef = ref MemoryMarshal.GetArrayDataReference(encodeTable);
        var written = 0;
        
        foreach (var b in data)
        {
            ref var entry = ref Unsafe.Add(ref tableRef, b);
            var bitsVal = entry.Bits;
            var bitsLength = entry.BitsCount;

            bitBuffer = (bitBuffer << bitsLength) | (uint)bitsVal;
            bitsInBuffer += bitsLength;

            if (bitsInBuffer <= 32) continue;

            var value48 = BinaryPrimitives.ReverseEndianness(bitBuffer >> (bitsInBuffer - 32)) >> 32;
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref pBufferRef, written), value48);
            
            written += 4;
            bitsInBuffer -= 32;
            bitBuffer &= (1UL << bitsInBuffer) - 1;
        }

        bufferLength = written;
        unfinishedBitsBits = (int)bitBuffer;
        unfinishedBitsBitsCount = bitsInBuffer;
    }
}