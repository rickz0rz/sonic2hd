namespace csvorbis;

internal class LookFloor1
{
	private static int VIF_POSIT = 63;

	internal int[] sorted_index = new int[VIF_POSIT + 2];

	internal int[] forward_index = new int[VIF_POSIT + 2];

	internal int[] reverse_index = new int[VIF_POSIT + 2];

	internal int[] hineighbor = new int[VIF_POSIT];

	internal int[] loneighbor = new int[VIF_POSIT];

	internal int posts;

	internal int n;

	internal int quant_q;

	internal InfoFloor1 vi;

	private void free()
	{
		sorted_index = null;
		forward_index = null;
		reverse_index = null;
		hineighbor = null;
		loneighbor = null;
	}
}
