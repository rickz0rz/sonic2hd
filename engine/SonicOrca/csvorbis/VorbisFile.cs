using System;
using System.IO;
using csogg;

namespace csvorbis;

public class VorbisFile
{
	private static int CHUNKSIZE = 8500;

	private static int SEEK_SET = 0;

	private static int SEEK_CUR = 1;

	private static int SEEK_END = 2;

	private static int OV_FALSE = -1;

	private static int OV_EOF = -2;

	private static int OV_HOLE = -3;

	private static int OV_EREAD = -128;

	private static int OV_EFAULT = -129;

	private static int OV_EIMPL = -130;

	private static int OV_EINVAL = -131;

	private static int OV_ENOTVORBIS = -132;

	private static int OV_EBADHEADER = -133;

	private static int OV_EVERSION = -134;

	private static int OV_ENOTAUDIO = -135;

	private static int OV_EBADPACKET = -136;

	private static int OV_EBADLINK = -137;

	private static int OV_ENOSEEK = -138;

	private FileStream datasource;

	private bool skable;

	private long offset;

	private long end;

	private SyncState oy = new SyncState();

	private int links;

	private long[] offsets;

	private long[] dataoffsets;

	private int[] serialnos;

	private long[] pcmlengths;

	private Info[] vi;

	private Comment[] vc;

	private long pcm_offset;

	private bool decode_ready;

	private int current_serialno;

	private int current_link;

	private float bittrack;

	private float samptrack;

	private StreamState os;

	private DspState vd;

	private Block vb;

	private VorbisFile()
	{
		os = new StreamState();
		vd = new DspState();
		vb = new Block(vd);
	}

	public VorbisFile(string file)
		: this()
	{
		FileStream fileStream = null;
		try
		{
			fileStream = new FileStream(file, FileMode.Open, FileAccess.Read);
		}
		catch (Exception ex)
		{
			throw new csorbisException("VorbisFile: " + ex.Message);
		}
		if (open(fileStream, null, 0) == -1)
		{
			throw new csorbisException("VorbisFile: open return -1");
		}
	}

	public VorbisFile(FileStream inst, byte[] initial, int ibytes)
		: this()
	{
		open(inst, initial, ibytes);
	}

	private int get_data()
	{
		int num = oy.buffer(CHUNKSIZE);
		byte[] data = oy.data;
		int num2 = 0;
		try
		{
			num2 = datasource.Read(data, num, CHUNKSIZE);
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine(ex.Message);
			return OV_EREAD;
		}
		oy.wrote(num2);
		if (num2 == -1)
		{
			num2 = 0;
		}
		return num2;
	}

	private void seek_helper(long offst)
	{
		fseek(datasource, offst, SEEK_SET);
		offset = offst;
		oy.reset();
	}

	private int get_next_page(Page page, long boundary)
	{
		if (boundary > 0)
		{
			boundary += offset;
		}
		int num;
		while (true)
		{
			if (boundary > 0 && offset >= boundary)
			{
				return OV_FALSE;
			}
			num = oy.pageseek(page);
			if (num < 0)
			{
				offset -= num;
				continue;
			}
			if (num != 0)
			{
				break;
			}
			if (boundary == 0L)
			{
				return OV_FALSE;
			}
			int data = get_data();
			if (data == 0)
			{
				return OV_EOF;
			}
			if (data >= 0)
			{
				continue;
			}
			return OV_EREAD;
		}
		int result = (int)offset;
		offset += num;
		return result;
	}

	private int get_prev_page(Page page)
	{
		long num = offset;
		int num2 = -1;
		int num3;
		while (num2 == -1)
		{
			num -= CHUNKSIZE;
			if (num < 0)
			{
				num = 0L;
			}
			seek_helper(num);
			while (offset < num + CHUNKSIZE)
			{
				num3 = get_next_page(page, num + CHUNKSIZE - offset);
				if (num3 == OV_EREAD)
				{
					return OV_EREAD;
				}
				if (num3 < 0)
				{
					break;
				}
				num2 = num3;
			}
		}
		seek_helper(num2);
		num3 = get_next_page(page, CHUNKSIZE);
		if (num3 < 0)
		{
			return OV_EFAULT;
		}
		return num2;
	}

