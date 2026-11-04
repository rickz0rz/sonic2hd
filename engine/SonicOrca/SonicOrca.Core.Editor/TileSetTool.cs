using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Hjg.Pngcs;
using SonicOrca.Geometry;
using SonicOrca.Graphics;
using SonicOrca.Resources;

namespace SonicOrca.Core.Editor;

/// <summary>
/// The tile set tool shows a texture of the tile set of the level, with a grid of the tile size. A click on an empty
/// cell adds a tile for this cell, and a drag moves a tile to another cell. The tiles keep their IDs, so the map
/// shows the moved tiles at once. The tool reads the texture again when the artist saves its file.
/// </summary>
internal sealed class TileSetTool : EditorTool
{
	private const int CellSize = Tile.Size;

	/// <summary>The tool looks at the times of the texture files after this number of frames.</summary>
	private const int WatchFrames = 30;

	private static readonly Colour BackgroundColour = new Colour(1.0, 0.08, 0.08, 0.1);

	private static readonly Colour GridColour = new Colour(0.25, 1.0, 1.0, 1.0);

	private static readonly Colour TileColour = new Colour(0.9, 0.95, 0.3, 0.85);

	private static readonly Colour NewCellColour = new Colour(1.0, 1.0, 0.9, 0.1);

	private static readonly Colour AnimatedColour = new Colour(0.9, 0.35, 0.75, 1.0);

	private int _textureIndex;

	/// <summary>The ID of the selected tile, or 0.</summary>
	private int _selected;

	/// <summary>The number of cells of the map that use the selected tile.</summary>
	private int _selectedUses;

	private bool _moving;

	private Vector2? _viewBefore;

	/// <summary>For each texture, the cells that have a pixel that is not fully transparent, or null when the tool cannot read the texture.</summary>
	private readonly Dictionary<int, bool[,]> _usedCells = new Dictionary<int, bool[,]>();

	/// <summary>The files of the textures that the tool watches, and their last write times.</summary>
	private readonly Dictionary<int, (string Path, DateTime Time)> _files = new Dictionary<int, (string, DateTime)>();

	private readonly List<Task<(int Index, ITexture Texture, byte[] Data)>> _reloads = new List<Task<(int, ITexture, byte[])>>();

	private int _frames;

	public TileSetTool(LevelEditor editor)
		: base(editor)
	{
	}

	public override string Name => "Tile set";

	public override string Hint => "Click an empty cell to add a tile. Drag a tile to another cell. Delete removes a tile that the map does not use.";

	public override string Status
	{
		get
		{
			(int column, int row) = GetCell(Editor.MouseWorld);
			string cell = InTexture(column, row) ? $"Cell {column}, {row}" : "Outside the texture";
			int tile = InTexture(column, row) ? GetTileAt(column, row) : 0;
			return tile != 0 ? $"{cell}: tile {tile}" : cell;
		}
	}

	public override string ListHeading => "Textures of the tile set (click to show)";

	public override string PropertiesHeading => _selected != 0 ? $"Tile {_selected}" : "Tile (click a tile to select it)";

	public override bool CoversLevel => true;

	public override bool HasSelection => _selected != 0;

	private TileSet TileSet => Editor.Level.TileSet;

	private ITexture Texture => _textureIndex < TileSet.Textures.Count ? TileSet.Textures[_textureIndex] : null;

	public override void OnNewLevel()
	{
		_textureIndex = 0;
		_selected = 0;
		_viewBefore = null;
		_usedCells.Clear();
		_files.Clear();
	}

	/// <summary>Shows the texture from its top left corner, under the top bar. The view of the level comes back later.</summary>
	public override void Activate()
	{
		Rectangle view = Editor.Camera.Bounds;
		_viewBefore ??= view.Centre;
		Editor.CentreOn(new Vector2(view.Width / 2.0 - LevelEditor.Padding / Editor.Zoom, view.Height / 2.0 - (LevelEditor.TopBarHeight + LevelEditor.Padding) / Editor.Zoom));
		WatchFiles();
	}

	public override void Deactivate()
	{
		_moving = false;
		if (_viewBefore is Vector2 centre)
		{
			Editor.CentreOn(centre);
			_viewBefore = null;
		}
	}

