using System;
using System.Collections.Generic;
using System.Linq;
using SonicOrca.Core.Collision;
using SonicOrca.Geometry;
using SonicOrca.Graphics;
using SonicOrca.Input;
using SonicOrca.Original;

namespace SonicOrca.Core.Editor;

/// <summary>
/// Paints the tiles of a layer with a brush. A brush is one tile from the tile set, or a rectangle of cells that the
/// user takes from the level with Ctrl and a drag. On the front layer, a brush also carries the priority tiles of
/// the cells ("front_high") and, when the zone has tile collision, the solidity of the cells. After each stroke, the
/// collision follows the tiles (see <see cref="LevelEditor.RequestTrace"/>).
/// </summary>
internal sealed class TilesTool : EditorTool
{
	private const int TileSize = 64;

	/// <summary>The size of a tile in the palette, in screen pixels.</summary>
	private const double PaletteTileSize = 54.0;

	private const int PaletteColumns = 8;

	/// <summary>The rows of settings above the palette.</summary>
	private const int SettingRows = 4;

	private const int FlipXFlag = 0x4000;

	private const int FlipYFlag = 0x8000;

	private const string HighLayerSuffix = "_high";

	private static readonly string[] SolidityNames = { "none", "top only", "all sides" };

	private static readonly Colour GridColour = new Colour(0.25, 1.0, 1.0, 1.0);

	private static readonly Colour CollisionColour = new Colour(0.7, 0.3, 1.0, 0.3);

	private readonly List<TileEdit> _strokeEdits = new List<TileEdit>();

	private readonly HashSet<(int Column, int Row)> _strokeStamps = new HashSet<(int, int)>();

	private int _layerIndex = -1;

	private Brush _brush;

	private int[] _paletteTiles = Array.Empty<int>();

	private double _paletteScroll;

	/// <summary>The solidity of a brush from the palette, for each path: 0 none, 1 top only, 2 all sides.</summary>
	private int _solidity1 = 2;

	private int _solidity2 = 2;

	private bool _painting;

	private bool _capturing;

	private EditorSnapshot _strokeStart;

	private (int Column, int Row) _strokeOrigin;

	/// <summary>The cell of the mouse on the last frame of the stroke.</summary>
	private (int Column, int Row) _lastCell;

	/// <summary>The cells to paint: the tiles of the layer, the tiles of its priority layer, and the solidity.</summary>
	private sealed class Brush
	{
		public int Columns;

		public int Rows;

		public int[,] Tiles;

		/// <summary>The tiles of the priority layer, or null to keep the priority layer as it is.</summary>
		public int[,] HighTiles;

		/// <summary>The solidity of the cells, or null to keep the solidity as it is.</summary>
		public byte[,] Solidity;

		public string Name;
	}

	public TilesTool(LevelEditor editor)
		: base(editor)
	{
	}

	public override string Name => "Tiles";

	public override string Hint => _brush == null
		? "Click a tile in the palette, or Ctrl+drag in the level to take tiles. Then click or drag to paint."
		: "Click or drag: paint.  Ctrl+drag: take tiles.  X, Y: flip the brush.  Esc: no brush.";

	public override string Status => _layerIndex >= 0
		? $"Layer {Layer.Name}{(EditorLayerView.IsShifted(Layer) ? " (alone, without parallax)" : "")}   Brush {(_brush == null ? "none" : $"{_brush.Columns} x {_brush.Rows}")}"
		: "";

	/// <summary>The layer that the tool paints on, or -1.</summary>
	public int LayerIndex => _layerIndex;

	public override string ListHeading => "Layers (click to paint on a layer)";

	public override string PropertiesHeading => Editor.TileCollision != null && IsCollisionLayer
		? "Brush and tiles (the collision follows)"
		: "Brush and tiles";

	public override bool HasCustomProperties => true;

	private IList<LevelLayer> Layers => Editor.Level.Map.Layers;

	private LevelLayer Layer => Layers[_layerIndex];

	private bool IsCollisionLayer => Editor.TileCollision != null && _layerIndex == Editor.TileCollision.Layer;

	public override void Begin()
	{
		_painting = false;
		_capturing = false;
		if (_layerIndex < 0 || _layerIndex >= Layers.Count)
		{
			IList<int> collisionLayers = Editor.Level.Map.CollisionPathLayers;
			_layerIndex = collisionLayers.Count > 0 ? collisionLayers[0] : Math.Max(0, Layers.Count - 1);
		}
		_paletteTiles = new[] { 0 }.Concat(Editor.Level.TileSet.Keys.Where(k => k != 0).OrderBy(k => k)).ToArray();
	}