	private int bisect_forward_serialno(long begin, long searched, long end, int currentno, int m)
	{
		long num = end;
		long num2 = end;
		Page page = new Page();
		int num4;
		while (searched < num)
		{
			long num3 = ((num - searched >= CHUNKSIZE) ? ((searched + num) / 2) : searched);
			seek_helper(num3);
			num4 = get_next_page(page, -1L);
			if (num4 == OV_EREAD)
			{
				return OV_EREAD;
			}
			if (num4 < 0 || page.serialno() != currentno)
			{
				num = num3;
				if (num4 >= 0)
				{
					num2 = num4;
				}
			}
			else
			{
				searched = num4 + page.header_len + page.body_len;
			}
		}
		seek_helper(num2);
		num4 = get_next_page(page, -1L);
		if (num4 == OV_EREAD)
		{
			return OV_EREAD;
		}
		if (searched >= end || num4 == -1)
		{
			links = m + 1;
			offsets = new long[m + 2];
			offsets[m + 1] = searched;
		}
		else
		{
			num4 = bisect_forward_serialno(num2, offset, end, page.serialno(), m + 1);
			if (num4 == OV_EREAD)
			{
				return OV_EREAD;
			}
		}
		offsets[m] = begin;
		return 0;
	}

	private int fetch_headers(Info vi, Comment vc, int[] serialno, Page og_ptr)
	{
		Page page = new Page();
		Packet op = new Packet();
		if (og_ptr == null)
		{
			int num = get_next_page(page, CHUNKSIZE);
			if (num == OV_EREAD)
			{
				return OV_EREAD;
			}
			if (num < 0)
			{
				return OV_ENOTVORBIS;
			}
			og_ptr = page;
		}
		if (serialno != null)
		{
			serialno[0] = og_ptr.serialno();
		}
		os.init(og_ptr.serialno());
		vi.init();
		vc.init();
		int i = 0;
		while (i < 3)
		{
			os.pagein(og_ptr);
			for (; i < 3; i++)
			{
				switch (os.packetout(op))
				{
				case -1:
					Console.Error.WriteLine("Corrupt header in logical bitstream.");
					vi.clear();
					vc.clear();
					os.clear();
					return -1;
				default:
					if (vi.synthesis_headerin(vc, op) == 0)
					{
						continue;
					}
					Console.Error.WriteLine("Illegal header in logical bitstream.");
					vi.clear();
					vc.clear();
					os.clear();
					return -1;
				case 0:
					break;
				}
				break;
			}
			if (i < 3 && get_next_page(og_ptr, 1L) < 0)
			{
				Console.Error.WriteLine("Missing header in logical bitstream.");
				vi.clear();
				vc.clear();
				os.clear();
				return -1;
			}
		}
		return 0;
	}

	private void prefetch_all_headers(Info first_i, Comment first_c, int dataoffset)
	{
		Page page = new Page();
		vi = new Info[links];
		vc = new Comment[links];
		dataoffsets = new long[links];
		pcmlengths = new long[links];
		serialnos = new int[links];
		for (int i = 0; i < links; i++)
		{
			if (first_i != null && first_c != null && i == 0)
			{
				vi[i] = first_i;
				vc[i] = first_c;
				dataoffsets[i] = dataoffset;
			}
			else
			{
				seek_helper(offsets[i]);
				if (fetch_headers(vi[i], vc[i], null, null) == -1)
				{
					Console.Error.WriteLine("Error opening logical bitstream #" + (i + 1) + "\n");
					dataoffsets[i] = -1L;
				}
				else
				{
					dataoffsets[i] = offset;
					os.clear();
				}
			}
			long offst = offsets[i + 1];
			seek_helper(offst);
			while (true)
			{
				if (get_prev_page(page) == -1)
				{
					Console.Error.WriteLine("Could not find last page of logical bitstream #" + i + "\n");
					vi[i].clear();
					vc[i].clear();
					break;
				}
				if (page.granulepos() != -1)
				{
					serialnos[i] = page.serialno();
					pcmlengths[i] = page.granulepos();
					break;
				}
			}
		}
	}

	private int make_decode_ready()
	{
		if (decode_ready)
		{
			Environment.Exit(1);
		}
		vd.synthesis_init(vi[0]);
		vb.init(vd);
		decode_ready = true;
		return 0;
	}

