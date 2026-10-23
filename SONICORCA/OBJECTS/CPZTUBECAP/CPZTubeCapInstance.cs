// Decompiled with JetBrains decompiler
// Type: SONICORCA.OBJECTS.CPZTUBECAP.CPZTubeCapInstance
// Assembly: S2HD, Version=2.0.1012.10521, Culture=neutral, PublicKeyToken=null
// MVID: 18631A0F-16CF-4E18-8563-1EC5E54750D6
// Assembly location: C:\Users\ricky\Downloads\S2HD_2.0.1012-rc2\S2HD_2.0.1012-rc2\S2HD.exe

using System.Collections.Generic;
using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

#nullable disable
namespace SONICORCA.OBJECTS.CPZTUBECAP;

/// <summary>
/// A cap on the end of a tube in Chemical Plant Zone. A character that hits the top or the bottom as a spinball
/// breaks it. The cap then breaks into four particles and gives points to the player.
/// </summary>
public class CPZTubeCapInstance : ActiveObject
{
	private const int AnimationCap = 0;

	private static readonly IReadOnlyCollection<Vector2> ParticleVelocities = new Vector2[4]
	{
		new Vector2(-4.0, -16.0),
		new Vector2(-3.0, -14.0),
		new Vector2(3.0, -14.0),
		new Vector2(4.0, -16.0)
	};

	private AnimationInstance _animation;
	private bool _broken;
	private Player _playerThatBroke;

	public CPZTubeCapInstance()
	{
		DesignBounds = new Rectanglei(-64, -64, 128, 128);
	}

	protected override void OnStart()
	{
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath("/ANIGROUP"));
		CollisionVectors = CollisionVector.FromRectangle(this, new Rectanglei(-64, -64, 128, 128));
		// The top and the bottom break the cap.
		CollisionVectors[1].Id = 1;
		CollisionVectors[3].Id = 1;
		Priority = -256;
	}

	protected override void OnUpdate()
	{
		if (!_broken)
		{
			return;
		}
		foreach (Vector2 particleVelocity in ParticleVelocities)
		{
			Level.ObjectManager.AddSubObject<CPZTubeCapParticleInstance>(this).Velocity = particleVelocity;
		}
		Level.SoundManager.PlaySound(this, Type.GetAbsolutePath("SONICORCA/SOUND/BREAKABLE"));
		Level.CreateScoreObject(_playerThatBroke.AwardChainedScore(), Position);
		FinishForever();
	}

	protected override void OnCollision(CollisionEvent e)
	{
		if (e.Id != 1 || e.ActiveObject.Type.Classification != ObjectClassification.Character)
		{
			return;
		}
		ICharacter character = (ICharacter)e.ActiveObject;
		if (!character.IsSpinball)
		{
			return;
		}
		_broken = true;
		_playerThatBroke = character.Player;
		// A falling character bounces up.
		if (character.Velocity.Y > 0.0)
		{
			character.Velocity = new Vector2(character.Velocity.X, -12.0);
		}
		e.MaintainVelocity = true;
	}

	protected override void OnAnimate() => _animation.Animate();

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		renderer.GetObjectRenderer().Render(_animation);
	}
}
