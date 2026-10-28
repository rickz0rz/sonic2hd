using System;
using SonicOrca.Core;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.SONIC;

/// <summary>
/// Sonic, a playable character. The Character base class has the movement. This class sets the sprites, draws the
/// body with the character shader, and plays the peel-out sounds in the winning pose. Sonic can become Super Sonic.
/// The HD data has no Super Sonic art, so the shader colours the skin texture gold. With the original sprites from the
/// import, Super Sonic uses them instead, with the palette cycle and the stars of the original. The setting
/// original_super_sonic in the graphics section turns this off.
/// </summary>
public class SonicInstance : Character
{
	/// <summary>The Character animation index of the tumble (somersault) animation.</summary>
	private const int AnimationTumble = 16;

	/// <summary>The winning tick on which the peel-out release sound plays.</summary>
	private const int PeeloutReleaseTick = 70;

	/// <summary>
	/// The height radius of Sonic in HD mode. The sprites line up with a collision box of this height. A taller box
	/// moves the centre up, so the sprite moves down by the difference.
	/// </summary>
	private const int HDCollisionRadiusY = 60;

	/// <summary>The animation in the original sprites of Super Sonic after the 30 values of CharacterAnimation.</summary>
	private const int SuperTransformAnimation = 30;

	/// <summary>
	/// The animation in the original sprites of Super Sonic after the transformation: the walk frames plus $20.
	/// SAnim_SuperWalk shows them on each fourth frame. The sprites of an import before version 6 do not have it.
	/// </summary>
	private const int SuperWalkVariantAnimation = 31;

	/// <summary>SAnim_SuperWalk shows the walk variant when the level frame counter is a multiple of this number.</summary>
	private const int SuperWalkVariantTicks = 4;

	/// <summary>
	/// The height radius of the original Sonic, at four times the size: y_radius $13, and $E in a roll. The original
	/// sprites line up with these.
	/// </summary>
	private const int OriginalCollisionRadiusY = 76;

	private const int OriginalSpinballCollisionRadiusY = 56;

	/// <summary>The stars (Obj7E) show their mapping frames 0 to 5, and then start again.</summary>
	private const int StarsFrames = 6;

	/// <summary>The stars come when the ground speed is at least $800.</summary>
	private const double StarsSpeed = 32.0;

	private int _winningTicks;

	/// <summary>The animation of the original sprites of Super Sonic, or null without them.</summary>
	private AnimationInstance _superAnimation;

	/// <summary>The walk variant of the original sprites of Super Sonic, or null without it.</summary>
	private AnimationInstance _superWalkVariant;

	/// <summary>
	/// The palette cycle of the original sprites of Super Sonic, or null without it. Sonic makes it when he first
	/// becomes Super Sonic, because it reads the PNG of the sprites.
	/// </summary>
	private SuperSonicPalette _superPalette;

	private bool _wasSuper;

	/// <summary>The animation of the stars, or null without them. Animation N shows mapping frame N of Obj7E.</summary>
	private AnimationInstance _starsAnimation;

	/// <summary>objoff_30 of Obj7E: the stars show their frames.</summary>
	private bool _starsActive;

	/// <summary>objoff_31 of Obj7E: the stars came again, so they stay where they start.</summary>
	private bool _starsRepeat;

	private bool _starsVisible;

	private int _starsFrame;

	private int _starsTimer;

	private Vector2 _starsPosition;

	protected override void OnStart()
	{
		AnimationGroupResourceKey = Type.GetAbsolutePath(SonicType.AnimationGroupResourceKey);
		HasSuperAbility = true;
		if (Level.OriginalPhysics)
		{
			// The collision box of the original Sonic: x_radius 9 and y_radius $13, and 7 and $E in a roll.
			NormalCollisionRadius = new Vector2i(36, 76);
			SpinballCollisionRadius = new Vector2i(28, 56);
		}
		base.OnStart();
		if (Level.GameContext.Configuration.GetPropertyBoolean("graphics", "original_super_sonic", true) &&
			ResourceTree.TryGetLoadedResource(SonicType.SuperAnimationGroupResourceKey, out AnimationGroup superAnimationGroup))
		{
			_superAnimation = new AnimationInstance(superAnimationGroup);
			if (superAnimationGroup.Count > SuperWalkVariantAnimation)
				_superWalkVariant = new AnimationInstance(superAnimationGroup) { Index = SuperWalkVariantAnimation };
			if (ResourceTree.TryGetLoadedResource(SonicType.SuperStarsAnimationGroupResourceKey, out AnimationGroup starsAnimationGroup))
				_starsAnimation = new AnimationInstance(starsAnimationGroup);
		}
	}

