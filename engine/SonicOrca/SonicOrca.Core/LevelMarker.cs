using SonicOrca.Geometry;

namespace SonicOrca.Core;

public class LevelMarker
{
	private string _tag = "";

	public string Name { get; set; }

	public string Tag
	{
		get
		{
			return _tag;
		}
		set
		{
			_tag = ((value == null) ? string.Empty : value);
		}
	}

	public Rectanglei Bounds { get; set; }

	public LevelLayer Layer { get; set; }

	public Vector2i Position
	{
		get
		{
			return Bounds.TopLeft;
		}
		set
		{
			Bounds = new Rectanglei(value.X, value.Y, 0, 0);
		}
	}

	public LevelMarker(string name, string tag, Vector2i position, LevelLayer layer)
	{
		Name = name;
		Tag = tag;
		Bounds = new Rectanglei(position.X, position.Y, 0, 0);
		Layer = layer;
	}

	public LevelMarker(string name, string tag, Rectanglei bounds, LevelLayer layer)
	{
		Name = name;
		Tag = tag;
		Bounds = bounds;
		Layer = layer;
	}

	public override string ToString()
	{
		return Name;
	}
}
