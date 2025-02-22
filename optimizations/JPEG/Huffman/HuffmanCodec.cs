using System;
using System.Collections.Generic;

namespace JPEG.Huffman;

public static class HuffmanCodec
{
	public static byte[] Encode(Span<byte> data, out Dictionary<BitsWithLength, byte> decodeTable,
		out long bitsCount)
	{
		var frequences = CalcFrequences(data);
		var root = BuildHuffmanTree(frequences);
		
		var encodeTable = new BitsWithLength[byte.MaxValue + 1];
		FillEncodeTable(root, encodeTable);
		
		var bitsBuffer = new BitsBuffer();
		foreach (var b in data)
			bitsBuffer.Add(encodeTable[b]);

		decodeTable = CreateDecodeTable(encodeTable);
		return bitsBuffer.ToArray(out bitsCount);
	}

	public static byte[] Decode(byte[] encodedData, Dictionary<BitsWithLength, byte> decodeTable, long bitsCount)
	{
		var result = new List<byte>();

		var sample = new BitsWithLength { Bits = 0, BitsCount = 0 };
		for (var byteNum = 0; byteNum < encodedData.Length; byteNum++)
		{
			var b = encodedData[byteNum];
			for (var bitNum = 0; bitNum < 8 && byteNum * 8 + bitNum < bitsCount; bitNum++)
			{
				sample.Bits = (sample.Bits << 1) + ((b & (1 << (8 - bitNum - 1))) != 0 ? 1 : 0);
				sample.BitsCount++;

				if (decodeTable.TryGetValue(sample, out var decodedByte))
				{
					result.Add(decodedByte);

					sample.BitsCount = 0;
					sample.Bits = 0;
				}
			}
		}

		return result.ToArray();
	}

	private static Dictionary<BitsWithLength, byte> CreateDecodeTable(BitsWithLength[] encodeTable)
	{
		var result = new Dictionary<BitsWithLength, byte>();
		for (var b = 0; b < encodeTable.Length; b++)
		{
			var bitsWithLength = encodeTable[b];
			result[bitsWithLength] = (byte)b;
		}

		return result;
	}

	private static void FillEncodeTable(HuffmanNode node, BitsWithLength[] encodeSubstitutionTable, int bitvector = 0, int depth = 0)
	{
		while (true)
		{
			if (node.LeafLabel != null)
				encodeSubstitutionTable[node.LeafLabel.Value] = new BitsWithLength { Bits = bitvector, BitsCount = depth };
			else
			{
				if (node.Left == null) return;
				FillEncodeTable(node.Left, encodeSubstitutionTable, (bitvector << 1) + 1, depth + 1);
				node = node.Right;
				bitvector = (bitvector << 1) + 0;
				depth += 1;
				continue;
			}

			break;
		}
	}

	private static HuffmanNode BuildHuffmanTree(int[] frequences)
	{
		var queue = new PriorityQueue<HuffmanNode, int>();
		
		for (var i = 0; i < frequences.Length; i++)
		{
			if (frequences[i] > 0)
			{
				var node = new HuffmanNode { Frequency = frequences[i], LeafLabel = (byte)i };
				queue.Enqueue(node, frequences[i]);
			}
		}
		
		while (queue.Count > 1)
		{
			var firstMin = queue.Dequeue();
			var secondMin = queue.Dequeue();
			
			var parent = new HuffmanNode
			{
				Frequency = firstMin.Frequency + secondMin.Frequency,
				Left = secondMin,
				Right = firstMin
			};

			queue.Enqueue(parent, parent.Frequency);
		}

		return queue.Dequeue();
	}

	private static int[] CalcFrequences(Span<byte> data)
	{
		var result = new int[byte.MaxValue + 1];
		foreach (var b in data)
		{
			result[b]++;
		}
		return result;
	}
}