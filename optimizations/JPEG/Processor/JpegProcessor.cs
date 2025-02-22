using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.CompilerServices;
using JPEG.Huffman;
using PixelFormat = System.Drawing.Imaging.PixelFormat;

namespace JPEG.Processor;

public class JpegProcessor : IJpegProcessor
{
    public static readonly JpegProcessor Init = new();
    public const int CompressionQuality = 70;
    private const int DctSize = 8;
    private const int BlockSize = DctSize * DctSize;
    private const int RgbBlockSize = DctSize * DctSize * 3;

    public void Compress(string imagePath, string compressedImagePath)
    {
        using var fileStream = File.OpenRead(imagePath);
        using var bmp = (Bitmap)Image.FromStream(fileStream, false, false);
        //Console.WriteLine($"{bmp.Width}x{bmp.Height} - {fileStream.Length / (1024.0 * 1024):F2} MB");
        var compressionResult = Compress(bmp, CompressionQuality);
        compressionResult.Save(compressedImagePath);
        
    }

    public void Uncompress(string compressedImagePath, string uncompressedImagePath)
    {
        var compressedImage = CompressedImage.Load(compressedImagePath);
        var uncompressedImage = Uncompress(compressedImage);
        var resultBmp = uncompressedImage;
        resultBmp.Save(uncompressedImagePath, ImageFormat.Bmp);
    }

    private static unsafe CompressedImage Compress(Bitmap matrix, int quality = 50)
    {
        var index = 0;
        var length = matrix.Height * matrix.Width * 3;
        Span<byte> allQuantizedBytes = new byte[length];
        Span<float> subMatrix = new float[RgbBlockSize];
        Span<float> channelFreqs = new float[RgbBlockSize];
        Span<int> quant = GetQuantizationMatrix(quality);

        var bmpData = matrix.LockBits(new Rectangle(0, 0, matrix.Width, matrix.Height), ImageLockMode.WriteOnly, matrix.PixelFormat);
        ref var scan0 = ref Unsafe.AsRef<byte>(bmpData.Scan0.ToPointer());
        
        for (var y = 0; y < matrix.Height; y += DctSize)
        {
            for (var x = 0; x < matrix.Width; x += DctSize)
            {
                var slice = allQuantizedBytes.Slice(index, RgbBlockSize);
                GetSubMatrix(ref scan0, y,x,subMatrix);
                Dct.DCT2D(subMatrix.Slice(0,64), channelFreqs.Slice(0,64));
                Dct.DCT2D(subMatrix.Slice(64,64), channelFreqs.Slice(64,64));
                Dct.DCT2D(subMatrix.Slice(128,64), channelFreqs.Slice(128,64));
                Quantize(channelFreqs.Slice(0,64), quant, slice.Slice(0,64));
                Quantize(channelFreqs.Slice(64,64), quant, slice.Slice(64,64));
                Quantize(channelFreqs.Slice(128,64), quant, slice.Slice(128,64));
                index += RgbBlockSize;
            }
        }
        matrix.UnlockBits(bmpData);
        
        var compressedBytes = HuffmanCodec.Encode(allQuantizedBytes, out var bitsCount,  out var root);
        
        return new CompressedImage
        {
            Quality = quality, CompressedBytes = compressedBytes, BitsCount = bitsCount,
            Height = matrix.Height, Width = matrix.Width, Huffman = root,
        };
    }

    private static unsafe Bitmap Uncompress(CompressedImage image)
    {
        var matrix = new Bitmap(image.Width, image.Height, PixelFormat.Format24bppRgb);
        
        var length = image.Height * image.Width * 3;
        var _y = new float[BlockSize];
        var cb = new float[BlockSize];
        var cr = new float[BlockSize];
        Span<float> channelFreqs = new float[BlockSize];
        Span<int> quant = GetQuantizationMatrix(image.Quality);
        var func = new[] { _y, cb, cr };
        
        var allQuantizedBytes = HuffmanCodec.Decode(image.CompressedBytes, image.BitsCount, length, image.Huffman);
        var index = 0;
        
        var bmpData = matrix.LockBits(new Rectangle(0, 0, matrix.Width, matrix.Height), ImageLockMode.WriteOnly, matrix.PixelFormat);
        ref var scan0 = ref Unsafe.AsRef<byte>(bmpData.Scan0.ToPointer());
        
        for (var y = 0; y < image.Height; y += DctSize)
        {
            for (var x = 0; x < image.Width; x += DctSize)
            {
                foreach (var channel in func)
                {
                    var quantizedBytes = allQuantizedBytes.Slice(index, BlockSize);
                    DeQuantize(quantizedBytes, quant, channelFreqs);
                    Dct.IDCT2D(channelFreqs, channel); 
                    index += BlockSize;
                }
                
                SetPixels(ref scan0, _y, cb, cr, y, x);
            }
        }
        matrix.UnlockBits(bmpData);
        
        return matrix;
    }

