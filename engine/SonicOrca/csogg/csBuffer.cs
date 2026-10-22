using System;

namespace csogg;

public class csBuffer
{
	private static int BUFFER_INCREMENT = 256;

	private static uint[] mask = new uint[33]
	{
		0u, 1u, 3u, 7u, 15u, 31u, 63u, 127u, 255u, 511u,
		1023u, 2047u, 4095u, 8191u, 16383u, 32767u, 65535u, 131071u, 262143u, 524287u,
		1048575u, 2097151u, 4194303u, 8388607u, 16777215u, 33554431u, 67108863u, 134217727u, 268435455u, 536870911u,
		1073741823u, 2147483647u, 4294967295u
	};

	private int ptr;

	private byte[] buffer;

	private int endbit;

	private int endbyte;

	private int storage;

	public void writeinit()
	{
		buffer = new byte[BUFFER_INCREMENT];
		ptr = 0;
		buffer[0] = 0;
		storage = BUFFER_INCREMENT;
	}

	public void write(byte[] s)
	{
		for (int i = 0; i < s.Length && s[i] != 0; i++)
		{
			write(s[i], 8);
		}
	}

	public void read(byte[] s, int bytes)
	{
		int num = 0;
		while (bytes-- != 0)
		{
			s[num++] = (byte)read(8);
		}
	}

	private void reset()
	{
		ptr = 0;
		buffer[0] = 0;
		endbit = (endbyte = 0);
	}

	public void writeclear()
	{
		buffer = null;
	}

	public void readinit(byte[] buf, int start, int bytes)
	{
		ptr = start;
		buffer = buf;
		endbit = (endbyte = 0);
		storage = bytes;
	}

	public void write(int vvalue, int bits)
	{
		if (endbyte + 4 >= storage)
		{
			byte[] destinationArray = new byte[storage + BUFFER_INCREMENT];
			Array.Copy(buffer, 0, destinationArray, 0, storage);
			buffer = destinationArray;
			storage += BUFFER_INCREMENT;
		}
		vvalue &= (int)mask[bits];
		bits += endbit;
		buffer[ptr] |= (byte)(vvalue << endbit);
		if (bits >= 8)
		{
			buffer[ptr + 1] = (byte)((uint)vvalue >> 8 - endbit);
			if (bits >= 16)
			{
				buffer[ptr + 2] = (byte)((uint)vvalue >> 16 - endbit);
				if (bits >= 24)
				{
					buffer[ptr + 3] = (byte)((uint)vvalue >> 24 - endbit);
					if (bits >= 32)
					{
						if (endbit > 0)
						{
							buffer[ptr + 4] = (byte)((uint)vvalue >> 32 - endbit);
						}
						else
						{
							buffer[ptr + 4] = 0;
						}
					}
				}
			}
		}
		endbyte += bits / 8;
		ptr += bits / 8;
		endbit = bits & 7;
	}

	public int look(int bits)
	{
		uint num = mask[bits];
		bits += endbit;
		if (endbyte + 4 >= storage && endbyte + (bits - 1) / 8 >= storage)
		{
			return -1;
		}
		int num2 = (buffer[ptr] & 0xFF) >> endbit;
		if (bits > 8)
		{
			num2 |= (buffer[ptr + 1] & 0xFF) << 8 - endbit;
			if (bits > 16)
			{
				num2 |= (buffer[ptr + 2] & 0xFF) << 16 - endbit;
				if (bits > 24)
				{
					num2 |= (buffer[ptr + 3] & 0xFF) << 24 - endbit;
					if (bits > 32 && endbit != 0)
					{
						num2 |= (buffer[ptr + 4] & 0xFF) << 32 - endbit;
					}
				}
			}
		}
		return (int)(num & num2);
	}

	public int look1()
	{
		if (endbyte >= storage)
		{
			return -1;
		}
		return (buffer[ptr] >> endbit) & 1;
	}

	public void adv(int bits)
	{
		bits += endbit;
		ptr += bits / 8;
		endbyte += bits / 8;
		endbit = bits & 7;
	}

	public void adv1()
	{
		endbit++;
		if (endbit > 7)
		{
			endbit = 0;
			ptr++;
			endbyte++;
		}
	}

	public int read(int bits)
	{
		uint num = mask[bits];
		bits += endbit;
		int result;
		if (endbyte + 4 >= storage)
		{
			result = -1;
			if (endbyte + (bits - 1) / 8 >= storage)
			{
				ptr += bits / 8;
				endbyte += bits / 8;
				endbit = bits & 7;
				return result;
			}
		}
		result = (buffer[ptr] & 0xFF) >> endbit;
		if (bits > 8)
		{
			result |= (buffer[ptr + 1] & 0xFF) << 8 - endbit;
			if (bits > 16)
			{
				result |= (buffer[ptr + 2] & 0xFF) << 16 - endbit;
				if (bits > 24)
				{
					result |= (buffer[ptr + 3] & 0xFF) << 24 - endbit;
					if (bits > 32 && endbit != 0)
					{
						result |= (buffer[ptr + 4] & 0xFF) << 32 - endbit;
					}
				}
			}
		}
		result &= (int)num;
		ptr += bits / 8;
		endbyte += bits / 8;
		endbit = bits & 7;
		return result;
	}

	public int read1()
	{
		if (endbyte >= storage)
		{
			endbit++;
			if (endbit > 7)
			{
				endbit = 0;
				ptr++;
				endbyte++;
			}
			return -1;
		}
		int result = (buffer[ptr] >> endbit) & 1;
		endbit++;
		if (endbit > 7)
		{
			endbit = 0;
			ptr++;
			endbyte++;
		}
		return result;
	}

	public int bytes()
	{
		return endbyte + (endbit + 7) / 8;
	}

	public int bits()
	{
		return endbyte * 8 + endbit;
	}

	public static int ilog(int v)
	{
		int num = 0;
		while (v > 0)
		{
			num++;
			v >>= 1;
		}
		return num;
	}

	public byte[] buf()
	{
		return buffer;
	}
}
