// Decompiled with JetBrains decompiler
// Type: SONICORCA.OBJECTS.SIGNPOST.SignpostInstance
// Assembly: S2HD, Version=2.0.1012.10521, Culture=neutral, PublicKeyToken=null
// MVID: 18631A0F-16CF-4E18-8563-1EC5E54750D6
// Assembly location: C:\Users\ricky\Downloads\S2HD_2.0.1012-rc2\S2HD_2.0.1012-rc2\S2HD.exe

using System;
using System.Collections.Generic;
using SonicOrca;
using SonicOrca.Core;
using SonicOrca.Core.Objects;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

#nullable disable
namespace SONICORCA.OBJECTS.SIGNPOST;

/// <summary>
/// The signpost at the end of an act. When the protagonist runs past it, it spins, shows sparkles, and stops on the
/// face of the protagonist. Then the camera moves down and the level completes.
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

	private Updater _updater;
	private AnimationInstance _animation;
	private bool _activated;
	private bool _triggeredEndGame;
	private int _sparkleIndex;
	private int _sparkleDelay;
	private int _targetCameraBottom;

	[StateVariable]
	private bool Activated
	{
		get => _activated;
		set => _activated = value;
	}

	public SignpostInstance()
	{
		DesignBounds = new Rectanglei(-96, -96, 192, 192);
	}

	protected override void OnStart()
	{
		_updater = new Updater(GetUpdates());
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(SignpostType.AnimationGroupResourceKey));
		_animation.Index = AnimationRobotnik + OffsetFront;
		if (!_activated)
			return;

		// The signpost was activated before (for example, before a restart): show the protagonist face.
		_animation.Index = GetAnimationIndexFromCharacter(GetProtagonistCharacterIndex()) + OffsetFront;
		_triggeredEndGame = true;
	}

	protected override void OnUpdate() => _updater.Update();

	/// <summary>The signpost script. It runs as a coroutine, one step for each frame.</summary>
	private IEnumerable<UpdateResult> GetUpdates()
	{
		if (!_triggeredEndGame)
		{
			while (!_activated)
			{
				foreach (ICharacter character in Level.ObjectManager.Characters)
				{
					if (!character.IsSidekick && !character.IsDebug && character.ShouldReactToLevel && character.Position.X >= Position.X && character.LastPosition.X < Position.X)
						Activate(character);
				}
				yield return UpdateResult.Next();
			}

			// Spin through the faces at least MinSpins times, and stop on the face of the protagonist.
			int characterIndex = CharacterIndexRobotnik;
			int targetCharacterIndex = GetProtagonistCharacterIndex();
			for (int spins = 0; spins < MinSpins || characterIndex != targetCharacterIndex; ++spins)
			{
				// Turn to the side. The animation goes to AnimationSide when it ends.
				++_animation.Index;
				while (_animation.Index != AnimationSide)
				{
					UpdateCamera();
					UpdateSparkles();
					yield return UpdateResult.Next();
				}

				// Turn to the front of the next character. The animation goes to the front animation when it ends.
				characterIndex = (characterIndex + 1) % CharacterCount;
				int animationIndex = GetAnimationIndexFromCharacter(characterIndex);
				_animation.Index = animationIndex;
				while (_animation.Index != animationIndex + OffsetFront)
				{
					UpdateCamera();
					UpdateSparkles();
					yield return UpdateResult.Next();
				}
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