	private int open_seekable()
	{
		Info first_i = new Info();
		Comment first_c = new Comment();
		Page page = new Page();
		int[] array = new int[1];
		int num = fetch_headers(first_i, first_c, array, null);
		int num2 = array[0];
		int dataoffset = (int)offset;
		os.clear();
		if (num == -1)
		{
			return -1;
		}
		skable = true;
		fseek(datasource, 0L, SEEK_END);
		offset = ftell(datasource);
		long num3 = offset;
		num3 = get_prev_page(page);
		if (page.serialno() != num2)
		{
			if (bisect_forward_serialno(0L, 0L, num3 + 1, num2, 0) < 0)
			{
				clear();
				return OV_EREAD;
			}
		}
		else if (bisect_forward_serialno(0L, num3, num3 + 1, num2, 0) < 0)
		{
			clear();
			return OV_EREAD;
		}
		prefetch_all_headers(first_i, first_c, dataoffset);
		return raw_seek(0);
	}

	private int open_nonseekable()
	{
		links = 1;
		vi = new Info[links];
		vi[0] = new Info();
		vc = new Comment[links];
		vc[0] = new Comment();
		int[] array = new int[1];
		if (fetch_headers(vi[0], vc[0], array, null) == -1)
		{
			return -1;
		}
		current_serialno = array[0];
		make_decode_ready();
		return 0;
	}

	private void decode_clear()
	{
		os.clear();
		vd.clear();
		vb.clear();
		decode_ready = false;
		bittrack = 0f;
		samptrack = 0f;
	}

	private int process_packet(int readp)
	{
		Page page = new Page();
		int num4;
		while (true)
		{
			if (decode_ready)
			{
				Packet packet = new Packet();
				if (os.packetout(packet) > 0)
				{
					long granulepos = packet.granulepos;
					if (vb.synthesis(packet) == 0)
					{
						int num = vd.synthesis_pcmout(null, null);
						vd.synthesis_blockin(vb);
						samptrack += vd.synthesis_pcmout(null, null) - num;
						bittrack += packet.bytes * 8;
						if (granulepos != -1 && packet.e_o_s == 0)
						{
							int num2 = (skable ? current_link : 0);
							int num3 = vd.synthesis_pcmout(null, null);
							granulepos -= num3;
							for (int i = 0; i < num2; i++)
							{
								granulepos += pcmlengths[i];
							}
							pcm_offset = granulepos;
						}
						return 1;
					}
				}
			}
			if (readp == 0)
			{
				return 0;
			}
			if (get_next_page(page, -1L) < 0)
			{
				return 0;
			}
			bittrack += page.header_len * 8;
			if (decode_ready && current_serialno != page.serialno())
			{
				decode_clear();
			}
			if (!decode_ready)
			{
				if (skable)
				{
					current_serialno = page.serialno();
					int j;
					for (j = 0; j < links && serialnos[j] != current_serialno; j++)
					{
					}
					if (j == links)
					{
						return -1;
					}
					current_link = j;
					os.init(current_serialno);
					os.reset();
				}
				else
				{
					int[] array = new int[1];
					num4 = fetch_headers(vi[0], vc[0], array, page);
					current_serialno = array[0];
					if (num4 != 0)
					{
						break;
					}
					current_link++;
					int j = 0;
				}
				make_decode_ready();
			}
			os.pagein(page);
		}
		return num4;
	}

	private int clear()
	{
		vb.clear();
		vd.clear();
		os.clear();
		if (vi != null && links != 0)
		{
			for (int i = 0; i < links; i++)
			{
				vi[i].clear();
				vc[i].clear();
			}
			vi = null;
			vc = null;
		}
		if (dataoffsets != null)
		{
			dataoffsets = null;
		}
		if (pcmlengths != null)
		{
			pcmlengths = null;
		}
		if (serialnos != null)
		{
			serialnos = null;
		}
		if (offsets != null)
		{
			offsets = null;
		}
		oy.clear();
		return 0;
	}

