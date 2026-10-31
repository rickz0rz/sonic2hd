using System;
using SonicOrca.Core.Objects;
using SonicOrca.Core.Objects.Base;
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

	/// <summary>
	/// The highest score. The original keeps the score in units of 10 and stops it at 999999 (AddPoints), which
	/// shows as 9999990.
	/// </summary>
	private const int MaxScore = 9999990;

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

	/// <summary>The number of continues (Continue_count in the original). Only the Original physics uses them.</summary>
	public int Continues { get; set; }

	public int StarpostIndex { get; set; }

	public Vector2i StarpostPosition { get; set; }

	public int StarpostTime { get; set; }

	/// <summary>The collision path of the protagonist at the last star post (top_solid_bit in Obj79_SaveData).</summary>
	public int StarpostPath { get; set; }

	/// <summary>The layer index (the priority) of the protagonist at the last star post, or -1 if it is not known.</summary>
	public int StarpostLayerIndex { get; set; } = -1;

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
		// As AddPoints in the original: one call gives at most one extra life.
		Score = Math.Min(Score + points, MaxScore);
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
			// resets the target. HD gives a life at each 100 rings.
			TargetRingCountForNextLife = !_level.OriginalPhysics || TargetRingCountForNextLife < LastRingsForLife
				? TargetRingCountForNextLife + RingsForLife
				: int.MaxValue;
			GainLives();
		}
	}

	/// <summary>
	/// Sets the score at the start of a level, and sets the score of the next extra life to the next multiple of
	/// <see cref="ScoreForLife"/>. The original keeps Next_Extra_life_score between levels.
	/// </summary>
	public void SetStartScore(int score)
	{
		Score = Math.Min(score, MaxScore);
		TargetScoreForNextLife = (Score / ScoreForLife + 1) * ScoreForLife;
	}

	/// <summary>
	/// Uses a continue after the game is over: the player gets the start lives again and the score goes back to 0,
	/// as the continue screen of the original.
	/// </summary>
	public void UseContinue()
	{
		Continues--;
		Lives = StartLives;
		SetStartScore(0);
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
		// The original only speeds up the music that plays. The HD data has a fast copy of the level music only.
		if (!_level.SoundManager.SpeedUpMusic())
		{
			_level.SoundManager.PlayJingle(JingleType.SpeedShoes);
		}
	}

	public void RemoveSpeedShoes()
	{
		if (Protagonist != null)
		{
			Protagonist.HasSpeedShoes = false;
		}
		_level.SoundManager.SlowDownMusic();
		_level.SoundManager.StopJingle(JingleType.SpeedShoes);
	}

	public void GiveInvincibility()
	{
		// invincible_monitor does nothing for Super Sonic, who is already invincible.
		if (Protagonist.StateFlags.HasFlag(CharacterState.Super))
		{
			return;
		}
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

	/// <summary>
	/// Removes the barrier, the invincibility and the speed shoes at the end of an act, as Load_EndOfAct in the
	/// original (it clears status_secondary). The characters also lose their abilities.
	/// </summary>
	public void RemoveStatusAtEndOfAct()
	{
		RemovePowerups();
		if (Protagonist != null)
		{
			Protagonist.Barrier = BarrierType.None;
			Protagonist.RemoveAllAbilities();
		}
		Sidekick?.RemoveAllAbilities();
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
		StarpostPath = Protagonist.Path;
		StarpostLayerIndex = _level.Map.Layers.IndexOf(Protagonist.Layer);
	}
}
