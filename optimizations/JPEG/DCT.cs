using System;
using System.Runtime.CompilerServices;

namespace JPEG;

public static class Dct
{
	private const double B0 = 1.00000000000000000000;
	private const double B1 = 0.72095982200694791383;
	private const double B2 = 0.76536686473017954350;
	private const double B3 = 0.85043009476725644878;
	private const double B4 = 1.00000000000000000000;
	private const double B5 = 1.27275858057283393842;
	private const double B6 = 1.84775906502257351242;
	private const double B7 = 3.62450978541155137218;

	private const double A1 = 0.70710678118654752438;
	private const double A5 = 0.38268343236508977170;
	private const double A7 = 0.9238795325112867;
	
	private static readonly double[] Scale =
	[
		B0 * B0 * 0.125, B0 * B1 * 0.125, B0 * B2 * 0.125, B0 * B3 * 0.125, B0 * B4 * 0.125, B0 * B5 * 0.125, B0 * B6 * 0.125, B0 * B7 * 0.125,
		B1 * B0 * 0.125, B1 * B1 * 0.125, B1 * B2 * 0.125, B1 * B3 * 0.125, B1 * B4 * 0.125, B1 * B5 * 0.125, B1 * B6 * 0.125, B1 * B7 * 0.125,
		B2 * B0 * 0.125, B2 * B1 * 0.125, B2 * B2 * 0.125, B2 * B3 * 0.125, B2 * B4 * 0.125, B2 * B5 * 0.125, B2 * B6 * 0.125, B2 * B7 * 0.125,
		B3 * B0 * 0.125, B3 * B1 * 0.125, B3 * B2 * 0.125, B3 * B3 * 0.125, B3 * B4 * 0.125, B3 * B5 * 0.125, B3 * B6 * 0.125, B3 * B7 * 0.125,
		B4 * B0 * 0.125, B4 * B1 * 0.125, B4 * B2 * 0.125, B4 * B3 * 0.125, B4 * B4 * 0.125, B4 * B5 * 0.125, B4 * B6 * 0.125, B4 * B7 * 0.125,
		B5 * B0 * 0.125, B5 * B1 * 0.125, B5 * B2 * 0.125, B5 * B3 * 0.125, B5 * B4 * 0.125, B5 * B5 * 0.125, B5 * B6 * 0.125, B5 * B7 * 0.125,
		B6 * B0 * 0.125, B6 * B1 * 0.125, B6 * B2 * 0.125, B6 * B3 * 0.125, B6 * B4 * 0.125, B6 * B5 * 0.125, B6 * B6 * 0.125, B6 * B7 * 0.125,
		B7 * B0 * 0.125, B7 * B1 * 0.125, B7 * B2 * 0.125, B7 * B3 * 0.125, B7 * B4 * 0.125, B7 * B5 * 0.125, B7 * B6 * 0.125, B7 * B7 * 0.125
	];
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void DCT2D(Span<double> input, Span<double> output)
	{
		for (var i = 0; i < 8; i++)
		{
			DCT1D(input.Slice(i*8,8), output, i);
		}
		for (var i = 0; i < 8; i++)
		{
			DCT1D(output.Slice(i*8,8), input, i);
		}
		for(var i = 0; i < 64; i++)
		{
			output[i] = input[i] * Scale[i];
		}
	}
	
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void DCT1D(Span<double> input, Span<double> output, int offset)
	{
		var tmp0 = input[0] + input[7];
		var tmp7 = input[0] - input[7];
		var tmp1 = input[1] + input[6];
		var tmp6 = input[1] - input[6];
		var tmp2 = input[2] + input[5];
		var tmp5 = input[2] - input[5];
		var tmp3 = input[3] + input[4];
		var tmp4 = input[3] - input[4];

		var tmp10 = tmp0 + tmp3;
		var tmp13 = tmp0 - tmp3;
		var tmp11 = tmp1 + tmp2;
		var tmp12 = tmp1 - tmp2;

		output[0*8+offset] = tmp10 + tmp11;
		output[4*8+offset] = tmp10 - tmp11;

		tmp12 += tmp13;
		tmp12 *= A1;

		output[2*8+offset] = tmp13 + tmp12;
		output[6*8+offset] = tmp13 - tmp12;

		tmp4 += tmp5;
		tmp5 += tmp6;
		tmp6 += tmp7;

		var z2 = tmp4 * A7 - tmp6 * A5;
		var z4 = tmp6 * A7 + tmp4 * A5;

		tmp5 *= A1;

		var z11 = tmp7 + tmp5;
		var z13 = tmp7 - tmp5;

		output[5*8+offset] = z13 + z2;
		output[3*8+offset] = z13 - z2;
		output[1*8+offset] = z11 + z4;
		output[7*8+offset] = z11 - z4;
	}
	
