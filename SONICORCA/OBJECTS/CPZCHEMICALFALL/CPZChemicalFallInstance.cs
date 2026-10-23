// Decompiled with JetBrains decompiler
// Type: SONICORCA.OBJECTS.CPZCHEMICALFALL.CPZChemicalFallInstance
// Assembly: S2HD, Version=2.0.1012.10521, Culture=neutral, PublicKeyToken=null
// MVID: 18631A0F-16CF-4E18-8563-1EC5E54750D6
// Assembly location: C:\Users\ricky\Downloads\S2HD_2.0.1012-rc2\S2HD_2.0.1012-rc2\S2HD.exe

using System;
using System.Collections.Generic;
using SonicOrca.Core;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

#nullable disable
namespace SONICORCA.OBJECTS.CPZCHEMICALFALL;

public class CPZChemicalFallInstance : ActiveObject
{
	private const int AnimationCorner = 0;
	private const int AnimationVertical = 1;
	private const int AnimationDrop = 2;
	private AnimationInstance _animationCorner;
	private AnimationInstance _animationVertical;
	private int _bottom;

	protected override void OnStart()
	{
		AnimationGroup loadedResource = ResourceTree.GetLoadedResource<AnimationGroup>(Type.GetAbsolutePath("/ANIGROUP"));
		_animationCorner = new AnimationInstance(loadedResource);
		_animationVertical = new AnimationInstance(loadedResource, 1);
		DesignBounds = new Rectanglei(-32, -32, 64 /*0x40*/, 128 /*0x80*/);
		Priority = -64;
	}

	protected override void OnUpdateEditor() => UpdateBottom();

	protected override void OnUpdate()
	{
		UpdateBottom();
		CreateDrops();
	}

	protected override void OnAnimate()
	{
		_animationCorner.Animate();
		_animationVertical.Animate();
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		IObjectRenderer objectRenderer = renderer.GetObjectRenderer();
		objectRenderer.Render(_animationCorner, new Vector2(0.0, 0.0));
		objectRenderer.Texture = _animationVertical.CurrentTexture;
		int y = _animationCorner.CurrentFrame.Source.Height / 2;
		Rectanglei source = _animationVertical.CurrentFrame.Source;
		Rectanglei destination = new Rectanglei(-source.Width / 2, y, source.Width, source.Height);
		for (int index = _bottom - Position.Y; destination.Y < index; destination.Y += 64 /*0x40*/)
		{
			int num = destination.Bottom - index;
			source.Height -= num;
			destination.Height -= num;
			objectRenderer.Render((Rectangle)source, (Rectangle)destination);
		}
	}

	private void UpdateBottom()
	{
		int y = Position.Y;
		int num1 = Position.X - 32 /*0x20*/;
		int num2 = Position.Y + 32 /*0x20*/;
		int val1 = Level.Map.Bounds.Height;
		foreach (Rectanglei waterArea in (IEnumerable<Rectanglei>)Level.WaterManager.WaterAreas)
		{
			if (waterArea.Left <= num2 && waterArea.Right >= num1 && waterArea.Bottom >= y)
				val1 = Math.Min(val1, waterArea.Top);
		}
		_bottom = val1;
	}

	private void CreateDrops()
	{
		if (Level.Ticks % 30 != 0)
			return;
		Random random = Level.Random;
		int y = Position.Y + 64 /*0x40*/;
		int bottom = _bottom;
		int num = 30;
		while (y < bottom)
		{
			int x = Position.X + num;
			if (random.Next(0, 4) == 0)
				CreateDrop(x, y);
			y += 32 /*0x20*/;
			num *= -1;
		}
	}

	private void CreateDrop(int x, int y)
	{
		ObjectManager objectManager = Level.ObjectManager;
		Rectanglei rect = new Rectanglei(x - 16 /*0x10*/, y - 16 /*0x10*/, 32 /*0x20*/, 32 /*0x20*/);
		if (!objectManager.IsInLifetimeArea(rect))
			return;
		objectManager.AddSubObject<CPZChemicalFallInstance.Drop>(this).Position = new Vector2i(x, y);
	}

	private class Drop : ParticleObject
	{
		public Drop()
		  : base("/ANIGROUP", 2)
		{
		}
	}
}
