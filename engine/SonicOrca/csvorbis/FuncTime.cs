using csogg;

namespace csvorbis;

internal abstract class FuncTime
{
	public static FuncTime[] time_P = new FuncTime[1]
	{
		new Time0()
	};

	public abstract void pack(object i, csBuffer opb);

	public abstract object unpack(Info vi, csBuffer opb);

	public abstract object look(DspState vd, InfoMode vm, object i);

	public abstract void free_info(object i);

	public abstract void free_look(object i);

	public abstract int forward(Block vb, object i);

	public abstract int inverse(Block vb, object i, float[] fin, float[] fout);
}
