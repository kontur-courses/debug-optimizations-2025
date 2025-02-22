using System;
using System.Collections.Generic;
using System.IO;
using JPEG.Huffman;

namespace JPEG;

public class CompressedImage
{
	public int Width { get; set; }
	public int Height { get; set; }

	public int Quality { get; set; }
	
	public HuffmanNode Huffman { get; set; }

	public long BitsCount { get; set; }
	public byte[] CompressedBytes { get; set; }

	public void Save(string path)
	{
		using(var sw = new FileStream(path, FileMode.Create))
		{
			byte[] buffer;

			buffer = BitConverter.GetBytes(Width);
			sw.Write(buffer, 0, buffer.Length);

			buffer = BitConverter.GetBytes(Height);
			sw.Write(buffer, 0, buffer.Length);

			buffer = BitConverter.GetBytes(Quality);
			sw.Write(buffer, 0, buffer.Length);
			
			SaveHuffmanTree(Huffman, sw);

			buffer = BitConverter.GetBytes(BitsCount);
			sw.Write(buffer, 0, buffer.Length);

			buffer = BitConverter.GetBytes(CompressedBytes.Length);
			sw.Write(buffer, 0, buffer.Length);

			sw.Write(CompressedBytes, 0, CompressedBytes.Length);
		}
	}

	public static CompressedImage Load(string path)
	{
		var result = new CompressedImage();
		using(var sr = new FileStream(path, FileMode.Open))
		{
			byte[] buffer = new byte[8];

			sr.Read(buffer, 0, 4);
			result.Width = BitConverter.ToInt32(buffer, 0);

			sr.Read(buffer, 0, 4);
			result.Height = BitConverter.ToInt32(buffer, 0);

			sr.Read(buffer, 0, 4);
			result.Quality = BitConverter.ToInt32(buffer, 0);
			
			result.Huffman = LoadHuffmanTree(sr);

			sr.Read(buffer, 0, 8);
			result.BitsCount = BitConverter.ToInt64(buffer, 0);

			sr.Read(buffer, 0, 4);
			var compressedBytesCount = BitConverter.ToInt32(buffer, 0);

			result.CompressedBytes = new byte[compressedBytesCount];
			var totalRead = 0;
			while(totalRead < compressedBytesCount)
				totalRead += sr.Read(result.CompressedBytes, totalRead, compressedBytesCount - totalRead);
		}
		return result;
	}

	private static void SaveHuffmanTree(HuffmanNode root, Stream stream)
	{
		if (root.LeafLabel.HasValue)
		{
			stream.WriteByte(1);
			stream.WriteByte(root.LeafLabel.Value);
		}
		else
		{
			stream.WriteByte(0);
			SaveHuffmanTree(root.Left, stream);
			SaveHuffmanTree(root.Right, stream);
		}
	}

	private static HuffmanNode LoadHuffmanTree(Stream stream)
	{
		var flag = stream.ReadByte();
		var node = new HuffmanNode();
		if (flag == 1)
		{
			var label = stream.ReadByte();
			node.LeafLabel = (byte)label;
		}
		else
		{
			node.Left = LoadHuffmanTree(stream);
			node.Right = LoadHuffmanTree(stream);
		}
    
		return node;
	}
}