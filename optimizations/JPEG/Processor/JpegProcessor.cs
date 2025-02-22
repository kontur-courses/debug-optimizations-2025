using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Channels;
using JPEG.Huffman;
using JPEG.Images;
using PixelFormat = JPEG.Images.PixelFormat;

namespace JPEG.Processor;

public class JpegProcessor : IJpegProcessor
{
    public static readonly JpegProcessor Init = new();
    public const int CompressionQuality = 70;
    private const int DctSize = 8;
    private const int BlockSize = DctSize * DctSize;

    public void Compress(string imagePath, string compressedImagePath)
    {
        using var fileStream = File.OpenRead(imagePath);
        using var bmp = (Bitmap)Image.FromStream(fileStream, false, false);
        var imageMatrix = (Matrix)bmp;
        //Console.WriteLine($"{bmp.Width}x{bmp.Height} - {fileStream.Length / (1024.0 * 1024):F2} MB");
        var compressionResult = Compress(imageMatrix, CompressionQuality);
        compressionResult.Save(compressedImagePath);
    }

    public void Uncompress(string compressedImagePath, string uncompressedImagePath)
    {
        var compressedImage = CompressedImage.Load(compressedImagePath);
        var uncompressedImage = Uncompress(compressedImage);
        var resultBmp = (Bitmap)uncompressedImage;
        resultBmp.Save(uncompressedImagePath, ImageFormat.Bmp);
    }

    private static CompressedImage Compress(Matrix matrix, int quality = 50)
    {
        var func = new Func<Pixel, double>[] { p => p.Y, p => p.Cb, p => p.Cr };
        
        var index = 0;
        var length = matrix.Pixels.Length * 3;
        Span<byte> allQuantizedBytes = new byte[length];
        
        Span<double> subMatrix = new double[BlockSize];
        Span<double> channelFreqs = new double[BlockSize];
        Span<int> quant = GetQuantizationMatrix(quality);
        
        for (var y = 0; y < matrix.Height; y += DctSize)
        {
            for (var x = 0; x < matrix.Width; x += DctSize)
            {
                foreach (var selector in func)
                {
                    var slice = allQuantizedBytes.Slice(index, BlockSize);
                    GetSubMatrix(matrix, y, x, selector, subMatrix);
                    Dct.DCT2D(subMatrix, channelFreqs);
                    Quantize(channelFreqs, quant, slice);
                    index += BlockSize;
                }
            }
        }

        var compressedBytes = HuffmanCodec.Encode(allQuantizedBytes, out var bitsCount,  out var root);

        return new CompressedImage
        {
            Quality = quality, CompressedBytes = compressedBytes, BitsCount = bitsCount,
            Height = matrix.Height, Width = matrix.Width, Huffman = root,
        };
    }

    private static Matrix Uncompress(CompressedImage image)
    {
        var result = new Matrix(image.Height, image.Width);
        var length = image.Height * image.Width * 3;
        var _y = new double[BlockSize];
        var cb = new double[BlockSize];
        var cr = new double[BlockSize];
        Span<double> channelFreqs = new double[BlockSize];
        Span<int> quant = GetQuantizationMatrix(image.Quality);
        var func = new[] { _y, cb, cr };
        
        var allQuantizedBytes = HuffmanCodec.Decode(image.CompressedBytes, image.BitsCount, length, image.Huffman);
        var index = 0;
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

                
                SetPixels(result, _y, cb, cr, y, x);
                
            }
        }
        
        return result;
    }

    private static void SetPixels(Matrix matrix, Span<double> a, Span<double> b, Span<double> c,
        int yOffset, int xOffset)
    {
        for (var y = 0; y < DctSize; y++)
        for (var x = 0; x < DctSize; x++)
            matrix.Pixels[(yOffset + y) * matrix.Width + xOffset + x] = new Pixel(a[y*8+x] + 128, b[y*8+x] + 128, c[y*8+x] + 128);
    }

    private static void GetSubMatrix(Matrix matrix, int yOffset, int xOffset,
        Func<Pixel, double> componentSelector, Span<double> subMatrix)
    {
        for (var j = 0; j < DctSize; j++)
        for (var i = 0; i < DctSize; i++)
            subMatrix[j*8+i] = componentSelector(matrix.Pixels[(yOffset + j) * matrix.Width + xOffset + i]) - 128;
    }

    private static void Quantize(Span<double> channelFreqs, Span<int> quantizationMatrix, Span<byte> output)
    {
        for (var y = 0; y < BlockSize; y++)
        {
            output[y] = (byte)(channelFreqs[y] / quantizationMatrix[y]);
        }
    }

    private static void DeQuantize(Span<byte> quantizedBytes, Span<int> quant, Span<double> output)
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

        for (var y = 0; y < DctSize; y++)
        {
            for (var x = 0; x < DctSize; x++)
            {
                result[y*8+x] = (multiplier * result[y*8+x] + 50) / 100;
            }
        }

        return result;
    }
}