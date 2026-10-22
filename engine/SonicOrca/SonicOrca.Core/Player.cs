using System;
using SonicOrca.Core.Objects;
using SonicOrca.Geometry;

namespace SonicOrca.Core;

public class Player
{
	private const int RingsForLife = 100;

	private const int ScoreForLife = 50000;

	private const int MaxLives = 999;

	private const int MaxRings = 999;

	private const int BigChainSize = 16;

	private const int BigChainScore = 10000;

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

	public Player(Level level)
	{
		_level = level;
		TargetRingCountForNextLife = 100;
		TargetScoreForNextLife = 50000;
		StarpostIndex = -1;
		Lives = 3;
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
			TargetScoreForNextLife += 50000;
			GainLives();
		}
	}

	public void GainRings(int count = 1)
	{
		TotalRings += count;
		CurrentRings = Math.Min(CurrentRings + count, 999);
		if (CurrentRings >= TargetRingCountForNextLife)
		{
			TargetRingCountForNextLife += 100;
			GainLives();
		}
	}

	public void GainLives(int count = 1)
	{
		if (Lives >= 0)
		{
			Lives = Math.Min(Lives + count, 999);
		}
		_level.SoundManager.PlayJingle(JingleType.Life);
	}

	public void LoseAllRings()
	{
		TargetRingCountForNextLife = 100;
		CurrentRings = 0;
	}

	public void ResetRings()
	{
		TargetRingCountForNextLife = 100;
		CurrentRings = 0;
		TotalRings = 0;
	}

	public int AwardChainedScore()
	{
		int num = ((_scoreChainIndex >= 16) ? 10000 : ChainScores[Math.Min(_scoreChainIndex, ChainScores.Length - 1)]);
		_scoreChainIndex++;
		GainScore(num);
		return num;
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
		SpeedShoesTicks = 1200;
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
		InvincibillityTicks = 1200;
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
