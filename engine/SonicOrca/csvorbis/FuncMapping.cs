using csogg;

namespace csvorbis;

internal abstract class FuncMapping
{
	public static FuncMapping[] mapping_P = new FuncMapping[1]
	{
		new Mapping0()
	};

	public abstract void pack(Info info, object imap, csBuffer buffer);

	public abstract object unpack(Info info, csBuffer buffer);

	public abstract object look(DspState vd, InfoMode vm, object m);

	public abstract void free_info(object imap);

	public abstract void free_look(object imap);

	public abstract int inverse(Block vd, object lm);
}
