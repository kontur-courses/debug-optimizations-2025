using System;
using System.Linq;

namespace JPEG.Images;

public record struct Pixel
{
	public Pixel(byte r, byte g, byte b)
	{
		R = r;
		G = g;
		B = b;
		
		Y = 16.0 + (65.738 * R + 129.057 * G + 24.064 * B) / 256.0;
		Cb = 128.0 + (-37.945 * R - 74.494 * G + 112.439 * B) / 256.0;
		Cr =  128.0 + (112.439 * R - 94.154 * G - 18.285 * B) / 256.0;
	} 
	
	public Pixel(double y, double cb, double cr)
	{
		Y = y;
		Cb = cb;
		Cr = cr;
		
		R = (298.082 * Y + 408.583 * Cr) / 256.0 - 222.921;
		G = (298.082 * Y - 100.291 * Cb - 208.120 * Cr) / 256.0 + 135.576;
		B = (298.082 * Y + 516.412 * Cb) / 256.0 - 276.836;
	}

	public readonly double R;
	public readonly double G;
	public readonly double B;

	public readonly double Y;
	public readonly double Cb;
	public readonly double Cr;
}