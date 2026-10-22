using System.Text;
using csogg;

namespace csvorbis;

public class Info
{
	private static int OV_EBADPACKET = -136;

	private static int OV_ENOTAUDIO = -135;

	private static string _vorbis = "vorbis";

	private static int VI_TIMEB = 1;

	private static int VI_FLOORB = 2;

	private static int VI_RESB = 3;

	private static int VI_MAPB = 1;

	private static int VI_WINDOWB = 1;

	public int version;

	public int channels;

	public int rate;

	internal int bitrate_upper;

	internal int bitrate_nominal;

	internal int bitrate_lower;

	internal int[] blocksizes = new int[2];

	internal int modes;

	internal int maps;

	internal int times;

	internal int floors;

	internal int residues;

	internal int books;

	internal int psys;

	internal InfoMode[] mode_param;

	internal int[] map_type;

	internal object[] map_param;

	internal int[] time_type;

	internal object[] time_param;

	internal int[] floor_type;

	internal object[] floor_param;

	internal int[] residue_type;

	internal object[] residue_param;

	internal StaticCodeBook[] book_param;

	internal PsyInfo[] psy_param = new PsyInfo[64];

	public void init()
	{
		rate = 0;
	}

	public void clear()
	{
		for (int i = 0; i < modes; i++)
		{
			mode_param[i] = null;
		}
		mode_param = null;
		for (int j = 0; j < maps; j++)
		{
			FuncMapping.mapping_P[map_type[j]].free_info(map_param[j]);
		}
		map_param = null;
		for (int k = 0; k < times; k++)
		{
			FuncTime.time_P[time_type[k]].free_info(time_param[k]);
		}
		time_param = null;
		for (int l = 0; l < floors; l++)
		{
			FuncFloor.floor_P[floor_type[l]].free_info(floor_param[l]);
		}
		floor_param = null;
		for (int m = 0; m < residues; m++)
		{
			FuncResidue.residue_P[residue_type[m]].free_info(residue_param[m]);
		}
		residue_param = null;
		for (int n = 0; n < books; n++)
		{
			if (book_param[n] != null)
			{
				book_param[n].clear();
				book_param[n] = null;
			}
		}
		book_param = null;
		for (int num = 0; num < psys; num++)
		{
			psy_param[num].free();
		}
	}

	private int unpack_info(csBuffer opb)
	{
		version = opb.read(32);
		if (version != 0)
		{
			return -1;
		}
		channels = opb.read(8);
		rate = opb.read(32);
		bitrate_upper = opb.read(32);
		bitrate_nominal = opb.read(32);
		bitrate_lower = opb.read(32);
		blocksizes[0] = 1 << opb.read(4);
		blocksizes[1] = 1 << opb.read(4);
		if (rate < 1 || channels < 1 || blocksizes[0] < 8 || blocksizes[1] < blocksizes[0] || opb.read(1) != 1)
		{
			clear();
			return -1;
		}
		return 0;
	}

	private int unpack_books(csBuffer opb)
	{
		books = opb.read(8) + 1;
		if (book_param == null || book_param.Length != books)
		{
			book_param = new StaticCodeBook[books];
		}
		for (int i = 0; i < books; i++)
		{
			book_param[i] = new StaticCodeBook();
			if (book_param[i].unpack(opb) != 0)
			{
				clear();
				return -1;
			}
		}
		times = opb.read(6) + 1;
		if (time_type == null || time_type.Length != times)
		{
			time_type = new int[times];
		}
		if (time_param == null || time_param.Length != times)
		{
			time_param = new object[times];
		}
		for (int j = 0; j < times; j++)
		{
			time_type[j] = opb.read(16);
			if (time_type[j] < 0 || time_type[j] >= VI_TIMEB)
			{
				clear();
				return -1;
			}
			time_param[j] = FuncTime.time_P[time_type[j]].unpack(this, opb);
			if (time_param[j] == null)
			{
				clear();
				return -1;
			}
		}
		floors = opb.read(6) + 1;
		if (floor_type == null || floor_type.Length != floors)
		{
			floor_type = new int[floors];
		}
		if (floor_param == null || floor_param.Length != floors)
		{
			floor_param = new object[floors];
		}
		for (int k = 0; k < floors; k++)
		{
			floor_type[k] = opb.read(16);
			if (floor_type[k] < 0 || floor_type[k] >= VI_FLOORB)
			{
				clear();
				return -1;
			}
			floor_param[k] = FuncFloor.floor_P[floor_type[k]].unpack(this, opb);
			if (floor_param[k] == null)
			{
				clear();
				return -1;
			}
		}
		residues = opb.read(6) + 1;
		if (residue_type == null || residue_type.Length != residues)
		{
			residue_type = new int[residues];
		}
		if (residue_param == null || residue_param.Length != residues)
		{
			residue_param = new object[residues];
		}
		for (int l = 0; l < residues; l++)
		{
			residue_type[l] = opb.read(16);
			if (residue_type[l] < 0 || residue_type[l] >= VI_RESB)
			{
				clear();
				return -1;
			}
			residue_param[l] = FuncResidue.residue_P[residue_type[l]].unpack(this, opb);
			if (residue_param[l] == null)
			{
				clear();
				return -1;
			}
		}
		maps = opb.read(6) + 1;
		if (map_type == null || map_type.Length != maps)
		{
			map_type = new int[maps];
		}
		if (map_param == null || map_param.Length != maps)
		{
			map_param = new object[maps];
		}
		for (int m = 0; m < maps; m++)
		{
			map_type[m] = opb.read(16);
			if (map_type[m] < 0 || map_type[m] >= VI_MAPB)
			{
				clear();
				return -1;
			}
			map_param[m] = FuncMapping.mapping_P[map_type[m]].unpack(this, opb);
			if (map_param[m] == null)
			{
				clear();
				return -1;
			}
		}
		modes = opb.read(6) + 1;
		if (mode_param == null || mode_param.Length != modes)
		{
			mode_param = new InfoMode[modes];
		}
		for (int n = 0; n < modes; n++)
		{
			mode_param[n] = new InfoMode();
			mode_param[n].blockflag = opb.read(1);
			mode_param[n].windowtype = opb.read(16);
			mode_param[n].transformtype = opb.read(16);
			mode_param[n].mapping = opb.read(8);
			if (mode_param[n].windowtype >= VI_WINDOWB || mode_param[n].transformtype >= VI_WINDOWB || mode_param[n].mapping >= maps)
			{
				clear();
				return -1;
			}
		}
		if (opb.read(1) != 1)
		{
			clear();
			return -1;
		}
		return 0;
	}

