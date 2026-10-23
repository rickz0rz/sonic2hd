// Decompiled with JetBrains decompiler
// Type: SONICORCA.OBJECTS.TAILS.TailsInstance
// Assembly: S2HD, Version=2.0.1012.10521, Culture=neutral, PublicKeyToken=null
// MVID: 18631A0F-16CF-4E18-8563-1EC5E54750D6
// Assembly location: C:\Users\ricky\Downloads\S2HD_2.0.1012-rc2\S2HD_2.0.1012-rc2\S2HD.exe

using System;
using SonicOrca.Core;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Drawing.Renderers;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;
using SonicOrca.Resources;

#nullable disable
namespace SONICORCA.OBJECTS.TAILS;

/// <summary>
/// Tails, a playable character that can fly. The Character base class has the movement. This class draws the body
/// with two more sprites: the tails, which are separate for some body animations, and a second head sprite.
/// </summary>
public class TailsInstance : Character
{
	// Character animation indices. They are the values that Character.OnAnimate sets.
	private const int AnimationStanding = 0;
	private const int AnimationLookUp = 1;
	private const int AnimationDuck = 2;
	private const int AnimationBalanceFar = 3;
	private const int AnimationBalance = 4;
	private const int AnimationBalanceBack = 5;
	private const int AnimationPush = 6;
	private const int AnimationBrake = 9;
	private const int AnimationSpinball = 10;
	private const int AnimationSpindash = 11;
	private const int AnimationTumble = 16;
	private const int AnimationGrabbed = 24;

	/// <summary>The index that hides the tail or head sprite.</summary>
	private const int NoAnimation = -1;

	private AnimationInstance _tailAnimationInstance;
	private AnimationInstance _headAnimationInstance;

	protected override void OnStart()
	{
		AnimationGroupResourceKey = Type.GetAbsolutePath(TailsType.AnimationGroupResourceKey);
		AnimationGroup tailAnimationGroup = ResourceTree.GetLoadedResource<AnimationGroup>(Type, TailsType.TailAnimationGroupResourceKey);
		AnimationGroup headAnimationGroup = ResourceTree.GetLoadedResource<AnimationGroup>(Type, TailsType.HeadAnimationGroupResourceKey);
		_tailAnimationInstance = new AnimationInstance(tailAnimationGroup);
		_headAnimationInstance = new AnimationInstance(headAnimationGroup);
		NormalCollisionRadius = new Vector2i(40, 60);
		CanFly = true;
		base.OnStart();
	}

	protected override void OnAnimate()
	{
		base.OnAnimate();

		// Select the tail animation that goes with the body animation. The other body animations include the tails.
		int tailAnimationIndex;
		switch (Animation.Index)
		{
			case AnimationStanding:
			case AnimationLookUp:
			case AnimationDuck:
			case AnimationPush:
				tailAnimationIndex = 0;
				break;
			case AnimationBalanceFar:
			case AnimationBalance:
			case AnimationBalanceBack:
			case AnimationBrake:
			case AnimationSpinball:
				tailAnimationIndex = 1;
				break;
			case AnimationSpindash:
				tailAnimationIndex = 2;
				break;
			default:
				tailAnimationIndex = NoAnimation;
				break;
		}
		_tailAnimationInstance.Index = tailAnimationIndex;
		if (tailAnimationIndex != NoAnimation)
			_tailAnimationInstance.Animate();
		if (Animation.Index == AnimationGrabbed)
		{
			_headAnimationInstance.Index = 0;
			_headAnimationInstance.Animate();
		}
		else
			_headAnimationInstance.Index = NoAnimation;
	}

	protected override void DrawBody(Renderer renderer, LayerViewOptions viewOptions)
	{
		I2dRenderer renderer2d = renderer.Get2dRenderer();
		CharacterRenderer characterRenderer = CharacterRenderer.FromRenderer(renderer);
		characterRenderer.ModelMatrix = renderer2d.ModelMatrix;
		double hueShift = Level.GameContext.Configuration.GetPropertyDouble("debug", "tails_hue_shift");
		double saturationShift = Level.GameContext.Configuration.GetPropertyDouble("debug", "tails_sat_shift");
		double luminanceShift = Level.GameContext.Configuration.GetPropertyDouble("debug", "tails_lum_shift");
		characterRenderer.Filter = viewOptions.Filter;
		characterRenderer.FilterAmount = viewOptions.FilterAmount;
		characterRenderer.Brightness = Brightness;
		if (DrawBodyRotated)
			characterRenderer.ModelMatrix *= Matrix4.CreateRotationZ(ShowAngle);

		// Texture 1 of each animation group is the skin texture and texture 0 is the body texture.
		if (_tailAnimationInstance.Index != NoAnimation)
		{
			SonicOrca.Graphics.Animation.Frame tailFrame = _tailAnimationInstance.CurrentFrame;
			Rectangle tailDestination = GetDestination(tailFrame);
			if (Animation.Index == AnimationSpinball)
			{
				// In a spinball, turn the tails to the direction of movement.
				double angle = Math.Atan2(Velocity.Y, Velocity.X);
				characterRenderer.ModelMatrix *= Matrix4.CreateRotationZ(angle);
				characterRenderer.RenderTexture(_tailAnimationInstance.AnimationGroup.Textures[1], _tailAnimationInstance.AnimationGroup.Textures[0], hueShift, saturationShift, luminanceShift, (Rectangle)tailFrame.Source, tailDestination, true, Velocity.X < 0.0);
				characterRenderer.ModelMatrix = renderer2d.ModelMatrix;
			}
			else
				characterRenderer.RenderTexture(_tailAnimationInstance.AnimationGroup.Textures[1], _tailAnimationInstance.AnimationGroup.Textures[0], hueShift, saturationShift, luminanceShift, (Rectangle)tailFrame.Source, tailDestination, IsFacingRight, false);
		}
		SonicOrca.Graphics.Animation.Frame bodyFrame = Animation.CurrentFrame;
		Rectangle bodyDestination = GetDestination(bodyFrame);
		characterRenderer.RenderTexture(Animation.AnimationGroup.Textures[1], Animation.AnimationGroup.Textures[0], hueShift, saturationShift, luminanceShift, (Rectangle)bodyFrame.Source, bodyDestination, IsFacingRight, IsFacingLeft && Animation.Index == AnimationTumble);
		if (_headAnimationInstance.Index == NoAnimation)
			return;
		SonicOrca.Graphics.Animation.Frame headFrame = _headAnimationInstance.CurrentFrame;
		Rectangle headDestination = GetDestination(headFrame);
		characterRenderer.RenderTexture(_headAnimationInstance.AnimationGroup.Textures[1], _headAnimationInstance.AnimationGroup.Textures[0], hueShift, saturationShift, luminanceShift, (Rectangle)headFrame.Source, headDestination, IsFacingRight, false);
	}

	/// <summary>Gets the destination rectangle of a frame, centred on the frame offset.</summary>
	private static Rectangle GetDestination(SonicOrca.Graphics.Animation.Frame frame)
	{
		Vector2 offset = (Vector2)frame.Offset;
		return new Rectangle(
			offset.X - frame.Source.Width / 2,
			offset.Y - frame.Source.Height / 2,
			frame.Source.Width,
			frame.Source.Height);
	}
}
