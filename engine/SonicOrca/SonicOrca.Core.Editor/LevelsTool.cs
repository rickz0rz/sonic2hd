using System.Collections.Generic;
using System.Linq;
using SonicOrca.Graphics;

namespace SonicOrca.Core.Editor;

/// <summary>
/// Changes the order of the levels of the story. The list shows the levels of the story in their order, then the
/// levels that are not in the story. The story goes from each level to the next level of the list. Save writes the
/// order into mods/editor/LEVELS.dat. Undo does not change the order.
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

	public LevelsTool(LevelEditor editor)
		: base(editor)
	{
	}

	public override string Name => "Levels";

	public override string Hint => "Click a level in the list. Then open it, move it in the story, or add it to the story or remove it.";

	public override string Status => $"Levels {_levels.Count}   In the story {_story.Count}";

	public override string ListHeading => "Levels (the story first, in its order)";

	public override string PropertiesHeading => _selected == null ? "Level (select a level)" : "Level (click a value to change it)";

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

	public override IReadOnlyList<EditorListRow> GetListRows()
	{
		if (Catalog == null)
		{
			return new[] { new EditorListRow { Text = "This game gives no list of levels", Dim = true } };
		}
		string open = OpenMnemonic;
		return GetOrderedLevels().Select(level =>
		{
			int position = EditorLevelOrder.IndexOf(_story, level.Mnemonic);
			return new EditorListRow
			{
				Text = position >= 0 ? $"{position + 1}: {level.Name}" : level.Name,
				Detail = level.Mnemonic == open ? "open" : position >= 0 ? "" : "not in story",
				DetailColour = level.Mnemonic == open ? OpenColour : null,
				Active = level.Mnemonic == _selected,
				Dim = position < 0
			};
		}).ToArray();
	}

	public override void OnListClick(int index, int direction)
	{
		IReadOnlyList<LevelCatalogEntry> levels = GetOrderedLevels();
		if (index >= 0 && index < levels.Count)
		{
			_selected = levels[index].Mnemonic;
			_confirmOpen = null;
		}
	}

	#endregion

	#region Properties

	public override List<EditorPropertyRow> GetPropertyRows()
	{
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
