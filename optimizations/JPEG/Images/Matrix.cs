using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace JPEG.Images;

public sealed class Matrix : IDisposable
{
    private readonly SimpleBitmap bmp;
    private readonly int stride;
    private readonly IntPtr scan0;
    private bool isDisposed;
    private bool isTaken;

    public int Height { get; }
    public int Width { get; }
    
    public Matrix(SimpleBitmap bitmap)
    {
        Height = bitmap.Height - bitmap.Height % 8;
        Width = bitmap.Width - bitmap.Width % 8;
        stride = bitmap.Stride;
        scan0 = bitmap.Scan0;
        isTaken = true;
    }

    public Matrix(int width, int height)
    {
        Height = height - height % 8;
        Width = width - width % 8;
        bmp = new SimpleBitmap(Width, Height);
        stride = bmp.Stride;
        scan0 = bmp.Scan0;
    }

    public void Dispose()
    {
        if (isDisposed || isTaken) return;
        bmp.Dispose();
        isDisposed = true;
    }
    
    public SimpleBitmap ToBitmap()
    {
        isTaken = true;
        return bmp;
    }

    private static readonly Vector128<byte> Ssse3RedIndices0 =
        Vector128.Create(0, 3, 6, 9, 12, 15, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1).AsByte();

    private static readonly Vector128<byte> Ssse3RedIndices1 =
        Vector128.Create(-1, -1, -1, -1, -1, -1, 2, 5, 8, 11, 14, -1, -1, -1, -1, -1).AsByte();

    private static readonly Vector128<byte> Ssse3RedIndices2 =
        Vector128.Create(-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 1, 4, 7, 10, 13).AsByte();

    private static readonly Vector128<byte> Ssse3GreenIndices0 =
        Vector128.Create(1, 4, 7, 10, 13, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1).AsByte();

    private static readonly Vector128<byte> Ssse3GreenIndices1 =
        Vector128.Create(-1, -1, -1, -1, -1, 0, 3, 6, 9, 12, 15, -1, -1, -1, -1, -1).AsByte();

    private static readonly Vector128<byte> Ssse3GreenIndices2 =
        Vector128.Create(-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 2, 5, 8, 11, 14).AsByte();

    private static readonly Vector128<byte> Ssse3BlueIndices0 =
        Vector128.Create(2, 5, 8, 11, 14, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1).AsByte();

    private static readonly Vector128<byte> Ssse3BlueIndices1 =
        Vector128.Create(-1, -1, -1, -1, -1, 1, 4, 7, 10, 13, -1, -1, -1, -1, -1, -1).AsByte();

    private static readonly Vector128<byte> Ssse3BlueIndices2 =
        Vector128.Create(-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 0, 3, 6, 9, 12, 15).AsByte();

    private static readonly Vector128<byte> MaskRed0 =
        Vector128.Create(0, -1, -1, 1, -1, -1, 2, -1, -1, 3, -1, -1, 8, -1, -1, 9).AsByte();

    private static readonly Vector128<byte> MaskGreen0 =
        Vector128.Create(-1, 0, -1, -1, 1, -1, -1, 2, -1, -1, 3, -1, -1, 8, -1, -1).AsByte();

    private static readonly Vector128<byte> MaskBlue0 =
        Vector128.Create(-1, -1, 0, -1, -1, 1, -1, -1, 2, -1, -1, 3, -1, -1, 8, -1).AsByte();

    private static readonly Vector128<byte> MaskRed1 =
        Vector128.Create(-1, -1, 10, -1, -1, 11, -1, -1, 4, -1, -1, 5, -1, -1, 6, -1).AsByte();

    private static readonly Vector128<byte> MaskGreen1 =
        Vector128.Create(9, -1, -1, 10, -1, -1, 11, -1, -1, 4, -1, -1, 5, -1, -1, 6).AsByte();

