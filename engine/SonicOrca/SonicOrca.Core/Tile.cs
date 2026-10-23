using System.Collections.Generic;
using System.Linq;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SonicOrca.Core;

public class Tile : ITile
{
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

	public const int IndexMask = 4095;

	public const int TileSetMask = 12288;

	public const int FlipXMask = 16384;

	public const int FlipYMask = 32768;

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
			int num = _currentFrameIndex + 1;
			if (num >= _frames.Length)
			{
				num = 0;
			}
			Frame frame2 = _frames[num];
			if (frame2.Opacity != _opacity && frame.Delay > 0)
			{
				_opacityChange = (frame2.Opacity - _opacity) / (frame.Delay + 1);
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
		_ = ref _frames[_currentFrameIndex];
		Draw(renderer, flags, new Rectangle(0.0, 0.0, 64.0, 64.0), new Rectangle(x, y, 64.0, 64.0));
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
		source.X = (((flags & 0x4000) == 0) ? (frame.X + source.X) : (frame.X + 64 - source.Width - source.X));
		source.Y = (((flags & 0x8000) == 0) ? (frame.Y + source.Y) : (frame.Y + 64 - source.Height - source.Y));
		ITexture texture = _tileSet.Textures[frame.TextureId];
		g.BlendMode = ((_blend != TileBlendMode.Additive) ? BlendMode.Alpha : BlendMode.Additive);
		g.Colour = new Colour(_opacity, 1.0, 1.0, 1.0);
		g.RenderTexture(texture, source, destination, (flags & 0x4000) != 0, (flags & 0x8000) != 0);
	}

	public void Draw(ITileRenderer tileRenderer, int flags, Rectanglei source, Rectanglei destination)
	{
		Frame frame = _frames[_currentFrameIndex];
		source.X = (((flags & 0x4000) == 0) ? (frame.X + source.X) : (frame.X + 64 - source.Width - source.X));
		source.Y = (((flags & 0x8000) == 0) ? (frame.Y + source.Y) : (frame.Y + 64 - source.Height - source.Y));
		tileRenderer.AddTile(source, destination, frame.TextureId, (flags & 0x4000) != 0, (flags & 0x8000) != 0, _opacity, _blend);
	}

	public override string ToString()
	{
		if (_frames.Length != 1)
		{
			return string.Format("Id = {0} Frames = {1}", _frames.Length);
		}
		return string.Format("Id = {0} TextureId = {1} X = {2} Y = {3}", _frames[0].TextureId, _frames[0].X, _frames[0].Y);
	}
}
