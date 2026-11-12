using System.Collections.Generic;
using System.Linq;
using SonicOrca.Geometry;
using SonicOrca.Graphics;
using SonicOrca.Resources;

namespace SonicOrca.Core;

/// <summary>
/// A 64 by 64 pixel tile from a tile set. A tile can have more than one frame, and it can fade between frames.
/// </summary>
public class Tile : ITile
{
	/// <summary>
	/// One frame of a tile: the area of a texture to draw, its opacity and how long it shows.
	/// </summary>
	public struct Frame
	{
		public int TextureId { get; set; }

		public int X { get; set; }

		public int Y { get; set; }

		public float Opacity { get; set; }

		public int Delay { get; set; }

		public override string ToString()
		{
			return $"TextureId = {TextureId} X = {X} Y = {Y} Opacity = {Opacity} Delay = {Delay}";
		}
	}

	public const int Size = 64;

	public const int IndexMask = 0xFFF;

	public const int TileSetMask = 0x3000;

	public const int FlipXMask = 0x4000;

	public const int FlipYMask = 0x8000;

	private readonly TileSet _tileSet;

	private readonly int _id;

	private Frame[] _frames;

	/// <summary>The animation group of the frames, or null when the tile set has the frames.</summary>
	private AnimationGroup _animationGroup;

	/// <summary>The animation that gave the frames. A new animation (for example after a change in the animation editor) gives new frames.</summary>
	private Animation _animation;

	private readonly TileBlendMode _blend;

	private int _currentFrameIndex;

	private int _frameTime;

	private float _opacity;

	private float _opacityChange;

	public int Id => _id;

	public bool Animated => _frames.Length > 1;

	public IReadOnlyList<Frame> Frames => _frames;

	public TileBlendMode Blend => _blend;

	/// <summary>
	/// Where the frames of the tile come from, for the level editor, or null. The importer writes it for the tiles that
	/// it animates, for example "palette cycle PalCycle_CNZ".
	/// </summary>
	public string Source { get; }

	/// <summary>
	/// The key of the animation group (ANIGROUP) that has the frames of the tile, or null when the tile set has the
	/// frames. The animation editor can change such an animation.
	/// </summary>
	public string AnimationGroupKey { get; }

	/// <summary>The index of the animation of the tile in <see cref="AnimationGroupKey"/>.</summary>
	public int AnimationIndex { get; }

	/// <summary>
	/// The index of a second animation in <see cref="AnimationGroupKey"/>, or -1. The tile shows it while
	/// <see cref="TileSet.UseAlternateAnimations"/> is true. For example, an object of Wing Fortress changes the colours
	/// of the conveyor belts and the fire.
	/// </summary>
	public int AlternateAnimationIndex { get; }

	/// <summary>The animation that the tile shows now.</summary>
	private int CurrentAnimationIndex => _tileSet != null && _tileSet.UseAlternateAnimations && AlternateAnimationIndex >= 0 ? AlternateAnimationIndex : AnimationIndex;

	public Tile(TileSet tileSet, int id, IEnumerable<Frame> frames, TileBlendMode blend = TileBlendMode.Alpha, string source = null,
		string animationGroupKey = null, int animationIndex = 0, int alternateAnimationIndex = -1)
	{
		_tileSet = tileSet;
		_id = id;
		Source = source;
		AnimationGroupKey = animationGroupKey;
		AnimationIndex = animationIndex;
		AlternateAnimationIndex = alternateAnimationIndex;
		_frames = frames.ToArray();
		_blend = blend;
		if (_frames.Length >= 1)
		{
			_opacity = _frames[0].Opacity;
			if (_frames.Length >= 2)
			{
				_opacityChange = (_frames[1].Opacity - _opacity) / (_frames[0].Delay + 1);
			}
		}
	}

	/// <summary>Takes the frames from the animation of <see cref="AnimationGroupKey"/>, when the tile has one.</summary>
	internal void LoadAnimation(ResourceTree resourceTree)
	{
		if (AnimationGroupKey == null)
		{
			return;
		}
		_animationGroup = resourceTree.GetLoadedResource<AnimationGroup>(AnimationGroupKey);
		UpdateAnimationFrames();
	}

