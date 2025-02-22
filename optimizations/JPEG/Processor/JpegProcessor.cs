using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using JPEG.Images;
using PixelFormat = JPEG.Images.PixelFormat;

namespace JPEG.Processor;

public class JpegProcessor : IJpegProcessor
{
    public static readonly JpegProcessor Init = new();
    public const int CompressionQuality = 70;
    private const int DctSize = 8;

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
        var allQuantizedBytes = new List<byte>();

        var subMatrix = new double[8, 8];
        var channelFreqs = new double[8, 8];
        var quantizedFreqs = new byte[8, 8];
        var result = new byte[8 * 8];
        var quant = GetQuantizationMatrix(quality);

        for (var y = 0; y < matrix.Height; y += DctSize)
        {
            for (var x = 0; x < matrix.Width; x += DctSize)
            {
                foreach (var selector in func)
                {
                    GetSubMatrix(matrix, y, DctSize, x, DctSize, selector, subMatrix);
                    ShiftMatrixValues(subMatrix, -128);
                    Dct.DCT2D(subMatrix, channelFreqs);
                    Quantize(channelFreqs, quant, quantizedFreqs);
                    ZigZagScan(quantizedFreqs, result);
                    allQuantizedBytes.AddRange(result);
                }
            }
        }

        var compressedBytes = HuffmanCodec.Encode(allQuantizedBytes, out var decodeTable, out var bitsCount);

