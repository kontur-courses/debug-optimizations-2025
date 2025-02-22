using System;
using System.Runtime.CompilerServices;

namespace JPEG;

public static class Dct
{
	private static readonly double[] Alpha = new double[8];
	private static readonly double[,] Cosine = new double[8,8];
	
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
	
	public static void DCT2D(double[,] input, double[,] output)
	{
		var height = input.GetLength(0);
		var width = input.GetLength(1);
		var beta = Beta(height, width);
		
		for (var u = 0; u < width; u++)
		for (var v = 0; v < height; v++)
		{
			var sum = 0.0d;
			for (var x = 0; x < width; x++)
			for (var y = 0; y < height; y++)
			{
				sum += BasisFunction(input[x, y], u, v, x, y);
			}
			
			output[u, v] = sum * beta * Alpha[u] * Alpha[v];
		}
	}

	public static void IDCT2D(double[,] coeffs, double[,] output)
	{
		var height = coeffs.GetLength(0);
		var width = coeffs.GetLength(1);
		var beta = Beta(height, width);
		
		for (var x = 0; x < width; x++)
		for (var y = 0; y < height; y++)
		{
			var sum = 0.0d;
			
			for (var u = 0; u < width; u++)
			for (var v = 0; v < height; v++)
			{
				sum += BasisFunction(coeffs[u, v], u, v, x, y) * Alpha[u] * Alpha[v];
			}
			
			output[x, y] = sum * beta;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static double BasisFunction(double a, int u, int v, int x, int y) => Cosine[x,u] * Cosine[y,v] * a;

	private static double Beta(int height, int width)
	{
		return 1d / width + 1d / height;
	}
}