	public override void Update()
	{
		if (_brush == null)
		{
			return;
		}
		if (Editor.IsCommandKeyPressed(KeyboardState.KEY_X))
		{
			FlipBrush(horizontal: true);
		}
		else if (Editor.IsCommandKeyPressed(KeyboardState.KEY_Y))
		{
			FlipBrush(horizontal: false);
		}
	}

	/// <summary>The priority layer of the current layer: the layer with the same name and "_high", or "front_high" for the front layer.</summary>
	private int GetHighLayerIndex()
	{
		string name = Layer.Name + HighLayerSuffix;
		int index = Layers.ToList().FindIndex(l => string.Equals(l.Name, name, StringComparison.OrdinalIgnoreCase));
		if (index < 0 && Editor.Level.Map.CollisionPathLayers.Contains(_layerIndex))
		{
			index = Layers.ToList().FindIndex(l => string.Equals(l.Name, "front" + HighLayerSuffix, StringComparison.OrdinalIgnoreCase));
		}
		return index;
	}

	#region List

	public override IReadOnlyList<EditorListRow> GetListRows()
	{
		return Layers.Select((layer, index) => new EditorListRow
		{
			Text = $"{index}: {layer.Name}",
			Detail = layer.Rows > 0 ? $"{layer.Columns} x {layer.Rows}" : "empty",
			Active = index == _layerIndex,
			Dim = !layer.Visible
		}).ToArray();
	}

	public override void OnListClick(int index, int direction)
	{
		if (index >= 0 && index < Layers.Count && index != _layerIndex)
		{
			_layerIndex = index;
			_brush = null;
			if (EditorLayerView.IsShifted(Layer))
			{
				// The layer shows from the top left of the level. Go there when the layer is not in view.
				Rectangle area = new Rectangle(0.0, 0.0, Layer.Columns * TileSize, Layer.Rows * TileSize);
				Rectangle view = Editor.Camera.Bounds;
				if (view.Right <= area.Left || view.Left >= area.Right || view.Bottom <= area.Top || view.Top >= area.Bottom)
				{
					Editor.CentreOn(new Vector2(view.Width / 2.0, view.Height / 2.0));
				}
				Editor.ShowMessage($"Layer {Layer.Name} shows alone, from the top left of the level, without parallax.");
			}
		}
	}

	#endregion

	#region Properties

	public override List<EditorPropertyRow> GetPropertyRows() => new List<EditorPropertyRow>();

	private List<EditorPropertyRow> GetSettingRows()
	{
		return new List<EditorPropertyRow>
		{
			new EditorPropertyRow { Label = "Brush", Value = _brush == null ? "none" : $"{_brush.Name} ({_brush.Columns} x {_brush.Rows})", IsHeading = true },
			new EditorPropertyRow { Label = "Flip (X, Y)", Value = "click to flip X, right click to flip Y", Step = direction => FlipBrush(direction > 0) },
			new EditorPropertyRow
			{
				Label = "Solid, path 1",
				Value = Editor.TileCollision == null ? "no tile collision" : SolidityNames[_solidity1],
				Step = Editor.TileCollision == null ? null : direction => _solidity1 = (_solidity1 + direction + 3) % 3
			},
			new EditorPropertyRow
			{
				Label = "Solid, path 2",
				Value = Editor.TileCollision == null ? "no tile collision" : SolidityNames[_solidity2],
				Step = Editor.TileCollision == null ? null : direction => _solidity2 = (_solidity2 + direction + 3) % 3
			}
		};
	}

	private Rectangle GetPaletteArea(Rectangle area)
	{
		double top = area.Y + SettingRows * LevelEditor.RowHeight + LevelEditor.Padding;
		return new Rectangle(area.X, top, area.Width, area.Bottom - top);
	}

	private Rectangle GetPaletteCell(Rectangle palette, int index)
	{
		double left = palette.X + (palette.Width - PaletteColumns * PaletteTileSize) / 2.0;
		return new Rectangle(left + index % PaletteColumns * PaletteTileSize, palette.Y + index / PaletteColumns * PaletteTileSize - _paletteScroll, PaletteTileSize, PaletteTileSize);
	}

