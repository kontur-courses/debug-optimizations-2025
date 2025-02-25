using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace JPEG;

public static class Dct
{
	private const float B0 = 1.00000000000000000000f;
	private const float B1 = 0.72095982200694791383f;
	private const float B2 = 0.76536686473017954350f;
	private const float B3 = 0.85043009476725644878f;
	private const float B4 = 1.00000000000000000000f;
	private const float B5 = 1.27275858057283393842f;
	private const float B6 = 1.84775906502257351242f;
	private const float B7 = 3.62450978541155137218f;

	private const float A1 = 0.70710678118654752438f;
	private const float A5 = 0.38268343236508977170f;
	private const float A7 = 0.9238795325112867f;
	
	private static readonly float[] Scale =
	[
		B0 * B0 * 0.125f, B0 * B1 * 0.125f, B0 * B2 * 0.125f, B0 * B3 * 0.125f, B0 * B4 * 0.125f, B0 * B5 * 0.125f, B0 * B6 * 0.125f, B0 * B7 * 0.125f,
		B1 * B0 * 0.125f, B1 * B1 * 0.125f, B1 * B2 * 0.125f, B1 * B3 * 0.125f, B1 * B4 * 0.125f, B1 * B5 * 0.125f, B1 * B6 * 0.125f, B1 * B7 * 0.125f,
		B2 * B0 * 0.125f, B2 * B1 * 0.125f, B2 * B2 * 0.125f, B2 * B3 * 0.125f, B2 * B4 * 0.125f, B2 * B5 * 0.125f, B2 * B6 * 0.125f, B2 * B7 * 0.125f,
		B3 * B0 * 0.125f, B3 * B1 * 0.125f, B3 * B2 * 0.125f, B3 * B3 * 0.125f, B3 * B4 * 0.125f, B3 * B5 * 0.125f, B3 * B6 * 0.125f, B3 * B7 * 0.125f,
		B4 * B0 * 0.125f, B4 * B1 * 0.125f, B4 * B2 * 0.125f, B4 * B3 * 0.125f, B4 * B4 * 0.125f, B4 * B5 * 0.125f, B4 * B6 * 0.125f, B4 * B7 * 0.125f,
		B5 * B0 * 0.125f, B5 * B1 * 0.125f, B5 * B2 * 0.125f, B5 * B3 * 0.125f, B5 * B4 * 0.125f, B5 * B5 * 0.125f, B5 * B6 * 0.125f, B5 * B7 * 0.125f,
		B6 * B0 * 0.125f, B6 * B1 * 0.125f, B6 * B2 * 0.125f, B6 * B3 * 0.125f, B6 * B4 * 0.125f, B6 * B5 * 0.125f, B6 * B6 * 0.125f, B6 * B7 * 0.125f,
		B7 * B0 * 0.125f, B7 * B1 * 0.125f, B7 * B2 * 0.125f, B7 * B3 * 0.125f, B7 * B4 * 0.125f, B7 * B5 * 0.125f, B7 * B6 * 0.125f, B7 * B7 * 0.125f
	];

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void Dct2D(ref float input, ref float output)
	{
		ref var scale = ref MemoryMarshal.GetArrayDataReference(Scale);
		
		TransposeInPlace(ref input);
		Dct1D(ref input, ref output);
		TransposeInPlace(ref output);
		Dct1D(ref output, ref input);
		
		Avx.Multiply(Vector256.LoadUnsafe(ref input, 0 ), Vector256.LoadUnsafe(ref scale, 0 )).StoreUnsafe(ref output, 0 );
		Avx.Multiply(Vector256.LoadUnsafe(ref input, 8 ), Vector256.LoadUnsafe(ref scale, 8 )).StoreUnsafe(ref output, 8 );
		Avx.Multiply(Vector256.LoadUnsafe(ref input, 16), Vector256.LoadUnsafe(ref scale, 16)).StoreUnsafe(ref output, 16);
		Avx.Multiply(Vector256.LoadUnsafe(ref input, 24), Vector256.LoadUnsafe(ref scale, 24)).StoreUnsafe(ref output, 24);
		Avx.Multiply(Vector256.LoadUnsafe(ref input, 32), Vector256.LoadUnsafe(ref scale, 32)).StoreUnsafe(ref output, 32);
		Avx.Multiply(Vector256.LoadUnsafe(ref input, 40), Vector256.LoadUnsafe(ref scale, 40)).StoreUnsafe(ref output, 40);
		Avx.Multiply(Vector256.LoadUnsafe(ref input, 48), Vector256.LoadUnsafe(ref scale, 48)).StoreUnsafe(ref output, 48);
		Avx.Multiply(Vector256.LoadUnsafe(ref input, 56), Vector256.LoadUnsafe(ref scale, 56)).StoreUnsafe(ref output, 56);
	}
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Dct1D(ref float input, ref float output)
    {
	    var data0 = Vector256.LoadUnsafe(ref input, 0);
	    var data1 = Vector256.LoadUnsafe(ref input, 8);
	    var data2 = Vector256.LoadUnsafe(ref input, 16);
	    var data3 = Vector256.LoadUnsafe(ref input, 24);
	    var data4 = Vector256.LoadUnsafe(ref input, 32);
	    var data5 = Vector256.LoadUnsafe(ref input, 40);
	    var data6 = Vector256.LoadUnsafe(ref input, 48);
	    var data7 = Vector256.LoadUnsafe(ref input, 56);

        var tmp0 = data0 + data7;
        var tmp7 = data0 - data7;
        var tmp1 = data1 + data6;
        var tmp6 = data1 - data6;
        var tmp2 = data2 + data5;
        var tmp5 = data2 - data5;
        var tmp3 = data3 + data4;
        var tmp4 = data3 - data4;

        var tmp10 = tmp0 + tmp3;
        var tmp13 = tmp0 - tmp3;
        var tmp11 = tmp1 + tmp2;
        var tmp12 = tmp1 - tmp2;

        (tmp10 + tmp11).StoreUnsafe(ref output);
        (tmp10 - tmp11).StoreUnsafe(ref output, 32);

        tmp12 += tmp13;
        tmp12 *= A1;

        (tmp13 + tmp12).StoreUnsafe(ref output, 16);
        (tmp13 - tmp12).StoreUnsafe(ref output, 48);

        tmp4 += tmp5;
        tmp5 += tmp6;
        tmp6 += tmp7;

        var z2 = tmp4 * A7 - tmp6 * A5;
        var z4 = tmp6 * A7 + tmp4 * A5;

        tmp5 *= A1;

        var z11 = tmp7 + tmp5;
        var z13 = tmp7 - tmp5;

        (z13 + z2).StoreUnsafe(ref output, 40);
        (z13 - z2).StoreUnsafe(ref output, 24);
        (z11 + z4).StoreUnsafe(ref output, 8);
        (z11 - z4).StoreUnsafe(ref output, 56);
    }

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void TransposeInPlace(ref float input)
	{
		var data0 = Vector256.LoadUnsafe(ref input, 0);
		var data1 = Vector256.LoadUnsafe(ref input, 8);
		var data2 = Vector256.LoadUnsafe(ref input, 16);
		var data3 = Vector256.LoadUnsafe(ref input, 24);
		var data4 = Vector256.LoadUnsafe(ref input, 32);
		var data5 = Vector256.LoadUnsafe(ref input, 40);
		var data6 = Vector256.LoadUnsafe(ref input, 48);
		var data7 = Vector256.LoadUnsafe(ref input, 56);

		var t0 = Avx.UnpackLow( data0, data1);
		var t1 = Avx.UnpackHigh(data0, data1);
		var t2 = Avx.UnpackLow( data2, data3);
		var t3 = Avx.UnpackHigh(data2, data3);
		var t4 = Avx.UnpackLow( data4, data5);
		var t5 = Avx.UnpackHigh(data4, data5);
		var t6 = Avx.UnpackLow( data6, data7);
		var t7 = Avx.UnpackHigh(data6, data7);

		var tt0 = Avx.UnpackLow(t0.AsDouble(), t2.AsDouble()).AsSingle();
		var tt1 = Avx.UnpackHigh(t0.AsDouble(), t2.AsDouble()).AsSingle();
		var tt2 = Avx.UnpackLow(t1.AsDouble(), t3.AsDouble()).AsSingle();
		var tt3 = Avx.UnpackHigh(t1.AsDouble(), t3.AsDouble()).AsSingle();
		var tt4 = Avx.UnpackLow(t4.AsDouble(), t6.AsDouble()).AsSingle();
		var tt5 = Avx.UnpackHigh(t4.AsDouble(), t6.AsDouble()).AsSingle();
		var tt6 = Avx.UnpackLow(t5.AsDouble(), t7.AsDouble()).AsSingle();
		var tt7 = Avx.UnpackHigh(t5.AsDouble(), t7.AsDouble()).AsSingle(); 

		Avx.InsertVector128(tt0, tt4.GetLower(), 1).StoreUnsafe(ref input, 0);
		Avx.InsertVector128(tt1, tt5.GetLower(), 1).StoreUnsafe(ref input, 8);
		Avx.InsertVector128(tt2, tt6.GetLower(), 1).StoreUnsafe(ref input, 16);
		Avx.InsertVector128(tt3, tt7.GetLower(), 1).StoreUnsafe(ref input, 24);
		Avx.Permute2x128(tt0, tt4, 0x31).StoreUnsafe(ref input, 32);
		Avx.Permute2x128(tt1, tt5, 0x31).StoreUnsafe(ref input, 40);
		Avx.Permute2x128(tt2, tt6, 0x31).StoreUnsafe(ref input, 48);
		Avx.Permute2x128(tt3, tt7, 0x31).StoreUnsafe(ref input, 56);
	}
	