	protected override void OnStop()
	{
		base.OnStop();
		_superPalette?.Dispose();
		_superPalette = null;
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();
		if (_superAnimation != null && IsSuper)
			UpdateSuperAnimation();
		if (IsSuper && !_wasSuper)
		{
			if (_superAnimation != null && _superPalette == null)
				_superPalette = SuperSonicPalette.Create(Level.GameContext.Window.GraphicsContext, _superAnimation.AnimationGroup);
			_superPalette?.Start();
			_starsActive = false;
			_starsRepeat = false;
			_starsTimer = 0;
		}
		_wasSuper = IsSuper;
		if (IsSuper)
		{
			_superPalette?.Update();
			if (_starsAnimation != null)
				UpdateStars();
		}
		if (IsWinning)
		{
			if (_winningTicks == 0)
				Level.SoundManager.PlaySound(this, SonicType.PeeloutChargeSoundResourceKey);
			else if (_winningTicks == PeeloutReleaseTick)
				Level.SoundManager.PlaySound(this, SonicType.PeeloutReleaseSoundResourceKey);
			++_winningTicks;
		}
		else
			_winningTicks = 0;
	}

	/// <summary>
	/// Plays the same animation as the HD sprites in the original sprites. The animations have the same index, but
	/// their frames differ, so the original sprites have their own animation instance.
	/// </summary>
	private void UpdateSuperAnimation()
	{
		if (IsTransformingToSuper)
		{
			_superAnimation.Index = SuperTransformAnimation;
			_superAnimation.Animate();
			return;
		}
		_superAnimation.Index = Animation.Index;
		if (Animation.Index == AnimationTumble)
		{
			// The tumble frame comes from the tumble angle: use the same part of the animation
			int hdFrames = Animation.AnimationGroup[Animation.Index].Frames.Count;
			int superFrames = _superAnimation.AnimationGroup[AnimationTumble].Frames.Count;
			_superAnimation.CurrentFrameIndex = hdFrames > 1 ? Animation.CurrentFrameIndex * (superFrames - 1) / (hdFrames - 1) : 0;
			return;
		}
		_superAnimation.OverrideDelay = Animation.OverrideDelay;
		_superAnimation.Animate();
	}

	/// <summary>
	/// Gets the animation instance that shows the current frame of Super Sonic. SAnim_SuperWalk adds $20 to the walk
	/// frame when the level frame counter is a multiple of 4, so the walk then shows the frame of the variant.
	/// </summary>
	private AnimationInstance GetSuperAnimationToDraw()
	{
		bool walk = _superAnimation.Index == (int)CharacterAnimation.Walk || _superAnimation.Index == (int)CharacterAnimation.Postspring;
		if (_superWalkVariant == null || !walk || IsTransformingToSuper || Level.Ticks % SuperWalkVariantTicks != 0)
			return _superAnimation;
		_superWalkVariant.CurrentFrameIndex = _superAnimation.CurrentFrameIndex;
		return _superWalkVariant;
	}

	/// <summary>
	/// Runs the stars of Super Sonic (Obj7E) for one tick. At a high speed, the stars show their frames at Sonic. When
	/// they come again while the speed stays high, they stay where they start, so they make a trail.
	/// </summary>
	private void UpdateStars()
	{
		_starsVisible = false;
		if (_starsActive)
		{
			if (--_starsTimer < 0)
			{
				_starsTimer = 1;
				if (++_starsFrame >= StarsFrames)
				{
					_starsFrame = 0;
					_starsActive = false;
					_starsRepeat = true;
					return;
				}
			}
		}
		else if (!IsObjectControlled && !IsTransformingToSuper && Math.Abs(GroundVelocity) >= StarsSpeed)
		{
			_starsFrame = 0;
			_starsActive = true;
			_starsPosition = PositionPrecise;
			_starsVisible = true;
			return;
		}
		else
		{
			_starsRepeat = false;
			return;
		}
		if (!_starsRepeat)
			_starsPosition = PositionPrecise;
		_starsVisible = true;
	}

