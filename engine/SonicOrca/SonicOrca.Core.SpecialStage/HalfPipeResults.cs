using System;

namespace SonicOrca.Core.SpecialStage;

/// <summary>The parts of the results screen of a special stage, in order (see <see cref="HalfPipeResults"/>).</summary>
public enum HalfPipeResultsPhase
{
	/// <summary>The texts move onto the screen.</summary>
	Wait,
	/// <summary>The ring bonus and the gems bonus move into the total.</summary>
	Tally,
	/// <summary>A wait after the tally, before the perfect bonus shows.</summary>
	PerfectWait,
	/// <summary>"PERFECT BONUS" replaces the ring rows and flashes.</summary>
	PerfectShow,
	/// <summary>The perfect bonus moves into the total.</summary>
	PerfectTally,
	/// <summary>The title moves away, and "NOW SONIC CAN CHANGE INTO SUPER SONIC" moves onto the screen.</summary>
	SuperMessage,
	/// <summary>The last wait, then the screen closes.</summary>
	End
}

/// <summary>
/// The results after a special stage, as in Sonic 2 (s2disasm: Obj6F): 100 points for each ring, 10000 for the
/// emerald, and 50000 when every ring was collected. The tally waits 180 frames, then moves 100 points of each bonus
/// into the total on each frame. With the perfect bonus, the screen waits 90 frames, shows "PERFECT BONUS" for 90
/// frames, then moves it into the total at 200 points a frame. With all seven emeralds, the Super Sonic message
/// follows. Then the screen waits 120 frames. Call <see cref="Update"/> once for each frame (60 a second).
/// </summary>
public sealed class HalfPipeResults
{
	public const int PointsPerRing = 100;

	public const int GemsBonusPoints = 10000;

	public const int PerfectBonusPoints = 50000;

	public const int WaitBeforeTally = 180;

	public const int WaitAfterTally = 120;

	/// <summary>Each wait before the tally of the perfect bonus (Obj6F: $5A frames).</summary>
	public const int PerfectWait = 90;

	/// <summary>The speed of the texts towards the centre (Obj34_MoveTowardsTargetPosition), in pixels of the original a frame.</summary>
	public const int TextSpeed = 0x10;

	/// <summary>The speed of the texts that leave for the Super Sonic message (Obj6F_MoveTowardsSourcePosition).</summary>
	public const int LeaveSpeed = 0x20;

	/// <summary>The title leaves from the centre (x 160) to its start (x 448) in this number of frames.</summary>
	public const int SuperMessageLeaveFrames = (448 - 160 + LeaveSpeed - 1) / LeaveSpeed;

	/// <summary>"CHANGE INTO" comes from x 448 to the centre in this number of frames, then the screen waits 180 frames.</summary>
	public const int SuperMessageFrames = SuperMessageLeaveFrames + (448 - 160) / TextSpeed + 180;

	/// <summary>The points that move from each bonus into the total on each frame of the tally.</summary>
	private const int TallyStep = 100;

	private const int PerfectTallyStep = 200;

	private int _phaseFrames;

	public int Rings { get; }

	public bool EmeraldWon { get; }

	/// <summary>The Super Sonic message shows after the tally: the player got the seventh emerald.</summary>
	public bool SuperMessage { get; }

	public int RingBonus { get; private set; }

	public int GemsBonus { get; private set; }

	public int PerfectBonus { get; private set; }

	public int Total { get; private set; }

	/// <summary>The points that the stage gives: the sum of the bonuses before the tally.</summary>
	public int Score { get; }

	public HalfPipeResultsPhase Phase { get; private set; } = HalfPipeResultsPhase.Wait;

	/// <summary>The frames since the phase started. The first frame of a phase is 1.</summary>
	public int PhaseFrames => _phaseFrames;

	/// <summary>"PERFECT BONUS" is in the place of the ring rows, from the moment it shows to the end of the screen.</summary>
	public bool PerfectShown => HasPerfectBonus && Phase >= HalfPipeResultsPhase.PerfectShow;