	public override void DrawProperties(Renderer renderer, I2dRenderer g, Rectangle area)
	{
		List<EditorPropertyRow> rows = GetSettingRows();
		for (int i = 0; i < rows.Count; i++)
		{
			Rectangle bounds = new Rectangle(area.X, area.Y + i * LevelEditor.RowHeight, area.Width, LevelEditor.RowHeight);
			if (rows[i].Step != null && bounds.Contains(Editor.Mouse))
			{
				g.RenderQuad(LevelEditor.ButtonHoverColour, bounds);
			}
			double textY = bounds.Y + 4.0;
			Editor.DrawText(renderer, rows[i].Label, bounds.X + LevelEditor.Padding, textY, rows[i].IsHeading ? LevelEditor.SelectionColour : LevelEditor.DimTextColour);
			Editor.DrawText(renderer, rows[i].Value, bounds.X + area.Width * 0.38, textY, LevelEditor.TextColour);
		}
		Rectangle palette = GetPaletteArea(area);
		int paletteRows = (_paletteTiles.Length + PaletteColumns - 1) / PaletteColumns;
		_paletteScroll = Math.Clamp(_paletteScroll, 0.0, Math.Max(0.0, paletteRows * PaletteTileSize - palette.Height));
		renderer.DeativateRenderer();
		g.ClipRectangle = palette;
		TileSet tileSet = Editor.Level.TileSet;
		for (int i = 0; i < _paletteTiles.Length; i++)
		{
			Rectangle cell = GetPaletteCell(palette, i);
			if (cell.Bottom < palette.Y || cell.Y > palette.Bottom)
			{
				continue;
			}
			int tile = _paletteTiles[i];
			Rectanglei destination = new Rectanglei((int)cell.X + 1, (int)cell.Y + 1, (int)PaletteTileSize - 2, (int)PaletteTileSize - 2);
			g.RenderQuad(LevelEditor.ButtonColour, (Rectangle)destination);
			if (tile != 0)
			{
				tileSet.DrawTile(renderer, tile, new Rectanglei(0, 0, TileSize, TileSize), destination);
			}
			else
			{
				g.RenderLine(LevelEditor.DimTextColour, new Vector2(cell.X + 8, cell.Y + 8), new Vector2(cell.Right - 8, cell.Bottom - 8), 2.0);
			}
			if (_brush != null && _brush.Columns == 1 && _brush.Rows == 1 && (_brush.Tiles[0, 0] & ~(FlipXFlag | FlipYFlag)) == tile)
			{
				g.RenderRectangle(LevelEditor.SelectionColour, cell, 3.0);
			}
			else if (cell.Contains(Editor.Mouse))
			{
				g.RenderRectangle(LevelEditor.HoverColour, cell, 2.0);
			}
		}
	}

	public override void OnPropertiesClick(Vector2 mouse, Rectangle area, int direction)
	{
		int row = (int)Math.Floor((mouse.Y - area.Y) / LevelEditor.RowHeight);
		List<EditorPropertyRow> rows = GetSettingRows();
		if (row >= 0 && row < rows.Count)
		{
			rows[row].Step?.Invoke(direction);
			return;
		}
		Rectangle palette = GetPaletteArea(area);
		for (int i = 0; i < _paletteTiles.Length; i++)
		{
			if (GetPaletteCell(palette, i).Contains(mouse) && palette.Contains(mouse))
			{
				SelectPaletteTile(_paletteTiles[i]);
				return;
			}
		}
	}

	public override void OnPropertiesWheel(double wheel)
	{
		_paletteScroll = Math.Max(0.0, _paletteScroll - wheel * PaletteTileSize);
	}

	private void SelectPaletteTile(int tile)
	{
		byte solidity = (byte)(ToFlags(_solidity1) | (ToFlags(_solidity2) << 2));
		bool front = IsCollisionLayer || GetHighLayerIndex() >= 0;
		_brush = new Brush
		{
			Columns = 1,
			Rows = 1,
			Tiles = new[,] { { tile } },
			// A tile from the palette has no priority part, so it clears the priority tile of the cell
			HighTiles = front && GetHighLayerIndex() >= 0 ? new[,] { { 0 } } : null,
			Solidity = IsCollisionLayer ? new[,] { { tile == 0 ? (byte)0 : solidity } } : null,
			Name = tile == 0 ? "empty" : $"tile {tile}"
		};
	}