    private static readonly Vector128<byte> MaskBlue1 =
        Vector128.Create(-1, 9, -1, -1, 10, -1, -1, 11, -1, -1, 4, -1, -1, 5, -1, -1).AsByte();

    private static readonly Vector128<byte> MaskRed2 =
        Vector128.Create(-1, 7, -1, -1, 12, -1, -1, 13, -1, -1, 14, -1, -1, 15, -1, -1).AsByte();

    private static readonly Vector128<byte> MaskGreen2 =
        Vector128.Create(-1, -1, 7, -1, -1, 12, -1, -1, 13, -1, -1, 14, -1, -1, 15, -1).AsByte();

    private static readonly Vector128<byte> MaskBlue2 =
        Vector128.Create(6, -1, -1, 7, -1, -1, 12, -1, -1, 13, -1, -1, 14, -1, -1, 15).AsByte();


    private static readonly Vector256<float> vRConst = Vector256.Create(-112f);
    private static readonly Vector256<float> vYFirst = Vector256.Create(0.2567890625f);
    private static readonly Vector256<float> vYSecond = Vector256.Create(0.50412890625f);
    private static readonly Vector256<float> vYThird = Vector256.Create(0.09790625f);
    private static readonly Vector256<float> vCbFirst = Vector256.Create(-0.14822265625f);
    private static readonly Vector256<float> vCbSecond = Vector256.Create(-0.2909921875f);
    private static readonly Vector256<float> vCbThird = Vector256.Create(0.43921484375f);
    private static readonly Vector256<float> vCrFirst = Vector256.Create(0.43921484375f);
    private static readonly Vector256<float> vCrSecond = Vector256.Create(-0.3677890625f);
    private static readonly Vector256<float> vCrThird = Vector256.Create(-0.07142578125f);
    private static Vector256<float> Half =      Vector256.Create(0.5f);
    private const nint Bound4 = 4;
    private static Vector256<int> controlMask = Vector256.Create(0, 2, 4, 6, 1, 3, 5, 7);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void GetSubMatrix(int yOffset, int xOffset, ref float output)
    {
        ref var data = ref Unsafe.Add(ref scan0.ToRef<byte>(), yOffset * stride + xOffset * 3);
        for (nint i = 0; i < Bound4; i++)
        {
            var chunk0 = Vector128.LoadUnsafe(ref data);
            var chunk1T = Vector128.LoadUnsafe(ref data, 16).AsSingle();
            data = ref Unsafe.Add(ref data, stride);
            var chunk1 = chunk1T.LoadHighUnsafe(ref data).AsByte();
            var chunk2 = Vector128.LoadUnsafe(ref data, 8);
            data = ref Unsafe.Add(ref data, stride);

            var r = Sse2.Or(
                Sse2.Or(Ssse3.Shuffle(chunk0, Ssse3RedIndices0), Ssse3.Shuffle(chunk1, Ssse3RedIndices1)),
                Ssse3.Shuffle(chunk2, Ssse3RedIndices2));
            var g = Sse2.Or(
                Sse2.Or(Ssse3.Shuffle(chunk0, Ssse3GreenIndices0), Ssse3.Shuffle(chunk1, Ssse3GreenIndices1)),
                Ssse3.Shuffle(chunk2, Ssse3GreenIndices2));
            var b = Sse2.Or(
                Sse2.Or(Ssse3.Shuffle(chunk0, Ssse3BlueIndices0), Ssse3.Shuffle(chunk1, Ssse3BlueIndices1)),
                Ssse3.Shuffle(chunk2, Ssse3BlueIndices2));

            var rLower =
                Avx.ConvertToVector256Single(
                    Avx2.ConvertToVector256Int32(Sse2.UnpackLow(b, Vector128<byte>.Zero).AsInt16()));
            var rHigh = Avx.ConvertToVector256Single(
                Avx2.ConvertToVector256Int32(Sse2.UnpackHigh(b, Vector128<byte>.Zero).AsInt16()));

            var gLower =
                Avx.ConvertToVector256Single(
                    Avx2.ConvertToVector256Int32(Sse2.UnpackLow(g, Vector128<byte>.Zero).AsInt16()));
            var gHigh = Avx.ConvertToVector256Single(
                Avx2.ConvertToVector256Int32(Sse2.UnpackHigh(g, Vector128<byte>.Zero).AsInt16()));

            var bLower =
                Avx.ConvertToVector256Single(
                    Avx2.ConvertToVector256Int32(Sse2.UnpackLow(r, Vector128<byte>.Zero).AsInt16()));
            var bHigh = Avx.ConvertToVector256Single(
                Avx2.ConvertToVector256Int32(Sse2.UnpackHigh(r, Vector128<byte>.Zero).AsInt16()));

            var Y = Vector256.FusedMultiplyAdd(vYThird, bLower,
                Vector256.FusedMultiplyAdd(vYSecond, gLower, Vector256.FusedMultiplyAdd(vYFirst, rLower, vRConst)));
            var Cb = Vector256.FusedMultiplyAdd(vCbThird, bLower,
                Vector256.FusedMultiplyAdd(vCbSecond, gLower, Vector256.Multiply(vCbFirst, rLower)));
            var Cr = Vector256.FusedMultiplyAdd(vCrThird, bLower,
                Vector256.FusedMultiplyAdd(vCrSecond, gLower, Vector256.Multiply(vCrFirst, rLower)));

            var cbcr = Avx.Multiply(Avx2.PermuteVar8x32(Avx.UnpackHigh(Avx.HorizontalAdd(Cb, Cb), Avx.HorizontalAdd(Cr, Cr)), controlMask), Half);
            Y.StoreUnsafe(ref output);
            cbcr.StoreUnsafe(ref output, 64);
            

            Y = Vector256.FusedMultiplyAdd(vYThird, bHigh,
                Vector256.FusedMultiplyAdd(vYSecond, gHigh, Vector256.FusedMultiplyAdd(vYFirst, rHigh, vRConst)));
            Cb = Vector256.FusedMultiplyAdd(vCbThird, bHigh,
                Vector256.FusedMultiplyAdd(vCbSecond, gHigh, Vector256.Multiply(vCbFirst, rHigh)));
            Cr = Vector256.FusedMultiplyAdd(vCrThird, bHigh,
                Vector256.FusedMultiplyAdd(vCrSecond, gHigh, Vector256.Multiply(vCrFirst, rHigh)));

            cbcr = Avx.Multiply(Avx2.PermuteVar8x32(Avx.UnpackHigh(Avx.HorizontalAdd(Cb, Cb), Avx.HorizontalAdd(Cr, Cr)), controlMask), Half);

            Y.StoreUnsafe(ref output, 8);
            cbcr.StoreUnsafe(ref output, 72);

            output = ref Unsafe.Add(ref output, 16);
        }
    }