	public override void Update()
	{
		UpdateReloads();
		if (++_frames % WatchFrames == 0)
		{
			LookAtFiles();
		}
	}

	#region List

	public override IReadOnlyList<EditorListRow> GetListRows()
	{
		List<EditorListRow> rows = new List<EditorListRow>();
		for (int i = 0; i < TileSet.Textures.Count; i++)
		{
			ITexture texture = TileSet.Textures[i];
			int tiles = TileSet.Values.Count(t => TileSetWriter.IsStill(t) && ((Tile)t).Frames[0].TextureId == i);
			rows.Add(new EditorListRow
			{
				Text = $"{i}: {GetTextureName(i)}",
				Detail = texture != null ? $"{texture.Width} x {texture.Height}, {tiles} tiles" : "not loaded",
				Active = i == _textureIndex,
			});
		}
		int newCells = GetNewCells().Count;
		rows.Add(new EditorListRow
		{
			Text = "Add the new cells",
			Detail = newCells == 1 ? "1 cell" : $"{newCells} cells",
			Dim = newCells == 0,
		});
		return rows;
	}

	public override void OnListClick(int index, int direction)
	{
		if (index >= 0 && index < TileSet.Textures.Count)
		{
			_textureIndex = index;
			_selected = 0;
			return;
		}
		if (index == TileSet.Textures.Count)
		{
			AddNewCells();
		}
	}

	#endregion

	#region Properties

	public override List<EditorPropertyRow> GetPropertyRows()
	{
		List<EditorPropertyRow> rows = new List<EditorPropertyRow>();
		if (_selected == 0 || TileSet[_selected] == null)
		{
			rows.Add(new EditorPropertyRow { Label = "Cells with a tile", Value = "a magenta outline, with the tile ID" });
			rows.Add(new EditorPropertyRow { Label = "Cells to add", Value = "a yellow outline: pixels, but no tile" });
			rows.Add(new EditorPropertyRow { Label = "Animated tiles", Value = "a blue outline on their frames" });
			rows.Add(new EditorPropertyRow { Label = "Texture file", Value = GetFileText() });
			return rows;
		}
		Tile tile = TileSet[_selected] as Tile;
		rows.Add(new EditorPropertyRow { Label = "ID", Value = _selected.ToString() });
		if (TileSetWriter.IsStill(tile))
		{
			Tile.Frame frame = tile.Frames[0];
			rows.Add(new EditorPropertyRow { Label = "Cell", Value = $"{frame.X / CellSize}, {frame.Y / CellSize} (pixels {frame.X}, {frame.Y})" });
		}
		else
		{
			rows.Add(new EditorPropertyRow { Label = "Frames", Value = "animated: change it in the animation editor" });
		}
		rows.Add(new EditorPropertyRow { Label = "Uses in the map", Value = _selectedUses == 1 ? "1 cell" : $"{_selectedUses} cells" });
		rows.Add(new EditorPropertyRow
		{
			Label = "Remove",
			Value = _selectedUses == 0 ? "click to remove the tile" : "the map uses the tile",
			Step = _ => DeleteSelection(),
		});
		return rows;
	}

	#endregion

	#region Level

	public override void OnLevelPress()
	{
		(int column, int row) = GetCell(Editor.MouseWorld);
		if (!InTexture(column, row))
		{
			Editor.ShowMessage("This cell is outside the texture.");
			return;
		}
		int tile = GetTileAt(column, row);
		if (tile == 0)
		{
			AddTile(column, row);
			return;
		}
		Select(tile);
		if (TileSetWriter.IsStill(TileSet[tile]))
		{
			_moving = true;
			Editor.BeginToolDrag();
		}
	}