	private static int fseek(FileStream fis, long off, int whence)
	{
		if (fis.CanSeek)
		{
			try
			{
				if (whence == SEEK_SET)
				{
					fis.Seek(off, SeekOrigin.Begin);
				}
				else if (whence == SEEK_END)
				{
					fis.Seek(fis.Length - off, SeekOrigin.Begin);
				}
				else
				{
					Console.Error.WriteLine("seek: " + whence + " is not supported");
				}
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine(ex.Message);
			}
			return 0;
		}
		try
		{
			if (whence == 0)
			{
				fis.Seek(0L, SeekOrigin.Begin);
			}
			fis.Seek(off, SeekOrigin.Begin);
		}
		catch (Exception ex2)
		{
			Console.Error.WriteLine(ex2.Message);
			return -1;
		}
		return 0;
	}

	private static long ftell(FileStream fis)
	{
		try
		{
			if (fis.CanSeek)
			{
				return fis.Position;
			}
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine(ex.Message);
		}
		return 0L;
	}

	private int open(FileStream iis, byte[] initial, int ibytes)
	{
		return open_callbacks(iis, initial, ibytes);
	}

	private int open_callbacks(FileStream iis, byte[] initial, int ibytes)
	{
		datasource = iis;
		oy.init();
		if (initial != null)
		{
			int destinationIndex = oy.buffer(ibytes);
			Array.Copy(initial, 0, oy.data, destinationIndex, ibytes);
			oy.wrote(ibytes);
		}
		int num = ((!iis.CanSeek) ? open_nonseekable() : open_seekable());
		if (num != 0)
		{
			datasource = null;
			clear();
		}
		return num;
	}

	public int streams()
	{
		return links;
	}

	public bool seekable()
	{
		return skable;
	}

	public int bitrate(int i)
	{
		if (i >= links)
		{
			return -1;
		}
		if (!skable && i != 0)
		{
			return bitrate(0);
		}
		if (i < 0)
		{
			long num = 0L;
			for (int j = 0; j < links; j++)
			{
				num += (offsets[j + 1] - dataoffsets[j]) * 8;
			}
			return (int)Math.Round((float)num / time_total(-1));
		}
		if (skable)
		{
			return (int)Math.Round((float)((offsets[i + 1] - dataoffsets[i]) * 8) / time_total(i));
		}
		if (vi[i].bitrate_nominal > 0)
		{
			return vi[i].bitrate_nominal;
		}
		if (vi[i].bitrate_upper > 0)
		{
			if (vi[i].bitrate_lower > 0)
			{
				return (vi[i].bitrate_upper + vi[i].bitrate_lower) / 2;
			}
			return vi[i].bitrate_upper;
		}
		return -1;
	}

	public int bitrate_instant()
	{
		int num = (skable ? current_link : 0);
		if (samptrack == 0f)
		{
			return -1;
		}
		int result = (int)((double)(bittrack / samptrack * (float)vi[num].rate) + 0.5);
		bittrack = 0f;
		samptrack = 0f;
		return result;
	}

	public int serialnumber(int i)
	{
		if (i >= links)
		{
			return -1;
		}
		if (!skable && i >= 0)
		{
			return serialnumber(-1);
		}
		if (i < 0)
		{
			return current_serialno;
		}
		return serialnos[i];
	}

	public long raw_total(int i)
	{
		if (!skable || i >= links)
		{
			return -1L;
		}
		if (i < 0)
		{
			long num = 0L;
			for (int j = 0; j < links; j++)
			{
				num += raw_total(j);
			}
			return num;
		}
		return offsets[i + 1] - offsets[i];
	}

	public long pcm_total(int i)
	{
		if (!skable || i >= links)
		{
			return -1L;
		}
		if (i < 0)
		{
			long num = 0L;
			for (int j = 0; j < links; j++)
			{
				num += pcm_total(j);
			}
			return num;
		}
		return pcmlengths[i];
	}

	public float time_total(int i)
	{
		if (!skable || i >= links)
		{
			return -1f;
		}
		if (i < 0)
		{
			float num = 0f;
			for (int j = 0; j < links; j++)
			{
				num += time_total(j);
			}
			return num;
		}
		return (float)pcmlengths[i] / (float)vi[i].rate;
	}