    private static void SetPixels(ref byte scan0, Span<float> a, Span<float> b, Span<float> c,
        int yOffset, int xOffset)
    {
        for (var y = 0; y < DctSize; y++)
        {
            ref var matrix = ref Unsafe.Add(ref scan0, (xOffset + (yOffset + y) * 1024) * 3);
            for (var x = 0; x < DctSize; x++)
            {
                var Y = a[y*8+x] + 128;
                var Cb= b[y*8+x] + 128;
                var Cr= c[y*8+x] + 128;
            
                Unsafe.Add(ref matrix, 0) = ToByte((298.082 * Y + 516.412 * Cb) / 256.0 - 276.836);
                Unsafe.Add(ref matrix, 1) = ToByte((298.082 * Y - 100.291 * Cb - 208.120 * Cr) / 256.0 + 135.576);
                Unsafe.Add(ref matrix, 2) = ToByte((298.082 * Y + 408.583 * Cr) / 256.0 - 222.921);
					
                matrix = ref Unsafe.Add(ref matrix, 3);
            }
        }
    }

    private static byte ToByte(double d)
    {
        return d switch
        {
            > byte.MaxValue => byte.MaxValue,
            < byte.MinValue => byte.MinValue,
            _ => (byte)d
        };
    }

    private static void GetSubMatrix(ref byte bmp, int yOffset, int xOffset, Span<float> subMatrix)
    {
        for (var j = 0; j < DctSize; j++)
        {
            ref var matrix = ref Unsafe.Add(ref bmp, (xOffset + (yOffset + j) * 1024) * 3);
            for (var i = 0; i < DctSize; i++)
            {
                var b = Unsafe.Add(ref matrix, 0);
                var g = Unsafe.Add(ref matrix, 1);
                var r = Unsafe.Add(ref matrix, 2);

                subMatrix[j * 8 + i] = -112f + (65.738f * r + 129.057f * g + 24.064f * b) / 256.0f;
                subMatrix[j * 8 + i + 64] = (-37.945f * r - 74.494f * g + 112.439f * b) / 256.0f;
                subMatrix[j * 8 + i + 128] = (112.439f * r - 94.154f * g - 18.285f * b) / 256.0f;

                matrix = ref Unsafe.Add(ref matrix, 3);
            }
        }
    }

    private static void Quantize(Span<float> channelFreqs, Span<int> quantizationMatrix, Span<byte> output)
    {
        for (var y = 0; y < BlockSize; y++)
        {
            output[y] = (byte)(channelFreqs[y] / quantizationMatrix[y]);
        }
    }

    private static void DeQuantize(Span<byte> quantizedBytes, Span<int> quant, Span<float> output)
    {
        for (var y = 0; y < BlockSize; y++)
        {
            output[y] = ((sbyte)quantizedBytes[y]) * quant[y]; //NOTE cast to sbyte not to loose negative numbers
        }
    }

    private static int[] GetQuantizationMatrix(int quality)
    {
        if (quality is < 1 or > 99)
            throw new ArgumentException("quality must be in [1,99] interval");

        var multiplier = quality < 50 ? 5000 / quality : 200 - 2 * quality;

        var result = new[]
        {
            16, 11, 10, 16, 24, 40, 51, 61 ,
            12, 12, 14, 19, 26, 58, 60, 55 ,
            14, 13, 16, 24, 40, 57, 69, 56 ,
            14, 17, 22, 29, 51, 87, 80, 62 ,
            18, 22, 37, 56, 68, 109, 103, 77 ,
            24, 35, 55, 64, 81, 104, 113, 92 ,
            49, 64, 78, 87, 103, 121, 120, 101 ,
            72, 92, 95, 98, 112, 100, 103, 99
        };

        for (var y = 0; y < BlockSize; y++)
        {
            result[y] = (multiplier * result[y] + 50) / 100;
        }

        return result;
    }
}