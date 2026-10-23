// Decompiled with JetBrains decompiler
// Type: SONICORCA.OBJECTS.SONIC.SonicInstance
// Assembly: S2HD, Version=2.0.1012.10521, Culture=neutral, PublicKeyToken=null
// MVID: 18631A0F-16CF-4E18-8563-1EC5E54750D6
// Assembly location: C:\Users\ricky\Downloads\S2HD_2.0.1012-rc2\S2HD_2.0.1012-rc2\S2HD.exe

using SonicOrca.Core;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

#nullable disable
namespace SONICORCA.OBJECTS.SONIC;

public class SonicInstance : Character
{
	private int _winningTicks;

	protected override void OnStart()
	{
		AnimationGroupResourceKey = Type.GetAbsolutePath("/ANIGROUP");
		base.OnStart();
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();
		if (IsWinning)
		{
			if (_winningTicks == 0)
				Level.SoundManager.PlaySound(this, "SONICORCA/SOUND/PEELOUT/CHARGE");
			else if (_winningTicks == 70)
				Level.SoundManager.PlaySound(this, "SONICORCA/SOUND/PEELOUT/RELEASE");
			++_winningTicks;
		}
		else
			_winningTicks = 0;
	}

	protected override void DrawBody(Renderer renderer, LayerViewOptions viewOptions)
	{
		I2dRenderer obj = renderer.Get2dRenderer();
		ICharacterRenderer characterRenderer = renderer.GetCharacterRenderer();
		characterRenderer.ModelMatrix = obj.ModelMatrix;
		double propertyDouble1 = Level.GameContext.Configuration.GetPropertyDouble("debug", "sonic_hue_shift");
		double propertyDouble2 = Level.GameContext.Configuration.GetPropertyDouble("debug", "sonic_sat_shift");
		double propertyDouble3 = Level.GameContext.Configuration.GetPropertyDouble("debug", "sonic_lum_shift");
		if (DrawBodyRotated)
			characterRenderer.ModelMatrix *= Matrix4.CreateRotationZ(ShowAngle);
		SonicOrca.Graphics.Animation.Frame currentFrame = Animation.CurrentFrame;
		Vector2 offset = (Vector2)currentFrame.Offset;
		Vector2 vector2_1 = new Vector2();
		vector2_1 = !IsSpinball ? new Vector2(offset.X, offset.Y - 16.0) : new Vector2(offset.X, offset.Y - 4.0);
		Vector2 vector2_2 = vector2_1;
		Rectangle destination;
		double x = vector2_2.X - currentFrame.Source.Width / 2;
		double y1 = vector2_2.Y;
		Rectanglei source = currentFrame.Source;
		double num = source.Height / 2;
		double y2 = y1 - num;
		source = currentFrame.Source;
		double width = source.Width;
		source = currentFrame.Source;
		double height = source.Height;
		destination = new Rectangle(x, y2, width, height);
		characterRenderer.Filter = viewOptions.Filter;
		characterRenderer.FilterAmount = viewOptions.FilterAmount;
		characterRenderer.Brightness = viewOptions.Shadows ? 0.0f : Brightness;
		characterRenderer.RenderTexture(Animation.AnimationGroup.Textures[1], Animation.AnimationGroup.Textures[0], propertyDouble1, propertyDouble2, propertyDouble3, (Rectangle)currentFrame.Source, destination, IsFacingRight, IsFacingLeft && Animation.Index == 16 /*0x10*/);
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		base.OnDraw(renderer, viewOptions);
	}
}