	/// <summary>The solidity flags of one path: 0 none, 1 top only, 2 top and sides.</summary>
	private static byte ToFlags(int solidity) => solidity switch
	{
		1 => TileCollision.TopSolidPath1,
		2 => TileCollision.TopSolidPath1 | TileCollision.SidesSolidPath1,
		_ => 0
	};

	#endregion

	#region Level

	private (int Column, int Row) GetCell(Vector2 world) => ((int)Math.Floor(world.X / TileSize), (int)Math.Floor(world.Y / TileSize));

	public override void OnLevelPress()
	{
		if (Editor.IsControlDown)
		{
			_capturing = true;
			Editor.BeginToolDrag();
			return;
		}
		if (_brush == null)
		{
			Editor.ShowMessage("Select a brush first: click a tile in the palette, or Ctrl+drag in the level.");
			return;
		}
		_painting = true;
		_strokeStart = Editor.CaptureSnapshot();
		_strokeEdits.Clear();
		_strokeStamps.Clear();
		_strokeOrigin = GetCell(Editor.MouseWorld);
		_lastCell = _strokeOrigin;
		Stamp(_strokeOrigin);
		Editor.BeginToolDrag();
	}

	public override void OnDrag()
	{
		if (!_painting)
		{
			return;
		}
		// The mouse can move more than one cell in a frame, so paint each cell on the way
		(int column, int row) = GetCell(Editor.MouseWorld);
		int steps = Math.Max(Math.Abs(column - _lastCell.Column), Math.Abs(row - _lastCell.Row));
		for (int step = 1; step <= steps; step++)
		{
			int x = _lastCell.Column + (int)Math.Round((double)(column - _lastCell.Column) * step / steps);
			int y = _lastCell.Row + (int)Math.Round((double)(row - _lastCell.Row) * step / steps);
			// A brush of more than one cell paints on a grid of its own size from the first cell of the stroke
			int stampColumn = _strokeOrigin.Column + (int)Math.Floor((double)(x - _strokeOrigin.Column) / _brush.Columns) * _brush.Columns;
			int stampRow = _strokeOrigin.Row + (int)Math.Floor((double)(y - _strokeOrigin.Row) / _brush.Rows) * _brush.Rows;
			Stamp((stampColumn, stampRow));
		}
		_lastCell = (column, row);
	}

	public override void OnDragEnd()
	{
		if (_capturing)
		{
			_capturing = false;
			CaptureBrush();
			return;
		}
		if (!_painting)
		{
			return;
		}
		_painting = false;
		if (_strokeEdits.Count == 0)
		{
			return;
		}
		Editor.RecordHistory(_strokeStart with { TileEdits = _strokeEdits.ToArray() });
		Editor.MarkTilesChanged();
		if (IsCollisionLayer)
		{
			int left = _strokeEdits.Min(e => e.Column);
			int top = _strokeEdits.Min(e => e.Row);
			Editor.RequestTrace(new Rectanglei(left, top, _strokeEdits.Max(e => e.Column) - left + 1, _strokeEdits.Max(e => e.Row) - top + 1));
		}
	}

	/// <summary>Paints the brush with its top left at a cell, once for each cell in a stroke.</summary>
	private void Stamp((int Column, int Row) origin)
	{
		if (!_strokeStamps.Add(origin))
		{
			return;
		}
		int highIndex = _brush.HighTiles != null ? GetHighLayerIndex() : -1;
		TileCollision tileCollision = IsCollisionLayer ? Editor.TileCollision : null;
		for (int y = 0; y < _brush.Rows; y++)
		{
			for (int x = 0; x < _brush.Columns; x++)
			{
				int column = origin.Column + x;
				int row = origin.Row + y;
				if (column < 0 || row < 0 || column >= Layer.Columns || row >= Layer.Rows)
				{
					continue;
				}
				byte oldSolidity = tileCollision?.GetSolidity(column, row) ?? 0;
				byte newSolidity = _brush.Solidity != null && tileCollision != null ? _brush.Solidity[x, y] : oldSolidity;
				Paint(_layerIndex, column, row, _brush.Tiles[x, y], oldSolidity, newSolidity);
				if (highIndex >= 0)
				{
					Paint(highIndex, column, row, _brush.HighTiles[x, y], 0, 0);
				}
			}
		}
	}