	public int synthesis_headerin(Comment vc, Packet op)
	{
		csBuffer csBuffer2 = new csBuffer();
		if (op != null)
		{
			csBuffer2.readinit(op.packet_base, op.packet, op.bytes);
			byte[] array = new byte[6];
			int num = csBuffer2.read(8);
			csBuffer2.read(array, 6);
			if (array[0] != 118 || array[1] != 111 || array[2] != 114 || array[3] != 98 || array[4] != 105 || array[5] != 115)
			{
				return -1;
			}
			switch (num)
			{
			case 1:
				if (op.b_o_s == 0)
				{
					return -1;
				}
				if (rate != 0)
				{
					return -1;
				}
				return unpack_info(csBuffer2);
			case 3:
				if (rate == 0)
				{
					return -1;
				}
				return vc.unpack(csBuffer2);
			case 5:
				if (rate == 0 || vc.vendor == null)
				{
					return -1;
				}
				return unpack_books(csBuffer2);
			}
		}
		return -1;
	}

	private int pack_info(csBuffer opb)
	{
		byte[] bytes = Encoding.UTF8.GetBytes(_vorbis);
		opb.write(1, 8);
		opb.write(bytes);
		opb.write(0, 32);
		opb.write(channels, 8);
		opb.write(rate, 32);
		opb.write(bitrate_upper, 32);
		opb.write(bitrate_nominal, 32);
		opb.write(bitrate_lower, 32);
		opb.write(ilog2(blocksizes[0]), 4);
		opb.write(ilog2(blocksizes[1]), 4);
		opb.write(1, 1);
		return 0;
	}

	private int pack_books(csBuffer opb)
	{
		byte[] bytes = Encoding.UTF8.GetBytes(_vorbis);
		opb.write(5, 8);
		opb.write(bytes);
		opb.write(books - 1, 8);
		for (int i = 0; i < books; i++)
		{
			if (book_param[i].pack(opb) != 0)
			{
				return -1;
			}
		}
		opb.write(times - 1, 6);
		for (int j = 0; j < times; j++)
		{
			opb.write(time_type[j], 16);
			FuncTime.time_P[time_type[j]].pack(time_param[j], opb);
		}
		opb.write(floors - 1, 6);
		for (int k = 0; k < floors; k++)
		{
			opb.write(floor_type[k], 16);
			FuncFloor.floor_P[floor_type[k]].pack(floor_param[k], opb);
		}
		opb.write(residues - 1, 6);
		for (int l = 0; l < residues; l++)
		{
			opb.write(residue_type[l], 16);
			FuncResidue.residue_P[residue_type[l]].pack(residue_param[l], opb);
		}
		opb.write(maps - 1, 6);
		for (int m = 0; m < maps; m++)
		{
			opb.write(map_type[m], 16);
			FuncMapping.mapping_P[map_type[m]].pack(this, map_param[m], opb);
		}
		opb.write(modes - 1, 6);
		for (int n = 0; n < modes; n++)
		{
			opb.write(mode_param[n].blockflag, 1);
			opb.write(mode_param[n].windowtype, 16);
			opb.write(mode_param[n].transformtype, 16);
			opb.write(mode_param[n].mapping, 8);
		}
		opb.write(1, 1);
		return 0;
	}

	public int blocksize(Packet op)
	{
		csBuffer csBuffer2 = new csBuffer();
		csBuffer2.readinit(op.packet_base, op.packet, op.bytes);
		if (csBuffer2.read(1) != 0)
		{
			return OV_ENOTAUDIO;
		}
		int num = 0;
		for (int num2 = modes; num2 > 1; num2 >>>= 1)
		{
			num++;
		}
		int num3 = csBuffer2.read(num);
		if (num3 == -1)
		{
			return OV_EBADPACKET;
		}
		return blocksizes[mode_param[num3].blockflag];
	}

	private static int ilog2(int v)
	{
		int num = 0;
		while (v > 1)
		{
			num++;
			v >>>= 1;
		}
		return num;
	}

	public string toString()
	{
		return "version:" + version + ", channels:" + channels + ", rate:" + rate + ", bitrate:" + bitrate_upper + "," + bitrate_nominal + "," + bitrate_lower;
	}
}