	private const float Ib0 = 1.0000000000000000000000f;
	private const float Ib1 = 1.3870398453221474618216f;
	private const float Ib2 = 1.3065629648763765278566f;
	private const float Ib3 = 1.1758756024193587169745f;
	private const float Ib4 = 1.0000000000000000000000f;
	private const float Ib5 = 0.7856949583871021812779f;
	private const float Ib6 = 0.5411961001461969843997f;
	private const float Ib7 = 0.2758993792829430123360f;
	
	private const float Ia2 = 1.8477590650225735f;
	private const float Ia4 = 1.4142135623730951f;
	private const float Iab4 = -0.7653668647301795f;
	
	private static readonly float[] InverseScale =
	[
		Ib0 * Ib0 * 0.125f, Ib0 * Ib1 * 0.125f, Ib0 * Ib2 * 0.125f, Ib0 * Ib3 * 0.125f, Ib0 * Ib4 * 0.125f, Ib0 * Ib5 * 0.125f, Ib0 * Ib6 * 0.125f, Ib0 * Ib7 * 0.125f,
		Ib1 * Ib0 * 0.125f, Ib1 * Ib1 * 0.125f, Ib1 * Ib2 * 0.125f, Ib1 * Ib3 * 0.125f, Ib1 * Ib4 * 0.125f, Ib1 * Ib5 * 0.125f, Ib1 * Ib6 * 0.125f, Ib1 * Ib7 * 0.125f,
		Ib2 * Ib0 * 0.125f, Ib2 * Ib1 * 0.125f, Ib2 * Ib2 * 0.125f, Ib2 * Ib3 * 0.125f, Ib2 * Ib4 * 0.125f, Ib2 * Ib5 * 0.125f, Ib2 * Ib6 * 0.125f, Ib2 * Ib7 * 0.125f,
		Ib3 * Ib0 * 0.125f, Ib3 * Ib1 * 0.125f, Ib3 * Ib2 * 0.125f, Ib3 * Ib3 * 0.125f, Ib3 * Ib4 * 0.125f, Ib3 * Ib5 * 0.125f, Ib3 * Ib6 * 0.125f, Ib3 * Ib7 * 0.125f,
		Ib4 * Ib0 * 0.125f, Ib4 * Ib1 * 0.125f, Ib4 * Ib2 * 0.125f, Ib4 * Ib3 * 0.125f, Ib4 * Ib4 * 0.125f, Ib4 * Ib5 * 0.125f, Ib4 * Ib6 * 0.125f, Ib4 * Ib7 * 0.125f,
		Ib5 * Ib0 * 0.125f, Ib5 * Ib1 * 0.125f, Ib5 * Ib2 * 0.125f, Ib5 * Ib3 * 0.125f, Ib5 * Ib4 * 0.125f, Ib5 * Ib5 * 0.125f, Ib5 * Ib6 * 0.125f, Ib5 * Ib7 * 0.125f,
		Ib6 * Ib0 * 0.125f, Ib6 * Ib1 * 0.125f, Ib6 * Ib2 * 0.125f, Ib6 * Ib3 * 0.125f, Ib6 * Ib4 * 0.125f, Ib6 * Ib5 * 0.125f, Ib6 * Ib6 * 0.125f, Ib6 * Ib7 * 0.125f,
		Ib7 * Ib0 * 0.125f, Ib7 * Ib1 * 0.125f, Ib7 * Ib2 * 0.125f, Ib7 * Ib3 * 0.125f, Ib7 * Ib4 * 0.125f, Ib7 * Ib5 * 0.125f, Ib7 * Ib6 * 0.125f, Ib7 * Ib7 * 0.125f
	];

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void InverseDct2D(ref float inp, ref float result)
	{
		ref var scale = ref MemoryMarshal.GetArrayDataReference(InverseScale);
		
		Avx.Multiply(Vector256.LoadUnsafe(ref inp, 0 ), Vector256.LoadUnsafe(ref scale, 0 )).StoreUnsafe(ref result, 0 );
		Avx.Multiply(Vector256.LoadUnsafe(ref inp, 8 ), Vector256.LoadUnsafe(ref scale, 8 )).StoreUnsafe(ref result, 8 );
		Avx.Multiply(Vector256.LoadUnsafe(ref inp, 16), Vector256.LoadUnsafe(ref scale, 16)).StoreUnsafe(ref result, 16);
		Avx.Multiply(Vector256.LoadUnsafe(ref inp, 24), Vector256.LoadUnsafe(ref scale, 24)).StoreUnsafe(ref result, 24);
		Avx.Multiply(Vector256.LoadUnsafe(ref inp, 32), Vector256.LoadUnsafe(ref scale, 32)).StoreUnsafe(ref result, 32);
		Avx.Multiply(Vector256.LoadUnsafe(ref inp, 40), Vector256.LoadUnsafe(ref scale, 40)).StoreUnsafe(ref result, 40);
		Avx.Multiply(Vector256.LoadUnsafe(ref inp, 48), Vector256.LoadUnsafe(ref scale, 48)).StoreUnsafe(ref result, 48);
		Avx.Multiply(Vector256.LoadUnsafe(ref inp, 56), Vector256.LoadUnsafe(ref scale, 56)).StoreUnsafe(ref result, 56);
		
		
		InverseDct1D(ref result, ref inp);
		TransposeInPlace(ref inp);
		InverseDct1D(ref inp, ref result);
		TransposeInPlace(ref result);
	}
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void InverseDct1D(ref float input, ref float output)
	{
		var data0 = Vector256.LoadUnsafe(ref input, 0);
		var data1 = Vector256.LoadUnsafe(ref input, 8);
		var data2 = Vector256.LoadUnsafe(ref input, 16);
		var data3 = Vector256.LoadUnsafe(ref input, 24);
		var data4 = Vector256.LoadUnsafe(ref input, 32);
		var data5 = Vector256.LoadUnsafe(ref input, 40);
		var data6 = Vector256.LoadUnsafe(ref input, 48);
		var data7 = Vector256.LoadUnsafe(ref input, 56);
		
		var s04= data0 + data4;
		var d04= data0 - data4;
		var s17= data1 + data7;
		var d17= data1 - data7;
		var s26= data2 + data6;
		var d26= data2 - data6;
		var s53= data5 + data3;
		var d53= data5 - data3;
		var os07= s04 + s26;
		var os34= s04 - s26;
		
		var od07=  s17 + s53;
		var od25= (s17 - s53)*Ia4;

		var od34=  d17*Iab4 - d53*Ia2;
		var od16=  d53*Iab4 + d17*Ia2;

		od16 -= od07;
		od25 -= od16;
		od34 += od25;
		
		d26*= Ia4;
		d26-= s26;
		
		var os16= d04 + d26;
		var os25= d04 - d26;
		
		(os07 + od07).StoreUnsafe(ref output, 0);
		(os16 + od16).StoreUnsafe(ref output, 8);
		(os25 + od25).StoreUnsafe(ref output, 16);
		(os34 - od34).StoreUnsafe(ref output, 24);
		(os34 + od34).StoreUnsafe(ref output, 32);
		(os25 - od25).StoreUnsafe(ref output, 40);
		(os16 - od16).StoreUnsafe(ref output, 48);
		(os07 - od07).StoreUnsafe(ref output, 56);
	}
}