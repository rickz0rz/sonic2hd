using System;
using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Lighting;
using SonicOrca.Core.Objects;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.LAVA;

/// <summary>
/// A rectangle of lava, measured in tiles of 64 units. It hurts characters that touch it, gives off light from its top
/// edge, and makes heat particles. It draws a glowing top row, a middle row, and bottom tiles for the rest.
/// </summary>
public class LavaInstance : ActiveObject
{
	private const int AnimationTop = 0;
	private const int AnimationMiddle = 1;
	private const int AnimationBottom = 2;
	private const int TileSize = 64;

	/// <summary>The lava makes a row of heat particles one time in this number of ticks.</summary>
	private const int HeatParticleInterval = 24;

	private AnimationInstance _animationTop;
	private AnimationInstance _animationMiddle;
	private AnimationInstance _animationBottom;
	private Vector2i _size = new Vector2i(1, 1);
	private VectorLightSource _lightSource;

	public int Width
	{
		get => _size.X;
		set => _size.X = value;
	}

	public int Height
	{
		get => _size.Y;
		set => _size.Y = value;
	}

	private Rectanglei Bounds
	{
		get
		{
			return new Rectanglei(-_size.X / 2 * TileSize, -_size.Y / 2 * TileSize, _size.X * TileSize, _size.Y * TileSize);
		}
	}

	protected override void OnStart()
	{
		AnimationGroup animationGroup = ResourceTree.GetLoadedResource<AnimationGroup>(Type.GetAbsolutePath("/ANIGROUP"));
		_animationTop = new AnimationInstance(animationGroup, AnimationTop);
		_animationMiddle = new AnimationInstance(animationGroup, AnimationMiddle);
		_animationBottom = new AnimationInstance(animationGroup, AnimationBottom);
		CollisionVectors = CollisionVector.FromRectangle(this, Bounds);
		CollisionVectors[1].Flags = CollisionFlags.Conveyor;
		Priority = 4096;
		Rectanglei bounds = Bounds;
		DesignBounds = bounds;
		bounds = bounds.OffsetBy(Position);
		_lightSource = new VectorLightSource(32, bounds.TopLeft, bounds.TopRight);
		Level.LightingManager.RegisterLightSource(_lightSource);
	}

	protected override void OnStop()
	{
		Level.LightingManager.UnregisterLightSource(_lightSource);
	}

	protected override void OnCollision(CollisionEvent e)
	{
		if (e.ActiveObject.Type.Classification != ObjectClassification.Character)
			return;
		((ICharacter)e.ActiveObject).Hurt(-1);
	}

	protected override void OnUpdate()
	{
		Rectanglei bounds = Bounds.OffsetBy(Position);
		_lightSource.A = bounds.TopLeft;
		_lightSource.B = bounds.TopRight;
		if (Level.Ticks % HeatParticleInterval != 0)
			return;
		ParticleManager particleManager = Level.ParticleManager;
		Random random = particleManager.Random;
		int left = -_size.X / 2 * TileSize;
		int top = -_size.Y / 2 * TileSize;
		// Make one heat particle near the centre of each tile in the top row.
		for (int column = 0; column < _size.X; ++column)
		{
			Vector2i tileCentre = Position + new Vector2i(left + column * TileSize + TileSize / 2, top + TileSize / 2);
			particleManager.Add(new Particle()
			{
				Type = ParticleType.Heat,
				Layer = Layer,
				Position = (Vector2)(tileCentre + new Vector2i(random.Next(-4, 8), random.Next(-4, 8))),
				Velocity = new Vector2(random.NextSignedDouble() * 1.0, random.NextDouble() * -1.4 - 0.8),
				Time = random.Next(60, 120)
			});
		}
	}

	protected override void OnAnimate()
	{
		_animationTop.Animate();
		_animationMiddle.Animate();
		_animationBottom.Animate();
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		if (viewOptions.Shadows)
			return;
		IObjectRenderer objectRenderer = renderer.GetObjectRenderer();
		objectRenderer.EmitsLight = true;
		int left = -_size.X / 2 * TileSize;
		int top = -_size.Y / 2 * TileSize;
		int right = (_size.X + 1) / 2 * TileSize;
		int bottom = (_size.Y + 1) / 2 * TileSize;

		// The top row glows above the top edge, with additive blending.
		Rectanglei topSource = _animationTop.CurrentFrame.Source;
		for (int x = left; x < right; x += topSource.Width)
		{
			Rectangle destination = new Rectangle(x, top - topSource.Height, topSource.Width, topSource.Height);
			destination.Right = Math.Min(destination.Right, right);
			destination.Bottom = Math.Min(destination.Bottom, bottom);
			Rectangle source = (Rectangle)_animationTop.CurrentFrame.Source with
			{
				Width = destination.Width,
				Height = destination.Height
			};
			objectRenderer.Texture = _animationTop.CurrentTexture;
			objectRenderer.BlendMode = BlendMode.Additive;
			ScaleRectangle(ref destination, objectRenderer.Scale);
			objectRenderer.Render(source, destination);
		}
		objectRenderer.BlendMode = BlendMode.Alpha;

		// The middle row starts at the top edge.
		int middleBottom = top;
		Rectanglei tileSource = _animationMiddle.CurrentFrame.Source;
		for (int x = left; x < right; x += tileSource.Width)
		{
			Rectangle destination = new Rectangle(x, top, tileSource.Width, tileSource.Height);
			destination.Right = Math.Min(destination.Right, right);
			destination.Bottom = Math.Min(destination.Bottom, bottom);
			Rectangle source = (Rectangle)_animationMiddle.CurrentFrame.Source with
			{
				Width = destination.Width,
				Height = destination.Height
			};
			middleBottom = (int)destination.Bottom;
			ScaleRectangle(ref destination, objectRenderer.Scale);
			objectRenderer.Texture = _animationMiddle.CurrentTexture;
			objectRenderer.Render(source, destination);
		}
		if (middleBottom >= bottom)
			return;

		// Bottom tiles fill the rest of the rectangle.
		tileSource = _animationBottom.CurrentFrame.Source;
		for (int y = middleBottom; y < bottom; y += tileSource.Height)
		{
			for (int x = left; x < right; x += tileSource.Width)
			{
				Rectangle destination = new Rectangle(x, y, tileSource.Width, tileSource.Height);
				destination.Right = Math.Min(destination.Right, right);
				destination.Bottom = Math.Min(destination.Bottom, bottom);
				Rectangle source = (Rectangle)_animationBottom.CurrentFrame.Source with
				{
					Width = destination.Width,
					Height = destination.Height
				};
				ScaleRectangle(ref destination, objectRenderer.Scale);
				objectRenderer.Texture = _animationBottom.CurrentTexture;
				objectRenderer.Render(source, destination);
			}
		}
	}

	/// <summary>Multiplies the position and the size of <paramref name="rectangle"/> by <paramref name="scale"/>.</summary>
	private static void ScaleRectangle(ref Rectangle rectangle, Vector2 scale)
	{
		rectangle.X *= scale.X;
		rectangle.Y *= scale.Y;
		rectangle.Width *= scale.X;
		rectangle.Height *= scale.Y;
	}
}
