using System;
using System.Collections.Generic;
using System.IO;
using JPEG.Huffman;

namespace JPEG;

public readonly ref struct CompressedImage
{
	public int Width { get; init; }
	public int Height { get; init; }

	public int Quality { get; init; }
	
	public HuffmanNode Huffman { get; init; }

	public long BitsCount { get; init; }
	public Span<byte> CompressedBytes { get; init; }

	public void Save(string path)
	{
		using var sw = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.SequentialScan);
		using var writer = new BinaryWriter(sw);
		
		writer.Write(Width);
		writer.Write(Height);
		writer.Write(Quality);
			
		SaveHuffmanTree(Huffman, sw);

		writer.Write(BitsCount);
		writer.Write(CompressedBytes.Length);
		writer.Write(CompressedBytes);
	}

	public static CompressedImage Load(string path)
	{
		using var sr = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
		using var reader = new BinaryReader(sr);

		var width = reader.ReadInt32();
		var height = reader.ReadInt32();
		var quality = reader.ReadInt32();
		
		var root = LoadHuffmanTree(sr);

		var bitsCounts = reader.ReadInt64();
		var compressedBytesCount = reader.ReadInt32();

		return new CompressedImage
		{
			Quality = quality,
			Height = height,
			Width = width,
			BitsCount = bitsCounts,
			Huffman = root,
			CompressedBytes = reader.ReadBytes(compressedBytesCount)
		};
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