	private void Paint(int layerIndex, int column, int row, int tile, byte oldSolidity, byte newSolidity)
	{
		LevelLayer layer = Layers[layerIndex];
		if (column < 0 || row < 0 || column >= layer.Columns || row >= layer.Rows)
		{
			return;
		}
		int oldTile = layer.Tiles[column, row];
		if (oldTile == tile && oldSolidity == newSolidity)
		{
			return;
		}
		_strokeEdits.Add(new TileEdit(layerIndex, column, row, oldTile, tile, oldSolidity, newSolidity));
		Editor.SetTile(layerIndex, column, row, tile, newSolidity);
	}

	/// <summary>Takes the cells in the box of the drag as the brush.</summary>
	private void CaptureBrush()
	{
		(int left, int top) = GetCell(new Vector2(Math.Min(Editor.DragStartWorld.X, Editor.MouseWorld.X), Math.Min(Editor.DragStartWorld.Y, Editor.MouseWorld.Y)));
		(int right, int bottom) = GetCell(new Vector2(Math.Max(Editor.DragStartWorld.X, Editor.MouseWorld.X), Math.Max(Editor.DragStartWorld.Y, Editor.MouseWorld.Y)));
		left = Math.Clamp(left, 0, Layer.Columns - 1);
		top = Math.Clamp(top, 0, Layer.Rows - 1);
		right = Math.Clamp(right, left, Layer.Columns - 1);
		bottom = Math.Clamp(bottom, top, Layer.Rows - 1);
		int columns = right - left + 1;
		int rows = bottom - top + 1;
		int highIndex = GetHighLayerIndex();
		TileCollision tileCollision = IsCollisionLayer ? Editor.TileCollision : null;
		Brush brush = new Brush
		{
			Columns = columns,
			Rows = rows,
			Tiles = new int[columns, rows],
			HighTiles = highIndex >= 0 ? new int[columns, rows] : null,
			Solidity = tileCollision != null ? new byte[columns, rows] : null,
			Name = "from the level"
		};
		for (int y = 0; y < rows; y++)
		{
			for (int x = 0; x < columns; x++)
			{
				brush.Tiles[x, y] = Layer.Tiles[left + x, top + y];
				if (brush.HighTiles != null)
				{
					LevelLayer high = Layers[highIndex];
					brush.HighTiles[x, y] = left + x < high.Columns && top + y < high.Rows ? high.Tiles[left + x, top + y] : 0;
				}
				if (brush.Solidity != null)
				{
					brush.Solidity[x, y] = tileCollision.GetSolidity(left + x, top + y);
				}
			}
		}
		_brush = brush;
		Editor.ShowMessage($"Brush: {columns} x {rows} cells. Click or drag to paint.");
	}

	/// <summary>Mirrors the brush. Each tile changes its flip flag, and the cells change places.</summary>
	private void FlipBrush(bool horizontal)
	{
		if (_brush == null)
		{
			return;
		}
		int flag = horizontal ? FlipXFlag : FlipYFlag;
		int[,] Flip(int[,] tiles, bool toggle)
		{
			if (tiles == null)
			{
				return null;
			}
			int[,] result = new int[_brush.Columns, _brush.Rows];
			for (int y = 0; y < _brush.Rows; y++)
			{
				for (int x = 0; x < _brush.Columns; x++)
				{
					int tile = horizontal ? tiles[_brush.Columns - 1 - x, y] : tiles[x, _brush.Rows - 1 - y];
					result[x, y] = toggle && tile != 0 ? tile ^ flag : tile;
				}
			}
			return result;
		}
		_brush.Tiles = Flip(_brush.Tiles, true);
		_brush.HighTiles = Flip(_brush.HighTiles, true);
		if (_brush.Solidity != null)
		{
			byte[,] solidity = new byte[_brush.Columns, _brush.Rows];
			for (int y = 0; y < _brush.Rows; y++)
			{
				for (int x = 0; x < _brush.Columns; x++)
				{
					solidity[x, y] = horizontal ? _brush.Solidity[_brush.Columns - 1 - x, y] : _brush.Solidity[x, _brush.Rows - 1 - y];
				}
			}
			_brush.Solidity = solidity;
		}
	}

	public override bool Cancel()
	{
		if (_brush == null)
		{
			return false;
		}
		_brush = null;
		return true;
	}

	#endregion

	#region Drawing

