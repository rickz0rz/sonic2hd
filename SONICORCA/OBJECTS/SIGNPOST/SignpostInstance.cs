using System;
using System.Collections.Generic;
using SonicOrca;
using SonicOrca.Core;
using SonicOrca.Core.Objects;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.SIGNPOST;

/// <summary>
/// The signpost at the end of an act. When the protagonist runs past it, it spins, shows sparkles, and stops on the
/// face of the protagonist. Then the camera moves down and the level completes. In the Original physics, the
/// protagonist then runs right with the controls locked, and the level completes when it reaches the right side of
/// the screen (Obj0D_Main_State3 in the original).
/// </summary>
public class SignpostInstance : ActiveObject
{
	private static readonly IReadOnlyList<Vector2i> SparkleParticleOffsets = new Vector2i[]
	{
		new Vector2i(16, 4),
		new Vector2i(-16, 12),
		new Vector2i(-12, -16),
		new Vector2i(20, -24)
	};
	private static readonly Vector2i[] SparklePositionOffsets = new Vector2i[]
	{
		new Vector2i(-96, -64),
		new Vector2i(32, 32),
		new Vector2i(-64, 32),
		new Vector2i(96, -32),
		new Vector2i(0, -32),
		new Vector2i(64, 0),
		new Vector2i(-96, 32),
		new Vector2i(96, 16)
	};

	// Each character has three animations: turn to the front, front, and turn to the side.
	// The animation index is the AnimationRobotnik, AnimationSonic or AnimationTails base plus an Offset* value.
	private const int AnimationSide = 0;
	private const int AnimationRobotnik = 1;
	private const int AnimationSonic = 4;
	private const int AnimationTails = 7;
	private const int OffsetToFront = 0;
	private const int OffsetFront = 1;
	private const int OffsetToSide = 2;
	private const int CharacterIndexRobotnik = 0;
	private const int CharacterIndexSonic = 1;
	private const int CharacterIndexTails = 2;
	private const int CharacterCount = 3;

	private const int MinSpins = 10;
	private const int CameraSpeed = 6;
	private const int CameraBottomBelowSignpost = 340;
	private const int SparkleDelayTicks = 12;
	private const int CompleteLevelDelayTicks = 80;

	/// <summary>The width of the area right of the signpost that activates it in the original ($20 px).</summary>
	private const int OriginalActivationWidth = 0x20 * 4;

	/// <summary>The ticks of the spin in the original: two animations of 61 frames each.</summary>
	private const int OriginalSpinTicks = 2 * 61;

	/// <summary>
	/// The distance from the right side of the screen where the run ends in the original: the run ends at
	/// Camera_Max_X_pos + $128, and the screen is $140 px wide.
	/// </summary>
	private const int OriginalRunEndFromRight = (0x140 - 0x128) * 4;

	private Updater _updater;
	private AnimationInstance _animation;
	private bool _activated;
	private bool _triggeredEndGame;
	private int _sparkleIndex;
	private int _sparkleDelay;
	private int _targetCameraBottom;
	private bool _holdingRight;

	[StateVariable]
	private bool Activated
	{
		get => _activated;
		set => _activated = value;
	}

	private bool _versusOnly;

	/// <summary>
	/// The signpost is only for versus mode, as the signposts of act 2 in the original. Outside versus mode, the act
	/// ends with its boss, so the signpost goes away.
	/// </summary>
	[StateVariable]
	private bool VersusOnly
	{
		get => _versusOnly;
		set => _versusOnly = value;
	}

	public SignpostInstance()
	{
		DesignBounds = new Rectanglei(-96, -96, 192, 192);
	}

	protected override void OnStart()
	{
		// The level editor shows the signpost, so that the user can see and move it
		if (_versusOnly && !Level.Versus && Level.State != LevelState.Editing)
		{
			FinishForever();
			return;
		}
		_updater = new Updater(Level.Versus ? GetVersusUpdates() : GetUpdates());
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(SignpostType.AnimationGroupResourceKey));
		_animation.Index = AnimationRobotnik + OffsetFront;
		if (!_activated)
			return;

