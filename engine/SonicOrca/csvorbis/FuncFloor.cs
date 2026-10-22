using csogg;

namespace csvorbis;

internal abstract class FuncFloor
{
	public static FuncFloor[] floor_P = new FuncFloor[2]
	{
		new Floor0(),
		new Floor1()
	};

	public abstract void pack(object i, csBuffer opb);

	public abstract object unpack(Info vi, csBuffer opb);

	public abstract object look(DspState vd, InfoMode mi, object i);

	public abstract void free_info(object i);

	public abstract void free_look(object i);

	public abstract void free_state(object vs);

	public abstract int forward(Block vb, object i, float[] fin, float[] fout, object vs);

	public abstract object inverse1(Block vb, object i, object memo);

	public abstract int inverse2(Block vb, object i, object memo, float[] fout);
}
