using System.Runtime.InteropServices;
using FluentAssertions;
using JPEG;

namespace JpegTests;

public class JpegTests
{
    [SetUp]
    public void Setup()
    {
    }
    
    [Test]
    public void Dct2D422()
    {
        var expected = new float[64]
        {
            343, -32, -160, -3,343, -32, -160, -3,
            -151, -55, 115, 127,-151, -55, 115, 127,
            141, 37, -128, -76,141, 37, -128, -76,
            -104, -46, 86, 126,-104, -46, 86, 126,
            48, 65, -45, -141,48, 65, -45, -141,
            -21, -62, -9, 162,-21, -62, -9, 162,
            36, 40, -31, -64,36, 40, -31, -64,
            -56, -25, -10, 97,-56, -25, -10, 97,
        };

        var input = new float[64]
        {
            16, 11, 120, 16,16, 11, 120, 16,
            24, 40, 51, 61,24, 40, 51, 61,
            12, 12, 112, 19,12, 12, 112, 19,
            26, 58, 60, 55,26, 58, 60, 55,
            14, 13, 16, 24,14, 13, 16, 24,
            40, 57, 69, 56,40, 57, 69, 56,
            14, 17, 222, 29,14, 17, 222, 29,
            51, 487, 80, 62,51, 487, 80, 62,
        };
        var output = new float[64];
        ref var inputRef = ref MemoryMarshal.GetArrayDataReference(input);
        ref var outputRef = ref MemoryMarshal.GetArrayDataReference(output);
        Dct.Dct2D422(ref inputRef, ref outputRef);

        for (var i = 0; i < 8; i++)
        {
            for (var j = 0; j < 8; j++)
            {
                Console.Write(output[i * 8 + j]);
                Console.Write(" ");
            }

            Console.WriteLine();
        }

        for (var i = 0; i < 64; i++)
        {
            output[i].Should().BeApproximately(expected[i], 3);
        }
    }
    
    [Test]
    public void IDct82D422()
    {
        var expected = new float[]
        {
            16, 11, 10, 16,16, 11, 10, 16,
            24, 39, 51, 61,24, 39, 51, 61,
            12, 12, 14, 18,12, 12, 14, 18,
            26, 58, 60, 55,26, 58, 60, 55,
            14, 13, 15, 23,14, 13, 15, 23,
            40, 57, 69, 56,40, 57, 69, 56,
            14, 17, 22, 29,14, 17, 22, 29,
            51, 87, 80, 62,51, 87, 80, 62,
        };
        var input = new float[]
        {
            200, -31, -17,  -5, 200, -31, -17,  -5,
            -52,  -2,  16,   0, -52,  -2,  16,   0,
            13,   6,  -1,  -1, 13,   6,  -1,  -1,
            -29,   3,  11,   6, -29,   3,  11,   6,
            10,   8,  -7,  -5, 10,   8,  -7,  -5,
            -35,   5,   3,   0, -35,   5,   3,   0,
            11,  13,  -6,   2, 11,  13,  -6,   2,
            -90,  14,  23,   2, -90,  14,  23,   2,
        };
        var output = new float[64];
        ref var inputRef = ref MemoryMarshal.GetArrayDataReference(input);
        ref var outputRef = ref MemoryMarshal.GetArrayDataReference(output);
        Dct.InverseDct2D422(ref inputRef, ref outputRef);

        for (var i = 0; i < 8; i++)
        {
            for (var j = 0; j < 8; j++)
            {
                Console.Write(output[i * 8 + j]);
                Console.Write(" ");
            }

            Console.WriteLine();
        }

        for (var i = 0; i < 64; i++)
        {
            output[i].Should().BeApproximately(expected[i], 3);
        }
    }
}