using System.Collections.Generic;
using System.Linq;
using SonicOrca.Geometry;

namespace SonicOrca.Graphics.V2.Animation;

/// <summary>
/// Describes one composition: its frame range, its size and its layers.
/// </summary>
public class Composition
{
	/// <summary>
	/// The state of one layer at one frame, as the renderer needs it.
	/// </summary>
	public struct Frame
	{
		public int TextureIndex { get; set; }

		public Rectanglei Source { get; set; }

		public double Opacity { get; set; }

		public Vector2 Position { get; set; }

		public double Rotation { get; set; }

		public Vector2 Scale { get; set; }
	}

	private string _version;

	private uint _frameRate;

	private uint _startFrame;

	private uint _endFrame;

	private uint _width;

	private uint _height;

	private string _name;

	private CompositionLayer[] _layers = new CompositionLayer[0];

	public uint StartFrame => _startFrame;

	public uint EndFrame => _endFrame;

	public IReadOnlyList<CompositionLayer> Layers
	{
		get
		{
			return _layers;
		}
		set
		{
			_layers = value.ToArray();
		}
	}

	public Composition(string version, uint frameRate, uint startFrame, uint endFrame, uint width, uint height, string name, IEnumerable<CompositionLayer> layers)
	{
		_version = version;
		_frameRate = frameRate;
		_startFrame = startFrame;
		_endFrame = endFrame;
		_width = width;
		_height = height;
		_name = name;
		_layers = layers.ToArray();
	}
}