	/// <summary>True on the frame that "PERFECT BONUS" shows, for the signpost sound.</summary>
	public bool PerfectJustShown { get; private set; }

	/// <summary>True on the frame that a tally ended, for the end sound.</summary>
	public bool TallyJustEnded { get; private set; }

	public bool Finished { get; private set; }

	private bool HasPerfectBonus { get; }

	/// <param name="rings">The rings that the players have at the end.</param>
	/// <param name="ringsCollected">The rings that the players collected, also those that bombs took away.</param>
	/// <param name="ringsInStage">The rings that came in the stage before the emerald.</param>
	/// <param name="allEmeralds">The player has all seven emeralds after this stage.</param>
	public HalfPipeResults(int rings, bool emeraldWon, int ringsCollected, int ringsInStage, bool allEmeralds = false)
	{
		Rings = rings;
		EmeraldWon = emeraldWon;
		SuperMessage = emeraldWon && allEmeralds;
		RingBonus = rings * PointsPerRing;
		GemsBonus = emeraldWon ? GemsBonusPoints : 0;
		// The original does not check for the emerald here (UNCERTAIN in the specification)
		HasPerfectBonus = ringsInStage > 0 && ringsCollected >= ringsInStage;
		PerfectBonus = HasPerfectBonus ? PerfectBonusPoints : 0;
		Score = RingBonus + GemsBonus + PerfectBonus;
	}

	/// <summary>True on a frame when points moved into the total, for the tally sound.</summary>
	public bool Update()
	{
		TallyJustEnded = false;
		PerfectJustShown = false;
		if (Finished)
		{
			return false;
		}
		_phaseFrames++;
		switch (Phase)
		{
			case HalfPipeResultsPhase.Wait:
				if (_phaseFrames >= WaitBeforeTally)
				{
					SetPhase(HalfPipeResultsPhase.Tally);
				}
				return false;
			case HalfPipeResultsPhase.Tally:
				// The original ends the tally on the first frame that moves nothing (Obj6F_TallyScore)
				int moved = Move(RingBonus, TallyStep, value => RingBonus = value) + Move(GemsBonus, TallyStep, value => GemsBonus = value);
				Total += moved;
				if (moved == 0)
				{
					TallyJustEnded = true;
					SetPhase(HasPerfectBonus ? HalfPipeResultsPhase.PerfectWait : AfterTally);
				}
				return moved > 0;
			case HalfPipeResultsPhase.PerfectWait:
			case HalfPipeResultsPhase.PerfectShow:
				if (_phaseFrames >= PerfectWait)
				{
					SetPhase(Phase + 1);
				}
				return false;
			case HalfPipeResultsPhase.PerfectTally:
				int perfectMoved = Move(PerfectBonus, PerfectTallyStep, value => PerfectBonus = value);
				Total += perfectMoved;
				if (perfectMoved == 0)
				{
					TallyJustEnded = true;
					SetPhase(AfterTally);
				}
				return perfectMoved > 0;
			case HalfPipeResultsPhase.SuperMessage:
				if (_phaseFrames >= SuperMessageFrames)
				{
					Finished = true;
				}
				return false;
			default:
				if (_phaseFrames >= WaitAfterTally)
				{
					Finished = true;
				}
				return false;
		}
	}

	private HalfPipeResultsPhase AfterTally => SuperMessage ? HalfPipeResultsPhase.SuperMessage : HalfPipeResultsPhase.End;

	private void SetPhase(HalfPipeResultsPhase phase)
	{
		Phase = phase;
		_phaseFrames = 0;
		PerfectJustShown = phase == HalfPipeResultsPhase.PerfectShow;
	}

	private static int Move(int bonus, int step, Action<int> set)
	{
		step = Math.Min(bonus, step);
		set(bonus - step);
		return step;
	}
}
