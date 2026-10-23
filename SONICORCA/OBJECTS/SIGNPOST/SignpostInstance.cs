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

public class SignpostInstance : ActiveObject
{
	private static readonly IReadOnlyList<Vector2i> SparkleParticleOffsets = new Vector2i[4]
	{
	new Vector2i(16 /*0x10*/, 4),
	new Vector2i(-16, 12),
	new Vector2i(-12, -16),
	new Vector2i(20, -24)
	};
	private static Vector2i[] SparklePositionOffsets = new Vector2i[8]
	{
	new Vector2i(-96, -64),
	new Vector2i(32 /*0x20*/, 32 /*0x20*/),
	new Vector2i(-64, 32 /*0x20*/),
	new Vector2i(96 /*0x60*/, -32),
	new Vector2i(0, -32),
	new Vector2i(64 /*0x40*/, 0),
	new Vector2i(-96, 32 /*0x20*/),
	new Vector2i(96 /*0x60*/, 16 /*0x10*/)
	};
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
	private Updater _updater;
	private AnimationInstance _animation;
	private bool _activated;
	private bool _triggedEndGame;
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
		DesignBounds = new Rectanglei(-96, -96, 192 /*0xC0*/, 192 /*0xC0*/);
	}

	protected override void OnStart()
	{
		_updater = new Updater(GetUpdates());
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath("/ANIGROUP"));
		_animation.Index = 2;
		if (!_activated)
			return;
		_animation.Index = GetAnimationIndexFromCharacter(GetProtagonistCharacterIndex()) + 1;
		_activated = true;
		_triggedEndGame = true;
	}

	protected override void OnUpdate() => _updater.Update();

	private IEnumerable<UpdateResult> GetUpdates()
	{
		if (!_triggedEndGame)
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
			int minSpins = 10;
			int spins = 0;
			int characterIndex = 0;
			int targetCharacterIndex = GetProtagonistCharacterIndex();
			for (spins = 0; spins < minSpins || characterIndex != targetCharacterIndex; ++spins)
			{
				++_animation.Index;
				while (_animation.Index != 0)
				{
					UpdateCamera();
					UpdateSparkles();
					yield return UpdateResult.Next();
				}
				characterIndex = (characterIndex + 1) % 3;
				int animationIndex = GetAnimationIndexFromCharacter(characterIndex);
				_animation.Index = animationIndex;
				while (_animation.Index != animationIndex + 1)
				{
					UpdateCamera();
					UpdateSparkles();
					yield return UpdateResult.Next();
				}
			}
			Rectanglei bounds = Level.Bounds;
			while (bounds.Bottom != _targetCameraBottom)
			{
				bounds.Bottom = MathX.GoTowards(bounds.Bottom, _targetCameraBottom, 6);
				Level.Bounds = bounds;
				yield return UpdateResult.Next();
			}
			yield return UpdateResult.Wait(80 /*0x50*/);
			Level.CompleteLevel();
			_triggedEndGame = true;
		}
	}

	private int GetAnimationIndexFromCharacter(int character)
	{
		switch (character)
		{
			case 1:
				return 4;
			case 2:
				return 7;
			default:
				return 1;
		}
	}

	private int GetProtagonistCharacterIndex()
	{
		switch (Level.Player.ProtagonistCharacterType)
		{
			case CharacterType.Tails:
				return 2;
			default:
				return 1;
		}
	}

	private void Activate(ICharacter character)
	{
		_activated = true;
		_targetCameraBottom = Position.Y + 340;
		Level.Bounds = Level.Bounds with
		{
			Bottom = Math.Max(_targetCameraBottom, (int)Level.Camera.Bounds.Bottom)
		};
		Level.JustAboutToCompleteLevel();
		Level.SoundManager.PlaySound(this, Type.GetAbsolutePath("SONICORCA/SOUND/SIGNPOST"));
		(Entry.State as SignpostInstance).Activated = true;
	}

	private void UpdateCamera()
	{
		Rectanglei bounds = Level.Bounds;
		bounds.Bottom = MathX.GoTowards(bounds.Bottom, _targetCameraBottom, 6);
		Level.Bounds = bounds;
	}

	private void UpdateSparkles()
	{
		--_sparkleDelay;
		if (_sparkleDelay > 0)
			return;
		CreateSparkles(SignpostInstance.SparklePositionOffsets[_sparkleIndex]);
		_sparkleDelay = 12;
		_sparkleIndex = (_sparkleIndex + 1) % SignpostInstance.SparklePositionOffsets.Length;
	}

	private void CreateSparkles(Vector2i relativePosition)
	{
		Vector2i vector2i = Position + relativePosition;
		int ticks = 0;
		foreach (Vector2i sparkleParticleOffset in (IEnumerable<Vector2i>)SignpostInstance.SparkleParticleOffsets)
		{
			ObjectPlacement objectPlacement = new ObjectPlacement(Type.GetAbsolutePath("SONICORCA/OBJECTS/RING/SPARKLE"), Level.Map.Layers.IndexOf(Layer), vector2i + sparkleParticleOffset);
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