	private const double iB0 = 1.0000000000000000000000;
	private const double iB1 = 1.3870398453221474618216;
	private const double iB2 = 1.3065629648763765278566;
	private const double iB3 = 1.1758756024193587169745;
	private const double iB4 = 1.0000000000000000000000;
	private const double iB5 = 0.7856949583871021812779;
	private const double iB6 = 0.5411961001461969843997;
	private const double iB7 = 0.2758993792829430123360;
	
	private const double iA2 = 1.8477590650225735;
	private const double iA4 = 1.4142135623730951;
	private const double iAB4 = -0.7653668647301795;
	
	private static readonly double[] IScale =
	[
		iB0 * iB0 * 0.125, iB0 * iB1 * 0.125, iB0 * iB2 * 0.125, iB0 * iB3 * 0.125, iB0 * iB4 * 0.125, iB0 * iB5 * 0.125, iB0 * iB6 * 0.125, iB0 * iB7 * 0.125,
		iB1 * iB0 * 0.125, iB1 * iB1 * 0.125, iB1 * iB2 * 0.125, iB1 * iB3 * 0.125, iB1 * iB4 * 0.125, iB1 * iB5 * 0.125, iB1 * iB6 * 0.125, iB1 * iB7 * 0.125,
		iB2 * iB0 * 0.125, iB2 * iB1 * 0.125, iB2 * iB2 * 0.125, iB2 * iB3 * 0.125, iB2 * iB4 * 0.125, iB2 * iB5 * 0.125, iB2 * iB6 * 0.125, iB2 * iB7 * 0.125,
		iB3 * iB0 * 0.125, iB3 * iB1 * 0.125, iB3 * iB2 * 0.125, iB3 * iB3 * 0.125, iB3 * iB4 * 0.125, iB3 * iB5 * 0.125, iB3 * iB6 * 0.125, iB3 * iB7 * 0.125,
		iB4 * iB0 * 0.125, iB4 * iB1 * 0.125, iB4 * iB2 * 0.125, iB4 * iB3 * 0.125, iB4 * iB4 * 0.125, iB4 * iB5 * 0.125, iB4 * iB6 * 0.125, iB4 * iB7 * 0.125,
		iB5 * iB0 * 0.125, iB5 * iB1 * 0.125, iB5 * iB2 * 0.125, iB5 * iB3 * 0.125, iB5 * iB4 * 0.125, iB5 * iB5 * 0.125, iB5 * iB6 * 0.125, iB5 * iB7 * 0.125,
		iB6 * iB0 * 0.125, iB6 * iB1 * 0.125, iB6 * iB2 * 0.125, iB6 * iB3 * 0.125, iB6 * iB4 * 0.125, iB6 * iB5 * 0.125, iB6 * iB6 * 0.125, iB6 * iB7 * 0.125,
		iB7 * iB0 * 0.125, iB7 * iB1 * 0.125, iB7 * iB2 * 0.125, iB7 * iB3 * 0.125, iB7 * iB4 * 0.125, iB7 * iB5 * 0.125, iB7 * iB6 * 0.125, iB7 * iB7 * 0.125
	];

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void IDCT2D(Span<double> input, Span<double> output)
	{
		for(var i = 0; i < 64; i++)
		{
			output[i] = input[i] * IScale[i];
		}
		for (var i = 0; i < 8; i++)
		{
			IDCT1D(output.Slice(i*8,8), input, i);
		}
		for (var i = 0; i < 8; i++)
		{
			IDCT1D(input.Slice(i*8,8), output, i);
		}
	}
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void IDCT1D(Span<double> input, Span<double> output, int offset)
	{
		var s17= input[1] + input[7];
		var d17= input[1] - input[7];
		var s53= input[5] + input[3];
		var d53= input[5] - input[3];

		var od07=  s17 + s53;
		var od25= (s17 - s53)*iA4;

		var od34=  d17*iAB4 - d53*iA2;
		var od16=  d53*iAB4 + d17*iA2;

		od16 -= od07;
		od25 -= od16;
		od34 += od25;

		var s26 = input[2] + input[6];
		var d26 = input[2] - input[6];
		d26*= iA4;
		d26-= s26;

		var s04= input[0] + input[4];
		var d04= input[0] - input[4];

		var os07= s04 + s26;
		var os34= s04 - s26;
		var os16= d04 + d26;
		var os25= d04 - d26;
		
		output[0*8+offset]= os07 + od07;
		output[7*8+offset]= os07 - od07;
		output[1*8+offset]= os16 + od16;
		output[6*8+offset]= os16 - od16;
		output[2*8+offset]= os25 + od25;
		output[5*8+offset]= os25 - od25;
		output[3*8+offset]= os34 - od34;
		output[4*8+offset]= os34 + od34;
	}
}