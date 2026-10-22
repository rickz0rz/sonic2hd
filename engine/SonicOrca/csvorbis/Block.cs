using csogg;

namespace csvorbis;

public class Block
{
	internal float[][] pcm = new float[0][];

	internal csBuffer opb = new csBuffer();

	internal int lW;

	internal int W;

	internal int nW;

	internal int pcmend;

	internal int mode;

	internal int eofflag;

	internal long granulepos;

	internal long sequence;

	internal DspState vd;

	internal int glue_bits;

	internal int time_bits;

	internal int floor_bits;

	internal int res_bits;

	public Block(DspState vd)
	{
		this.vd = vd;
		if (vd.analysisp != 0)
		{
			opb.writeinit();
		}
	}

	public void init(DspState vd)
	{
		this.vd = vd;
	}

	public int clear()
	{
		if (vd != null && vd.analysisp != 0)
		{
			opb.writeclear();
		}
		return 0;
	}

	public int synthesis(Packet op)
	{
		Info vi = vd.vi;
		opb.readinit(op.packet_base, op.packet, op.bytes);
		if (opb.read(1) != 0)
		{
			return -1;
		}
		int num = opb.read(vd.modebits);
		if (num == -1)
		{
			return -1;
		}
		mode = num;
		W = vi.mode_param[mode].blockflag;
		if (W != 0)
		{
			lW = opb.read(1);
			nW = opb.read(1);
			if (nW == -1)
			{
				return -1;
			}
		}
		else
		{
			lW = 0;
			nW = 0;
		}
		granulepos = op.granulepos;
		sequence = op.packetno - 3;
		eofflag = op.e_o_s;
		pcmend = vi.blocksizes[W];
		if (pcm.Length < vi.channels)
		{
			pcm = new float[vi.channels][];
		}
		for (int i = 0; i < vi.channels; i++)
		{
			if (pcm[i] == null || pcm[i].Length < pcmend)
			{
				pcm[i] = new float[pcmend];
				continue;
			}
			for (int j = 0; j < pcmend; j++)
			{
				pcm[i][j] = 0f;
			}
		}
		int num2 = vi.map_type[vi.mode_param[mode].mapping];
		return FuncMapping.mapping_P[num2].inverse(this, vd.mode[mode]);
	}
}
