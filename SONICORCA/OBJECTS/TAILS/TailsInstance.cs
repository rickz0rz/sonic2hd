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

public class TailsInstance : Character
{
	private AnimationInstance _tailAnimationInstance;
	private AnimationInstance _headAnimationInstance;

	protected override void OnStart()
	{
		AnimationGroupResourceKey = Type.GetAbsolutePath("/ANIGROUP");
		AnimationGroup loadedResource1 = ResourceTree.GetLoadedResource<AnimationGroup>(Type, "/TAIL/ANIGROUP");
		AnimationGroup loadedResource2 = ResourceTree.GetLoadedResource<AnimationGroup>(Type, "/HEAD/ANIGROUP");
		_tailAnimationInstance = new AnimationInstance(loadedResource1);
		_headAnimationInstance = new AnimationInstance(loadedResource2);
		NormalCollisionRadius = new Vector2i(40, 60);
		CanFly = true;
		base.OnStart();
	}

	protected override void OnAnimate()
	{
		base.OnAnimate();
		int index = _tailAnimationInstance.Index;
		int num;
		switch (Animation.Index)
		{
			case 0:
			case 1:
			case 2:
				num = 0;
				break;
			case 3:
				num = 1;
				break;
			case 4:
				num = 1;
				break;
			case 5:
				num = 1;
				break;
			case 6:
				num = 0;
				break;
			case 9:
				num = 1;
				break;
			case 10:
				num = 1;
				break;
			case 11:
				num = 2;
				break;
			default:
				num = -1;
				break;
		}
		_tailAnimationInstance.Index = num;
		if (num != -1)
			_tailAnimationInstance.Animate();
		if (Animation.Index == 24)
		{
			_headAnimationInstance.Index = 0;
			_headAnimationInstance.Animate();
		}
		else
			_headAnimationInstance.Index = -1;
	}

	protected override void DrawBody(Renderer renderer, LayerViewOptions viewOptions)
	{
		I2dRenderer obj = renderer.Get2dRenderer();
		CharacterRenderer characterRenderer = CharacterRenderer.FromRenderer(renderer);
		characterRenderer.ModelMatrix = obj.ModelMatrix;
		double propertyDouble1 = Level.GameContext.Configuration.GetPropertyDouble("debug", "tails_hue_shift");
		double propertyDouble2 = Level.GameContext.Configuration.GetPropertyDouble("debug", "tails_sat_shift");
		double propertyDouble3 = Level.GameContext.Configuration.GetPropertyDouble("debug", "tails_lum_shift");
		characterRenderer.Filter = viewOptions.Filter;
		characterRenderer.FilterAmount = viewOptions.FilterAmount;
		characterRenderer.Brightness = Brightness;
		if (DrawBodyRotated)
			characterRenderer.ModelMatrix *= Matrix4.CreateRotationZ(ShowAngle);
		Vector2 offset;
		Rectangle destination;
		Rectanglei source;
		if (_tailAnimationInstance.Index != -1)
		{
			SonicOrca.Graphics.Animation.Frame currentFrame = _tailAnimationInstance.CurrentFrame;
			offset = (Vector2)currentFrame.Offset;
			double x = offset.X - currentFrame.Source.Width / 2;
			double y = offset.Y - currentFrame.Source.Height / 2;
			double width = currentFrame.Source.Width;
			source = currentFrame.Source;
			double height = source.Height;
			destination = new Rectangle(x, y, width, height);
			if (Animation.Index == 10)
			{
				double angle = Math.Atan2(Velocity.Y, Velocity.X);
				characterRenderer.ModelMatrix *= Matrix4.CreateRotationZ(angle);
				characterRenderer.RenderTexture(_tailAnimationInstance.AnimationGroup.Textures[1], _tailAnimationInstance.AnimationGroup.Textures[0], propertyDouble1, propertyDouble2, propertyDouble3, (Rectangle)currentFrame.Source, destination, true, Velocity.X < 0.0);
				characterRenderer.ModelMatrix = obj.ModelMatrix;
			}
			else
				characterRenderer.RenderTexture(_tailAnimationInstance.AnimationGroup.Textures[1], _tailAnimationInstance.AnimationGroup.Textures[0], propertyDouble1, propertyDouble2, propertyDouble3, (Rectangle)currentFrame.Source, destination, IsFacingRight, false);
		}
		SonicOrca.Graphics.Animation.Frame currentFrame1 = Animation.CurrentFrame;
		offset = (Vector2)currentFrame1.Offset;
		double x1 = offset.X;
		source = currentFrame1.Source;
		double num1 = source.Width / 2;
		double x2 = x1 - num1;
		double y1 = offset.Y;
		source = currentFrame1.Source;
		double num2 = source.Height / 2;
		double y2 = y1 - num2;
		source = currentFrame1.Source;
		double width1 = source.Width;
		source = currentFrame1.Source;
		double height1 = source.Height;
		destination = new Rectangle(x2, y2, width1, height1);
		characterRenderer.RenderTexture(Animation.AnimationGroup.Textures[1], Animation.AnimationGroup.Textures[0], propertyDouble1, propertyDouble2, propertyDouble3, (Rectangle)currentFrame1.Source, destination, IsFacingRight, IsFacingLeft && Animation.Index == 16 /*0x10*/);
		if (_headAnimationInstance.Index == -1)
			return;
		SonicOrca.Graphics.Animation.Frame currentFrame2 = _headAnimationInstance.CurrentFrame;
		offset = (Vector2)currentFrame2.Offset;
		ref Rectangle local2 = ref destination;
		double x3 = offset.X;
		source = currentFrame2.Source;
		double num3 = source.Width / 2;
		double x4 = x3 - num3;
		double y3 = offset.Y;
		source = currentFrame2.Source;
		double num4 = source.Height / 2;
		double y4 = y3 - num4;
		source = currentFrame2.Source;
		double width2 = source.Width;
		source = currentFrame2.Source;
		double height2 = source.Height;
		local2 = new Rectangle(x4, y4, width2, height2);
		characterRenderer.RenderTexture(_headAnimationInstance.AnimationGroup.Textures[1], _headAnimationInstance.AnimationGroup.Textures[0], propertyDouble1, propertyDouble2, propertyDouble3, (Rectangle)currentFrame2.Source, destination, IsFacingRight, false);
	}
}
