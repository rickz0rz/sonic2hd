using csogg;

namespace csvorbis;

internal class Time0 : FuncTime
{
	public override void pack(object i, csBuffer opb)
	{
	}

	public override object unpack(Info vi, csBuffer opb)
	{
		return "";
	}

	public override object look(DspState vd, InfoMode mi, object i)
	{
		return "";
	}

	public override void free_info(object i)
	{
	}

	public override void free_look(object i)
	{
	}

	public override int forward(Block vb, object i)
	{
		return 0;
	}

	public override int inverse(Block vb, object i, float[] fin, float[] fout)
	{
		return 0;
	}
}