	public override void OnDragEnd()
	{
		if (!_moving)
		{
			return;
		}
		_moving = false;
		(int column, int row) = GetCell(Editor.MouseWorld);
		if (!InTexture(column, row) || TileSet[_selected] is not Tile tile)
		{
			return;
		}
		Tile.Frame frame = tile.Frames[0];
		if (frame.X == column * CellSize && frame.Y == row * CellSize)
		{
			return;
		}
		int other = GetTileAt(column, row);
		if (other != 0)
		{
			Editor.ShowMessage($"Cell {column}, {row} has tile {other}. Move a tile to an empty cell.");
			return;
		}
		frame.X = column * CellSize;
		frame.Y = row * CellSize;
		TileSet[_selected] = new Tile(TileSet, _selected, new[] { frame }, tile.Blend, tile.Source);
		Editor.MarkTileSetChanged();
		Editor.ShowMessage($"Tile {_selected} is now at cell {column}, {row}. The map shows the new cell at each use of the tile.");
	}

	public override bool Cancel()
	{
		if (_moving)
		{
			_moving = false;
			return true;
		}
		if (_selected != 0)
		{
			_selected = 0;
			return true;
		}
		return false;
	}

	public override void ClearSelection() => _selected = 0;

	/// <summary>Removes the selected tile, when the map does not use it.</summary>
	public override void DeleteSelection()
	{
		if (_selected == 0 || TileSet[_selected] == null)
		{
			return;
		}
		if (_selectedUses > 0)
		{
			Editor.ShowMessage($"The map uses tile {_selected} in {_selectedUses} cells. Paint over these cells first.");
			return;
		}
		if (!TileSetWriter.IsStill(TileSet[_selected]))
		{
			Editor.ShowMessage($"Tile {_selected} is animated. The tile set tool removes only tiles with one frame.");
			return;
		}
		TileSet.Remove(_selected);
		Editor.MarkTileSetChanged(removed: true);
		Editor.ShowMessage($"Removed tile {_selected}. The undo history is clear.");
		_selected = 0;
	}

	private void AddTile(int column, int row)
	{
		int id = GetFreeId();
		if (id == 0)
		{
			Editor.ShowMessage($"The tile set has the most tiles that the map can use ({Tile.IndexMask}).");
			return;
		}
		Tile.Frame frame = new Tile.Frame { TextureId = _textureIndex, X = column * CellSize, Y = row * CellSize, Opacity = 1f };
		TileSet[id] = new Tile(TileSet, id, new[] { frame });
		Editor.MarkTileSetChanged();
		Select(id);
		Editor.ShowMessage($"Added tile {id} at cell {column}, {row}. The palette of the tiles tool has it now.");
	}

	private void AddNewCells()
	{
		List<(int Column, int Row)> cells = GetNewCells();
		if (cells.Count == 0)
		{
			Editor.ShowMessage(_usedCells.TryGetValue(_textureIndex, out bool[,] used) && used != null
				? "Each cell with pixels has a tile."
				: "The tool cannot read the pixels of this texture. Click each cell to add its tile.");
			return;
		}
		int first = 0;
		int added = 0;
		foreach ((int column, int row) in cells)
		{
			int id = GetFreeId();
			if (id == 0)
			{
				break;
			}
			TileSet[id] = new Tile(TileSet, id, new[] { new Tile.Frame { TextureId = _textureIndex, X = column * CellSize, Y = row * CellSize, Opacity = 1f } });
			first = first == 0 ? id : first;
			added++;
		}
		Editor.MarkTileSetChanged();
		Editor.ShowMessage(added == cells.Count
			? $"Added {added} tiles, from tile {first}."
			: $"Added {added} of {cells.Count} tiles. The tile set has the most tiles that the map can use ({Tile.IndexMask}).");
	}

	private void Select(int id)
	{
		_selected = id;
		_selectedUses = 0;
		foreach (LevelLayer layer in Editor.Level.Map.Layers)
		{
			int[,] tiles = layer.Tiles;
			for (int x = 0; x < layer.Columns; x++)
			{
				for (int y = 0; y < layer.Rows; y++)
				{
					if ((tiles[x, y] & Tile.IndexMask) == id)
					{
						_selectedUses++;
					}
				}
			}
		}
	}

	/// <summary>The lowest ID that no tile has, or 0 when the map cannot use more tiles.</summary>
	private int GetFreeId()
	{
		for (int id = 1; id <= Tile.IndexMask; id++)
		{
			if (TileSet[id] == null)
			{
				return id;
			}
		}
		return 0;
	}

	#endregion

	#region Cells

