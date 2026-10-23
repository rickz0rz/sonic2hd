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
		CollisionVectors = CollisionVector.FromRectangle(this, (Rectanglei)new Rectangle(-64.0, -64.0, 128.0, 64.0)).Take<CollisionVector>(3).ToArray<CollisionVector>();
		CollisionVectors[1].Id = 1;
		Priority = 1256;
	}

	protected override void OnUpdate()
	{
		bool flag = false;
		foreach (IActiveObject character in Level.ObjectManager.Characters)
		{
			Vector2 position = (Vector2)character.Position;
			if (position.X >= Position.X - 64 /*0x40*/ && position.X <= Position.X + 64 /*0x40*/ && position.Y >= Position.Y - 48 /*0x30*/ && position.Y <= Position.Y + 72)
			{
				flag = true;
				break;
			}
		}
		if (flag)
		{
			_animation.Index = 2;
		}
		else
		{
			if (_animation.Index != 2)
				return;
			_animation.Index = 3;
		}
	}

	protected override void OnCollision(CollisionEvent e)
	{
		if (e.Id != 1 || e.ActiveObject.Type.Classification != ObjectClassification.Character || _animation.Index == 3 || _animation.Index == 2)
			return;
		ICharacter activeObject = (ICharacter)e.ActiveObject;
		activeObject.IsAirborne = true;
		activeObject.IsSpinball = false;
		activeObject.TumbleAngle = 0.0;
		activeObject.TumbleTurns = 1;
		activeObject.GroundVelocity = 1.0;
		ICharacter character = activeObject;
		Vector2 velocity;
		double x;
		if (_bounceVelocity.X == 0.0)
		{
			velocity = activeObject.Velocity;
			x = velocity.X;
		}
		else
			x = _bounceVelocity.X;
		double y;
		if (_bounceVelocity.Y == 0.0)
		{
			velocity = activeObject.Velocity;
			y = velocity.Y;
		}
		else
			y = _bounceVelocity.Y;
		Vector2 vector2 = new Vector2(x, y);
		character.Velocity = vector2;
		activeObject.IsHurt = false;
		_animation.Index = 1;
		e.MaintainVelocity = true;
		Level.SoundManager.PlaySound(this, Type.GetAbsolutePath("SONICORCA/SOUND/SPRING"));
	}

	protected override void OnAnimate() => _animation.Animate();

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		renderer.GetObjectRenderer().Render(_animation, new Vector2(0.0, -66.0));
	}
}
