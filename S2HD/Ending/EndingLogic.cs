namespace S2HD.Ending;

/// <summary>RandomNumber of the original: the random numbers of the clouds and the birds of the ending.</summary>
internal sealed class EndingRandom
{
	/// <summary>
	/// The value that RandomNumber uses when the seed is 0. The ending does not clear the seed: it keeps its value
	/// from the play of the level, so the clouds and the birds are different on each play.
	/// </summary>
	private const uint ResetSeed = 0x2A6D365A;

	/// <summary>RNG_seed.</summary>
	public uint Seed { get; set; }

	/// <summary>RandomNumber: the main loop calls it on each frame. The new seed is the seed times 41, with the sum of its two words in the high word.</summary>
	public void Next()
	{
		uint product = (Seed == 0 ? ResetSeed : Seed) * 41;
		ushort low = (ushort)product;
		ushort sum = (ushort)(low + (ushort)(product >> 16));
		Seed = (uint)sum << 16 | low;
	}

	/// <summary>ror.l of the seed, as ObjCB and ObjCD do before they read it.</summary>
	public uint RotateRight(int count)
	{
		Seed = Seed >> count | Seed << (32 - count);
		return Seed;
	}
}

/// <summary>
/// The scroll of the rows of the background of Death Egg Zone, as SwScrl_DEZ makes it: empty space, 28 rows of stars
/// that move at their own speeds, the edge of the Earth, and the sky. The values are in original pixels.
/// </summary>
internal sealed class EndingStarRows
{
	/// <summary>The height of each row (SwScrl_DEZ_RowHeights).</summary>
	public static readonly int[] Heights =
	{
		128,
		8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8,
		3, 5, 8, 16,
		128, 128, 128
	};

	/// <summary>The speeds of the rows of stars 1 to 24.</summary>
	private static readonly int[] StarSpeeds = { 3, 2, 4, 1, 2, 4, 3, 4, 2, 6, 3, 4, 1, 2, 4, 3, 2, 3, 4, 1, 3, 4, 2, 1 };

	/// <summary>The scroll of each row (TempArray_LayerDef), 16 bits as in the original.</summary>
	public ushort[] Values { get; } = new ushort[36];

	/// <summary>One frame of SwScrl_DEZ, with the camera at x 0.</summary>
	public void Update(ushort cameraX = 0)
	{
		ushort[] v = Values;
		v[0] = cameraX;
		for (int i = 0; i < StarSpeeds.Length; i++)
		{
			v[1 + i] += (ushort)StarSpeeds[i];
		}
		// Row 25 moves at half the speed of row 24
		uint row24 = v[24];
		v[25] = (ushort)(row24 >> 1);
		v[26] += 3;
		v[27] += 2;
		v[28] += 4;
		// The edge of the Earth: 5/8, 6/8 and 7/8 of row 24, with 16 bits of fraction as the original
		uint d1 = row24 << 16;
		uint eighth = d1 >> 3;
		uint d0 = d1 - eighth;
		v[31] = (ushort)(d0 >> 16);
		d0 -= eighth;
		v[30] = (ushort)(d0 >> 16);
		d0 -= eighth;
		v[29] = (ushort)(d0 >> 16);
		v[32] += 1;
		v[33] = cameraX;
		v[34] = cameraX;
		v[35] = cameraX;
	}
}

/// <summary>
/// The palette fades of ObjC9 in the ending, for whole palette lines. A line has a whiteness from 0 (its own colours)
/// to <see cref="Steps"/> (white). The ending starts with all lines white. A fade changes the whiteness by one step
/// every few frames, as ObjC9 changes each colour by one step of the Mega Drive.
/// </summary>
internal sealed class EndingPalette
{
	/// <summary>The steps of a fade: the colours go from $E to 0 in 7 steps.</summary>
	public const int Steps = 7;

	private readonly int[] _whiteness = { Steps, Steps, Steps, Steps };

	/// <summary>The fade of the palette changer (EndSeqPaletteChanger). There is one, so a new fade replaces the old one.</summary>
	private Fade _fade;

	private sealed class Fade
	{
		public int FirstLine;

		public int Lines;

		/// <summary>-1 to fade from white (loc_1344C), 1 to fade to white (loc_1348A).</summary>
		public int Direction;

		public int Time;

		public int TimeLeft;

		public int Amount;

		/// <summary>ObjC9_Init runs on the frame after the spawn.</summary>
		public bool Started;
	}

	/// <summary>The whiteness of a line, from 0 (its colours) to 1 (white).</summary>
	public double GetWhiteness(int line) => _whiteness[line] / (double)Steps;

	/// <summary>Starts a fade of ObjC9: a subtype of PaletteChangerDataIndex in the ending.</summary>
	public void Start(int subtype)
	{
		_fade = subtype switch
		{
			// Pal_AD1E (the photos) into line 0: from white, then to white
			4 => new Fade { FirstLine = 0, Lines = 1, Direction = -1, Time = 8, Amount = 7 },
			6 => new Fade { FirstLine = 0, Lines = 1, Direction = 1, Time = 8, Amount = 7 },
			// Pal_AC7E (Sonic) and Pal_AC9E (Tails) into lines 0 and 1, Pal_ACDE (the background) into lines 2 and 3
			8 => new Fade { FirstLine = 0, Lines = 2, Direction = -1, Time = 4, Amount = 7 },
			0xA => new Fade { FirstLine = 2, Lines = 2, Direction = -1, Time = 4, Amount = 7 },
			// Pal_AD3E (Super Sonic) into line 0
			0xC => new Fade { FirstLine = 0, Lines = 1, Direction = -1, Time = 4, Amount = 7 },
			0xE => new Fade { FirstLine = 0, Lines = 2, Direction = -1, Time = 4, Amount = 7 },
			_ => null
		};
	}

	/// <summary>ObjC9 on one frame.</summary>
	public void Update()
	{
		if (_fade == null)
		{
			return;
		}
		if (!_fade.Started)
		{
			_fade.Started = true;
			_fade.TimeLeft = _fade.Time;
			return;
		}
		if (--_fade.TimeLeft >= 0)
		{
			return;
		}
		_fade.TimeLeft = _fade.Time;
		if (--_fade.Amount < 0)
		{
			_fade = null;
			return;
		}
		for (int line = _fade.FirstLine; line < _fade.FirstLine + _fade.Lines; line++)
		{
			_whiteness[line] = System.Math.Clamp(_whiteness[line] + _fade.Direction, 0, Steps);
		}
	}
}
