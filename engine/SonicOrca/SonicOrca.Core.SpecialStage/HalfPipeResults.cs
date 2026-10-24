using System;

namespace SonicOrca.Core.SpecialStage;

/// <summary>
/// The results after a special stage, as in Sonic 2 (s2disasm: the special stage results screen): 100 points for
/// each ring, 10000 for the emerald, and 50000 when every ring was collected. The tally waits 180 frames, moves the
/// bonuses into the total, then waits 120 frames. Call <see cref="Update"/> once for each frame.
/// </summary>
public sealed class HalfPipeResults
{
	public const int PointsPerRing = 100;

	public const int GemsBonusPoints = 10000;

	public const int PerfectBonusPoints = 50000;

	public const int WaitBeforeTally = 180;

	public const int WaitAfterTally = 120;

	/// <summary>The points that move from each bonus into the total on each frame of the tally.</summary>
	private const int TallyStep = 100;

	private int _frame;

	private int _tallyEndFrame = -1;

	public int Rings { get; }

	public bool EmeraldWon { get; }

	public int RingBonus { get; private set; }

	public int GemsBonus { get; private set; }

	public int PerfectBonus { get; private set; }

	public int Total { get; private set; }

	/// <summary>The points that the stage gives: the sum of the bonuses before the tally.</summary>
	public int Score { get; }

	public bool Finished { get; private set; }

	/// <param name="rings">The rings that the players have at the end.</param>
	/// <param name="ringsCollected">The rings that the players collected, also those that bombs took away.</param>
	/// <param name="ringsInStage">The rings that came in the stage before the emerald.</param>
	public HalfPipeResults(int rings, bool emeraldWon, int ringsCollected, int ringsInStage)
	{
		Rings = rings;
		EmeraldWon = emeraldWon;
		RingBonus = rings * PointsPerRing;
		GemsBonus = emeraldWon ? GemsBonusPoints : 0;
		// The original does not check for the emerald here (UNCERTAIN in the specification)
		PerfectBonus = ringsInStage > 0 && ringsCollected >= ringsInStage ? PerfectBonusPoints : 0;
		Score = RingBonus + GemsBonus + PerfectBonus;
	}

	/// <summary>True on a frame when points moved into the total, for the tally sound.</summary>
	public bool Update()
	{
		if (Finished)
		{
			return false;
		}
		_frame++;
		if (_frame <= WaitBeforeTally)
		{
			return false;
		}
		if (_tallyEndFrame < 0)
		{
			int moved = Move(RingBonus, value => RingBonus = value) + Move(GemsBonus, value => GemsBonus = value) + Move(PerfectBonus, value => PerfectBonus = value);
			Total += moved;
			if (RingBonus == 0 && GemsBonus == 0 && PerfectBonus == 0)
			{
				_tallyEndFrame = _frame;
			}
			return moved > 0;
		}
		if (_frame >= _tallyEndFrame + WaitAfterTally)
		{
			Finished = true;
		}
		return false;
	}

	/// <summary>True on the frame that the tally ended, for the end sound.</summary>
	public bool TallyJustEnded => _tallyEndFrame == _frame;

	private static int Move(int bonus, Action<int> set)
	{
		int step = Math.Min(bonus, TallyStep);
		set(bonus - step);
		return step;
	}
}