	private static (int Column, int Row) GetCell(Vector2 world) => ((int)Math.Floor(world.X / CellSize), (int)Math.Floor(world.Y / CellSize));

	private bool InTexture(int column, int row)
	{
		ITexture texture = Texture;
		return texture != null && column >= 0 && row >= 0 && column < texture.Width / CellSize && row < texture.Height / CellSize;
	}

	/// <summary>The still tile at a cell of the current texture, or 0.</summary>
	private int GetTileAt(int column, int row)
	{
		foreach (int id in TileSet.Keys)
		{
			if (TileSetWriter.IsStill(TileSet[id]))
			{
				Tile.Frame frame = ((Tile)TileSet[id]).Frames[0];
				if (frame.TextureId == _textureIndex && frame.X == column * CellSize && frame.Y == row * CellSize)
				{
					return id;
				}
			}
		}
		return 0;
	}

	/// <summary>For each cell of the current texture, the still tile there, and the cells that frames of animated tiles use.</summary>
	private (Dictionary<(int, int), int> Tiles, HashSet<(int, int)> Animated) GetCellTiles()
	{
		Dictionary<(int, int), int> tiles = new Dictionary<(int, int), int>();
		HashSet<(int, int)> animated = new HashSet<(int, int)>();
		foreach (int id in TileSet.Keys)
		{
			if (TileSet[id] is not Tile tile)
			{
				continue;
			}
			bool still = TileSetWriter.IsStill(tile);
			foreach (Tile.Frame frame in tile.Frames)
			{
				if (frame.TextureId != _textureIndex)
				{
					continue;
				}
				(int, int) cell = (frame.X / CellSize, frame.Y / CellSize);
				if (still)
				{
					tiles.TryAdd(cell, id);
				}
				else
				{
					animated.Add(cell);
				}
			}
		}
		return (tiles, animated);
	}

	/// <summary>The cells of the current texture that have pixels, but no tile.</summary>
	private List<(int Column, int Row)> GetNewCells()
	{
		List<(int, int)> cells = new List<(int, int)>();
		if (!_usedCells.TryGetValue(_textureIndex, out bool[,] used))
		{
			used = ReadUsedCells(_textureIndex);
			_usedCells[_textureIndex] = used;
		}
		if (used == null)
		{
			return cells;
		}
		(Dictionary<(int, int), int> tiles, HashSet<(int, int)> animated) = GetCellTiles();
		for (int row = 0; row < used.GetLength(1); row++)
		{
			for (int column = 0; column < used.GetLength(0); column++)
			{
				if (used[column, row] && !tiles.ContainsKey((column, row)) && !animated.Contains((column, row)))
				{
					cells.Add((column, row));
				}
			}
		}
		return cells;
	}

	private bool[,] ReadUsedCells(int index)
	{
		Resource resource = GetTextureResource(index);
		if (resource == null)
		{
			return null;
		}
		try
		{
			using Stream source = resource.Source.ReadUncompressed();
			using MemoryStream memory = new MemoryStream();
			source.CopyTo(memory);
			return FindUsedCells(memory.ToArray());
		}
		catch (Exception ex) when (ex is IOException or ResourceException)
		{
			return null;
		}
	}

	/// <summary>The cells of a PNG image with a pixel that is not fully transparent, or null when the tool cannot read the image.</summary>
	internal static bool[,] FindUsedCells(byte[] png)
	{
		PngReader reader;
		try
		{
			reader = new PngReader(new MemoryStream(png));
		}
		catch (Exception ex) when (ex is not OutOfMemoryException)
		{
			return null;
		}
		try
		{
			int channels = reader.ImgInfo.Channels;
			if (reader.ImgInfo.BitDepth != 8 || channels < 3 || reader.ImgInfo.Indexed)
			{
				return null;
			}
			int width = reader.ImgInfo.Cols;
			int height = reader.ImgInfo.Rows;
			bool[,] used = new bool[width / CellSize, height / CellSize];
			for (int y = 0; y < height / CellSize * CellSize; y++)
			{
				byte[] line = reader.ReadRowByte(y).ScanlineB;
				for (int x = 0; x < width / CellSize * CellSize; x++)
				{
					if (channels < 4 || line[x * channels + 3] != 0)
					{
						used[x / CellSize, y / CellSize] = true;
					}
				}
			}
			return used;
		}
		catch (Exception ex) when (ex is not OutOfMemoryException)
		{
			return null;
		}
		finally
		{
			reader.End();
		}
	}

