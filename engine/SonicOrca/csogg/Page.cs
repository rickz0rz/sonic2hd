namespace csogg;

public class Page
{
	private static uint[] crc_lookup = new uint[256];

	public byte[] header_base;

	public int header;

	public int header_len;

	public byte[] body_base;

	public int body;

	public int body_len;

	private static uint crc_entry(uint index)
	{
		uint num = index << 24;
		for (int i = 0; i < 8; i++)
		{
			num = (((num & 0x80000000u) == 0) ? (num << 1) : ((num << 1) ^ 0x4C11DB7));
		}
		return num & 0xFFFFFFFFu;
	}

	internal int version()
	{
		return header_base[header + 4] & 0xFF;
	}

	internal int continued()
	{
		return header_base[header + 5] & 1;
	}

	public int bos()
	{
		return header_base[header + 5] & 2;
	}

	public int eos()
	{
		return header_base[header + 5] & 4;
	}

	public long granulepos()
	{
		return ((((((((((((((long)(header_base[header + 13] & 0xFF) << 8) | (uint)(header_base[header + 12] & 0xFF)) << 8) | (uint)(header_base[header + 11] & 0xFF)) << 8) | (uint)(header_base[header + 10] & 0xFF)) << 8) | (uint)(header_base[header + 9] & 0xFF)) << 8) | (uint)(header_base[header + 8] & 0xFF)) << 8) | (uint)(header_base[header + 7] & 0xFF)) << 8) | (uint)(header_base[header + 6] & 0xFF);
	}

	public int serialno()
	{
		return (header_base[header + 14] & 0xFF) | ((header_base[header + 15] & 0xFF) << 8) | ((header_base[header + 16] & 0xFF) << 16) | ((header_base[header + 17] & 0xFF) << 24);
	}

	internal int pageno()
	{
		return (header_base[header + 18] & 0xFF) | ((header_base[header + 19] & 0xFF) << 8) | ((header_base[header + 20] & 0xFF) << 16) | ((header_base[header + 21] & 0xFF) << 24);
	}

	internal void checksum()
	{
		uint num = 0u;
		for (int i = 0; i < header_len; i++)
		{
			uint num2 = (uint)(header_base[header + i] & 0xFF);
			uint num3 = (num >> 24) & 0xFF;
			num = (num << 8) ^ crc_lookup[num2 ^ num3];
		}
		for (int j = 0; j < body_len; j++)
		{
			uint num2 = (uint)(body_base[body + j] & 0xFF);
			uint num3 = (num >> 24) & 0xFF;
			num = (num << 8) ^ crc_lookup[num2 ^ num3];
		}
		header_base[header + 22] = (byte)num;
		header_base[header + 23] = (byte)(num >> 8);
		header_base[header + 24] = (byte)(num >> 16);
		header_base[header + 25] = (byte)(num >> 24);
	}

	public Page()
	{
		for (uint num = 0u; num < crc_lookup.Length; num++)
		{
			crc_lookup[num] = crc_entry(num);
		}
	}
}