	/// <summary>
	/// Draws the original sprite of Super Sonic. The original art faces right, and the HD art faces left. The sprite
	/// lines up with the collision box of the original, so it moves by the difference to the box of the character.
	/// </summary>
	private void DrawOriginalSuperSonic(Renderer renderer)
	{
		IObjectRenderer objectRenderer = renderer.GetObjectRenderer();
		using (objectRenderer.BeginMatixState())
		{
			if (DrawBodyRotated)
				objectRenderer.ModelMatrix *= Matrix4.CreateRotationZ(ShowAngle);
			double offsetY = IsSpinball ? SpinballCollisionRadius.Y - OriginalSpinballCollisionRadiusY : NormalCollisionRadius.Y - OriginalCollisionRadiusY;
			AnimationInstance animation = GetSuperAnimationToDraw();
			if (_superPalette != null)
				_superPalette.Render(objectRenderer, animation, new Vector2(0.0, offsetY), IsFacingLeft);
			else
				objectRenderer.RenderOriginal(animation, new Vector2(0.0, offsetY), IsFacingLeft);
		}
	}

	protected override void DrawBody(Renderer renderer, LayerViewOptions viewOptions)
	{
		if (_superAnimation != null && IsSuper)
		{
			DrawOriginalSuperSonic(renderer);
			return;
		}
		I2dRenderer renderer2d = renderer.Get2dRenderer();
		ICharacterRenderer characterRenderer = renderer.GetCharacterRenderer();
		characterRenderer.ModelMatrix = renderer2d.ModelMatrix;
		double hueShift = Level.GameContext.Configuration.GetPropertyDouble("debug", "sonic_hue_shift");
		double saturationShift = Level.GameContext.Configuration.GetPropertyDouble("debug", "sonic_sat_shift");
		double luminanceShift = Level.GameContext.Configuration.GetPropertyDouble("debug", "sonic_lum_shift");
		(double superHue, double superSaturation, double superLuminance) = GetSuperColourShift();
		hueShift += superHue;
		saturationShift += superSaturation;
		luminanceShift += superLuminance;
		if (DrawBodyRotated)
			characterRenderer.ModelMatrix *= Matrix4.CreateRotationZ(ShowAngle);
		SonicOrca.Graphics.Animation.Frame currentFrame = Animation.CurrentFrame;
		Vector2 offset = (Vector2)currentFrame.Offset;

		// Move the sprite up, so that it lines up with the collision box. A spinball needs less offset.
		double standingOffsetY = -16.0 + (NormalCollisionRadius.Y - HDCollisionRadiusY);
		Vector2 origin = !IsSpinball ? new Vector2(offset.X, offset.Y + standingOffsetY) : new Vector2(offset.X, offset.Y - 4.0);
		Rectangle destination = new Rectangle(
			origin.X - currentFrame.Source.Width / 2,
			origin.Y - currentFrame.Source.Height / 2,
			currentFrame.Source.Width,
			currentFrame.Source.Height);
		characterRenderer.Filter = viewOptions.Filter;
		characterRenderer.FilterAmount = viewOptions.FilterAmount;
		characterRenderer.Brightness = viewOptions.Shadows ? 0.0f : Brightness;

		// Texture 1 is the skin texture and texture 0 is the body texture.
		characterRenderer.RenderTexture(Animation.AnimationGroup.Textures[1], Animation.AnimationGroup.Textures[0], hueShift, saturationShift, luminanceShift, (Rectangle)currentFrame.Source, destination, IsFacingRight, IsFacingLeft && Animation.Index == AnimationTumble);
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		base.OnDraw(renderer, viewOptions);
		// Obj7E has a higher priority than Sonic, so the stars are in front of him
		if (_starsAnimation != null && IsSuper && _starsVisible && !viewOptions.Shadows)
		{
			_starsAnimation.Index = _starsFrame;
			renderer.GetObjectRenderer().RenderOriginal(_starsAnimation, _starsPosition - PositionPrecise);
		}
	}
}
