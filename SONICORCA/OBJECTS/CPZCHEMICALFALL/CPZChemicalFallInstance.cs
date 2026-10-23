// Decompiled with JetBrains decompiler
// Type: SONICORCA.OBJECTS.CPZCHEMICALFALL.CPZChemicalFallInstance
// Assembly: S2HD, Version=2.0.1012.10521, Culture=neutral, PublicKeyToken=null
// MVID: 18631A0F-16CF-4E18-8563-1EC5E54750D6
// Assembly location: C:\Users\ricky\Downloads\S2HD_2.0.1012-rc2\S2HD_2.0.1012-rc2\S2HD.exe

using System;
using SonicOrca.Core;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

#nullable disable
namespace SONICORCA.OBJECTS.CPZCHEMICALFALL;

/// <summary>
/// A column of chemical liquid that falls from a pipe in Chemical Plant Zone. It is decoration only.
/// The column goes down to the top of the water below, or to the bottom of the map. It also releases drops.
/// </summary>
public class CPZChemicalFallInstance : ActiveObject
{
	private const int AnimationCorner = 0;
	private const int AnimationVertical = 1;
	private const int AnimationDrop = 2;
	private const int DropInterval = 30;
	private AnimationInstance _animationCorner;
	private AnimationInstance _animationVertical;

	/// <summary>The Y position where the column stops.</summary>
	private int _bottom;

	protected override void OnStart()
	{
		AnimationGroup animationGroup = ResourceTree.GetLoadedResource<AnimationGroup>(Type.GetAbsolutePath("/ANIGROUP"));
		_animationCorner = new AnimationInstance(animationGroup);
		_animationVertical = new AnimationInstance(animationGroup, AnimationVertical);
		DesignBounds = new Rectanglei(-32, -32, 64, 128);
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
		int top = _animationCorner.CurrentFrame.Source.Height / 2;
		Rectanglei source = _animationVertical.CurrentFrame.Source;
		Rectanglei destination = new Rectanglei(-source.Width / 2, top, source.Width, source.Height);
		// Draw the column in steps of 64 pixels down to the bottom.
		// The height changes by the overlap with the bottom on each step, also when the overlap is negative.
		for (int bottom = _bottom - Position.Y; destination.Y < bottom; destination.Y += 64)
		{
			int overlap = destination.Bottom - bottom;
			source.Height -= overlap;
			destination.Height -= overlap;
			objectRenderer.Render((Rectangle)source, (Rectangle)destination);
		}
	}

	/// <summary>Sets the bottom of the column to the top of the highest water area below it, or to the bottom of the map.</summary>
	private void UpdateBottom()
	{
		int top = Position.Y;
		int left = Position.X - 32;
		// This uses Position.Y, not Position.X, as the right edge. Keep it to keep the same behavior.
		int right = Position.Y + 32;
		int bottom = Level.Map.Bounds.Height;
		foreach (Rectanglei waterArea in Level.WaterManager.WaterAreas)
		{
			if (waterArea.Left <= right && waterArea.Right >= left && waterArea.Bottom >= top)
			{
				bottom = Math.Min(bottom, waterArea.Top);
			}
		}
		_bottom = bottom;
	}

	/// <summary>
	/// At a fixed interval, releases drops at random points along the column.
	/// The drops are on alternate sides of the column.
	/// </summary>
	private void CreateDrops()
	{
		if (Level.Ticks % DropInterval != 0)
		{
			return;
		}
		Random random = Level.Random;
		int y = Position.Y + 64;
		int bottom = _bottom;
		int offsetX = 30;
		while (y < bottom)
		{
			int x = Position.X + offsetX;
			if (random.Next(0, 4) == 0)
			{
				CreateDrop(x, y);
			}
			y += 32;
			offsetX *= -1;
		}
	}

	private void CreateDrop(int x, int y)
	{
		ObjectManager objectManager = Level.ObjectManager;
		Rectanglei area = new Rectanglei(x - 16, y - 16, 32, 32);
		if (objectManager.IsInLifetimeArea(area))
		{
			objectManager.AddSubObject<Drop>(this).Position = new Vector2i(x, y);
		}
	}

	/// <summary>A drop of chemical liquid that plays its animation one time.</summary>
	private class Drop : ParticleObject
	{
		public Drop()
			: base("/ANIGROUP", AnimationDrop)
		{
		}
	}
}
