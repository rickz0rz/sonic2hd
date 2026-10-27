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
/// import, Super Sonic uses them instead. The setting original_super_sonic in the graphics section turns this off.
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
	/// The height radius of the original Sonic, at four times the size: y_radius $13, and $E in a roll. The original
	/// sprites line up with these.
	/// </summary>
	private const int OriginalCollisionRadiusY = 76;

	private const int OriginalSpinballCollisionRadiusY = 56;

	private int _winningTicks;

	/// <summary>The animation of the original sprites of Super Sonic, or null without them.</summary>
	private AnimationInstance _superAnimation;

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
		}
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();
		if (_superAnimation != null && IsSuper)
			UpdateSuperAnimation();
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
			objectRenderer.RenderOriginal(_superAnimation, new Vector2(0.0, offsetY), IsFacingLeft);
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
	}
}
