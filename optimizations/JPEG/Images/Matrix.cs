using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.CompilerServices;

namespace JPEG.Images;

internal class Matrix(int height, int width)
{
	public readonly Pixel[] Pixels = new Pixel[height*width];
	public readonly int Height = height;
	public readonly int Width = width;

	public static explicit operator Matrix(Bitmap bmp)
	{
		var height = bmp.Height - bmp.Height % 8;
		var width = bmp.Width - bmp.Width % 8;
		var matrix = new Matrix(height, width);
		var bData = bmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, bmp.PixelFormat);

		unsafe
		{
			var size = height * width;
			ref var scan0 = ref Unsafe.AsRef<byte>(bData.Scan0.ToPointer());
			
			for (var j = 0; j < size; j++)
			{
				var b = Unsafe.Add(ref scan0, 0);
				var g = Unsafe.Add(ref scan0, 1);
				var r = Unsafe.Add(ref scan0, 2);
					
				matrix.Pixels[j] = new Pixel(r, g, b);
				scan0 = ref Unsafe.Add(ref scan0, 3);
			}
		}

		bmp.UnlockBits(bData);
		
		return matrix;
	}

	public static explicit operator Bitmap(Matrix matrix)
	{
		var height = matrix.Height;
		var width = matrix.Width;
		var bmp = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

		var bData = bmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, bmp.PixelFormat);

		unsafe
		{
			var size = height * width;
			ref var scan0 = ref Unsafe.AsRef<byte>(bData.Scan0.ToPointer());
			
			for (var j = 0; j < size; j++)
			{
				var pixel = matrix.Pixels[j];
				Unsafe.Add(ref scan0, 0) = ToByte(pixel.B);
				Unsafe.Add(ref scan0, 1) = ToByte(pixel.G);
				Unsafe.Add(ref scan0, 2) = ToByte(pixel.R);
					
				scan0 = ref Unsafe.Add(ref scan0, 3);
			}
		}
		
		bmp.UnlockBits(bData);

		return bmp;
	}

	private static byte ToByte(double d)
	{
		return d switch
		{
			> byte.MaxValue => byte.MaxValue,
			< byte.MinValue => byte.MinValue,
			_ => (byte)d
		};
	}
}