	public int raw_seek(int pos)
	{
		if (!skable)
		{
			return -1;
		}
		if (pos < 0 || pos > offsets[links])
		{
			pcm_offset = -1L;
			decode_clear();
			return -1;
		}
		pcm_offset = -1L;
		decode_clear();
		seek_helper(pos);
		switch (process_packet(1))
		{
		case 0:
			pcm_offset = pcm_total(-1);
			return 0;
		case -1:
			pcm_offset = -1L;
			decode_clear();
			return -1;
		}
		while (true)
		{
			switch (process_packet(0))
			{
			case 0:
				return 0;
			case -1:
				pcm_offset = -1L;
				decode_clear();
				return -1;
			}
		}
	}

	public int pcm_seek(long pos)
	{
		int num = -1;
		long num2 = pcm_total(-1);
		if (!skable)
		{
			return -1;
		}
		if (pos < 0 || pos > num2)
		{
			pcm_offset = -1L;
			decode_clear();
			return -1;
		}
		for (num = links - 1; num >= 0; num--)
		{
			num2 -= pcmlengths[num];
			if (pos >= num2)
			{
				break;
			}
		}
		long num3 = pos - num2;
		long num4 = offsets[num + 1];
		long num5 = offsets[num];
		int pos2 = (int)num5;
		Page page = new Page();
		while (num5 < num4)
		{
			long num6 = ((num4 - num5 >= CHUNKSIZE) ? ((num4 + num5) / 2) : num5);
			seek_helper(num6);
			int num7 = get_next_page(page, num4 - num6);
			if (num7 == -1)
			{
				num4 = num6;
			}
			else if (page.granulepos() < num3)
			{
				pos2 = num7;
				num5 = offset;
			}
			else
			{
				num4 = num6;
			}
		}
		if (raw_seek(pos2) != 0)
		{
			pcm_offset = -1L;
			decode_clear();
			return -1;
		}
		if (pcm_offset >= pos)
		{
			pcm_offset = -1L;
			decode_clear();
			return -1;
		}
		if (pos > pcm_total(-1))
		{
			pcm_offset = -1L;
			decode_clear();
			return -1;
		}
		while (pcm_offset < pos)
		{
			int num8 = (int)(pos - pcm_offset);
			float[][][] array = new float[1][][];
			int[] index = new int[getInfo(-1).channels];
			int num9 = vd.synthesis_pcmout(array, index);
			_ = array[0];
			if (num9 > num8)
			{
				num9 = num8;
			}
			vd.synthesis_read(num9);
			pcm_offset += num9;
			if (num9 < num8 && process_packet(1) == 0)
			{
				pcm_offset = pcm_total(-1);
			}
		}
		return 0;
	}

	public int time_seek(float seconds)
	{
		int num = -1;
		long num2 = pcm_total(-1);
		float num3 = time_total(-1);
		if (!skable)
		{
			return -1;
		}
		if (seconds < 0f || seconds > num3)
		{
			pcm_offset = -1L;
			decode_clear();
			return -1;
		}
		for (num = links - 1; num >= 0; num--)
		{
			num2 -= pcmlengths[num];
			num3 -= time_total(num);
			if (seconds >= num3)
			{
				break;
			}
		}
		long pos = (long)((float)num2 + (seconds - num3) * (float)vi[num].rate);
		return pcm_seek(pos);
	}

	public long raw_tell()
	{
		return offset;
	}

	public long pcm_tell()
	{
		return pcm_offset;
	}

	public float time_tell()
	{
		int num = -1;
		long num2 = 0L;
		float num3 = 0f;
		if (skable)
		{
			num2 = pcm_total(-1);
			num3 = time_total(-1);
			for (num = links - 1; num >= 0; num--)
			{
				num2 -= pcmlengths[num];
				num3 -= time_total(num);
				if (pcm_offset >= num2)
				{
					break;
				}
			}
		}
		return num3 + (float)(pcm_offset - num2) / (float)vi[num].rate;
	}

	public Info getInfo(int link)
	{
		if (skable)
		{
			if (link < 0)
			{
				if (decode_ready)
				{
					return vi[current_link];
				}
				return null;
			}
			if (link >= links)
			{
				return null;
			}
			return vi[link];
		}
		if (decode_ready)
		{
			return vi[0];
		}
		return null;
	}