		// The signpost was activated before (for example, before a restart): show the protagonist face.
		_animation.Index = GetAnimationIndexFromCharacter(GetProtagonistCharacterIndex()) + OffsetFront;
		_triggeredEndGame = true;
	}

	protected override void OnUpdate() => _updater?.Update();

	/// <summary>
	/// The signpost of versus mode, as Obj0D of the original in two-player mode: each player who passes it finishes
	/// the act, and their time stops. Each pass makes the sign spin, and the spin stops on the face of the first player
	/// who passed it. The level ends the act when all players finished, or when the time limit of the others runs out.
	/// </summary>
	private IEnumerable<UpdateResult> GetVersusUpdates()
	{
		int? winnerCharacterIndex = null;
		IEnumerator<UpdateResult> spin = null;
		// The X position of each player at the last update of the sign. Only a pass from the left counts, because the
		// map of act 2 also has the signpost of act 1, left of the start of act 2. The sign can update before the
		// characters, so it keeps the positions itself.
		Dictionary<Player, int> lastX = new Dictionary<Player, int>();
		while (true)
		{
			foreach (Player player in Level.Players)
			{
				ICharacter character = player.Protagonist;
				if (character == null)
					continue;
				int x = character.Position.X;
				bool passed = lastX.TryGetValue(player, out int previousX) && previousX < Position.X && x >= Position.X;
				lastX[player] = x;
				if (!passed || player.StoppedTime != null || character.IsDebug || character.IsDead || character.IsDying)
					continue;
				Level.FinishVersusPlayer(player);
				Level.SoundManager.PlaySound(this, Type.GetAbsolutePath(SignpostType.SoundResourceKey));
				winnerCharacterIndex ??= player.ProtagonistCharacterType == CharacterType.Tails ? CharacterIndexTails : CharacterIndexSonic;
				// A new pass starts the spin again, as in the original
				spin = GetSpinUpdates(winnerCharacterIndex.Value, moveCamera: false).GetEnumerator();
			}
			if (spin != null && !spin.MoveNext())
				spin = null;
			yield return UpdateResult.Next();
		}
	}

	/// <summary>
	/// Spins the sign through the faces at least <see cref="MinSpins"/> times (HD) or for
	/// <see cref="OriginalSpinTicks"/> (Original), with sparkles, and stops on the face of a character.
	/// </summary>
	private IEnumerable<UpdateResult> GetSpinUpdates(int targetCharacterIndex, bool moveCamera)
	{
		int characterIndex = CharacterIndexRobotnik;
		int spinTicks = 0;
		_animation.Index = GetAnimationIndexFromCharacter(characterIndex) + OffsetFront;
		for (int spins = 0; !IsSpinDone(spins, spinTicks) || characterIndex != targetCharacterIndex; ++spins)
		{
			// Turn to the side. The animation goes to AnimationSide when it ends.
			++_animation.Index;
			while (_animation.Index != AnimationSide)
			{
				if (moveCamera)
					UpdateCamera();
				UpdateSparkles();
				++spinTicks;
				yield return UpdateResult.Next();
			}

			// Turn to the front of the next character. The animation goes to the front animation when it ends.
			// The original stops on the face of the target at once when the spin time ends.
			characterIndex = Level.OriginalPhysics && spinTicks >= OriginalSpinTicks
				? targetCharacterIndex
				: (characterIndex + 1) % CharacterCount;
			int animationIndex = GetAnimationIndexFromCharacter(characterIndex);
			_animation.Index = animationIndex;
			while (_animation.Index != animationIndex + OffsetFront)
			{
				if (moveCamera)
					UpdateCamera();
				UpdateSparkles();
				++spinTicks;
				yield return UpdateResult.Next();
			}
		}
	}

	/// <summary>
	/// Holds right for the protagonist during the run at the end of the act. This runs before the update of the
	/// characters, after the level reads the controller or a recording.
	/// </summary>
	protected override void OnUpdatePrepare()
	{
		if (_holdingRight)
			Level.Player.Protagonist.Input = new CharacterInputState { Throttle = 1.0 };
	}

	/// <summary>The signpost script. It runs as a coroutine, one step for each frame.</summary>
	private IEnumerable<UpdateResult> GetUpdates()
	{
		if (!_triggeredEndGame)
		{
			while (!_activated)
			{
				if (Level.OriginalPhysics)
					CheckOriginalActivation();
				else
				{
					foreach (ICharacter character in Level.ObjectManager.Characters)
					{
						if (!character.IsSidekick && !character.IsDebug && character.ShouldReactToLevel && character.Position.X >= Position.X && character.LastPosition.X < Position.X)
							Activate(character);
					}
				}
				yield return UpdateResult.Next();
			}

			foreach (UpdateResult result in GetSpinUpdates(GetProtagonistCharacterIndex(), moveCamera: true))
				yield return result;
			if (Level.OriginalPhysics)
			{
				// The protagonist runs right with the controls locked. The level completes when it reaches the right
				// side of the screen.
				while (!UpdateOriginalRun())
				{
					UpdateCamera();
					yield return UpdateResult.Next();
				}
				_holdingRight = false;
				Level.Player.RemoveStatusAtEndOfAct();
				Level.CompleteLevel();
				_triggeredEndGame = true;
				yield break;
			}
			Rectanglei bounds = Level.Bounds;
			while (bounds.Bottom != _targetCameraBottom)
			{
				bounds.Bottom = MathX.GoTowards(bounds.Bottom, _targetCameraBottom, CameraSpeed);
				Level.Bounds = bounds;
				yield return UpdateResult.Next();
			}
			yield return UpdateResult.Wait(CompleteLevelDelayTicks);
			Level.CompleteLevel();
			_triggeredEndGame = true;
		}
	}

	private bool IsSpinDone(int spins, int spinTicks) => Level.OriginalPhysics ? spinTicks >= OriginalSpinTicks : spins >= MinSpins;

	/// <summary>
	/// The original activates the signpost when the protagonist is in the $20 px right of the signpost while the
	/// timer runs.
	/// </summary>
	private void CheckOriginalActivation()
	{
		ICharacter character = Level.Player.Protagonist;
		if (character == null || character.IsDebug || !Level.StateFlags.HasFlag(LevelStateFlags.UpdateTime))
			return;
		int distance = character.Position.X - Position.X;
		if (distance >= 0 && distance < OriginalActivationWidth)
			Activate(character);
	}

	/// <summary>
	/// Locks the controls of the protagonist with right held when it is on the ground. The lock stays until the end
	/// of the act. Returns true when the protagonist has reached the end of the run.
	/// </summary>
	private bool UpdateOriginalRun()
	{
		ICharacter character = Level.Player.Protagonist;
		if (character.IsDebug)
			return false;
		if (!character.IsAirborne && !_holdingRight)
		{
			Level.StateFlags &= ~LevelStateFlags.AllowCharacterControl;
			_holdingRight = true;
		}
		return character.Position.X >= Level.Bounds.Right - OriginalRunEndFromRight;
	}

	/// <summary>Gets the "turn to the front" animation index of a character.</summary>
	private int GetAnimationIndexFromCharacter(int character)
	{
		switch (character)
		{
			case CharacterIndexSonic:
				return AnimationSonic;
			case CharacterIndexTails:
				return AnimationTails;
			default:
				return AnimationRobotnik;
		}
	}

	private int GetProtagonistCharacterIndex()
	{
		switch (Level.Player.ProtagonistCharacterType)
		{
			case CharacterType.Tails:
				return CharacterIndexTails;
			default:
				return CharacterIndexSonic;
		}
	}

	private void Activate(ICharacter character)
	{
		_activated = true;
		_targetCameraBottom = Position.Y + CameraBottomBelowSignpost;
		Level.Bounds = Level.Bounds with
		{
			Bottom = Math.Max(_targetCameraBottom, (int)Level.Camera.Bounds.Bottom)
		};
		Level.JustAboutToCompleteLevel();
		Level.SoundManager.PlaySound(this, Type.GetAbsolutePath(SignpostType.SoundResourceKey));
		(Entry.State as SignpostInstance).Activated = true;
	}

	private void UpdateCamera()
	{
		Rectanglei bounds = Level.Bounds;
		bounds.Bottom = MathX.GoTowards(bounds.Bottom, _targetCameraBottom, CameraSpeed);
		Level.Bounds = bounds;
	}

	private void UpdateSparkles()
	{
		--_sparkleDelay;
		if (_sparkleDelay > 0)
			return;
		CreateSparkles(SparklePositionOffsets[_sparkleIndex]);
		_sparkleDelay = SparkleDelayTicks;
		_sparkleIndex = (_sparkleIndex + 1) % SparklePositionOffsets.Length;
	}

	/// <summary>Creates four sparkle objects around a position, one every two ticks.</summary>
	private void CreateSparkles(Vector2i relativePosition)
	{
		Vector2i center = Position + relativePosition;
		int ticks = 0;
		foreach (Vector2i sparkleParticleOffset in SparkleParticleOffsets)
		{
			ObjectPlacement objectPlacement = new ObjectPlacement(Type.GetAbsolutePath(SignpostType.SparkleObjectResourceKey), Level.Map.Layers.IndexOf(Layer), center + sparkleParticleOffset);
			Level.SetInterval(ticks, () => Level.ObjectManager.AddObject(objectPlacement));
			ticks += 2;
		}
	}

	protected override void OnAnimate() => _animation.Animate();

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		renderer.GetObjectRenderer().Render(_animation);
	}
}
