using System.Collections.Generic;
using System.Linq;
using SonicOrca.Geometry;

namespace SonicOrca.Graphics;

public class Animation
{
	public struct Frame
	{
		public int TextureIndex { get; set; }

		public Rectanglei Source { get; set; }

		public Vector2i Offset { get; set; }

		public int Delay { get; set; }
	}

	private Frame[] _frames = new Frame[0];

	public IReadOnlyList<Frame> Frames
	{
		get
		{
			return _frames;
		}
		set
		{
			_frames = value.ToArray();
		}
	}

	public int? NextFrameIndex { get; set; }

	public int? LoopFrameIndex { get; set; }

	public int Duration => _frames.Sum((Frame x) => x.Delay + 1);

	public Animation()
	{
	}

	public Animation(IEnumerable<Frame> frames)
	{
		_frames = frames.ToArray();
	}

	public Animation(IEnumerable<Frame> frames, int? nextFrameIndex, int? loopFrameIndex)
	{
		_frames = frames.ToArray();
		NextFrameIndex = nextFrameIndex;
		LoopFrameIndex = loopFrameIndex;
	}
}
