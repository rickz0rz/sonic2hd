// Decompiled with JetBrains decompiler
// Type: SONICORCA.OBJECTS.SOL.SolInstance
// Assembly: S2HD, Version=2.0.1012.10521, Culture=neutral, PublicKeyToken=null
// MVID: 18631A0F-16CF-4E18-8563-1EC5E54750D6
// Assembly location: C:\Users\ricky\Downloads\S2HD_2.0.1012-rc2\S2HD_2.0.1012-rc2\S2HD.exe

using System;
using SonicOrca;
using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

#nullable disable
namespace SONICORCA.OBJECTS.SOL;

public class SolInstance : Badnik
{
	private const int AnimationSol = 0;
	private const int AnimationFireball = 2;
	private AnimationInstance _animation;
	private bool _waitingForCharacter;

	public SolInstance() => DesignBounds = new Rectanglei(-36, -36, 72, 72);

	protected override void OnStart()
	{
		base.OnStart();
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath("/ANIGROUP"));
		CollisionRectangles = new CollisionRectangle[1]
		{
	  new CollisionRectangle( this, 0, -36, -36, 72, 72)
		};
		_waitingForCharacter = true;
		int num = 0;
		for (int index = 0; index < 4; ++index)
		{
			SolInstance.Fireball fireball = Level.ObjectManager.AddSubObject<SolInstance.Fireball>(this);
			fireball.Angle = num;
			fireball.UpdateOrbitPosition();
			num += 64 /*0x40*/;
		}
	}

	protected override void OnStop()
	{
		base.OnStop();
		if (_waitingForCharacter)
			return;
		FinishForever();
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();
		Position = Position + new Vector2i(-1, 0);
		if (!_waitingForCharacter || !IsCharacterCloseEnough(Level.Player.Protagonist))
			return;
		_waitingForCharacter = false;
	}

	private bool IsCharacterCloseEnough(ICharacter character)
	{
		if (character.IsDebug)
			return false;
		Vector2i vector2i = character.Position - Position;
		vector2i.X = Math.Abs(vector2i.X);
		vector2i.Y = Math.Abs(vector2i.Y);
		return vector2i.X < 640 && vector2i.Y < 320;
	}

	protected override void OnAnimate() => _animation.Animate();

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		renderer.GetObjectRenderer().Render(_animation);
	}

	private class Fireball : Enemy
	{
		private AnimationInstance _animation;
		private bool _fired;

		public int Angle { get; set; }

		public SolInstance Sol => (SolInstance)ParentObject;

		protected override void OnStart()
		{
			base.OnStart();
			_animation = new AnimationInstance(Sol._animation.AnimationGroup, 2);
			CollisionRectangles = new CollisionRectangle[1]
			{
		new CollisionRectangle( this, 0, -8, -8, 16 /*0x10*/, 16 /*0x10*/)
			};
			LockLifetime = true;
		}

		protected override void OnUpdate()
		{
			base.OnUpdate();
			if (!_fired)
			{
				if (Sol.Finished)
					Finish();
				else if (Sol._waitingForCharacter || Angle != 64 /*0x40*/)
				{
					Angle = MathX.Wrap(Angle + 1, 256 /*0x0100*/);
					UpdateOrbitPosition();
				}
				else
				{
					_fired = true;
					LockLifetime = false;
				}
			}
			else
				PositionPrecise = PositionPrecise + new Vector2(-8.0, 0.0);
		}

		protected override void OnUpdateEditor()
		{
			base.OnUpdateEditor();
			UpdateOrbitPosition();
		}

		public void UpdateOrbitPosition()
		{
			double num = Angle * (Math.PI / 128.0);
			PositionPrecise = Sol.PositionPrecise + new Vector2(64.0 * Math.Cos(num), 64.0 * Math.Sin(num));
		}

		protected override void OnAnimate() => _animation.Animate();

		protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
		{
			IObjectRenderer objectRenderer = renderer.GetObjectRenderer();
			objectRenderer.EmitsLight = true;
			objectRenderer.Render(_animation);
		}
	}
}
