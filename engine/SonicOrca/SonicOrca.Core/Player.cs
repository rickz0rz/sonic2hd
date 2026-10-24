using System;
using SonicOrca.Core.Objects;
using SonicOrca.Geometry;

namespace SonicOrca.Core;

/// <summary>
/// The state of the human player: score, lives, rings, power-ups, starpost and characters.
/// </summary>
public class Player
{
	private const int RingsForLife = 100;

	/// <summary>The ring count of the last extra life. The original gives lives only at 100 and 200 rings.</summary>
	private const int LastRingsForLife = 200;

	private const int ScoreForLife = 50000;

	private const int MaxLives = 999;

	private const int MaxRings = 999;

	private const int BigChainSize = 16;

	private const int BigChainScore = 10000;

	private const int StartLives = 3;

	/// <summary>
	/// The number of ticks that speed shoes and invincibility stay active (20 seconds at 60 ticks each second).
	/// </summary>
	private const int PowerupTicks = 1200;

	private static readonly int[] ChainScores = new int[4] { 100, 200, 500, 1000 };

	public const int UndefinedStarpostIndex = -1;

	private readonly Level _level;

	private int _scoreChainIndex;

	public int SpeedShoesTicks { get; private set; }

	public int InvincibillityTicks { get; private set; }

	public int TargetRingCountForNextLife { get; private set; }

	public int TargetScoreForNextLife { get; private set; }

	public int StarpostIndex { get; set; }

	public Vector2i StarpostPosition { get; set; }

	public int StarpostTime { get; set; }

	public ICharacter Protagonist { get; set; }

	public CharacterType ProtagonistCharacterType { get; set; }

	public int ProtagonistGamepadIndex { get; set; }

	public ICharacter Sidekick { get; set; }

	public CharacterType SidekickCharacterType { get; set; }

	public int SidekickGamepadIndex { get; set; }

	public int Score { get; set; }

	public int Lives { get; set; }

	public int TotalRings { get; set; }

	public int CurrentRings { get; set; }

	/// <summary>The Chaos Emeralds that the player has, from 0 to 7.</summary>
	public int Emeralds { get; set; }

	public Player(Level level)
	{
		_level = level;
		TargetRingCountForNextLife = RingsForLife;
		TargetScoreForNextLife = ScoreForLife;
		StarpostIndex = UndefinedStarpostIndex;
		Lives = StartLives;
	}

	public void Update()
	{
		if (SpeedShoesTicks > 0)
		{
			SpeedShoesTicks--;
			if (SpeedShoesTicks == 0)
			{
				RemoveSpeedShoes();
			}
		}
		if (InvincibillityTicks > 0)
		{
			InvincibillityTicks--;
			if (InvincibillityTicks == 0)
			{
				RemoveInvincibility();
			}
		}
	}

	public void GainScore(int points)
	{
		Score += points;
		if (Score >= TargetScoreForNextLife)
		{
			TargetScoreForNextLife += ScoreForLife;
			GainLives();
		}
	}

	public void GainRings(int count = 1)
	{
		TotalRings += count;
		CurrentRings = Math.Min(CurrentRings + count, MaxRings);
		if (CurrentRings >= TargetRingCountForNextLife)
		{
			// As Extra_life_flags in the original: one life at 100 rings and one at 200 rings. Losing the rings
			// resets the target.
			TargetRingCountForNextLife = TargetRingCountForNextLife < LastRingsForLife
				? TargetRingCountForNextLife + RingsForLife
				: int.MaxValue;
			GainLives();
		}
	}

	public void GainLives(int count = 1)
	{
		if (Lives >= 0)
		{
			Lives = Math.Min(Lives + count, MaxLives);
		}
		_level.SoundManager.PlayJingle(JingleType.Life);
	}

	public void LoseAllRings()
	{
		TargetRingCountForNextLife = RingsForLife;
		CurrentRings = 0;
	}

	public void ResetRings()
	{
		TargetRingCountForNextLife = RingsForLife;
		CurrentRings = 0;
		TotalRings = 0;
	}

	public int AwardChainedScore()
	{
		// Each object in a chain gives more points. From the 16th object, each object gives the big chain score.
		// The index of the 16th object is 15.
		int points = (_scoreChainIndex >= BigChainSize - 1) ? BigChainScore : ChainScores[Math.Min(_scoreChainIndex, ChainScores.Length - 1)];
		_scoreChainIndex++;
		GainScore(points);
		return points;
	}

	public void ResetScoreChain()
	{
		_scoreChainIndex = 0;
	}

	public void GiveBarrier(BarrierType type)
	{
		Protagonist.Barrier = type;
	}

	public void GiveSpeedShoes()
	{
		SpeedShoesTicks = PowerupTicks;
		Protagonist.HasSpeedShoes = true;
		_level.SoundManager.PlayJingle(JingleType.SpeedShoes);
	}

	public void RemoveSpeedShoes()
	{
		if (Protagonist != null)
		{
			Protagonist.HasSpeedShoes = false;
		}
		_level.SoundManager.StopJingle(JingleType.SpeedShoes);
	}

	public void GiveInvincibility()
	{
		InvincibillityTicks = PowerupTicks;
		Protagonist.IsInvincible = true;
		_level.SoundManager.PlayJingle(JingleType.Invincibility);
	}

	public void RemoveInvincibility()
	{
		if (Protagonist != null)
		{
			Protagonist.IsInvincible = false;
		}
		_level.SoundManager.StopJingle(JingleType.Invincibility);
	}

	public void RemovePowerups()
	{
		RemoveInvincibility();
		RemoveSpeedShoes();
	}

	public bool IsStarpostActivated(int index)
	{
		return index <= StarpostIndex;
	}

	public void ActivateStarpost(int index, Vector2i position)
	{
		StarpostIndex = index;
		StarpostPosition = position;
		StarpostTime = _level.Time;
	}
}
