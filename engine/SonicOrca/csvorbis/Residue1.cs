namespace csvorbis;

internal class Residue1 : Residue0
{
	private new int forward(Block vb, object vl, float[][] fin, int ch)
	{
		return 0;
	}

	public override int inverse(Block vb, object vl, float[][] fin, int[] nonzero, int ch)
	{
		int num = 0;
		for (int i = 0; i < ch; i++)
		{
			if (nonzero[i] != 0)
			{
				fin[num++] = fin[i];
			}
		}
		if (num != 0)
		{
			return Residue0._01inverse(vb, vl, fin, num, 1);
		}
		return 0;
	}
}