	public Comment getComment(int link)
	{
		if (skable)
		{
			if (link < 0)
			{
				if (decode_ready)
				{
					return vc[current_link];
				}
				return null;
			}
			if (link >= links)
			{
				return null;
			}
			return vc[link];
		}
		if (decode_ready)
		{
			return vc[0];
		}
		return null;
	}

	private int host_is_big_endian()
	{
		return 0;
	}

	public int read(byte[] buffer, int length, int bigendianp, int word, int sgned, int[] bitstream)
	{
		int num = host_is_big_endian();
		int num2 = 0;
		int[] array2;
		int num3;
		float[][] array3;
		while (true)
		{
			if (decode_ready)
			{
				float[][][] array = new float[1][][];
				array2 = new int[getInfo(-1).channels];
				num3 = vd.synthesis_pcmout(array, array2);
				array3 = array[0];
				if (num3 != 0)
				{
					break;
				}
			}
			switch (process_packet(1))
			{
			case 0:
				return 0;
			case -1:
				return -1;
			}
		}
		int channels = getInfo(-1).channels;
		int num4 = word * channels;
		if (num3 > length / num4)
		{
			num3 = length / num4;
		}
		if (word == 1)
		{
			int num5 = ((sgned == 0) ? 128 : 0);
			for (int i = 0; i < num3; i++)
			{
				for (int j = 0; j < channels; j++)
				{
					int num6 = (int)((double)array3[j][array2[j] + i] * 128.0 + 0.5);
					if (num6 > 127)
					{
						num6 = 127;
					}
					else if (num6 < -128)
					{
						num6 = -128;
					}
					buffer[num2++] = (byte)(num6 + num5);
				}
			}
		}
		else
		{
			int num7 = ((sgned == 0) ? 32768 : 0);
			if (num == bigendianp)
			{
				if (sgned != 0)
				{
					for (int k = 0; k < channels; k++)
					{
						int num8 = array2[k];
						int num9 = k * 2;
						for (int l = 0; l < num3; l++)
						{
							int num6 = (int)((double)array3[k][num8 + l] * 32767.0);
							if (num6 > 32767)
							{
								num6 = 32767;
							}
							else if (num6 < -32768)
							{
								num6 = -32768;
							}
							buffer[num9] = (byte)num6;
							buffer[num9 + 1] = (byte)((uint)num6 >> 8);
							num9 += num4;
						}
					}
				}
				else
				{
					for (int m = 0; m < channels; m++)
					{
						float[] array4 = array3[m];
						int num10 = m;
						for (int n = 0; n < num3; n++)
						{
							int num6 = (int)((double)array4[n] * 32768.0 + 0.5);
							if (num6 > 32767)
							{
								num6 = 32767;
							}
							else if (num6 < -32768)
							{
								num6 = -32768;
							}
							buffer[num10] = (byte)((uint)(num6 + num7) >> 8);
							buffer[num10 + 1] = (byte)(num6 + num7);
							num10 += channels * 2;
						}
					}
				}
			}
			else if (bigendianp != 0)
			{
				for (int num11 = 0; num11 < num3; num11++)
				{
					for (int num12 = 0; num12 < channels; num12++)
					{
						int num6 = (int)((double)array3[num12][num11] * 32768.0 + 0.5);
						if (num6 > 32767)
						{
							num6 = 32767;
						}
						else if (num6 < -32768)
						{
							num6 = -32768;
						}
						num6 += num7;
						buffer[num2++] = (byte)((uint)num6 >> 8);
						buffer[num2++] = (byte)num6;
					}
				}
			}
			else
			{
				for (int num13 = 0; num13 < num3; num13++)
				{
					for (int num14 = 0; num14 < channels; num14++)
					{
						int num6 = (int)((double)array3[num14][num13] * 32768.0 + 0.5);
						if (num6 > 32767)
						{
							num6 = 32767;
						}
						else if (num6 < -32768)
						{
							num6 = -32768;
						}
						num6 += num7;
						buffer[num2++] = (byte)num6;
						buffer[num2++] = (byte)((uint)num6 >> 8);
					}
				}
			}
		}
		vd.synthesis_read(num3);
		pcm_offset += num3;
		if (bitstream != null)
		{
			bitstream[0] = current_link;
		}
		return num3 * num4;
	}

	public Info[] getInfo()
	{
		return vi;
	}

	public Comment[] getComment()
	{
		return vc;
	}
}
