using System.Collections.Generic;
using System.Linq;
using SonicOrca.Graphics;

namespace SonicOrca.Core.Editor;

/// <summary>
/// Opens levels, makes new levels and changes the order of the levels of the story. The list shows the levels of the
/// story in their order, then the levels that are not in the story. The story goes from each level to the next level
/// of the list. Save writes the order into mods/editor/LEVELS.dat. Undo does not change the order.
/// </summary>
internal sealed class LevelsTool : EditorTool
{
	private static readonly Colour OpenColour = new Colour(1.0, 0.45, 0.85, 1.0);

	/// <summary>The levels of the game, from the catalog.</summary>
	private IReadOnlyList<LevelCatalogEntry> _levels = new LevelCatalogEntry[0];

	/// <summary>The short names of the levels of the story, in their order, with the changes that are not saved.</summary>
	private readonly List<string> _story = new List<string>();

	private string _selected;

	/// <summary>The level of the first click on "Open" while the editor has changes that are not saved.</summary>
	private string _confirmOpen;

	/// <summary>True while the properties show the form of a new level.</summary>
	private bool _creating;

	private string _newName = "";

	private string _newMnemonic = "";

	/// <summary>The short name of the level with the tile set for the new level.</summary>
	private string _newTilesFrom;

	/// <summary>True after the first click on "Create" while the editor has changes that are not saved.</summary>
	private bool _confirmCreate;

	public LevelsTool(LevelEditor editor)
		: base(editor)
	{
	}

	public override string Name => "Levels";

	public override string Hint => _creating
		? "Type a name, choose the level with the tiles, then click Create. The new level opens in the editor."
		: "Click a level in the list. Then open it, move it in the story, or add it to the story or remove it.";

	public override string Status => $"Levels {_levels.Count}   In the story {_story.Count}";

	public override string ListHeading => "Levels (the story first, in its order)";

	public override string PropertiesHeading => _creating ? "New level (click a value to change it)"
		: _selected == null ? "Level (select a level)" : "Level (click a value to change it)";

	/// <summary>True when the order has changes that are not saved.</summary>
	public bool OrderDirty { get; private set; }

	private ILevelCatalog Catalog => Editor.GameContext.LevelCatalog;

	public override void Begin()
	{
		if (!OrderDirty)
		{
			Reload();
		}
	}

	/// <summary>Writes the order of the story. Returns the path of the file.</summary>
	public string SaveOrder()
	{
		string path = Catalog.SaveStoryOrder(_story);
		OrderDirty = false;
		Reload();
		return path;
	}

	public override void OnNewLevel()
	{
		OrderDirty = false;
		Reload();
	}

	private void Reload()
	{
		_levels = Catalog?.GetLevels() ?? new LevelCatalogEntry[0];
		_story.Clear();
		_story.AddRange(_levels.Where(level => level.InStory).Select(level => level.Mnemonic));
	}

	/// <summary>The levels in the order of the list: the story, then the other levels.</summary>
	private IReadOnlyList<LevelCatalogEntry> GetOrderedLevels()
	{
		IEnumerable<LevelCatalogEntry> story = _story.Select(mnemonic => _levels.First(level => level.Mnemonic == mnemonic));
		IEnumerable<LevelCatalogEntry> others = _levels.Where(level => EditorLevelOrder.IndexOf(_story, level.Mnemonic) < 0);
		return story.Concat(others).ToArray();
	}

	private LevelCatalogEntry Selected => _levels.FirstOrDefault(level => level.Mnemonic == _selected);

	/// <summary>The short name of the level that the editor has open, from the key of its binding.</summary>
	private string OpenMnemonic
	{
		get
		{
			string[] parts = Editor.Level.Binding.Resource.FullKeyPath.Split('/');
			return parts.Length >= 2 ? parts[parts.Length - 2].ToLowerInvariant() : null;
		}
	}

	#region List

	/// <summary>The first row makes a new level. The other rows are the levels.</summary>
	private const int NewRowCount = 1;

