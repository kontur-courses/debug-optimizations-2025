using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
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
        Span<byte> quantizedFreqs = new byte[BlockSize];
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
                    Quantize(channelFreqs, quant, quantizedFreqs);
                    ZigZagScan(quantizedFreqs, slice);
                    index += BlockSize;
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
        var _y = new double[BlockSize];
        var cb = new double[BlockSize];
        var cr = new double[BlockSize];
        Span<byte> quantizedFreqs = new byte[BlockSize];
        Span<double> channelFreqs = new double[BlockSize];
        Span<int> quant = GetQuantizationMatrix(image.Quality);
        var func = new[] { _y, cb, cr };

        var allQuantizedBytes = HuffmanCodec.Decode(image.CompressedBytes, image.DecodeTable, image.BitsCount).AsSpan();
        var index = 0;
        for (var y = 0; y < image.Height; y += DctSize)
        {
            for (var x = 0; x < image.Width; x += DctSize)
            {
                foreach (var channel in func)
                {
                    var quantizedBytes = allQuantizedBytes.Slice(index, BlockSize);
                    ZigZagUnScan(quantizedBytes, quantizedFreqs);
                    DeQuantize(quantizedFreqs, quant, channelFreqs);
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

    private static void ZigZagScan(Span<byte> channelFreqs, Span<byte> output)
    {
        output[0] = channelFreqs[0*8+0];
        output[1] = channelFreqs[0*8+1];
        output[2] = channelFreqs[1*8+0];
        output[3] = channelFreqs[2*8+0];
        output[4] = channelFreqs[1*8+1];
        output[5] = channelFreqs[0*8+2];
        output[6] = channelFreqs[0*8+3];
        output[7] = channelFreqs[1*8+2];
        output[8] = channelFreqs[2*8+1];
        output[9] = channelFreqs[3*8+0];
        output[10] = channelFreqs[4*8+0];
        output[11] = channelFreqs[3*8+1];
        output[12] = channelFreqs[2*8+2];
        output[13] = channelFreqs[1*8+3];
        output[14] = channelFreqs[0*8+4];
        output[15] = channelFreqs[0*8+5];
        output[16] = channelFreqs[1*8+4];
        output[17] = channelFreqs[2*8+3];
        output[18] = channelFreqs[3*8+2];
        output[19] = channelFreqs[4*8+1];
        output[20] = channelFreqs[5*8+0];
        output[21] = channelFreqs[6*8+0];
        output[22] = channelFreqs[5*8+1];
        output[23] = channelFreqs[4*8+2];
        output[24] = channelFreqs[3*8+3];
        output[25] = channelFreqs[2*8+4];
        output[26] = channelFreqs[1*8+5];
        output[27] = channelFreqs[0*8+6];
        output[28] = channelFreqs[0*8+7];
        output[29] = channelFreqs[1*8+6];
        output[30] = channelFreqs[2*8+5];
        output[31] = channelFreqs[3*8+4];
        output[32] = channelFreqs[4*8+3];
        output[33] = channelFreqs[5*8+2];
        output[34] = channelFreqs[6*8+1];
        output[35] = channelFreqs[7*8+0];
        output[36] = channelFreqs[7*8+1];
        output[37] = channelFreqs[6*8+2];
        output[38] = channelFreqs[5*8+3];
        output[39] = channelFreqs[4*8+4];
        output[40] = channelFreqs[3*8+5];
        output[41] = channelFreqs[2*8+6];
        output[42] = channelFreqs[1*8+7];
        output[43] = channelFreqs[2*8+7];
        output[44] = channelFreqs[3*8+6];
        output[45] = channelFreqs[4*8+5];
        output[46] = channelFreqs[5*8+4];
        output[47] = channelFreqs[6*8+3];
        output[48] = channelFreqs[7*8+2];
        output[49] = channelFreqs[7*8+3];
        output[50] = channelFreqs[6*8+4];
        output[51] = channelFreqs[5*8+5];
        output[52] = channelFreqs[4*8+6];
        output[53] = channelFreqs[3*8+7];
        output[54] = channelFreqs[4*8+7];
        output[55] = channelFreqs[5*8+6];
        output[56] = channelFreqs[6*8+5];
        output[57] = channelFreqs[7*8+4];
        output[58] = channelFreqs[7*8+5];
        output[59] = channelFreqs[6*8+6];
        output[60] = channelFreqs[5*8+7];
        output[61] = channelFreqs[6*8+7];
        output[62] = channelFreqs[7*8+6];
        output[63] = channelFreqs[7*8+7];
    }

    private static void ZigZagUnScan(Span<byte> quantizedBytes, Span<byte> output)
    {
        output[0] = quantizedBytes[0];
        output[1] = quantizedBytes[1];
        output[2] = quantizedBytes[5];
        output[3] = quantizedBytes[6];
        output[4] = quantizedBytes[14];
        output[5] = quantizedBytes[15];
        output[6] = quantizedBytes[27];
        output[7] = quantizedBytes[28];
        output[8] = quantizedBytes[2];
        output[9] = quantizedBytes[4];
        output[10] = quantizedBytes[7];
        output[11] = quantizedBytes[13];
        output[12] = quantizedBytes[16];
        output[13] = quantizedBytes[26];
        output[14] = quantizedBytes[29];
        output[15] = quantizedBytes[42];
        output[16] = quantizedBytes[3];
        output[17] = quantizedBytes[8];
        output[18] = quantizedBytes[12];
        output[19] = quantizedBytes[17];
        output[20] = quantizedBytes[25];
        output[21] = quantizedBytes[30];
        output[22] = quantizedBytes[41];
        output[23] = quantizedBytes[43];
        output[24] = quantizedBytes[9];
        output[25] = quantizedBytes[11];
        output[26] = quantizedBytes[18];
        output[27] = quantizedBytes[24];
        output[28] = quantizedBytes[31];
        output[29] = quantizedBytes[40];
        output[30] = quantizedBytes[44];
        output[31] = quantizedBytes[53];
        output[32] = quantizedBytes[10];
        output[33] = quantizedBytes[19];
        output[34] = quantizedBytes[23];
        output[35] = quantizedBytes[32];
        output[36] = quantizedBytes[39];
        output[37] = quantizedBytes[45];
        output[38] = quantizedBytes[52];
        output[39] = quantizedBytes[54];
        output[40] = quantizedBytes[20];
        output[41] = quantizedBytes[22];
        output[42] = quantizedBytes[33];
        output[43] = quantizedBytes[38];
        output[44] = quantizedBytes[46];
        output[45] = quantizedBytes[51];
        output[46] = quantizedBytes[55];
        output[47] = quantizedBytes[60];
        output[48] = quantizedBytes[21];
        output[49] = quantizedBytes[34];
        output[50] = quantizedBytes[37];
        output[51] = quantizedBytes[47];
        output[52] = quantizedBytes[50];
        output[53] = quantizedBytes[56];
        output[54] = quantizedBytes[59];
        output[55] = quantizedBytes[61];
        output[56] = quantizedBytes[35];
        output[57] = quantizedBytes[36];
        output[58] = quantizedBytes[48];
        output[59] = quantizedBytes[49];
        output[60] = quantizedBytes[57];
        output[61] = quantizedBytes[58];
        output[62] = quantizedBytes[62];
        output[63] = quantizedBytes[63];
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