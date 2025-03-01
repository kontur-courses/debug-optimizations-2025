using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using JPEG.Huffman;
using JPEG.Images;

namespace JPEG.Processor;

public class JpegProcessor : IJpegProcessor
{
    public static readonly JpegProcessor Init = new();
    public const int CompressionQuality = 70;
    private const int DctSize = 8;
    private const int BlockSize = DctSize * DctSize;
    private const int YuvBlockSize = BlockSize * 2;

    public void Compress(string imagePath, string compressedImagePath)
    {
        using var bmp = new SimpleBitmap(imagePath);
        var compressionResult = Compress(bmp, CompressionQuality);
        compressionResult.Save(compressedImagePath);
    }

    public void Uncompress(string compressedImagePath, string uncompressedImagePath)
    {
        var compressedImage = CompressedImage.Load(compressedImagePath);
        using var uncompressedImage = Uncompress(compressedImage);
        uncompressedImage.Save(uncompressedImagePath);
    }

    private static CompressedImage Compress(SimpleBitmap bmp, int quality = 50)
    {
        using var matrix = new Matrix(bmp);

        var length = matrix.Height * matrix.Width * 2;
        Span<byte> allQuantizedBytes = new byte[length];
        Span<float> subMatrix = new float[YuvBlockSize];
        Span<float> channelFreqs = new float[YuvBlockSize];
        Span<float> quantMatrix = ReverseInplace(GetQuantizationMatrix(quality));
        Span<float> quantMatrixChrominance = ReverseInplace(GetQuantizationMatrixChrominance(quality));

        ref var slice = ref allQuantizedBytes.GetPinnableReference();
        ref var sub = ref subMatrix.GetPinnableReference();
        ref var freq = ref channelFreqs.GetPinnableReference();
        ref var quant = ref quantMatrix.GetPinnableReference();
        ref var quantChrominance = ref quantMatrixChrominance.GetPinnableReference();

        for (var y = 0; y < matrix.Height; y += DctSize)
        {
            for (var x = 0; x < matrix.Width; x += DctSize)
            {
                matrix.GetSubMatrix(y, x, ref sub);
                Dct.Dct2D(ref sub, ref freq);
                Dct.Dct2D422(ref Unsafe.Add(ref sub, 64), ref Unsafe.Add(ref freq, 64));

                Quantize(ref freq, ref quant, ref slice);
                Quantize(ref Unsafe.Add(ref freq, 64), ref quantChrominance, ref Unsafe.Add(ref slice, 64));

                slice = ref Unsafe.Add(ref slice, 128);
            }
        }

        var compressedBytes = HuffmanCodec.Encode(allQuantizedBytes, out var bitsCount, out var root);

        return new CompressedImage
        {
            Quality = quality, CompressedBytes = compressedBytes, BitsCount = bitsCount,
            Height = matrix.Height, Width = matrix.Width, Huffman = root,
        };
    }