    private static readonly Vector256<float> YMul = Vector256.Create(298.082f);
    private static readonly Vector256<float> Const516 = Vector256.Create(516.412f);
    private static readonly Vector256<float> Const100 = Vector256.Create(100.291f);
    private static readonly Vector256<float> Const208 = Vector256.Create(208.120f);
    private static readonly Vector256<float> Const408 = Vector256.Create(408.583f);
    private static readonly Vector256<float> Const128 = Vector256.Create(128.0f);
    private static readonly Vector256<float> Const276 = Vector256.Create(276.836f);
    private static readonly Vector256<float> Const135 = Vector256.Create(135.576f);
    private static readonly Vector256<float> Const222 = Vector256.Create(222.921f);
    private static readonly Vector256<float> Const256 = Vector256.Create(1f / 256f);
    private static readonly Vector256<int> mask = Vector256.Create(0, 0, 1, 1, 2, 2, 3, 3);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetPixels(ref float sub, int yOffset, int xOffset)
    {
        ref var matrix = ref Unsafe.Add(ref scan0.ToRef<byte>(), (yOffset) * stride + xOffset * 3);
        for (nint i = 0; i < Bound4; i++)
        {
            var yVec = Vector256.LoadUnsafe(ref Unsafe.Add(ref sub, 0));
            var cbcr = Vector256.LoadUnsafe(ref Unsafe.Add(ref sub, 64));
            
            var lowerHalf = Avx.ExtractVector128(cbcr, 0);
            var upperHalf = Avx.ExtractVector128(cbcr, 1);

            var cbVec = Avx2.PermuteVar8x32(
                Avx.InsertVector128(Vector256<float>.Zero, lowerHalf, 0),
                mask
            );

            var crVec = Avx2.PermuteVar8x32(
                Avx.InsertVector128(Vector256<float>.Zero, upperHalf, 0),
                mask
            );
            
            yVec = Avx.Add(yVec, Const128);
            cbVec = Avx.Add(cbVec, Const128);
            crVec = Avx.Add(crVec, Const128);
            var yScaled = Avx.Multiply(yVec, YMul);

            var r = (yScaled + Const516 * cbVec) * Const256 - Const276;
            var g = (yScaled - Const100 * cbVec - Const208 * crVec) * Const256 + Const135;
            var b = (yScaled + Const408 * crVec) * Const256 - Const222;

            var yVec2 = Vector256.LoadUnsafe(ref Unsafe.Add(ref sub, 8));
            var cbcr2 = Vector256.LoadUnsafe(ref Unsafe.Add(ref sub, 72));
            
            var lowerHalf2 = Avx.ExtractVector128(cbcr2, 0);
            var upperHalf2 = Avx.ExtractVector128(cbcr2, 1);

            var cbVec2 = Avx2.PermuteVar8x32(
                Avx.InsertVector128(Vector256<float>.Zero, lowerHalf2, 0),
                mask
            );

            var crVec2 = Avx2.PermuteVar8x32(
                Avx.InsertVector128(Vector256<float>.Zero, upperHalf2, 0),
                mask
            );

            yVec2 = Avx.Add(yVec2, Const128);
            cbVec2 = Avx.Add(cbVec2, Const128);
            crVec2 = Avx.Add(crVec2, Const128);

            var yScaled2 = Avx.Multiply(yVec2, YMul);

            var r2 = (yScaled2 + Const516 * cbVec2) * Const256 - Const276;
            var g2 = (yScaled2 - Const100 * cbVec2 - Const208 * crVec2) * Const256 + Const135;
            var b2 = (yScaled2 + Const408 * crVec2) * Const256 - Const222;

            var rmin1 = Avx.ConvertToVector256Int32(r);
            var rmin2 = Avx.ConvertToVector256Int32(r2);
            var rsat = Avx2.PackSignedSaturate(rmin1, rmin2);
            var predLow  = Avx2.Permute2x128(rsat, rsat, 0x20).GetLower();
            var predHigh = Avx2.Permute2x128(rsat, rsat, 0x31).GetLower();
            
            var gmin1 = Avx.ConvertToVector256Int32(g);
            var gmin2 = Avx.ConvertToVector256Int32(g2);
            var gsat = Avx2.PackSignedSaturate(gmin1, gmin2);
            var pgreenLow  = Avx2.Permute2x128(gsat, gsat, 0x20).GetLower();
            var pgreenHigh = Avx2.Permute2x128(gsat, gsat, 0x31).GetLower();
            
            var bmin1 = Avx.ConvertToVector256Int32(b);
            var bmin2 = Avx.ConvertToVector256Int32(b2);
            var bsat = Avx2.PackSignedSaturate(bmin1, bmin2);
            var pblueLow  = Avx2.Permute2x128(bsat, bsat, 0x20).GetLower();
            var pblueHigh = Avx2.Permute2x128(bsat, bsat, 0x31).GetLower();
            
            var red = Sse2.PackUnsignedSaturate(predLow, predHigh);
            var green = Sse2.PackUnsignedSaturate(pgreenLow, pgreenHigh);
            var blue = Sse2.PackUnsignedSaturate(pblueLow, pblueHigh);
            
            var output0 = Sse2.Or(Sse2.Or(Ssse3.Shuffle(blue, MaskBlue0), Ssse3.Shuffle(green, MaskGreen0)), Ssse3.Shuffle(red, MaskRed0));
            var output1 = Sse2.Or(Sse2.Or(Ssse3.Shuffle(blue, MaskBlue1), Ssse3.Shuffle(green, MaskGreen1)), Ssse3.Shuffle(red, MaskRed1));
            var output2 = Sse2.Or(Sse2.Or(Ssse3.Shuffle(blue, MaskBlue2), Ssse3.Shuffle(green, MaskGreen2)), Ssse3.Shuffle(red, MaskRed2));


            output0.StoreUnsafe(ref matrix);
            output1.StoreLowUnsafe(ref matrix, 16);
            matrix = ref Unsafe.Add(ref matrix, stride);
            output1.StoreHighUnsafe(ref matrix);
            output2.StoreUnsafe(ref matrix, 8);
            matrix = ref Unsafe.Add(ref matrix, stride);
            
            sub = ref Unsafe.Add(ref sub, 16);
        }
    }

