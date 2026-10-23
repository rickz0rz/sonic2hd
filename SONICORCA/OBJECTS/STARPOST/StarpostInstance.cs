// Decompiled with JetBrains decompiler
// Type: SONICORCA.OBJECTS.STARPOST.StarpostInstance
// Assembly: S2HD, Version=2.0.1012.10521, Culture=neutral, PublicKeyToken=null
// MVID: 18631A0F-16CF-4E18-8563-1EC5E54750D6
// Assembly location: C:\Users\ricky\Downloads\S2HD_2.0.1012-rc2\S2HD_2.0.1012-rc2\S2HD.exe

using System;
using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

#nullable disable
namespace SONICORCA.OBJECTS.STARPOST;

public class StarpostInstance : ActiveObject
{
	public const int AnimationStand = 0;
	public const int AnimationStarInactive = 1;
	public const int AnimationStarActive = 2;
	public const int AnimationStarActiveGlow = 3;
	private static double StarAxelRadius = 44.0;
	private static Vector2 StarAxelMidpoint = new Vector2(0.0, -52.0);
	private AnimationInstance _standAnimation;
	private AnimationInstance _starAnimation;
	private AnimationInstance _starGlowAnimation;
	private Vector2 _starPosition;
	private int _index;
	private bool _activated;
	private bool _glowing;
	private bool _starSpinning;
	private double _starAngle;

	[StateVariable]
	private int Index
	{
		get => _index;
		set => _index = value;
	}

	public StarpostInstance() => DesignBounds = new Rectanglei(-50, -150, 100, 300);

	protected override void OnStart()
	{
		AnimationGroup loadedResource = Level.GameContext.ResourceTree.GetLoadedResource<AnimationGroup>(Type, "/ANIGROUP");
		_standAnimation = new AnimationInstance(loadedResource);
		_starAnimation = new AnimationInstance(loadedResource);
		_starGlowAnimation = new AnimationInstance(loadedResource);
		_standAnimation.Index = 0;
		_starAnimation.Index = 1;
		_starGlowAnimation.Index = 3;
		CollisionRectangles = new CollisionRectangle[1]
		{
	  new CollisionRectangle( this, 0, -32,  sbyte.MinValue, 64 /*0x40*/, 256 /*0x0100*/)
		};
		_activated = Level.Player.IsStarpostActivated(_index);
		_glowing = _activated;
		UpdateStarPosition();
	}

	protected override void OnUpdate()
	{
		if (!_activated && Level.Player.IsStarpostActivated(_index))
		{
			_activated = true;
			_glowing = true;
		}
		if (_starSpinning)
		{
			_starAngle += 2.0 * Math.PI / 15.0;
			if (_starAngle >= 4.0 * Math.PI)
			{
				_starAngle = 0.0;
				_starSpinning = false;
				_glowing = true;
			}
		}
		UpdateStarPosition();
	}

	private void UpdateStarPosition()
	{
		_starPosition = new Vector2(StarpostInstance.StarAxelMidpoint.X + Math.Sin(_starAngle) * StarpostInstance.StarAxelRadius, StarpostInstance.StarAxelMidpoint.Y - Math.Cos(_starAngle) * StarpostInstance.StarAxelRadius);
	}

	protected override void OnCollision(CollisionEvent e)
	{
		if (_activated || e.ActiveObject.Type.Classification != ObjectClassification.Character)
			return;
		ICharacter activeObject = (ICharacter)e.ActiveObject;
		if (activeObject.IsSidekick)
			return;
		activeObject.Player.ActivateStarpost(_index, Position + new Vector2i(0, 64 /*0x40*/));
		_activated = true;
		_starSpinning = true;
		Level.SoundManager.PlaySound(this, Type.GetAbsolutePath("SONICORCA/SOUND/STARPOST"));
	}

	protected override void OnAnimate()
	{
		if (_glowing)
		{
			_starAnimation.Index = 2;
			_starGlowAnimation.CurrentFrameIndex = _starAnimation.CurrentFrameIndex;
		}
		_standAnimation.Animate();
		_starAnimation.Animate();
		_starGlowAnimation.Animate();
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		IObjectRenderer objectRenderer = renderer.GetObjectRenderer();
		objectRenderer.Render(_standAnimation);
		objectRenderer.Render(_starAnimation, _starPosition);
		if (!_glowing)
			return;
		objectRenderer.BlendMode = BlendMode.Additive;
		objectRenderer.EmitsLight = true;
		objectRenderer.Render(_starGlowAnimation, _starPosition);
	}
}
