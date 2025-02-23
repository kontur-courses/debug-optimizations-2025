using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace JPEG.Huffman;

public static class HuffmanCodec
{
	public static Span<byte> Encode(Span<byte> data, out long bitsCount, out HuffmanNode root)
	{
		var frequences = CalcFrequences(data);
		root = BuildHuffmanTree(frequences);
		var encodeTable = new BitsWithLength[byte.MaxValue + 1];
		FillEncodeTable(root, encodeTable);
		
		var bitsBuffer = new BitsBuffer(data.Length);
		bitsBuffer.Fill(data, encodeTable);
		
		return bitsBuffer.ToArray(out bitsCount);
	}

	public static Span<byte> Decode(Span<byte> encodedData, long bitsCount, int length, HuffmanNode root)
	{
		var result = new byte[length].AsSpan();
		var resultIndex = 0;
		var current = root;
		long bitsProcessed = 0;

		foreach (var b in encodedData)
		{
			for (var i = 7; i >= 0 && bitsProcessed < bitsCount; i--)
			{
				var bit = (b >> i) & 1;
				current = bit == 1 ? current.Left : current.Right;
				bitsProcessed++;

				if (current.LeafLabel != null)
				{
					result[resultIndex++] = current.LeafLabel.Value;
					current = root;
				}
			}
		}

		return result;
	}

	private static void FillEncodeTable(HuffmanNode node, BitsWithLength[] encodeSubstitutionTable, int bitvector = 0, int depth = 0)
	{
		while (true)
		{
			if (node.LeafLabel != null)
				encodeSubstitutionTable[node.LeafLabel.Value] = new BitsWithLength(bitvector, depth);
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
		var lenght = data.Length;
		
		ref var start = ref data.GetPinnableReference();
		ref var end = ref Unsafe.Add(ref start, lenght);
		
		while (Unsafe.IsAddressLessThan(ref start, ref end))
		{
			result[start]++;
			start = ref Unsafe.Add(ref start, 1);
			result[start]++;
			start = ref Unsafe.Add(ref start, 1);
			result[start]++;
			start = ref Unsafe.Add(ref start, 1);
			result[start]++;
			start = ref Unsafe.Add(ref start, 1);
		}
		return result;
	}
	
	private static int[] CalcFrequencesSkip(Span<byte> data)
	{
		var result = new int[byte.MaxValue + 1];
		var lenght = data.Length;

		for (var i = 0; i < 256; i++)
		{
			result[i] = 1;
		}
		
		ref var start = ref data.GetPinnableReference();
		ref var end = ref Unsafe.Add(ref start, lenght);
		
		while (Unsafe.IsAddressLessThan(ref start, ref end))
		{
			result[start]++;
			start = ref Unsafe.Add(ref start, 16);
			result[start]++;
			start = ref Unsafe.Add(ref start, 16);
			result[start]++;
			start = ref Unsafe.Add(ref start, 16);
			result[start]++;
			start = ref Unsafe.Add(ref start, 16);
		}
		return result;
	}
}