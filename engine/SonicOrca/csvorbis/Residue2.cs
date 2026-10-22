namespace csvorbis;

internal class Residue2 : Residue0
{
	public override int forward(Block vb, object vl, float[][] fin, int ch)
	{
		return 0;
	}

	public override int inverse(Block vb, object vl, float[][] fin, int[] nonzero, int ch)
	{
		int num = 0;
		for (num = 0; num < ch && nonzero[num] == 0; num++)
		{
		}
		if (num == ch)
		{
			return 0;
		}
		return Residue0._2inverse(vb, vl, fin, ch);
	}
}
