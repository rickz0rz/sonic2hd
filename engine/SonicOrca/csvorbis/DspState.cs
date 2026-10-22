using System;

namespace csvorbis;

public class DspState
{
	private static float M_PI = (float)Math.PI;

	private static int VI_TRANSFORMB = 1;

	private static int VI_WINDOWB = 1;

	internal int analysisp;

	internal Info vi;

	internal int modebits;

	private float[][] pcm;

	private int pcm_storage;

	private int pcm_current;

	private int pcm_returned;

	private float[] multipliers;

	private int envelope_storage;

	private int envelope_current;

	private int eofflag;

	private int lW;

	private int W;

	private int nW;

	private int centerW;

	private long granulepos;

	public long sequence;

	private long glue_bits;

	private long time_bits;

	private long floor_bits;

	private long res_bits;

	internal float[][][][][] wnd;

	internal object[][] transform;

	internal CodeBook[] fullbooks;

	internal object[] mode;

	private byte[] header;

	private byte[] header1;

	private byte[] header2;

	public DspState()
	{
		transform = new object[2][];
		wnd = new float[2][][][][];
		wnd[0] = new float[2][][][];
		wnd[0][0] = new float[2][][];
		wnd[0][1] = new float[2][][];
		wnd[0][0][0] = new float[2][];
		wnd[0][0][1] = new float[2][];
		wnd[0][1][0] = new float[2][];
		wnd[0][1][1] = new float[2][];
		wnd[1] = new float[2][][][];
		wnd[1][0] = new float[2][][];
		wnd[1][1] = new float[2][][];
		wnd[1][0][0] = new float[2][];
		wnd[1][0][1] = new float[2][];
		wnd[1][1][0] = new float[2][];
		wnd[1][1][1] = new float[2][];
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

	internal static float[] window(int type, int wnd, int left, int right)
	{
		float[] array = new float[wnd];
		if (type == 0)
		{
			int num = wnd / 4 - left / 2;
			int num2 = wnd - wnd / 4 - right / 2;
			for (int i = 0; i < left; i++)
			{
				float num3 = (float)(((double)i + 0.5) / (double)left * (double)M_PI / 2.0);
				num3 = (float)Math.Sin(num3);
				num3 *= num3;
				num3 *= (float)((double)M_PI / 2.0);
				num3 = (float)Math.Sin(num3);
				array[i + num] = num3;
			}
			for (int j = num + left; j < num2; j++)
			{
				array[j] = 1f;
			}
			for (int k = 0; k < right; k++)
			{
				float num4 = (float)(((double)(right - k) - 0.5) / (double)right * (double)M_PI / 2.0);
				num4 = (float)Math.Sin(num4);
				num4 *= num4;
				num4 *= (float)((double)M_PI / 2.0);
				num4 = (float)Math.Sin(num4);
				array[k + num2] = num4;
			}
			return array;
		}
		return null;
	}

	private int init(Info vi, bool encp)
	{
		this.vi = vi;
		modebits = ilog2(vi.modes);
		transform[0] = new object[VI_TRANSFORMB];
		transform[1] = new object[VI_TRANSFORMB];
		transform[0][0] = new Mdct();
		transform[1][0] = new Mdct();
		((Mdct)transform[0][0]).init(vi.blocksizes[0]);
		((Mdct)transform[1][0]).init(vi.blocksizes[1]);
		wnd[0][0][0] = new float[VI_WINDOWB][];
		wnd[0][0][1] = wnd[0][0][0];
		wnd[0][1][0] = wnd[0][0][0];
		wnd[0][1][1] = wnd[0][0][0];
		wnd[1][0][0] = new float[VI_WINDOWB][];
		wnd[1][0][1] = new float[VI_WINDOWB][];
		wnd[1][1][0] = new float[VI_WINDOWB][];
		wnd[1][1][1] = new float[VI_WINDOWB][];
		for (int i = 0; i < VI_WINDOWB; i++)
		{
			wnd[0][0][0][i] = window(i, vi.blocksizes[0], vi.blocksizes[0] / 2, vi.blocksizes[0] / 2);
			wnd[1][0][0][i] = window(i, vi.blocksizes[1], vi.blocksizes[0] / 2, vi.blocksizes[0] / 2);
			wnd[1][0][1][i] = window(i, vi.blocksizes[1], vi.blocksizes[0] / 2, vi.blocksizes[1] / 2);
			wnd[1][1][0][i] = window(i, vi.blocksizes[1], vi.blocksizes[1] / 2, vi.blocksizes[0] / 2);
			wnd[1][1][1][i] = window(i, vi.blocksizes[1], vi.blocksizes[1] / 2, vi.blocksizes[1] / 2);
		}
		fullbooks = new CodeBook[vi.books];
		for (int j = 0; j < vi.books; j++)
		{
			fullbooks[j] = new CodeBook();
			fullbooks[j].init_decode(vi.book_param[j]);
		}
		pcm_storage = 8192;
		pcm = new float[vi.channels][];
		for (int k = 0; k < vi.channels; k++)
		{
			pcm[k] = new float[pcm_storage];
		}
		lW = 0;
		W = 0;
		centerW = vi.blocksizes[1] / 2;
		pcm_current = centerW;
		mode = new object[vi.modes];
		for (int l = 0; l < vi.modes; l++)
		{
			int mapping = vi.mode_param[l].mapping;
			int num = vi.map_type[mapping];
			mode[l] = FuncMapping.mapping_P[num].look(this, vi.mode_param[l], vi.map_param[mapping]);
		}
		return 0;
	}

	public int synthesis_init(Info vi)
	{
		init(vi, encp: false);
		pcm_returned = centerW;
		centerW -= vi.blocksizes[W] / 4 + vi.blocksizes[lW] / 4;
		granulepos = -1L;
		sequence = -1L;
		return 0;
	}

	private DspState(Info vi)
		: this()
	{
		init(vi, encp: false);
		pcm_returned = centerW;
		centerW -= vi.blocksizes[W] / 4 + vi.blocksizes[lW] / 4;
		granulepos = -1L;
		sequence = -1L;
	}

	public int synthesis_blockin(Block vb)
	{
		if (centerW > vi.blocksizes[1] / 2 && pcm_returned > 8192)
		{
			int num = centerW - vi.blocksizes[1] / 2;
			num = ((pcm_returned < num) ? pcm_returned : num);
			pcm_current -= num;
			centerW -= num;
			pcm_returned -= num;
			if (num != 0)
			{
				for (int i = 0; i < vi.channels; i++)
				{
					Array.Copy(pcm[i], num, pcm[i], 0, pcm_current);
				}
			}
		}
		lW = W;
		W = vb.W;
		nW = -1;
		glue_bits += vb.glue_bits;
		time_bits += vb.time_bits;
		floor_bits += vb.floor_bits;
		res_bits += vb.res_bits;
		if (sequence + 1 != vb.sequence)
		{
			granulepos = -1L;
		}
		sequence = vb.sequence;
		int num2 = vi.blocksizes[W];
		int num3 = centerW + vi.blocksizes[lW] / 4 + num2 / 4;
		int num4 = num3 - num2 / 2;
		int num5 = num4 + num2;
		int num6 = 0;
		int num7 = 0;
		if (num5 > pcm_storage)
		{
			pcm_storage = num5 + vi.blocksizes[1];
			for (int j = 0; j < vi.channels; j++)
			{
				float[] array = new float[pcm_storage];
				Array.Copy(pcm[j], 0, array, 0, pcm[j].Length);
				pcm[j] = array;
			}
		}
		switch (W)
		{
		case 0:
			num6 = 0;
			num7 = vi.blocksizes[0] / 2;
			break;
		case 1:
			num6 = vi.blocksizes[1] / 4 - vi.blocksizes[lW] / 4;
			num7 = num6 + vi.blocksizes[lW] / 2;
			break;
		}
		for (int k = 0; k < vi.channels; k++)
		{
			int num8 = num4;
			int num9 = 0;
			for (num9 = num6; num9 < num7; num9++)
			{
				pcm[k][num8 + num9] += vb.pcm[k][num9];
			}
			for (; num9 < num2; num9++)
			{
				pcm[k][num8 + num9] = vb.pcm[k][num9];
			}
		}
		if (granulepos == -1)
		{
			granulepos = vb.granulepos;
		}
		else
		{
			granulepos += num3 - centerW;
			if (vb.granulepos != -1 && granulepos != vb.granulepos)
			{
				if (granulepos > vb.granulepos && vb.eofflag != 0)
				{
					num3 -= (int)(granulepos - vb.granulepos);
				}
				granulepos = vb.granulepos;
			}
		}
		centerW = num3;
		pcm_current = num5;
		if (vb.eofflag != 0)
		{
			eofflag = 1;
		}
		return 0;
	}

	public int synthesis_pcmout(float[][][] _pcm, int[] index)
	{
		if (pcm_returned < centerW)
		{
			if (_pcm != null)
			{
				for (int i = 0; i < vi.channels; i++)
				{
					index[i] = pcm_returned;
				}
				_pcm[0] = pcm;
			}
			return centerW - pcm_returned;
		}
		return 0;
	}

	public int synthesis_read(int bytes)
	{
		if (bytes != 0 && pcm_returned + bytes > centerW)
		{
			return -1;
		}
		pcm_returned += bytes;
		return 0;
	}

	public void clear()
	{
	}
}
