using System;
using System.IO;
using System.Runtime.InteropServices;

namespace JPEG.Images;

public unsafe class SimpleBitmap : IDisposable
{
    public int Width { get; private set; }
    public int Height { get; private set; }
    public int Stride { get; private set; }

    public IntPtr Scan0 => fileBytes + offset;
    private IntPtr fileBytes;
    private int fileLength;
    private int offset;
    private bool isBottomUp;
    private const int HeaderSize = 54;

    public byte* GetPointer() => offset + (byte*)fileBytes;

    public SimpleBitmap(string filePath)
    {
        FillData(filePath);
        var header = new Span<byte>(GetPointer(), HeaderSize);
        ParseHeader(header);
        LoadImageData();
    }
    
    public SimpleBitmap(int width, int height)
    {
        Width = width;
        Height = height;
        Stride = (Width * 3 + 3) & ~3;
        isBottomUp = false;
        fileLength = HeaderSize + (Stride * Height);
        fileBytes = Marshal.AllocHGlobal(fileLength);
    }

    private void FillData(string filePath)
    {
        using var fs = File.OpenRead(filePath);
        fileLength = (int)fs.Length;
        fileBytes = Marshal.AllocHGlobal(fileLength);
        var fileSpan = new Span<byte>(GetPointer(), fileLength);
        fs.ReadExactly(fileSpan);
    }

    private void ParseHeader(Span<byte> header)
    {
        ValidateFormat(header);

        Width = BitConverter.ToInt32(header.Slice(18, 4));
        Height = BitConverter.ToInt32(header.Slice(22, 4));
        offset = BitConverter.ToInt32(header.Slice(10, 4));
        
        Stride = (Width * 3 + 3) & ~3;
        isBottomUp = Height > 0;
        Height = Math.Abs(Height);
    }

    private void ValidateFormat(Span<byte> header)
    {
        if (header[0] != 'B' || header[1] != 'M')
            throw new ArgumentException("Not a valid BMP file");

        var dibHeaderSize = BitConverter.ToInt32(header.Slice(14, 4));
        if (dibHeaderSize < 40)
            throw new ArgumentException("Unsupported BMP format");

        var bpp = BitConverter.ToInt16(header.Slice(28, 2));
        if (bpp != 24)
            throw new ArgumentException("Only 24bpp BMPs are supported");

        var compression = BitConverter.ToInt32(header.Slice(30, 4));
        if (compression != 0)
            throw new ArgumentException("Compressed BMPs are not supported");
    }

    private void LoadImageData()
    {
        var imgData = new Span<byte>((byte*)fileBytes + offset, Stride * Height);
        if (isBottomUp)
        {
            FlipImageInPlace(imgData);
        }
    }

    private void FlipImageInPlace(Span<byte> imgData)
    {
        Span<byte> temp = stackalloc byte[Stride];
        var halfHeight = Height / 2;
        for (var i = 0; i < halfHeight; i++)
        {
            var topOffset = i * Stride;
            var bottomOffset = (Height - 1 - i) * Stride;
            var topRow = imgData.Slice(topOffset, Stride);
            var bottomRow = imgData.Slice(bottomOffset, Stride);

            bottomRow.CopyTo(temp);
            topRow.CopyTo(bottomRow);
            temp.CopyTo(topRow);
        }
    }

    
    public void Dispose()
    {
        if (fileBytes != IntPtr.Zero)
        {
            Marshal.FreeHGlobal(fileBytes);
        }
    }
    
    public void Save(string filePath)
    {
        using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write);
        using var writer = new BinaryWriter(fs);

        writer.Write((byte)'B');
        writer.Write((byte)'M');
        writer.Write(HeaderSize + (Stride * Height));
        writer.Write(0);
        writer.Write(HeaderSize);
        writer.Write(40);
        writer.Write(Width);
        writer.Write(-Height);
        writer.Write((short)1);
        writer.Write((short)24);
        writer.Write(0);
        writer.Write(Stride * Height);
        writer.Write(2835);
        writer.Write(2835);
        writer.Write(0);
        writer.Write(0);

        var imgData = new Span<byte>((byte*)fileBytes + offset, Stride * Height);
        writer.Write(imgData);
    }
}