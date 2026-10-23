using System.Collections.Generic;
using System.Linq;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

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

	private readonly Frame[] _frames;

	private readonly TileBlendMode _blend;

	private int _currentFrameIndex;

	private int _frameTime;

	private float _opacity;

	private float _opacityChange;

	public int Id => _id;

	public bool Animated => _frames.Length > 1;

	public IReadOnlyList<Frame> Frames => _frames;

	public TileBlendMode Blend => _blend;

	public Tile(TileSet tileSet, int id, IEnumerable<Frame> frames, TileBlendMode blend = TileBlendMode.Alpha)
	{
		_tileSet = tileSet;
		_id = id;
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

	public void Animate()
	{
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
			_opacity = frame.Opacity;
			// Fade the opacity to the opacity of the next frame during the delay of this frame.
			int nextFrameIndex = _currentFrameIndex + 1;
			if (nextFrameIndex >= _frames.Length)
			{
				nextFrameIndex = 0;
			}
			Frame nextFrame = _frames[nextFrameIndex];
			if (nextFrame.Opacity != _opacity && frame.Delay > 0)
			{
				_opacityChange = (nextFrame.Opacity - _opacity) / (frame.Delay + 1);
			}
			else
			{
				_opacityChange = 0f;
			}
		}
		else
		{
			_frameTime++;
			_opacity += _opacityChange;
		}
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