	/// <summary>
	/// Makes the frames of the tile from its animation. The textures of the animation become textures of the tile set.
	/// </summary>
	private void UpdateAnimationFrames()
	{
		int index = CurrentAnimationIndex;
		if (_animationGroup == null || index < 0 || index >= _animationGroup.Count)
		{
			return;
		}
		_animation = _animationGroup[index];
		if (_animation.Frames.Count == 0)
		{
			return;
		}
		_frames = _animation.Frames.Select(frame => new Frame
		{
			TextureId = _tileSet.GetTextureIndex(_animationGroup.Textures[frame.TextureIndex]),
			X = frame.Source.X,
			Y = frame.Source.Y,
			Delay = frame.Delay,
			Opacity = frame.Opacity
		}).ToArray();
		_currentFrameIndex %= _frames.Length;
		_frameTime = 0;
		StartOpacityFade();
	}

	public void Animate()
	{
		// The animation editor replaces the animations of a group when the user changes them. The tile set can also
		// change to the alternate animations.
		int index = CurrentAnimationIndex;
		if (_animationGroup != null && index < _animationGroup.Count && _animationGroup[index] != _animation)
		{
			UpdateAnimationFrames();
		}
		if (_frames.Length <= 1)
		{
			return;
		}
		Frame frame = _frames[_currentFrameIndex];
		if (_frameTime >= frame.Delay)
		{
			_currentFrameIndex = (_currentFrameIndex + 1) % _frames.Length;
			_frameTime = 0;
			frame = _frames[_currentFrameIndex];
			StartOpacityFade();
		}
		else
		{
			_frameTime++;
			_opacity += _opacityChange;
		}
	}

	/// <summary>
	/// Sets the opacity of the current frame, and fades it to the opacity of the next frame during the delay of this frame.
	/// </summary>
	private void StartOpacityFade()
	{
		Frame frame = _frames[_currentFrameIndex];
		Frame nextFrame = _frames[(_currentFrameIndex + 1) % _frames.Length];
		_opacity = frame.Opacity;
		_opacityChange = nextFrame.Opacity != _opacity && frame.Delay > 0 ? (nextFrame.Opacity - _opacity) / (frame.Delay + 1) : 0f;
	}

	public void Draw(Renderer renderer, int flags, int x, int y)
	{
		// This array access has no result. It stays because it throws when the tile has no frames.
		_ = ref _frames[_currentFrameIndex];
		Draw(renderer, flags, new Rectanglei(0, 0, Size, Size), new Rectanglei(x, y, Size, Size));
	}

	public void Draw(Renderer renderer, int flags, Rectanglei source, Rectanglei destination)
	{
		ITileRenderer tileRenderer = renderer.GetTileRenderer();
		if (tileRenderer.Rendering)
		{
			Draw(tileRenderer, flags, source, destination);
		}
		else
		{
			Draw(renderer.Get2dRenderer(), flags, source, destination);
		}
	}

	public void Draw(I2dRenderer g, int flags, Rectanglei source, Rectanglei destination)
	{
		Frame frame = _frames[_currentFrameIndex];
		source = GetFrameSource(frame, flags, source);
		ITexture texture = _tileSet.Textures[frame.TextureId];
		g.BlendMode = (_blend != TileBlendMode.Additive) ? BlendMode.Alpha : BlendMode.Additive;
		g.Colour = new Colour(_opacity, 1.0, 1.0, 1.0);
		g.RenderTexture(texture, source, destination, (flags & FlipXMask) != 0, (flags & FlipYMask) != 0);
	}

	public void Draw(ITileRenderer tileRenderer, int flags, Rectanglei source, Rectanglei destination)
	{
		Frame frame = _frames[_currentFrameIndex];
		source = GetFrameSource(frame, flags, source);
		tileRenderer.AddTile(source, destination, frame.TextureId, (flags & FlipXMask) != 0, (flags & FlipYMask) != 0, _opacity, _blend);
	}

	/// <summary>
	/// Moves a source rectangle that is relative to the tile into the texture area of the frame.
	/// A flipped tile mirrors the source rectangle in the frame.
	/// </summary>
	private static Rectanglei GetFrameSource(Frame frame, int flags, Rectanglei source)
	{
		source.X = ((flags & FlipXMask) == 0) ? (frame.X + source.X) : (frame.X + Size - source.Width - source.X);
		source.Y = ((flags & FlipYMask) == 0) ? (frame.Y + source.Y) : (frame.Y + Size - source.Height - source.Y);
		return source;
	}

	public override string ToString()
	{
		if (_frames.Length != 1)
		{
			return string.Format("Id = {0} Frames = {1}", _id, _frames.Length);
		}
		return string.Format("Id = {0} TextureId = {1} X = {2} Y = {3}", _id, _frames[0].TextureId, _frames[0].X, _frames[0].Y);
	}
}