	#endregion

	#region Texture files

	private Resource GetTextureResource(int index) =>
		index < TileSet.TextureKeys.Count ? Editor.GameContext.ResourceTree[TileSet.TextureKeys[index]]?.Resource : null;

	private string GetTextureName(int index) => index < TileSet.TextureKeys.Count ? TileSet.TextureKeys[index].Split('/')[^1] : "texture";

	/// <summary>The file of a texture of a mod folder, which the artist can change, or null for a texture in a .dat file.</summary>
	private string GetTextureFile(int index)
	{
		Resource resource = GetTextureResource(index);
		if (resource?.Source is FileResourceSource file && file.Offset == 0 &&
			string.Equals(Path.GetExtension(file.Path), ".png", StringComparison.OrdinalIgnoreCase))
		{
			return file.Path;
		}
		return _files.TryGetValue(index, out (string Path, DateTime) watched) ? watched.Path : null;
	}

	private string GetFileText()
	{
		string path = GetTextureFile(_textureIndex);
		return path != null ? Path.GetFileName(path) + " (reloads on a save)" : "in a .dat file (unpack it to edit it)";
	}

	private void WatchFiles()
	{
		for (int i = 0; i < TileSet.Textures.Count; i++)
		{
			string path = GetTextureFile(i);
			if (path != null && File.Exists(path) && !_files.ContainsKey(i))
			{
				_files[i] = (path, File.GetLastWriteTimeUtc(path));
			}
		}
	}

	private void LookAtFiles()
	{
		foreach ((int index, (string path, DateTime time)) in _files.ToArray())
		{
			if (File.Exists(path) && File.GetLastWriteTimeUtc(path) != time)
			{
				Reload(index, path);
			}
		}
	}

	/// <summary>Reads a texture file and makes a texture from it in the background. A file that is not ready yet is read at the next look.</summary>
	private void Reload(int index, string path)
	{
		DateTime time = File.GetLastWriteTimeUtc(path);
		byte[] data = AnimationTextureFiles.TryRead(path);
		if (data == null || !AnimationTextureFiles.IsPng(data))
		{
			return;
		}
		_files[index] = (path, time);
		ResourceTree tree = Editor.GameContext.ResourceTree;
		string key = TileSet.TextureKeys[index];
		_reloads.Add(Task.Run(async () =>
		{
			Resource resource = new Resource(key, ResourceTypeIdentifier.TexturePNG, new MemoryResourceSource(data));
			ILoadedResource texture = await ResourceType.FromIdentifier(ResourceTypeIdentifier.TexturePNG)
				.LoadAsync(new ResourceLoadArgs(tree, resource, new MemoryStream(data)));
			return (index, (ITexture)texture, data);
		}));
	}

	/// <summary>Puts the textures that finished loading in place. The resource of the game gets the new data for the later loads.</summary>
	private void UpdateReloads()
	{
		for (int i = _reloads.Count - 1; i >= 0; i--)
		{
			Task<(int Index, ITexture Texture, byte[] Data)> task = _reloads[i];
			if (!task.IsCompleted)
			{
				continue;
			}
			_reloads.RemoveAt(i);
			if (task.IsFaulted)
			{
				Editor.ShowMessage("Cannot read the texture: " + task.Exception?.GetBaseException().Message);
				continue;
			}
			(int index, ITexture texture, byte[] data) = task.Result;
			if (index >= TileSet.Textures.Count)
			{
				texture.Dispose();
				continue;
			}
			Resource resource = GetTextureResource(index);
			if (resource != null)
			{
				resource.Source = new MemoryResourceSource(data);
			}
			TileSet.SetTexture(index, texture);
			_usedCells[index] = FindUsedCells(data);
			Editor.ShowMessage($"Reloaded {GetTextureName(index)} ({texture.Width} x {texture.Height}). {GetNewCells().Count} cells have pixels, but no tile.");
		}
	}