    public unsafe void ApplyDeblockingFilter()
    {
        var stride = bmp.Stride;
        var p = (byte*)bmp.Scan0.ToPointer();
        const int threshold = 20;
        for (var y = 0; y < Height; y++)
        {
            for (var x = 8; x <Width; x += 8)
            {
                var leftPixel = p + y * stride + (x - 1) * 3;
                var rightPixel = p + y * stride + x * 3;
    
                for (var c = 0; c < 3; c++)
                {
                    if (Math.Abs(leftPixel[c] - rightPixel[c]) > threshold) continue;
                    
                    var avg = (leftPixel[c] + rightPixel[c]) / 2;
                    leftPixel[c] = (byte)((leftPixel[c] + avg) / 2);
                    rightPixel[c] = (byte)((rightPixel[c] + avg) / 2);
                }
            }
        }
        
        for (var x = 0; x < Width; x++)
        {
            for (var y = 8; y < Height; y += 8)
            {
                var topPixel = p + (y - 1) * stride + x * 3;
                var bottomPixel = p + y * stride + x * 3;
    
                for (var c = 0; c < 3; c++)
                {
                    if (Math.Abs(topPixel[c] - bottomPixel[c]) > threshold) continue;
                    
                    var avg = (topPixel[c] + bottomPixel[c]) / 2;
                    topPixel[c] = (byte)((topPixel[c] + avg) / 2);
                    bottomPixel[c] = (byte)((bottomPixel[c] + avg) / 2);
                }
            }
        }
    }
}

public static class IntPtrExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe ref T ToRef<T>(this IntPtr ptr) => ref Unsafe.AsRef<T>(ptr.ToPointer());
}

public static class Vector128Extensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe Vector128<float> LoadHighUnsafe<T>(this Vector128<float> lower, ref T address)
    {
        return Sse.LoadHigh(lower, (float*)Unsafe.AsPointer(ref address));
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void StoreHighUnsafe<T>(this Vector128<T> source, ref T address, nuint offset = 0)
    {
        Sse.StoreHigh((float*)Unsafe.AsPointer(ref Unsafe.Add(ref address, offset)), source.AsSingle());
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void StoreLowUnsafe<T>(this Vector128<T> source, ref T address, nuint offset = 0)
    {
        Sse.StoreLow((float*)Unsafe.AsPointer(ref Unsafe.Add(ref address, offset)), source.AsSingle());
    }
}