	public override void DrawOverlay(Renderer renderer, I2dRenderer g)
	{
		Rectangle view = Editor.Camera.Bounds;
		// The grid of the tiles, when the tiles are large enough to see it
		if (Editor.Zoom >= 0.5)
		{
			int firstColumn = (int)Math.Floor(view.X / TileSize);
			int firstRow = (int)Math.Floor(view.Y / TileSize);
			for (int column = firstColumn; column * TileSize < view.Right; column++)
			{
				double x = Editor.ToScreen(new Vector2(column * TileSize, 0)).X;
				g.RenderLine(GridColour, new Vector2(x, 0), new Vector2(x, LevelEditor.ScreenHeight), 1.0);
			}
			for (int row = firstRow; row * TileSize < view.Bottom; row++)
			{
				double y = Editor.ToScreen(new Vector2(0, row * TileSize)).Y;
				g.RenderLine(GridColour, new Vector2(0, y), new Vector2(LevelEditor.ScreenWidth, y), 1.0);
			}
		}
		// The collision of the front layer, so that the user sees how the tiles change it
		if (IsCollisionLayer)
		{
			foreach (CollisionVector vector in Editor.Vectors.Where(v => (v.Paths & 3) != 0))
			{
				Vector2 a = Editor.ToScreen((Vector2)vector.AbsoluteA);
				Vector2 b = Editor.ToScreen((Vector2)vector.AbsoluteB);
				if (Math.Max(a.X, b.X) >= 0 && Math.Min(a.X, b.X) <= LevelEditor.ScreenWidth && Math.Max(a.Y, b.Y) >= 0 && Math.Min(a.Y, b.Y) <= LevelEditor.ScreenHeight)
				{
					g.RenderLine(LevelEditor.ShadowColour, a, b, 4.0);
					g.RenderLine(CollisionColour, a, b, 2.0);
				}
			}
		}
		if (_capturing && Editor.DragMoved)
		{
			(int left, int top) = GetCell(new Vector2(Math.Min(Editor.DragStartWorld.X, Editor.MouseWorld.X), Math.Min(Editor.DragStartWorld.Y, Editor.MouseWorld.Y)));
			(int right, int bottom) = GetCell(new Vector2(Math.Max(Editor.DragStartWorld.X, Editor.MouseWorld.X), Math.Max(Editor.DragStartWorld.Y, Editor.MouseWorld.Y)));
			Rectangle box = Editor.ToScreen(new Rectangle(left * TileSize, top * TileSize, (right - left + 1) * TileSize, (bottom - top + 1) * TileSize));
			g.RenderQuad(LevelEditor.SelectionFillColour, box);
			g.RenderRectangle(LevelEditor.SelectionColour, box, 2.0);
		}
	}

	public override string GetCursorLabel(I2dRenderer g)
	{
		if (_capturing || !Editor.IsMouseOverLevel)
		{
			return null;
		}
		(int column, int row) = GetCell(Editor.MouseWorld);
		int columns = _brush?.Columns ?? 1;
		int rows = _brush?.Rows ?? 1;
		Rectangle cell = Editor.ToScreen(new Rectangle(column * TileSize, row * TileSize, columns * TileSize, rows * TileSize));
		if (_brush != null && !_painting)
		{
			// A preview of the brush at the mouse
			TileSet tileSet = Editor.Level.TileSet;
			int size = (int)Math.Round(TileSize * Editor.Zoom);
			for (int y = 0; y < rows; y++)
			{
				for (int x = 0; x < columns; x++)
				{
					int tile = _brush.Tiles[x, y];
					if (tile != 0)
					{
						tileSet.DrawTile(Editor.Renderer, tile, new Rectanglei(0, 0, TileSize, TileSize), new Rectanglei((int)cell.X + x * size, (int)cell.Y + y * size, size, size));
					}
				}
			}
		}
		g.RenderRectangle(LevelEditor.SelectionColour, cell, 2.0);
		if (column < 0 || row < 0 || column >= Layer.Columns || row >= Layer.Rows)
		{
			return null;
		}
		int value = Layer.Tiles[column, row];
		string flips = ((value & FlipXFlag) != 0 ? " X" : "") + ((value & FlipYFlag) != 0 ? " Y" : "");
		string solidity = IsCollisionLayer ? $"  solid {Editor.TileCollision.GetSolidity(column, row)}" : "";
		return $"tile {value & ~(FlipXFlag | FlipYFlag)}{flips}{solidity}";
	}

	#endregion
}