	public override IReadOnlyList<EditorListRow> GetListRows()
	{
		if (Catalog == null)
		{
			return new[] { new EditorListRow { Text = "This game gives no list of levels", Dim = true } };
		}
		string open = OpenMnemonic;
		EditorListRow newRow = new EditorListRow { Text = "+ New level", Active = _creating };
		return GetOrderedLevels().Select(level =>
		{
			int position = EditorLevelOrder.IndexOf(_story, level.Mnemonic);
			return new EditorListRow
			{
				Text = position >= 0 ? $"{position + 1}: {level.Name}" : level.Name,
				Detail = level.Mnemonic == open ? "open" : position >= 0 ? "" : "not in story",
				DetailColour = level.Mnemonic == open ? OpenColour : null,
				Active = !_creating && level.Mnemonic == _selected,
				Dim = position < 0
			};
		}).Prepend(newRow).ToArray();
	}

	public override void OnListClick(int index, int direction)
	{
		_confirmOpen = null;
		_confirmCreate = false;
		if (index < NewRowCount)
		{
			_creating = true;
			_newTilesFrom ??= OpenMnemonic;
			return;
		}
		IReadOnlyList<LevelCatalogEntry> levels = GetOrderedLevels();
		index -= NewRowCount;
		if (index >= 0 && index < levels.Count)
		{
			_creating = false;
			_selected = levels[index].Mnemonic;
		}
	}

	#endregion

	#region Properties

	public override List<EditorPropertyRow> GetPropertyRows()
	{
		if (_creating)
		{
			return GetNewLevelRows();
		}
		LevelCatalogEntry level = Selected;
		if (level == null)
		{
			return new List<EditorPropertyRow>();
		}
		int position = EditorLevelOrder.IndexOf(_story, level.Mnemonic);
		List<EditorPropertyRow> rows = new List<EditorPropertyRow>
		{
			new EditorPropertyRow { Label = "Open", Value = GetOpenText(level), Step = _ => Open(level) },
			new EditorPropertyRow { Label = "Name", Value = level.Name },
			new EditorPropertyRow { Label = "Short name", Value = level.Mnemonic },
			new EditorPropertyRow { Label = "Acts", Value = level.Acts.ToString() },
			new EditorPropertyRow { Label = "From", Value = level.IsMod ? "a mod" : "Sonic 2" },
			new EditorPropertyRow
			{
				Label = "In the story",
				Value = position >= 0 ? $"yes, level {position + 1} of {_story.Count}" : "no",
				Step = _ => Change(EditorLevelOrder.SetInStory(_story, level.Mnemonic, position < 0))
			}
		};
		if (position >= 0)
		{
			rows.Add(new EditorPropertyRow { Label = "Move up", Value = position > 0 ? "click" : "first level", Step = _ => Change(EditorLevelOrder.Move(_story, level.Mnemonic, -1)) });
			rows.Add(new EditorPropertyRow { Label = "Move down", Value = position < _story.Count - 1 ? "click" : "last level", Step = _ => Change(EditorLevelOrder.Move(_story, level.Mnemonic, 1)) });
			string next = position < _story.Count - 1 ? _levels.First(x => x.Mnemonic == _story[position + 1]).Name : "the end of the game";
			rows.Add(new EditorPropertyRow { Label = "Next level", Value = next });
		}
		return rows;
	}

	/// <summary>The form of a new level: the name, the short name, the level with the tiles, and Create.</summary>
	private List<EditorPropertyRow> GetNewLevelRows()
	{
		LevelCatalogEntry tilesFrom = _levels.FirstOrDefault(level => level.Mnemonic == _newTilesFrom) ?? _levels.FirstOrDefault();
		return new List<EditorPropertyRow>
		{
			new EditorPropertyRow
			{
				Label = "Name",
				Value = _newName.Length > 0 ? _newName : "click to type",
				Commit = text =>
				{
					// The short name follows the name until the user types a short name
					if (_newMnemonic == MakeMnemonic(_newName))
					{
						_newMnemonic = MakeMnemonic(text);
					}
					_newName = text.Trim();
					return true;
				}
			},
			new EditorPropertyRow
			{
				Label = "Short name",
				Value = _newMnemonic.Length > 0 ? _newMnemonic : "letters and digits",
				Commit = text =>
				{
					string mnemonic = text.Trim().ToLowerInvariant();
					if (mnemonic.Length == 0 || !mnemonic.All(char.IsAsciiLetterOrDigit))
					{
						Editor.ShowMessage("The short name can have only letters and digits.");
						return false;
					}
					_newMnemonic = mnemonic;
					return true;
				}
			},
			new EditorPropertyRow
			{
				Label = "Tiles from",
				Value = tilesFrom?.Name ?? "no level",
				Step = direction => _newTilesFrom = StepLevel(tilesFrom, direction)
			},
			new EditorPropertyRow
			{
				Label = "Create",
				Value = _confirmCreate ? "click again to open without a save" : "click to make the level and open it",
				Step = _ => Create(tilesFrom)
			}
		};
	}

