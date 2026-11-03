using System;
using System.Collections.Generic;
using System.Linq;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SonicOrca.Core.Editor;

/// <summary>
/// A small map of the whole level in the bottom right corner of the view. The tiles of the layers without parallax
/// are white, the objects are dots, and a yellow box shows the view. A click or a drag in the map moves the view.
/// </summary>
internal sealed class EditorMiniMap
{
	private const double MaxWidth = 720.0;

	private const double MaxHeight = 240.0;

	private const double Margin = 12.0;

	private static readonly Colour BackgroundColour = new Colour(0.75, 0.0, 0.0, 0.0);

	private static readonly Colour TileColour = new Colour(0.85, 1.0, 1.0, 1.0);

	private static readonly Colour ViewColour = new Colour(1.0, 1.0, 0.9, 0.1);

	private static readonly Colour RingColour = new Colour(1.0, 0.3, 0.9, 0.3);

	private static readonly Colour EnemyColour = new Colour(1.0, 1.0, 0.25, 0.25);

	private static readonly Colour ObjectColour = new Colour(1.0, 0.35, 0.6, 1.0);

	private readonly LevelEditor _editor;

	private readonly Dictionary<string, ObjectClassification> _classifications = new Dictionary<string, ObjectClassification>(StringComparer.OrdinalIgnoreCase);

	/// <summary>The runs of filled pixels of the tiles, in map pixels.</summary>
	private List<Rectangle> _tileRuns = new List<Rectangle>();

	/// <summary>The <see cref="LevelEditor.TilesVersion"/> of <see cref="_tileRuns"/>, or -1.</summary>
	private int _version = -1;

	/// <summary>The offset from the point under the mouse to the centre of the view while the user drags in the map.</summary>
	private Vector2 _dragOffset;

	public EditorMiniMap(LevelEditor editor)
	{
		_editor = editor;
	}

	public bool Visible { get; set; } = true;

	/// <summary>The area of the map on the screen. The map keeps the shape of the level.</summary>
	public Rectangle Area
	{
		get
		{
			Rectanglei level = _editor.Level.Map.Bounds;
			double scale = GetScale();
			double width = Math.Max(1.0, level.Width * scale);
			double height = Math.Max(1.0, level.Height * scale);
			return new Rectangle(LevelEditor.ScreenWidth - LevelEditor.PanelWidth - Margin - width,
				LevelEditor.ScreenHeight - LevelEditor.StatusBarHeight - Margin - height, width, height);
		}
	}

	public bool Contains(Vector2 screen) => Visible && Area.Contains(screen);

	/// <summary>Map pixels for each level pixel.</summary>
	private double GetScale()
	{
		Rectanglei level = _editor.Level.Map.Bounds;
		return level.Width <= 0 || level.Height <= 0 ? 1.0 : Math.Min(MaxWidth / level.Width, MaxHeight / level.Height);
	}

	private Vector2 ToWorld(Vector2 screen)
	{
		Rectangle area = Area;
		return (screen - new Vector2(area.X, area.Y)) / GetScale();
	}

	/// <summary>Starts a drag at a point of the map. A press in the view box keeps the box under the mouse. A press outside it centres the view there.</summary>
	public void BeginDrag(Vector2 screen)
	{
		Rectangle view = _editor.Camera.Bounds;
		Vector2 world = ToWorld(screen);
		_dragOffset = view.Contains(world) ? view.Centre - world : default;
		Drag(screen);
	}

	public void Drag(Vector2 screen)
	{
		_editor.CentreOn(ToWorld(screen) + _dragOffset);
	}

