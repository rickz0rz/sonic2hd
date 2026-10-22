namespace Hjg.Pngcs.Chunks;

public class PngChunkIDAT : PngChunkMultiple
{
	public const string ID = "IDAT";

	public PngChunkIDAT(ImageInfo i, int len, long offset)
		: base("IDAT", i)
	{
		Length = len;
		Offset = offset;
	}

	public override ChunkOrderingConstraint GetOrderingConstraint()
	{
		return ChunkOrderingConstraint.NA;
	}

	public override ChunkRaw CreateRawChunk()
	{
		return null;
	}

	public override void ParseFromRaw(ChunkRaw c)
	{
	}

	public override void CloneDataFromRead(PngChunk other)
	{
	}
}