	#endregion

	#region Drawing

	public override void DrawOverlay(Renderer renderer, I2dRenderer g)
	{
		Rectangle area = new Rectangle(0.0, LevelEditor.TopBarHeight, LevelEditor.ScreenWidth - LevelEditor.PanelWidth,
			LevelEditor.ScreenHeight - LevelEditor.TopBarHeight - LevelEditor.StatusBarHeight);
		g.RenderQuad(BackgroundColour, area);
		ITexture texture = Texture;
		if (texture == null)
		{
			return;
		}
		Rectangle image = Editor.ToScreen(new Rectangle(0.0, 0.0, texture.Width, texture.Height));
		g.Colour = Colours.White;
		g.RenderTexture(texture, new Rectangle(0.0, 0.0, texture.Width, texture.Height), image);
		int columns = texture.Width / CellSize;
		int rows = texture.Height / CellSize;
		double cell = CellSize * Editor.Zoom;
		for (int column = 0; column <= columns; column++)
		{
			g.RenderLine(GridColour, new Vector2(image.X + column * cell, image.Y), new Vector2(image.X + column * cell, image.Y + rows * cell), 1.0);
		}
		for (int row = 0; row <= rows; row++)
		{
			g.RenderLine(GridColour, new Vector2(image.X, image.Y + row * cell), new Vector2(image.X + columns * cell, image.Y + row * cell), 1.0);
		}
		(Dictionary<(int, int), int> tiles, HashSet<(int, int)> animated) = GetCellTiles();
		foreach ((int column, int row) in GetNewCells())
		{
			g.RenderRectangle(NewCellColour, CellRectangle(image, column, row, cell).Inflate(new Vector2(-3.0, -3.0)), 2.0);
		}
		foreach ((int column, int row) in animated)
		{
			g.RenderRectangle(AnimatedColour, CellRectangle(image, column, row, cell).Inflate(new Vector2(-3.0, -3.0)), 1.0);
		}
		bool labels = cell >= 48.0;
		foreach (((int column, int row), int id) in tiles)
		{
			Rectangle box = CellRectangle(image, column, row, cell);
			if (!box.IntersectsWith(area))
			{
				continue;
			}
			g.RenderRectangle(TileColour, box.Inflate(new Vector2(-1.0, -1.0)), 1.0);
			if (labels && Editor.HasFont)
			{
				// A shadow keeps the ID readable on a light tile
				g.RenderQuad(LevelEditor.ShadowColour, new Rectangle(box.X + 1.0, box.Y + 1.0, Editor.MeasureText(id.ToString()) + 6.0, 26.0));
				Editor.DrawText(renderer, id.ToString(), box.X + 4.0, box.Y + 2.0, Colours.White);
			}
		}
		if (_selected != 0 && TileSet[_selected] is Tile selected && TileSetWriter.IsStill(selected) && selected.Frames[0].TextureId == _textureIndex)
		{
			Tile.Frame frame = selected.Frames[0];
			g.RenderRectangle(LevelEditor.SelectionColour, CellRectangle(image, frame.X / CellSize, frame.Y / CellSize, cell), 3.0);
		}
		(int mouseColumn, int mouseRow) = GetCell(Editor.MouseWorld);
		if (InTexture(mouseColumn, mouseRow) && area.Contains(Editor.Mouse))
		{
			Colour colour = _moving ? LevelEditor.SelectionColour : Colours.White;
			g.RenderRectangle(colour, CellRectangle(image, mouseColumn, mouseRow, cell), 2.0);
		}
	}

	private static Rectangle CellRectangle(Rectangle image, int column, int row, double cell) =>
		new Rectangle(image.X + column * cell, image.Y + row * cell, cell, cell);

	public override string GetCursorLabel(I2dRenderer g)
	{
		(int column, int row) = GetCell(Editor.MouseWorld);
		if (!InTexture(column, row))
		{
			return null;
		}
		int tile = GetTileAt(column, row);
		if (_moving)
		{
			return tile == 0 ? $"move tile {_selected} here" : $"tile {tile} is here";
		}
		return tile != 0 ? $"tile {tile}" : "click to add a tile";
	}

	#endregion
}
