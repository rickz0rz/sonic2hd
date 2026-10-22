namespace csvorbis;

internal class InfoMapping0
{
	internal int submaps;

	internal int[] chmuxlist = new int[256];

	internal int[] timesubmap = new int[16];

	internal int[] floorsubmap = new int[16];

	internal int[] residuesubmap = new int[16];

	internal int[] psysubmap = new int[16];

	internal int coupling_steps;

	internal int[] coupling_mag = new int[256];

	internal int[] coupling_ang = new int[256];

	internal void free()
	{
		chmuxlist = null;
		timesubmap = null;
		floorsubmap = null;
		residuesubmap = null;
		psysubmap = null;
		coupling_mag = null;
		coupling_ang = null;
	}
}