    private static SimpleBitmap Uncompress(CompressedImage image)
    {
        using var matrix = new Matrix(image.Width, image.Height);
        var length = image.Height * image.Width * 2;

        Span<float> subMatrix = new float[YuvBlockSize];
        Span<float> channelFreqs = new float[YuvBlockSize];
        Span<float> quantMatrix = GetQuantizationMatrix(image.Quality);
        Span<float> quantMatrixChrominance = GetQuantizationMatrixChrominance(image.Quality);

        ref var sub = ref subMatrix.GetPinnableReference();
        ref var freq = ref channelFreqs.GetPinnableReference();
        ref var quant = ref quantMatrix.GetPinnableReference();
        ref var quantChrominance = ref quantMatrixChrominance.GetPinnableReference();

        var allQuantizedBytes = HuffmanCodec.Decode(image.CompressedBytes, image.BitsCount, length, image.Huffman);
        ref var quantizedBytesRef = ref Unsafe.As<byte, sbyte>(ref allQuantizedBytes.GetPinnableReference());
        
        for (var y = 0; y < image.Height; y += DctSize)
        {
            for (var x = 0; x < image.Width; x += DctSize)
            {
                DeQuantize(ref quantizedBytesRef, ref quant, ref freq);
                DeQuantize(ref Unsafe.Add(ref quantizedBytesRef, 64), ref quantChrominance, ref Unsafe.Add(ref freq, 64));

                Dct.InverseDct2D(ref freq, ref sub);
                Dct.InverseDct2D422(ref Unsafe.Add(ref freq, 64), ref Unsafe.Add(ref sub, 64));

                matrix.SetPixels(ref sub, y, x);

                quantizedBytesRef = ref Unsafe.Add(ref quantizedBytesRef, 128);
            }
        }

        //matrix.ApplyDeblockingFilter();

        return matrix.ToBitmap();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Quantize(ref float freq, ref float pQuantRev, ref byte output)
    {
        var permutationMask = Vector256.Create(0, 4, 1, 5, 2, 6, 3, 7);

        var mul0 = Vector256.LoadUnsafe(ref freq, 0) * Vector256.LoadUnsafe(ref pQuantRev, 0);
        var mul1 = Vector256.LoadUnsafe(ref freq, 8) * Vector256.LoadUnsafe(ref pQuantRev, 8);
        var mul2 = Vector256.LoadUnsafe(ref freq, 16) * Vector256.LoadUnsafe(ref pQuantRev, 16);
        var mul3 = Vector256.LoadUnsafe(ref freq, 24) * Vector256.LoadUnsafe(ref pQuantRev, 24);
        var mul4 = Vector256.LoadUnsafe(ref freq, 32) * Vector256.LoadUnsafe(ref pQuantRev, 32);
        var mul5 = Vector256.LoadUnsafe(ref freq, 40) * Vector256.LoadUnsafe(ref pQuantRev, 40);
        var mul6 = Vector256.LoadUnsafe(ref freq, 48) * Vector256.LoadUnsafe(ref pQuantRev, 48);
        var mul7 = Vector256.LoadUnsafe(ref freq, 56) * Vector256.LoadUnsafe(ref pQuantRev, 56);

        var masked0 = Avx.ConvertToVector256Int32(mul0);
        var masked1 = Avx.ConvertToVector256Int32(mul1);
        var tmp0 = Avx2.PackSignedSaturate(masked0, masked1);

        var masked2 = Avx.ConvertToVector256Int32(mul2);
        var masked3 = Avx.ConvertToVector256Int32(mul3);
        var tmp1 = Avx2.PackSignedSaturate(masked2, masked3);

        var masked4 = Avx.ConvertToVector256Int32(mul4);
        var masked5 = Avx.ConvertToVector256Int32(mul5);
        var tmp2 = Avx2.PackSignedSaturate(masked4, masked5);

        var masked6 = Avx.ConvertToVector256Int32(mul6);
        var masked7 = Avx.ConvertToVector256Int32(mul7);
        var tmp3 = Avx2.PackSignedSaturate(masked6, masked7);

        var result = Avx2.PackSignedSaturate(tmp0, tmp1);
        var result2 = Avx2.PackSignedSaturate(tmp2, tmp3);

        Avx2.PermuteVar8x32(result.AsSingle(), permutationMask).AsByte().StoreUnsafe(ref output);
        Avx2.PermuteVar8x32(result2.AsSingle(), permutationMask).AsByte().StoreUnsafe(ref output, 32);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void DeQuantize(ref sbyte quantizedBytes, ref float quant, ref float output)
    {
        for (UIntPtr i = 0; i < 64; i += 8)
        {
            var byteVec = Avx2.ConvertToVector256Int32(Vector128.LoadUnsafe(ref quantizedBytes, i));
            var floatVec = Avx.ConvertToVector256Single(byteVec);

            var quantVec = Vector256.LoadUnsafe(ref quant, i);
            var result = floatVec * quantVec;
            result.StoreUnsafe(ref output, i);
        }
    }

    private static float[] GetQuantizationMatrix(int quality)
    {
        if (quality is < 1 or > 99)
            throw new ArgumentException("quality must be in [1,99] interval");

        var multiplier = quality < 50 ? 5000 / quality : 200 - 2 * quality;

        var result = new float[64];

        for (var y = 0; y < 64; y++)
        {
            result[y] = Math.Max(10, Math.Min((multiplier * LuminanceTable[y] + 50) / 100f, 255));
        }

        return result;
    }

    private static float[] GetQuantizationMatrixChrominance(int quality)
    {
        if (quality is < 1 or > 99)
            throw new ArgumentException("quality must be in [1,99] interval");

        var multiplier = quality < 50 ? 5000 / quality : 200 - 2 * quality;

        var result = new float[64];

        for (var y = 0; y < 64; y++)
        {
            result[y] = Math.Max(10, Math.Min((multiplier * ChrominanceTable[y] + 50) / 100f, 255));
        }

        return result;
    }

    private static float[] ReverseInplace(float[] matrix)
    {
        for (var y = 0; y < 64; y++)
        {
            matrix[y] = 1f / matrix[y];
        }

        return matrix;
    }

    private static readonly int[] LuminanceTable =
    [
        16, 11, 10, 16, 24, 40, 51, 61,
        12, 12, 14, 19, 26, 58, 60, 55,
        14, 13, 16, 24, 40, 57, 69, 56,
        14, 17, 22, 29, 51, 87, 80, 62,
        18, 22, 37, 56, 68, 109, 103, 77,
        24, 35, 55, 64, 81, 104, 113, 92,
        49, 64, 78, 87, 103, 121, 120, 101,
        72, 92, 95, 98, 112, 100, 103, 99
    ];

    private static readonly int[] ChrominanceTable =
    [
        17, 24, 99, 99, 17, 24, 99, 99,
        18, 26, 99, 99, 18, 26, 99, 99,
        24, 56, 99, 99, 24, 56, 99, 99,
        47, 99, 99, 99, 47, 99, 99, 99,
        99, 99, 99, 99, 99, 99, 99, 99,
        99, 99, 99, 99, 99, 99, 99, 99,
        99, 99, 99, 99, 99, 99, 99, 99,
        99, 99, 99, 99, 99, 99, 99, 99,
    ];
}