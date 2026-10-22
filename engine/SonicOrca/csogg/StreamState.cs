using System;
using System.Text;

namespace csogg;

public class StreamState
{
	private byte[] body_data;

	private int body_storage;

	private int body_fill;

	private int body_returned;

	private int[] lacing_vals;

	private long[] granule_vals;

	private int lacing_storage;

	private int lacing_fill;

	private int lacing_packet;

	private int lacing_returned;

	private byte[] header = new byte[282];

	private int header_fill;

	public int e_o_s;

	private int b_o_s;

	private int serialno;

	private int pageno;

	private long packetno;

	private long granulepos;

	private StreamState(int serialno)
		: this()
	{
		init(serialno);
	}

	public StreamState()
	{
		init();
	}

	private void init()
	{
		body_storage = 16384;
		body_data = new byte[body_storage];
		lacing_storage = 1024;
		lacing_vals = new int[lacing_storage];
		granule_vals = new long[lacing_storage];
	}

	public void init(int serialno)
	{
		if (body_data == null)
		{
			init();
		}
		else
		{
			for (int i = 0; i < body_data.Length; i++)
			{
				body_data[i] = 0;
			}
			for (int j = 0; j < lacing_vals.Length; j++)
			{
				lacing_vals[j] = 0;
			}
			for (int k = 0; k < granule_vals.Length; k++)
			{
				granule_vals[k] = 0L;
			}
		}
		this.serialno = serialno;
	}

	public void clear()
	{
		body_data = null;
		lacing_vals = null;
		granule_vals = null;
	}

	private void destroy()
	{
		clear();
	}

	private void body_expand(int needed)
	{
		if (body_storage <= body_fill + needed)
		{
			body_storage += needed + 1024;
			byte[] destinationArray = new byte[body_storage];
			Array.Copy(body_data, 0, destinationArray, 0, body_data.Length);
			body_data = destinationArray;
		}
	}

	private void lacing_expand(int needed)
	{
		if (lacing_storage <= lacing_fill + needed)
		{
			lacing_storage += needed + 32;
			int[] destinationArray = new int[lacing_storage];
			Array.Copy(lacing_vals, 0, destinationArray, 0, lacing_vals.Length);
			lacing_vals = destinationArray;
			long[] destinationArray2 = new long[lacing_storage];
			Array.Copy(granule_vals, 0, destinationArray2, 0, granule_vals.Length);
			granule_vals = destinationArray2;
		}
	}

	public int packetin(Packet op)
	{
		int num = op.bytes / 255 + 1;
		if (body_returned != 0)
		{
			body_fill -= body_returned;
			if (body_fill != 0)
			{
				Array.Copy(body_data, body_returned, body_data, 0, body_fill);
			}
			body_returned = 0;
		}
		body_expand(op.bytes);
		lacing_expand(num);
		Array.Copy(op.packet_base, op.packet, body_data, body_fill, op.bytes);
		body_fill += op.bytes;
		int i;
		for (i = 0; i < num - 1; i++)
		{
			lacing_vals[lacing_fill + i] = 255;
			granule_vals[lacing_fill + i] = granulepos;
		}
		lacing_vals[lacing_fill + i] = op.bytes % 255;
		granulepos = (granule_vals[lacing_fill + i] = op.granulepos);
		lacing_vals[lacing_fill] |= 256;
		lacing_fill += num;
		packetno++;
		if (op.e_o_s != 0)
		{
			e_o_s = 1;
		}
		return 0;
	}

	public int packetout(Packet op)
	{
		int num = lacing_returned;
		if (lacing_packet <= num)
		{
			return 0;
		}
		if ((lacing_vals[num] & 0x400) != 0)
		{
			lacing_returned++;
			packetno++;
			return -1;
		}
		int num2 = lacing_vals[num] & 0xFF;
		int num3 = 0;
		op.packet_base = body_data;
		op.packet = body_returned;
		op.e_o_s = lacing_vals[num] & 0x200;
		op.b_o_s = lacing_vals[num] & 0x100;
		num3 += num2;
		while (num2 == 255)
		{
			int num4 = lacing_vals[++num];
			num2 = num4 & 0xFF;
			if ((num4 & 0x200) != 0)
			{
				op.e_o_s = 512;
			}
			num3 += num2;
		}
		op.packetno = packetno;
		op.granulepos = granule_vals[num];
		op.bytes = num3;
		body_returned += num3;
		lacing_returned = num + 1;
		packetno++;
		return 1;
	}

