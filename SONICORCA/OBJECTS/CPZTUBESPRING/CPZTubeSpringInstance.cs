// Decompiled with JetBrains decompiler
// Type: SONICORCA.OBJECTS.CPZTUBESPRING.CPZTubeSpringInstance
// Assembly: S2HD, Version=2.0.1012.10521, Culture=neutral, PublicKeyToken=null
// MVID: 18631A0F-16CF-4E18-8563-1EC5E54750D6
// Assembly location: C:\Users\ricky\Downloads\S2HD_2.0.1012-rc2\S2HD_2.0.1012-rc2\S2HD.exe

using System.Linq;
using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

#nullable disable
namespace SONICORCA.OBJECTS.CPZTUBESPRING;

/// <summary>
/// A spring in a tube in Chemical Plant Zone. It opens when a character comes near.
/// A character that lands on the shut spring bounces high up into the air.
/// </summary>
public class CPZTubeSpringInstance : ActiveObject
{
	private const int AnimationStationary = 0;
	private const int AnimationBounce = 1;
	private const int AnimationOpen = 2;
	private const int AnimationClose = 3;
	private AnimationInstance _animation;
	private Vector2 _bounceVelocity;

	public CPZTubeSpringInstance() => DesignBounds = new Rectanglei(-67, -66, 134, 132);

	protected override void OnStart()
	{
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath("/ANIGROUP"));
		_bounceVelocity = new Vector2(0.0, -42.0);
		// Use the left, top and right sides only.
		CollisionVectors = CollisionVector.FromRectangle(this, (Rectanglei)new Rectangle(-64.0, -64.0, 128.0, 64.0)).Take(3).ToArray();
		CollisionVectors[1].Id = 1;
		Priority = 1256;
	}

	protected override void OnUpdate()
	{
		bool characterNear = false;
		foreach (IActiveObject character in Level.ObjectManager.Characters)
		{
			Vector2 position = (Vector2)character.Position;
			if (position.X >= Position.X - 64 && position.X <= Position.X + 64 && position.Y >= Position.Y - 48 && position.Y <= Position.Y + 72)
			{
				characterNear = true;
				break;
			}
		}
		if (characterNear)
		{
			_animation.Index = AnimationOpen;
		}
		else if (_animation.Index == AnimationOpen)
		{
			_animation.Index = AnimationClose;
		}
	}

	protected override void OnCollision(CollisionEvent e)
	{
		// The top bounces the character, but only while the spring is shut.
		if (e.Id != 1 || e.ActiveObject.Type.Classification != ObjectClassification.Character || _animation.Index == AnimationClose || _animation.Index == AnimationOpen)
		{
			return;
		}
		ICharacter character = (ICharacter)e.ActiveObject;
		character.IsAirborne = true;
		character.IsSpinball = false;
		character.TumbleAngle = 0.0;
		character.TumbleTurns = 1;
		character.GroundVelocity = 1.0;
		// A zero component of the bounce velocity keeps that component of the character velocity.
		double velocityX = _bounceVelocity.X == 0.0 ? character.Velocity.X : _bounceVelocity.X;
		double velocityY = _bounceVelocity.Y == 0.0 ? character.Velocity.Y : _bounceVelocity.Y;
		character.Velocity = new Vector2(velocityX, velocityY);
		character.IsHurt = false;
		_animation.Index = AnimationBounce;
		e.MaintainVelocity = true;
		Level.SoundManager.PlaySound(this, Type.GetAbsolutePath("SONICORCA/SOUND/SPRING"));
	}

	protected override void OnAnimate() => _animation.Animate();

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		renderer.GetObjectRenderer().Render(_animation, new Vector2(0.0, -66.0));
	}
}