	/// <summary>Gives the short name of the next level (1) or the level before (-1) in the list.</summary>
	private string StepLevel(LevelCatalogEntry current, int direction)
	{
		IReadOnlyList<LevelCatalogEntry> levels = GetOrderedLevels();
		if (levels.Count == 0)
		{
			return null;
		}
		int index = current == null ? 0 : levels.ToList().FindIndex(level => level.Mnemonic == current.Mnemonic);
		return levels[((index + direction) % levels.Count + levels.Count) % levels.Count].Mnemonic;
	}

	/// <summary>The short name from a name: the letters and digits, in lower case.</summary>
	private static string MakeMnemonic(string name)
	{
		string mnemonic = new string(name.Where(char.IsAsciiLetterOrDigit).Select(char.ToLowerInvariant).ToArray());
		return mnemonic.Length > 16 ? mnemonic.Substring(0, 16) : mnemonic;
	}

	/// <summary>
	/// Makes the new level and opens it. With changes that are not saved, the first click only warns, as for Open.
	/// </summary>
	private void Create(LevelCatalogEntry tilesFrom)
	{
		if (tilesFrom == null)
		{
			return;
		}
		if (Editor.Dirty && !_confirmCreate)
		{
			_confirmCreate = true;
			Editor.ShowMessage("The editor has changes that are not saved. Save first, or click Create again to make the level without them.");
			return;
		}
		_confirmCreate = false;
		LevelCatalogEntry level;
		try
		{
			level = Catalog.CreateLevel(_newMnemonic, _newName, tilesFrom.Mnemonic);
		}
		catch (System.Exception ex) when (ex is System.InvalidOperationException || ex is System.IO.IOException || ex is System.UnauthorizedAccessException)
		{
			Editor.ShowMessage("The level was not made: " + ex.Message);
			return;
		}
		_creating = false;
		_selected = level.Mnemonic;
		_newName = "";
		_newMnemonic = "";
		Editor.ShowMessage($"Made the level {level.Name}. Opening it.");
		Editor.RequestOpenLevel(level);
	}

	private string GetOpenText(LevelCatalogEntry level)
	{
		if (level.Mnemonic == OpenMnemonic)
		{
			return "open now";
		}
		return _confirmOpen == level.Mnemonic ? "click again to open without a save" : "click to open in the editor";
	}

	/// <summary>
	/// Opens the level in the editor. With changes that are not saved, the first click only warns. The editor then
	/// keeps the changes in its backup folder, not in the level.
	/// </summary>
	private void Open(LevelCatalogEntry level)
	{
		if (level.Mnemonic == OpenMnemonic)
		{
			return;
		}
		if (Editor.Dirty && _confirmOpen != level.Mnemonic)
		{
			_confirmOpen = level.Mnemonic;
			Editor.ShowMessage("The editor has changes that are not saved. Save first, or click Open again to open the level without them.");
			return;
		}
		_confirmOpen = null;
		Editor.ShowMessage("Opening " + level.Name);
		Editor.RequestOpenLevel(level);
	}

	private void Change(bool changed)
	{
		if (changed)
		{
			OrderDirty = true;
		}
	}

	#endregion

	public override void OnLevelPress()
	{
	}

	public override void DrawOverlay(Renderer renderer, I2dRenderer g)
	{
	}
}
