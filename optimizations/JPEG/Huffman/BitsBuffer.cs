using System.Collections.Generic;

namespace JPEG.Huffman;

internal class BitsBuffer
{
    private List<byte> buffer = [];
    private int unfinishedBitsBits = 0;
    private int unfinishedBitsBitsCount = 0;

    public void Add(BitsWithLength bitsWithLength)
    {
        var bitsCount = bitsWithLength.BitsCount;
        var bits = bitsWithLength.Bits;

        int neededBits = 8 - unfinishedBitsBitsCount;
       
        while (bitsCount >= neededBits)
        {
            bitsCount -= neededBits;
            buffer.Add((byte)((unfinishedBitsBits << neededBits) + (bits >> bitsCount)));

            bits &= ((1 << bitsCount) - 1);

            unfinishedBitsBits = 0;
            unfinishedBitsBitsCount = 0;

            neededBits = 8;
        }

        unfinishedBitsBitsCount += bitsCount;
        unfinishedBitsBits = (unfinishedBitsBits << bitsCount) + bits;
    }

    public byte[] ToArray(out long bitsCount)
    {
        bitsCount = buffer.Count * 8L + unfinishedBitsBitsCount;
        var result = new byte[bitsCount / 8 + (bitsCount % 8 > 0 ? 1 : 0)];
        buffer.CopyTo(result);
        if (unfinishedBitsBitsCount > 0)
            result[buffer.Count] = (byte)(unfinishedBitsBits << (8 - unfinishedBitsBitsCount));
        return result;
    }
}