        return new CompressedImage
        {
            Quality = quality, CompressedBytes = compressedBytes, BitsCount = bitsCount, DecodeTable = decodeTable,
            Height = matrix.Height, Width = matrix.Width
        };
    }

    private static Matrix Uncompress(CompressedImage image)
    {
        var result = new Matrix(image.Height, image.Width);
        var _y = new double[DctSize, DctSize];
        var cb = new double[DctSize, DctSize];
        var cr = new double[DctSize, DctSize];
        var quantizedBytes = new byte[DctSize * DctSize];
        var quantizedFreqs = new byte[DctSize, DctSize];
        var channelFreqs = new double[DctSize, DctSize];
        var quant = GetQuantizationMatrix(image.Quality);
        var func = new[] { _y, cb, cr };

        using var allQuantizedBytes =
            new MemoryStream(HuffmanCodec.Decode(image.CompressedBytes, image.DecodeTable, image.BitsCount));

        for (var y = 0; y < image.Height; y += DctSize)
        {
            for (var x = 0; x < image.Width; x += DctSize)
            {
                foreach (var channel in func)
                {
                    allQuantizedBytes.ReadExactly(quantizedBytes, 0, quantizedBytes.Length);
                    ZigZagUnScan(quantizedBytes, quantizedFreqs);
                    DeQuantize(quantizedFreqs, quant, channelFreqs);
                    Dct.IDCT2D(channelFreqs, channel);
                    ShiftMatrixValues(channel, 128);
                }

                SetPixels(result, _y, cb, cr, PixelFormat.YCbCr, y, x);
            }
        }

        return result;
    }

    private static void ShiftMatrixValues(double[,] subMatrix, int shiftValue)
    {
        var height = subMatrix.GetLength(0);
        var width = subMatrix.GetLength(1);

        for (var y = 0; y < height; y++)
        for (var x = 0; x < width; x++)
            subMatrix[y, x] += shiftValue;
    }

    private static void SetPixels(Matrix matrix, double[,] a, double[,] b, double[,] c, PixelFormat format,
        int yOffset, int xOffset)
    {
        var height = a.GetLength(0);
        var width = a.GetLength(1);

        for (var y = 0; y < height; y++)
        for (var x = 0; x < width; x++)
            matrix.Pixels[yOffset + y, xOffset + x] = new Pixel(a[y, x], b[y, x], c[y, x], format);
    }

    private static void GetSubMatrix(Matrix matrix, int yOffset, int yLength, int xOffset, int xLength,
        Func<Pixel, double> componentSelector, double[,] subMatrix)
    {
        for (var j = 0; j < yLength; j++)
        for (var i = 0; i < xLength; i++)
            subMatrix[j, i] = componentSelector(matrix.Pixels[yOffset + j, xOffset + i]);
    }

    private static void ZigZagScan(byte[,] channelFreqs, byte[] output)
    {
        output[0] = channelFreqs[0, 0];
        output[1] = channelFreqs[0, 1];
        output[2] = channelFreqs[1, 0];
        output[3] = channelFreqs[2, 0];
        output[4] = channelFreqs[1, 1];
        output[5] = channelFreqs[0, 2];
        output[6] = channelFreqs[0, 3];
        output[7] = channelFreqs[1, 2];
        output[8] = channelFreqs[2, 1];
        output[9] = channelFreqs[3, 0];
        output[10] = channelFreqs[4, 0];
        output[11] = channelFreqs[3, 1];
        output[12] = channelFreqs[2, 2];
        output[13] = channelFreqs[1, 3];
        output[14] = channelFreqs[0, 4];
        output[15] = channelFreqs[0, 5];
        output[16] = channelFreqs[1, 4];
        output[17] = channelFreqs[2, 3];
        output[18] = channelFreqs[3, 2];
        output[19] = channelFreqs[4, 1];
        output[20] = channelFreqs[5, 0];
        output[21] = channelFreqs[6, 0];
        output[22] = channelFreqs[5, 1];
        output[23] = channelFreqs[4, 2];
        output[24] = channelFreqs[3, 3];
        output[25] = channelFreqs[2, 4];
        output[26] = channelFreqs[1, 5];
        output[27] = channelFreqs[0, 6];
        output[28] = channelFreqs[0, 7];
        output[29] = channelFreqs[1, 6];
        output[30] = channelFreqs[2, 5];
        output[31] = channelFreqs[3, 4];
        output[32] = channelFreqs[4, 3];
        output[33] = channelFreqs[5, 2];
        output[34] = channelFreqs[6, 1];
        output[35] = channelFreqs[7, 0];
        output[36] = channelFreqs[7, 1];
        output[37] = channelFreqs[6, 2];
        output[38] = channelFreqs[5, 3];
        output[39] = channelFreqs[4, 4];
        output[40] = channelFreqs[3, 5];
        output[41] = channelFreqs[2, 6];
        output[42] = channelFreqs[1, 7];
        output[43] = channelFreqs[2, 7];
        output[44] = channelFreqs[3, 6];
        output[45] = channelFreqs[4, 5];
        output[46] = channelFreqs[5, 4];
        output[47] = channelFreqs[6, 3];
        output[48] = channelFreqs[7, 2];
        output[49] = channelFreqs[7, 3];
        output[50] = channelFreqs[6, 4];
        output[51] = channelFreqs[5, 5];
        output[52] = channelFreqs[4, 6];
        output[53] = channelFreqs[3, 7];
        output[54] = channelFreqs[4, 7];
        output[55] = channelFreqs[5, 6];
        output[56] = channelFreqs[6, 5];
        output[57] = channelFreqs[7, 4];
        output[58] = channelFreqs[7, 5];
        output[59] = channelFreqs[6, 6];
        output[60] = channelFreqs[5, 7];
        output[61] = channelFreqs[6, 7];
        output[62] = channelFreqs[7, 6];
        output[63] = channelFreqs[7, 7];
    }

    private static void ZigZagUnScan(byte[] quantizedBytes, byte[,] output)
    {
        output[0, 0] = quantizedBytes[0];
        output[0, 1] = quantizedBytes[1];
        output[0, 2] = quantizedBytes[5];
        output[0, 3] = quantizedBytes[6];
        output[0, 4] = quantizedBytes[14];
        output[0, 5] = quantizedBytes[15];
        output[0, 6] = quantizedBytes[27];
        output[0, 7] = quantizedBytes[28];

        output[1, 0] = quantizedBytes[2];
        output[1, 1] = quantizedBytes[4];
        output[1, 2] = quantizedBytes[7];
        output[1, 3] = quantizedBytes[13];
        output[1, 4] = quantizedBytes[16];
        output[1, 5] = quantizedBytes[26];
        output[1, 6] = quantizedBytes[29];
        output[1, 7] = quantizedBytes[42];

        output[2, 0] = quantizedBytes[3];
        output[2, 1] = quantizedBytes[8];
        output[2, 2] = quantizedBytes[12];
        output[2, 3] = quantizedBytes[17];
        output[2, 4] = quantizedBytes[25];
        output[2, 5] = quantizedBytes[30];
        output[2, 6] = quantizedBytes[41];
        output[2, 7] = quantizedBytes[43];

        output[3, 0] = quantizedBytes[9];
        output[3, 1] = quantizedBytes[11];
        output[3, 2] = quantizedBytes[18];
        output[3, 3] = quantizedBytes[24];
        output[3, 4] = quantizedBytes[31];
        output[3, 5] = quantizedBytes[40];
        output[3, 6] = quantizedBytes[44];
        output[3, 7] = quantizedBytes[53];

        output[4, 0] = quantizedBytes[10];
        output[4, 1] = quantizedBytes[19];
        output[4, 2] = quantizedBytes[23];
        output[4, 3] = quantizedBytes[32];
        output[4, 4] = quantizedBytes[39];
        output[4, 5] = quantizedBytes[45];
        output[4, 6] = quantizedBytes[52];
        output[4, 7] = quantizedBytes[54];

        output[5, 0] = quantizedBytes[20];
        output[5, 1] = quantizedBytes[22];
        output[5, 2] = quantizedBytes[33];
        output[5, 3] = quantizedBytes[38];
        output[5, 4] = quantizedBytes[46];
        output[5, 5] = quantizedBytes[51];
        output[5, 6] = quantizedBytes[55];
        output[5, 7] = quantizedBytes[60];

        output[6, 0] = quantizedBytes[21];
        output[6, 1] = quantizedBytes[34];
        output[6, 2] = quantizedBytes[37];
        output[6, 3] = quantizedBytes[47];
        output[6, 4] = quantizedBytes[50];
        output[6, 5] = quantizedBytes[56];
        output[6, 6] = quantizedBytes[59];
        output[6, 7] = quantizedBytes[61];

        output[7, 0] = quantizedBytes[35];
        output[7, 1] = quantizedBytes[36];
        output[7, 2] = quantizedBytes[48];
        output[7, 3] = quantizedBytes[49];
        output[7, 4] = quantizedBytes[57];
        output[7, 5] = quantizedBytes[58];
        output[7, 6] = quantizedBytes[62];
        output[7, 7] = quantizedBytes[63];
    }

    private static void Quantize(double[,] channelFreqs, int[,] quantizationMatrix, byte[,] output)
    {
        var height = channelFreqs.GetLength(0);
        var width = channelFreqs.GetLength(1);

        for (var y = 0; y < height; y++)
        for (var x = 0; x < width; x++)
        {
            output[y, x] = (byte)(channelFreqs[y, x] / quantizationMatrix[y, x]);
        }
    }

    private static void DeQuantize(byte[,] quantizedBytes, int[,] quant, double[,] output)
    {
        var height = quantizedBytes.GetLength(0);
        var width = quantizedBytes.GetLength(1);
        
        for (var y = 0; y < height; y++)
        for (var x = 0; x < width; x++)
        {
            output[y, x] = ((sbyte)quantizedBytes[y, x]) * quant[y, x]; //NOTE cast to sbyte not to loose negative numbers
        }
    }

    private static int[,] GetQuantizationMatrix(int quality)
    {
        if (quality is < 1 or > 99)
            throw new ArgumentException("quality must be in [1,99] interval");

        var multiplier = quality < 50 ? 5000 / quality : 200 - 2 * quality;

        var result = new[,]
        {
            { 16, 11, 10, 16, 24, 40, 51, 61 },
            { 12, 12, 14, 19, 26, 58, 60, 55 },
            { 14, 13, 16, 24, 40, 57, 69, 56 },
            { 14, 17, 22, 29, 51, 87, 80, 62 },
            { 18, 22, 37, 56, 68, 109, 103, 77 },
            { 24, 35, 55, 64, 81, 104, 113, 92 },
            { 49, 64, 78, 87, 103, 121, 120, 101 },
            { 72, 92, 95, 98, 112, 100, 103, 99 }
        };

        for (var y = 0; y < result.GetLength(0); y++)
        {
            for (var x = 0; x < result.GetLength(1); x++)
            {
                result[y, x] = (multiplier * result[y, x] + 50) / 100;
            }
        }

        return result;
    }
}