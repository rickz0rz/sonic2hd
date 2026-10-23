using SonicOrca.Core;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.SONIC;

/// <summary>
/// Sonic, a playable character. The Character base class has the movement. This class sets the sprites, draws the
/// body with the character shader, and plays the peel-out sounds in the winning pose.
/// </summary>
public class SonicInstance : Character
{
	/// <summary>The Character animation index of the tumble (somersault) animation.</summary>
	private const int AnimationTumble = 16;

	/// <summary>The winning tick on which the peel-out release sound plays.</summary>
	private const int PeeloutReleaseTick = 70;

	private int _winningTicks;

	protected override void OnStart()
	{
		AnimationGroupResourceKey = Type.GetAbsolutePath(SonicType.AnimationGroupResourceKey);
		base.OnStart();
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();
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

	protected override void DrawBody(Renderer renderer, LayerViewOptions viewOptions)
	{
		I2dRenderer renderer2d = renderer.Get2dRenderer();
		ICharacterRenderer characterRenderer = renderer.GetCharacterRenderer();
		characterRenderer.ModelMatrix = renderer2d.ModelMatrix;
		double hueShift = Level.GameContext.Configuration.GetPropertyDouble("debug", "sonic_hue_shift");
		double saturationShift = Level.GameContext.Configuration.GetPropertyDouble("debug", "sonic_sat_shift");
		double luminanceShift = Level.GameContext.Configuration.GetPropertyDouble("debug", "sonic_lum_shift");
		if (DrawBodyRotated)
			characterRenderer.ModelMatrix *= Matrix4.CreateRotationZ(ShowAngle);
		SonicOrca.Graphics.Animation.Frame currentFrame = Animation.CurrentFrame;
		Vector2 offset = (Vector2)currentFrame.Offset;

		// Move the sprite up, so that it lines up with the collision box. A spinball needs less offset.
		Vector2 origin = !IsSpinball ? new Vector2(offset.X, offset.Y - 16.0) : new Vector2(offset.X, offset.Y - 4.0);
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
