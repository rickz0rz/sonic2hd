using csogg;

namespace csvorbis;

internal class Mapping0 : FuncMapping
{
	private float[][] pcmbundle;

	private int[] zerobundle;

	private int[] nonzero;

	private object[] floormemo;

	public override void free_info(object imap)
	{
	}

	public override void free_look(object imap)
	{
	}

	public override object look(DspState vd, InfoMode vm, object m)
	{
		Info vi = vd.vi;
		LookMapping0 lookMapping = new LookMapping0();
		InfoMapping0 infoMapping = (lookMapping.map = (InfoMapping0)m);
		lookMapping.mode = vm;
		lookMapping.time_look = new object[infoMapping.submaps];
		lookMapping.floor_look = new object[infoMapping.submaps];
		lookMapping.residue_look = new object[infoMapping.submaps];
		lookMapping.time_func = new FuncTime[infoMapping.submaps];
		lookMapping.floor_func = new FuncFloor[infoMapping.submaps];
		lookMapping.residue_func = new FuncResidue[infoMapping.submaps];
		for (int i = 0; i < infoMapping.submaps; i++)
		{
			int num = infoMapping.timesubmap[i];
			int num2 = infoMapping.floorsubmap[i];
			int num3 = infoMapping.residuesubmap[i];
			lookMapping.time_func[i] = FuncTime.time_P[vi.time_type[num]];
			lookMapping.time_look[i] = lookMapping.time_func[i].look(vd, vm, vi.time_param[num]);
			lookMapping.floor_func[i] = FuncFloor.floor_P[vi.floor_type[num2]];
			lookMapping.floor_look[i] = lookMapping.floor_func[i].look(vd, vm, vi.floor_param[num2]);
			lookMapping.residue_func[i] = FuncResidue.residue_P[vi.residue_type[num3]];
			lookMapping.residue_look[i] = lookMapping.residue_func[i].look(vd, vm, vi.residue_param[num3]);
		}
		if (vi.psys != 0)
		{
			_ = vd.analysisp;
		}
		lookMapping.ch = vi.channels;
		return lookMapping;
	}

	public override void pack(Info vi, object imap, csBuffer opb)
	{
		InfoMapping0 infoMapping = (InfoMapping0)imap;
		if (infoMapping.submaps > 1)
		{
			opb.write(1, 1);
			opb.write(infoMapping.submaps - 1, 4);
		}
		else
		{
			opb.write(0, 1);
		}
		if (infoMapping.coupling_steps > 0)
		{
			opb.write(1, 1);
			opb.write(infoMapping.coupling_steps - 1, 8);
			for (int i = 0; i < infoMapping.coupling_steps; i++)
			{
				opb.write(infoMapping.coupling_mag[i], ilog2(vi.channels));
				opb.write(infoMapping.coupling_ang[i], ilog2(vi.channels));
			}
		}
		else
		{
			opb.write(0, 1);
		}
		opb.write(0, 2);
		if (infoMapping.submaps > 1)
		{
			for (int j = 0; j < vi.channels; j++)
			{
				opb.write(infoMapping.chmuxlist[j], 4);
			}
		}
		for (int k = 0; k < infoMapping.submaps; k++)
		{
			opb.write(infoMapping.timesubmap[k], 8);
			opb.write(infoMapping.floorsubmap[k], 8);
			opb.write(infoMapping.residuesubmap[k], 8);
		}
	}

	public override object unpack(Info vi, csBuffer opb)
	{
		InfoMapping0 infoMapping = new InfoMapping0();
		if (opb.read(1) != 0)
		{
			infoMapping.submaps = opb.read(4) + 1;
		}
		else
		{
			infoMapping.submaps = 1;
		}
		if (opb.read(1) != 0)
		{
			infoMapping.coupling_steps = opb.read(8) + 1;
			for (int i = 0; i < infoMapping.coupling_steps; i++)
			{
				int num = (infoMapping.coupling_mag[i] = opb.read(ilog2(vi.channels)));
				int num2 = (infoMapping.coupling_ang[i] = opb.read(ilog2(vi.channels)));
				if (num < 0 || num2 < 0 || num == num2 || num >= vi.channels || num2 >= vi.channels)
				{
					infoMapping.free();
					return null;
				}
			}
		}
		if (opb.read(2) > 0)
		{
			infoMapping.free();
			return null;
		}
		if (infoMapping.submaps > 1)
		{
			for (int j = 0; j < vi.channels; j++)
			{
				infoMapping.chmuxlist[j] = opb.read(4);
				if (infoMapping.chmuxlist[j] >= infoMapping.submaps)
				{
					infoMapping.free();
					return null;
				}
			}
		}
		for (int k = 0; k < infoMapping.submaps; k++)
		{
			infoMapping.timesubmap[k] = opb.read(8);
			if (infoMapping.timesubmap[k] >= vi.times)
			{
				infoMapping.free();
				return null;
			}
			infoMapping.floorsubmap[k] = opb.read(8);
			if (infoMapping.floorsubmap[k] >= vi.floors)
			{
				infoMapping.free();
				return null;
			}
			infoMapping.residuesubmap[k] = opb.read(8);
			if (infoMapping.residuesubmap[k] >= vi.residues)
			{
				infoMapping.free();
				return null;
			}
		}
		return infoMapping;
	}

