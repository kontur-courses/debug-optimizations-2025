using System;
using System.Runtime.CompilerServices;

namespace JPEG;

public static class Dct
{
	private static readonly double[] Alpha = new double[8];
	private static readonly double[,] Cosine = new double[8,8];
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
			Cosine[x,u] = Math.Cos(((2d * x + 1d) * u * Math.PI) / (2 * 8));
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void DCT1D(Span<double> input, Span<double> output)
	{
		for (var u = 0; u < 8; u++)
		{
			var sum = 0.0;
			for (var x = 0; x < 8; x++)
			{
				sum += Cosine[x, u] * input[x];
			}
			output[u] = sum * Alpha[u];
		}
	}
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void IDCT1D(Span<double> input, Span<double> output)
	{
		for (var x = 0; x < 8; x++)
		{
			var sum = 0.0;
			for (var u = 0; u < 8; u++)
			{
				sum += Cosine[x, u] * input[u];
			}
			output[x] = sum;
		}
	}
	
	public static void DCT2D(Span<double> input, Span<double> output)
	{
		Span<double> buffer = stackalloc double[8];
		Span<double> transformed = stackalloc double[8];

		for (var i = 0; i < 8; i++)
		{
			for (var j = 0; j < 8; j++)
			{
				buffer[j] = input[i*8+j];
			}
			DCT1D(buffer, transformed);
			for (var j = 0; j < 8; j++)
			{
				input[i*8+j] = transformed[j];
			}
		}

		for (var j = 0; j < 8; j++)
		{
			for (var i = 0; i < 8; i++)
			{
				buffer[i] = input[i*8+j];
			}
			DCT1D(buffer, transformed);
			for (var i = 0; i < 8; i++)
			{
				output[i*8+j] = transformed[i] * Beta;
			}
		}
	}

	public static void IDCT2D(Span<double> coeffs, Span<double> output)
	{
		Span<double> buffer = stackalloc double[8];
		Span<double> transformed = stackalloc double[8];

		for (var u = 0; u < 8; u++)
		{
			for (var v = 0; v < 8; v++)
			{
				buffer[v] = coeffs[u*8+v] * Alpha[v];
			}
			IDCT1D(buffer, transformed);
			for (var y = 0; y < 8; y++)
			{
				coeffs[u*8+y] = transformed[y];
			}
		}

		for (var y = 0; y < 8; y++)
		{
			for (var u = 0; u < 8; u++)
			{
				buffer[u] = coeffs[u*8+y] * Alpha[u];
			}
			IDCT1D(buffer, transformed);
			for (var x = 0; x < 8; x++)
			{
				output[x*8+y] = transformed[x] * Beta;
			}
		}
	}
}