	public int pagein(Page og)
	{
		byte[] header_base = og.header_base;
		int num = og.header;
		byte[] body_base = og.body_base;
		int num2 = og.body;
		int num3 = og.body_len;
		int i = 0;
		int num4 = og.version();
		int num5 = og.continued();
		int num6 = og.bos();
		int num7 = og.eos();
		long num8 = og.granulepos();
		int num9 = og.serialno();
		int num10 = og.pageno();
		int num11 = header_base[num + 26] & 0xFF;
		int num12 = lacing_returned;
		int num13 = body_returned;
		if (num13 != 0)
		{
			body_fill -= num13;
			if (body_fill != 0)
			{
				Array.Copy(body_data, num13, body_data, 0, body_fill);
			}
			body_returned = 0;
		}
		if (num12 != 0)
		{
			if (lacing_fill - num12 != 0)
			{
				Array.Copy(lacing_vals, num12, lacing_vals, 0, lacing_fill - num12);
				Array.Copy(granule_vals, num12, granule_vals, 0, lacing_fill - num12);
			}
			lacing_fill -= num12;
			lacing_packet -= num12;
			lacing_returned = 0;
		}
		if (num9 != serialno)
		{
			return -1;
		}
		if (num4 > 0)
		{
			return -1;
		}
		lacing_expand(num11 + 1);
		if (num10 != pageno)
		{
			for (int j = lacing_packet; j < lacing_fill; j++)
			{
				body_fill -= lacing_vals[j] & 0xFF;
			}
			lacing_fill = lacing_packet;
			if (pageno != -1)
			{
				lacing_vals[lacing_fill++] = 1024;
				lacing_packet++;
			}
			if (num5 != 0)
			{
				num6 = 0;
				for (; i < num11; i++)
				{
					int num14 = header_base[num + 27 + i] & 0xFF;
					num2 += num14;
					num3 -= num14;
					if (num14 < 255)
					{
						i++;
						break;
					}
				}
			}
		}
		if (num3 != 0)
		{
			body_expand(num3);
			Array.Copy(body_base, num2, body_data, body_fill, num3);
			body_fill += num3;
		}
		int num15 = -1;
		while (i < num11)
		{
			int num16 = header_base[num + 27 + i] & 0xFF;
			lacing_vals[lacing_fill] = num16;
			granule_vals[lacing_fill] = -1L;
			if (num6 != 0)
			{
				lacing_vals[lacing_fill] |= 256;
				num6 = 0;
			}
			if (num16 < 255)
			{
				num15 = lacing_fill;
			}
			lacing_fill++;
			i++;
			if (num16 < 255)
			{
				lacing_packet = lacing_fill;
			}
		}
		if (num15 != -1)
		{
			granule_vals[num15] = num8;
		}
		if (num7 != 0)
		{
			e_o_s = 1;
			if (lacing_fill > 0)
			{
				lacing_vals[lacing_fill - 1] |= 512;
			}
		}
		pageno = num10 + 1;
		return 0;
	}

	public int flush(Page og)
	{
		int num = 0;
		int num2 = ((lacing_fill > 255) ? 255 : lacing_fill);
		int num3 = 0;
		int num4 = 0;
		long num5 = granule_vals[0];
		if (num2 == 0)
		{
			return 0;
		}
		if (b_o_s == 0)
		{
			num5 = 0L;
			for (num = 0; num < num2; num++)
			{
				if ((lacing_vals[num] & 0xFF) < 255)
				{
					num++;
					break;
				}
			}
		}
		else
		{
			for (num = 0; num < num2; num++)
			{
				if (num4 > 4096)
				{
					break;
				}
				num4 += lacing_vals[num] & 0xFF;
				num5 = granule_vals[num];
			}
		}
		string s = "OggS";
		byte[] bytes = Encoding.UTF8.GetBytes(s);
		Array.Copy(bytes, 0, header, 0, bytes.Length);
		header[4] = 0;
		header[5] = 0;
		if ((lacing_vals[0] & 0x100) == 0)
		{
			header[5] |= 1;
		}
		if (b_o_s == 0)
		{
			header[5] |= 2;
		}
		if (e_o_s != 0 && lacing_fill == num)
		{
			header[5] |= 4;
		}
		b_o_s = 1;
		for (int i = 6; i < 14; i++)
		{
			header[i] = (byte)num5;
			num5 >>= 8;
		}
		int num6 = serialno;
		for (int i = 14; i < 18; i++)
		{
			header[i] = (byte)num6;
			num6 >>= 8;
		}
		if (pageno == -1)
		{
			pageno = 0;
		}
		int num7 = pageno++;
		for (int i = 18; i < 22; i++)
		{
			header[i] = (byte)num7;
			num7 >>= 8;
		}
		header[22] = 0;
		header[23] = 0;
		header[24] = 0;
		header[25] = 0;
		header[26] = (byte)num;
		for (int i = 0; i < num; i++)
		{
			header[i + 27] = (byte)lacing_vals[i];
			num3 += header[i + 27] & 0xFF;
		}
		og.header_base = header;
		og.header = 0;
		og.header_len = (header_fill = num + 27);
		og.body_base = body_data;
		og.body = body_returned;
		og.body_len = num3;
		lacing_fill -= num;
		Array.Copy(lacing_vals, num, lacing_vals, 0, lacing_fill * 4);
		Array.Copy(granule_vals, num, granule_vals, 0, lacing_fill * 8);
		body_returned += num3;
		og.checksum();
		return 1;
	}

	public int pageout(Page og)
	{
		if ((e_o_s != 0 && lacing_fill != 0) || body_fill - body_returned > 4096 || lacing_fill >= 255 || (lacing_fill != 0 && b_o_s == 0))
		{
			return flush(og);
		}
		return 0;
	}

	public int eof()
	{
		return e_o_s;
	}

	public int reset()
	{
		body_fill = 0;
		body_returned = 0;
		lacing_fill = 0;
		lacing_packet = 0;
		lacing_returned = 0;
		header_fill = 0;
		e_o_s = 0;
		b_o_s = 0;
		pageno = -1;
		packetno = 0L;
		granulepos = 0L;
		return 0;
	}
}