	public override int inverse(Block vb, object l)
	{
		lock (this)
		{
			DspState vd = vb.vd;
			Info vi = vd.vi;
			LookMapping0 lookMapping = (LookMapping0)l;
			InfoMapping0 map = lookMapping.map;
			InfoMode mode = lookMapping.mode;
			int num = (vb.pcmend = vi.blocksizes[vb.W]);
			float[] array = vd.wnd[vb.W][vb.lW][vb.nW][mode.windowtype];
			if (pcmbundle == null || pcmbundle.Length < vi.channels)
			{
				pcmbundle = new float[vi.channels][];
				nonzero = new int[vi.channels];
				zerobundle = new int[vi.channels];
				floormemo = new object[vi.channels];
			}
			for (int i = 0; i < vi.channels; i++)
			{
				float[] array2 = vb.pcm[i];
				int num2 = map.chmuxlist[i];
				floormemo[i] = lookMapping.floor_func[num2].inverse1(vb, lookMapping.floor_look[num2], floormemo[i]);
				if (floormemo[i] != null)
				{
					nonzero[i] = 1;
				}
				else
				{
					nonzero[i] = 0;
				}
				for (int j = 0; j < num / 2; j++)
				{
					array2[j] = 0f;
				}
			}
			for (int k = 0; k < map.coupling_steps; k++)
			{
				if (nonzero[map.coupling_mag[k]] != 0 || nonzero[map.coupling_ang[k]] != 0)
				{
					nonzero[map.coupling_mag[k]] = 1;
					nonzero[map.coupling_ang[k]] = 1;
				}
			}
			for (int m = 0; m < map.submaps; m++)
			{
				int num3 = 0;
				for (int n = 0; n < vi.channels; n++)
				{
					if (map.chmuxlist[n] == m)
					{
						if (nonzero[n] != 0)
						{
							zerobundle[num3] = 1;
						}
						else
						{
							zerobundle[num3] = 0;
						}
						pcmbundle[num3++] = vb.pcm[n];
					}
				}
				lookMapping.residue_func[m].inverse(vb, lookMapping.residue_look[m], pcmbundle, zerobundle, num3);
			}
			for (int num4 = map.coupling_steps - 1; num4 >= 0; num4--)
			{
				float[] array3 = vb.pcm[map.coupling_mag[num4]];
				float[] array4 = vb.pcm[map.coupling_ang[num4]];
				for (int num5 = 0; num5 < num / 2; num5++)
				{
					float num6 = array3[num5];
					float num7 = array4[num5];
					if (num6 > 0f)
					{
						if (num7 > 0f)
						{
							array3[num5] = num6;
							array4[num5] = num6 - num7;
						}
						else
						{
							array4[num5] = num6;
							array3[num5] = num6 + num7;
						}
					}
					else if (num7 > 0f)
					{
						array3[num5] = num6;
						array4[num5] = num6 + num7;
					}
					else
					{
						array4[num5] = num6;
						array3[num5] = num6 - num7;
					}
				}
			}
			for (int num8 = 0; num8 < vi.channels; num8++)
			{
				float[] fout = vb.pcm[num8];
				int num9 = map.chmuxlist[num8];
				lookMapping.floor_func[num9].inverse2(vb, lookMapping.floor_look[num9], floormemo[num8], fout);
			}
			for (int num10 = 0; num10 < vi.channels; num10++)
			{
				float[] array5 = vb.pcm[num10];
				((Mdct)vd.transform[vb.W][0]).backward(array5, array5);
			}
			for (int num11 = 0; num11 < vi.channels; num11++)
			{
				float[] array6 = vb.pcm[num11];
				if (nonzero[num11] != 0)
				{
					for (int num12 = 0; num12 < num; num12++)
					{
						array6[num12] *= array[num12];
					}
				}
				else
				{
					for (int num13 = 0; num13 < num; num13++)
					{
						array6[num13] = 0f;
					}
				}
			}
			return 0;
		}
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
}
