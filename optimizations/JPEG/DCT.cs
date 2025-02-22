using System;
using System.Runtime.CompilerServices;

namespace JPEG;

public static class Dct
{
	private static readonly double[] Alpha = new double[8];
	private static readonly double[] Cosine = new double[64];
	private static readonly double[] Scale = new double[64];
	private static readonly double Beta = 1d / 8 + 1d / 8;
	
	static Dct()
	{
		for(var i = 0; i < 8; i++)
		{
			Alpha[i] = i == 0 ? 1 / Math.Sqrt(2) : 1;
		}
		
		for(var x = 0; x < 8; x++)
		for(var u = 0; u < 8; u++)
		{
			Cosine[x*8+u] = Math.Cos(((2d * x + 1d) * u * Math.PI) / (2 * 8));
			
			Scale[x*8+u] = Beta * Alpha[x] * Alpha[u];
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void DCT1D(Span<double> input, Span<double> output, int offset)
	{
		for (var u = 0; u < 8; u++)
		{
			var sum = 0.0;
			sum += Cosine[0*8+u] * input[0];
			sum += Cosine[1*8+u] * input[1];
			sum += Cosine[2*8+u] * input[2];
			sum += Cosine[3*8+u] * input[3];
			sum += Cosine[4*8+u] * input[4];
			sum += Cosine[5*8+u] * input[5];
			sum += Cosine[6*8+u] * input[6];
			sum += Cosine[7*8+u] * input[7];
			output[u*8+offset] = sum;
		}
	}
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void IDCT1D(Span<double> input, Span<double> output, int offset)
	{
		for (var x = 0; x < 8; x++)
		{
			var sum = 0.0;
			sum += Cosine[x*8+0] * input[0];
			sum += Cosine[x*8+1] * input[1];
			sum += Cosine[x*8+2] * input[2];
			sum += Cosine[x*8+3] * input[3];
			sum += Cosine[x*8+4] * input[4];
			sum += Cosine[x*8+5] * input[5];
			sum += Cosine[x*8+6] * input[6];
			sum += Cosine[x*8+7] * input[7];
			output[x*8+offset] = sum;
		}
	}
	
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

	public static void IDCT2D(Span<double> input, Span<double> output)
	{
		for(var i = 0; i < 64; i++)
		{
			output[i] = input[i] * Scale[i];
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
}