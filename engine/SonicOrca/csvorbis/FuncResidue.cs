using csogg;

namespace csvorbis;

internal abstract class FuncResidue
{
	public static FuncResidue[] residue_P = new FuncResidue[3]
	{
		new Residue0(),
		new Residue1(),
		new Residue2()
	};

	public abstract void pack(object vr, csBuffer opb);

	public abstract object unpack(Info vi, csBuffer opb);

	public abstract object look(DspState vd, InfoMode vm, object vr);

	public abstract void free_info(object i);

	public abstract void free_look(object i);

	public abstract int forward(Block vb, object vl, float[][] fin, int ch);

	public abstract int inverse(Block vb, object vl, float[][] fin, int[] nonzero, int ch);
}
