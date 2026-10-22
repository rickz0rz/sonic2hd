using System;

namespace csogg;

public class SyncState
{
	public byte[] data;

	private int storage;

	private int fill;

	private int returned;

	private int unsynced;

	private int headerbytes;

	private int bodybytes;

	private Page pageseek_p = new Page();

	private byte[] chksum = new byte[4];

	public int clear()
	{
		data = null;
		return 0;
	}

	public int buffer(int size)
	{
		if (returned != 0)
		{
			fill -= returned;
			if (fill > 0)
			{
				Array.Copy(data, returned, data, 0, fill);
			}
			returned = 0;
		}
		if (size > storage - fill)
		{
			int num = size + fill + 4096;
			if (data != null)
			{
				byte[] destinationArray = new byte[num];
				Array.Copy(data, 0, destinationArray, 0, data.Length);
				data = destinationArray;
			}
			else
			{
				data = new byte[num];
			}
			storage = num;
		}
		return fill;
	}

	public int wrote(int bytes)
	{
		if (fill + bytes > storage)
		{
			return -1;
		}
		fill += bytes;
		return 0;
	}

	public int pageseek(Page og)
	{
		int num = returned;
		int num2 = fill - returned;
		if (headerbytes == 0)
		{
			if (num2 < 27)
			{
				return 0;
			}
			if (data[num] != 79 || data[num + 1] != 103 || data[num + 2] != 103 || data[num + 3] != 83)
			{
				headerbytes = 0;
				bodybytes = 0;
				int num3 = 0;
				for (int i = 0; i < num2 - 1; i++)
				{
					if (data[num + 1 + i] == 79)
					{
						num3 = num + 1 + i;
						break;
					}
				}
				if (num3 == 0)
				{
					num3 = fill;
				}
				returned = num3;
				return -(num3 - num);
			}
			int num4 = (data[num + 26] & 0xFF) + 27;
			if (num2 < num4)
			{
				return 0;
			}
			for (int j = 0; j < (data[num + 26] & 0xFF); j++)
			{
				bodybytes += data[num + 27 + j] & 0xFF;
			}
			headerbytes = num4;
		}
		if (bodybytes + headerbytes > num2)
		{
			return 0;
		}
		lock (chksum)
		{
			Array.Copy(data, num + 22, chksum, 0, 4);
			data[num + 22] = 0;
			data[num + 23] = 0;
			data[num + 24] = 0;
			data[num + 25] = 0;
			Page page = pageseek_p;
			page.header_base = data;
			page.header = num;
			page.header_len = headerbytes;
			page.body_base = data;
			page.body = num + headerbytes;
			page.body_len = bodybytes;
			page.checksum();
			if (chksum[0] != data[num + 22] || chksum[1] != data[num + 23] || chksum[2] != data[num + 24] || chksum[3] != data[num + 25])
			{
				Array.Copy(chksum, 0, data, num + 22, 4);
				headerbytes = 0;
				bodybytes = 0;
				int num3 = 0;
				for (int k = 0; k < num2 - 1; k++)
				{
					if (data[num + 1 + k] == 79)
					{
						num3 = num + 1 + k;
						break;
					}
				}
				if (num3 == 0)
				{
					num3 = fill;
				}
				returned = num3;
				return -(num3 - num);
			}
		}
		num = returned;
		if (og != null)
		{
			og.header_base = data;
			og.header = num;
			og.header_len = headerbytes;
			og.body_base = data;
			og.body = num + headerbytes;
			og.body_len = bodybytes;
		}
		unsynced = 0;
		returned += (num2 = headerbytes + bodybytes);
		headerbytes = 0;
		bodybytes = 0;
		return num2;
	}

	public int pageout(Page og)
	{
		do
		{
			int num = pageseek(og);
			if (num > 0)
			{
				return 1;
			}
			if (num == 0)
			{
				return 0;
			}
		}
		while (unsynced != 0);
		unsynced = 1;
		return -1;
	}

	public int reset()
	{
		fill = 0;
		returned = 0;
		unsynced = 0;
		headerbytes = 0;
		bodybytes = 0;
		return 0;
	}

	public void init()
	{
	}
}