	public void Draw(Renderer renderer, I2dRenderer g)
	{
		if (!Visible)
		{
			return;
		}
		Rectangle area = Area;
		double scale = GetScale();
		if (_version != _editor.TilesVersion)
		{
			_tileRuns = BuildTileRuns(scale, (int)Math.Ceiling(area.Width), (int)Math.Ceiling(area.Height));
			_version = _editor.TilesVersion;
		}
		g.RenderQuad(BackgroundColour, area.Inflate(new Vector2(3.0, 3.0)));
		foreach (Rectangle run in _tileRuns)
		{
			g.RenderQuad(TileColour, run.OffsetBy(new Vector2(area.X, area.Y)));
		}
		foreach (ObjectPlacement placement in _editor.Placements)
		{
			ObjectClassification classification = GetClassification(placement.Key);
			Colour colour = classification switch
			{
				ObjectClassification.Ring => RingColour,
				ObjectClassification.Badnik => EnemyColour,
				_ => ObjectColour
			};
			double size = classification == ObjectClassification.Ring ? 2.0 : 3.0;
			Vector2 point = new Vector2(area.X + placement.Position.X * scale, area.Y + placement.Position.Y * scale);
			g.RenderQuad(colour, new Rectangle(point.X - size / 2.0, point.Y - size / 2.0, size, size));
		}
		// The view, kept inside the map so that the box shows also near the edges
		Rectangle view = _editor.Camera.Bounds;
		Rectangle box = new Rectangle(area.X + view.X * scale, area.Y + view.Y * scale, Math.Max(10.0, view.Width * scale), Math.Max(8.0, view.Height * scale));
		g.ClipRectangle = area.Inflate(new Vector2(3.0, 3.0));
		g.RenderRectangle(ViewColour, box, 2.0);
		renderer.DeativateRenderer();
		g.ClipRectangle = new Rectangle(0.0, 0.0, LevelEditor.ScreenWidth, LevelEditor.ScreenHeight);
		g.RenderRectangle(LevelEditor.OutlineColour, area.Inflate(new Vector2(3.0, 3.0)), 1.0);
	}

	/// <summary>
	/// Marks each map pixel that a tile of a layer without parallax covers, then joins the marked pixels of each row
	/// into runs. The layers with parallax are backgrounds, which are not at their level positions.
	/// </summary>
	private List<Rectangle> BuildTileRuns(double scale, int width, int height)
	{
		bool[,] filled = new bool[width, height];
		const int TileSize = 64;
		foreach (LevelLayer layer in _editor.Level.Map.Layers.Where(l => l.Visible && !EditorLayerView.IsShifted(l)))
		{
			int[,] tiles = layer.Tiles;
			for (int row = 0; row < layer.Rows; row++)
			{
				// A tile covers one map pixel or more
				int top = Math.Min(height - 1, (int)(row * TileSize * scale));
				int bottom = Math.Clamp((int)((row + 1) * TileSize * scale) - 1, top, height - 1);
				for (int column = 0; column < layer.Columns; column++)
				{
					if (tiles[column, row] == 0)
					{
						continue;
					}
					int left = Math.Min(width - 1, (int)(column * TileSize * scale));
					int right = Math.Clamp((int)((column + 1) * TileSize * scale) - 1, left, width - 1);
					for (int y = top; y <= bottom; y++)
					{
						for (int x = left; x <= right; x++)
						{
							filled[x, y] = true;
						}
					}
				}
			}
		}
		List<Rectangle> runs = new List<Rectangle>();
		for (int y = 0; y < height; y++)
		{
			int x = 0;
			while (x < width)
			{
				if (!filled[x, y])
				{
					x++;
					continue;
				}
				int start = x;
				while (x < width && filled[x, y])
				{
					x++;
				}
				runs.Add(new Rectangle(start, y, x - start, 1.0));
			}
		}
		return runs;
	}

	private ObjectClassification GetClassification(string key)
	{
		if (!_classifications.TryGetValue(key, out ObjectClassification classification))
		{
			ObjectType type = _editor.Level.ObjectManager.RegisteredTypes.FirstOrDefault(t => t.Resource != null && string.Equals(t.ResourceKey, key, StringComparison.OrdinalIgnoreCase));
			// A type that is not loaded yet is checked again later
			if (type == null)
			{
				return ObjectClassification.None;
			}
			classification = type.Classification;
			_classifications[key] = classification;
		}
		return classification;
	}
}
