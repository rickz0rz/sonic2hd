using System;
using System.Buffers.Binary;

namespace SonicOrca.Original;

/// <summary>
/// The MD5 hash (RFC 1321) in managed code. The web build cannot use System.Security.Cryptography.MD5, because the
/// browser runtime does not have it. The importers use the hash only to make stable IDs, not for security.
/// </summary>
public static class Md5
{
	private static readonly int[] Shifts =
	{
		7, 12, 17, 22, 7, 12, 17, 22, 7, 12, 17, 22, 7, 12, 17, 22,
		5, 9, 14, 20, 5, 9, 14, 20, 5, 9, 14, 20, 5, 9, 14, 20,
		4, 11, 16, 23, 4, 11, 16, 23, 4, 11, 16, 23, 4, 11, 16, 23,
		6, 10, 15, 21, 6, 10, 15, 21, 6, 10, 15, 21, 6, 10, 15, 21
	};

	private static readonly uint[] Constants = MakeConstants();

	/// <summary>Returns the 16 bytes of the MD5 hash of the data.</summary>
	public static byte[] HashData(ReadOnlySpan<byte> data)
	{
		// The message, a 1 bit, zero bits to 56 bytes modulo 64, and the length in bits
		int length = (data.Length + 8) / 64 * 64 + 64;
		byte[] message = new byte[length];
		data.CopyTo(message);
		message[data.Length] = 0x80;
		BinaryPrimitives.WriteUInt64LittleEndian(message.AsSpan(length - 8), (ulong)data.Length * 8);
		uint a0 = 0x67452301, b0 = 0xEFCDAB89, c0 = 0x98BADCFE, d0 = 0x10325476;
		Span<uint> words = stackalloc uint[16];
		for (int block = 0; block < length; block += 64)
		{
			for (int i = 0; i < 16; i++)
			{
				words[i] = BinaryPrimitives.ReadUInt32LittleEndian(message.AsSpan(block + i * 4));
			}
			uint a = a0, b = b0, c = c0, d = d0;
			for (int i = 0; i < 64; i++)
			{
				uint f;
				int g;
				if (i < 16)
				{
					f = (b & c) | (~b & d);
					g = i;
				}
				else if (i < 32)
				{
					f = (d & b) | (~d & c);
					g = (5 * i + 1) % 16;
				}
				else if (i < 48)
				{
					f = b ^ c ^ d;
					g = (3 * i + 5) % 16;
				}
				else
				{
					f = c ^ (b | ~d);
					g = 7 * i % 16;
				}
				f += a + Constants[i] + words[g];
				a = d;
				d = c;
				c = b;
				b += (f << Shifts[i]) | (f >> (32 - Shifts[i]));
			}
			a0 += a;
			b0 += b;
			c0 += c;
			d0 += d;
		}
		byte[] hash = new byte[16];
		BinaryPrimitives.WriteUInt32LittleEndian(hash.AsSpan(0), a0);
		BinaryPrimitives.WriteUInt32LittleEndian(hash.AsSpan(4), b0);
		BinaryPrimitives.WriteUInt32LittleEndian(hash.AsSpan(8), c0);
		BinaryPrimitives.WriteUInt32LittleEndian(hash.AsSpan(12), d0);
		return hash;
	}

	/// <summary>The constants of the rounds: the integer part of 2^32 times abs(sin(i + 1)).</summary>
	private static uint[] MakeConstants()
	{
		uint[] constants = new uint[64];
		for (int i = 0; i < 64; i++)
		{
			constants[i] = (uint)(long)Math.Floor(Math.Abs(Math.Sin(i + 1)) * 4294967296.0);
		}
		return constants;
	}
}
