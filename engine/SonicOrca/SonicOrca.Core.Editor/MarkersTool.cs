using System;
using System.Collections.Generic;
using System.Linq;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SonicOrca.Core.Editor;

/// <summary>
/// Selects, moves, resizes, adds and deletes the markers of a map. A marker is a named point, for example a start
/// position or a camera bound, or an area with a tag, for example an inside area or a clipping area. The level reads
/// the markers when it starts, so a test play uses the changes.
/// </summary>
internal sealed class MarkersTool : EditorTool
{
	/// <summary>The distance in screen pixels from a point marker where a click selects it.</summary>
	private const double PointHitRadius = 20.0;

	/// <summary>The size in screen pixels of the handle at the bottom right of an area marker.</summary>
	private const double HandleSize = 16.0;

	/// <summary>The size of a new area marker.</summary>
	private const int NewAreaSize = 256;

	private static readonly Colour PointColour = new Colour(1.0, 1.0, 0.45, 0.2);

	private static readonly Colour AreaColour = new Colour(1.0, 0.3, 0.9, 0.5);

	private static readonly Colour AreaFillColour = new Colour(0.12, 0.3, 0.9, 0.5);

	private static readonly List<LevelMarker> Clipboard = new List<LevelMarker>();

	private readonly Dictionary<LevelMarker, Rectanglei> _dragStartBounds = new Dictionary<LevelMarker, Rectanglei>();

	private AddMode _adding;

	private DragAction _dragAction;

	private LevelMarker _dragAnchor;

	private Vector2i _dragDelta;

	private enum AddMode
	{
		None,
		Point,
		Area
	}

	private enum DragAction
	{
		None,
		Move,
		Resize
	}

	public MarkersTool(LevelEditor editor)
		: base(editor)
	{
	}

	public override string Name => "Markers";

	public override string Hint => _adding != AddMode.None
		? "Click in the level to add the marker. Esc to stop."
		: "Drag: move a marker.  Drag the corner of an area: resize it.  Click a marker in the list to go to it.";

	public override string Status => $"Markers {Markers.Count}   Selected {Selection.Count}";

	public override string ListHeading => "Markers (click to go to a marker)";

	public override string PropertiesHeading => Selection.Count == 0 ? "Properties (select a marker)" : "Properties (click a value to change it)";

	public override bool HasSelection => Selection.Count > 0;

	private IList<LevelMarker> Markers => Editor.Markers;

	private HashSet<LevelMarker> Selection => Editor.MarkerSelection;

	private IList<LevelLayer> Layers => Editor.Level.Map.Layers;

	public override void Begin()
	{
		_adding = AddMode.None;
		_dragAction = DragAction.None;
		Selection.IntersectWith(Markers);
	}

	#region List

	/// <summary>The first two rows add a marker. The other rows are the markers.</summary>
	private const int AddRowCount = 2;

	public override IReadOnlyList<EditorListRow> GetListRows()
	{
		List<EditorListRow> rows = new List<EditorListRow>
		{
			new EditorListRow { Text = "+ Add a point marker", Active = _adding == AddMode.Point },
			new EditorListRow { Text = "+ Add an area marker", Active = _adding == AddMode.Area }
		};
		rows.AddRange(Markers.Select(marker => new EditorListRow
		{
			Text = GetLabel(marker),
			Detail = IsArea(marker) ? "area" : "",
			Active = Selection.Contains(marker)
		}));
		return rows;
	}

	public override void OnListClick(int index, int direction)
	{
		if (index < AddRowCount)
		{
			AddMode mode = index == 0 ? AddMode.Point : AddMode.Area;
			_adding = _adding == mode ? AddMode.None : mode;
			return;
		}
		LevelMarker marker = Markers[index - AddRowCount];
		Selection.Clear();
		Selection.Add(marker);
		Rectanglei bounds = marker.Bounds;
		Editor.CentreOn(new Vector2(bounds.X + bounds.Width / 2.0, bounds.Y + bounds.Height / 2.0));
	}

	#endregion

	#region Level

	public override void OnLevelPress()
	{
		if (_adding != AddMode.None)
		{
			AddMarker(Editor.Snap(Editor.MouseWorld), _adding == AddMode.Area);
			_adding = AddMode.None;
			return;
		}
		LevelMarker handle = Selection.FirstOrDefault(m => IsArea(m) && GetHandle(m).Contains(Editor.Mouse));
		if (handle != null)
		{
			BeginDrag(DragAction.Resize, handle, new[] { handle });
			return;
		}
		LevelMarker hit = HitTest();
		if (hit == null)
		{
			if (!Editor.IsShiftDown)
			{
				Selection.Clear();
			}
			Editor.BeginBoxSelect();
			return;
		}
		if (Editor.IsShiftDown && Selection.Contains(hit))
		{
			Selection.Remove(hit);
			return;
		}
		if (!Selection.Contains(hit))
		{
			if (!Editor.IsShiftDown)
			{
				Selection.Clear();
			}
			Selection.Add(hit);
		}
		BeginDrag(DragAction.Move, hit, Selection.ToArray());
	}

	private void BeginDrag(DragAction action, LevelMarker anchor, IEnumerable<LevelMarker> markers)
	{
		_dragAction = action;
		_dragAnchor = anchor;
		_dragDelta = new Vector2i(0, 0);
		_dragStartBounds.Clear();
		foreach (LevelMarker marker in markers)
		{
			_dragStartBounds[marker] = marker.Bounds;
		}
		Editor.BeginToolDrag();
	}

	public override void OnDrag()
	{
		if (_dragAction == DragAction.None || !_dragStartBounds.TryGetValue(_dragAnchor, out Rectanglei anchorStart))
		{
			return;
		}
		Vector2 corner = _dragAction == DragAction.Move
			? new Vector2(anchorStart.X, anchorStart.Y)
			: new Vector2(anchorStart.X + anchorStart.Width, anchorStart.Y + anchorStart.Height);
		_dragDelta = Editor.Snap(corner + (Editor.MouseWorld - Editor.DragStartWorld)) - (Vector2i)corner;
	}

	public override void OnDragEnd()
	{
		DragAction action = _dragAction;
		_dragAction = DragAction.None;
		if (action == DragAction.None || _dragDelta == new Vector2i(0, 0))
		{
			return;
		}
		Editor.RecordHistory();
		foreach (KeyValuePair<LevelMarker, Rectanglei> item in _dragStartBounds)
		{
			Replace(item.Key, WithBounds(item.Key, GetDraggedBounds(item.Value, action)));
		}
		Editor.MarkMarkersChanged();
	}

	/// <summary>The bounds of a marker during a drag, from its bounds at the start of the drag.</summary>
	private Rectanglei GetDraggedBounds(Rectanglei start, DragAction action)
	{
		if (action == DragAction.Resize)
		{
			int grid = Editor.GridSize;
			return new Rectanglei(start.X, start.Y, Math.Max(grid, start.Width + _dragDelta.X), Math.Max(grid, start.Height + _dragDelta.Y));
		}
		return new Rectanglei(start.X + _dragDelta.X, start.Y + _dragDelta.Y, start.Width, start.Height);
	}

	public override void SelectInBox(Rectangle box)
	{
		foreach (LevelMarker marker in Markers.Where(m => box.IntersectsWith(GetWorldBounds(m))))
		{
			Selection.Add(marker);
		}
	}

	public override bool Cancel()
	{
		if (_adding == AddMode.None)
		{
			return false;
		}
		_adding = AddMode.None;
		return true;
	}

	public override void ClearSelection() => Selection.Clear();

	#endregion

	#region Commands

	private void AddMarker(Vector2i position, bool area)
	{
		LevelLayer layer = Markers.Where(m => m.Layer != null).GroupBy(m => m.Layer).OrderByDescending(g => g.Count()).Select(g => g.Key).FirstOrDefault();
		LevelMarker marker = area
			? new LevelMarker(null, "area", new Rectanglei(position.X, position.Y, NewAreaSize, NewAreaSize), null)
			: new LevelMarker(GetNewName(), "", position, layer);
		Editor.RecordHistory();
		Markers.Add(marker);
		Selection.Clear();
		Selection.Add(marker);
		Editor.MarkMarkersChanged();
	}

	private string GetNewName()
	{
		int number = 0;
		while (Markers.Any(m => m.Name == $"marker_{number}"))
		{
			number++;
		}
		return $"marker_{number}";
	}

	public override void DeleteSelection()
	{
		if (Selection.Count == 0)
		{
			return;
		}
		Editor.RecordHistory();
		foreach (LevelMarker marker in Selection)
		{
			Markers.Remove(marker);
		}
		Selection.Clear();
		Editor.MarkMarkersChanged();
	}

	public override void Nudge(Vector2i delta)
	{
		Editor.RecordHistory();
		foreach (LevelMarker marker in Selection.ToArray())
		{
			Rectanglei bounds = marker.Bounds;
			Replace(marker, WithBounds(marker, new Rectanglei(bounds.X + delta.X, bounds.Y + delta.Y, bounds.Width, bounds.Height)));
		}
		Editor.MarkMarkersChanged();
	}

	public override void Copy()
	{
		if (Selection.Count == 0)
		{
			return;
		}
		Clipboard.Clear();
		Clipboard.AddRange(Markers.Where(Selection.Contains));
		Editor.ShowMessage($"Copied {Clipboard.Count} markers.");
	}

	public override void Paste(Vector2 position)
	{
		if (Clipboard.Count == 0)
		{
			return;
		}
		Vector2 centre = new Vector2(Clipboard.Average(m => (double)m.Bounds.X), Clipboard.Average(m => (double)m.Bounds.Y));
		AddCopies(Clipboard, Editor.Snap(position) - Editor.Snap(centre));
	}

	public override void Duplicate()
	{
		LevelMarker[] selected = Markers.Where(Selection.Contains).ToArray();
		if (selected.Length > 0)
		{
			AddCopies(selected, new Vector2i(Editor.GridSize * 2, Editor.GridSize * 2));
		}
	}

	/// <summary>Adds copies of markers. A named copy gets a new name, because the level finds a point marker by its name.</summary>
	private void AddCopies(IReadOnlyList<LevelMarker> sources, Vector2i offset)
	{
		Editor.RecordHistory();
		Selection.Clear();
		foreach (LevelMarker source in sources)
		{
			Rectanglei bounds = source.Bounds;
			string name = string.IsNullOrEmpty(source.Name) ? source.Name : GetNewName();
			// A copy from another map uses a layer of this map with the same index, if there is one
			LevelLayer layer = source.Layer != null && Layers.Contains(source.Layer) ? source.Layer : null;
			LevelMarker copy = new LevelMarker(name, source.Tag, new Rectanglei(bounds.X + offset.X, bounds.Y + offset.Y, bounds.Width, bounds.Height), layer);
			Markers.Add(copy);
			Selection.Add(copy);
		}
		Editor.MarkMarkersChanged();
	}

	public override void SelectAllInView()
	{
		Rectangle view = Editor.Camera.Bounds;
		foreach (LevelMarker marker in Markers.Where(m => view.IntersectsWith(GetWorldBounds(m))))
		{
			Selection.Add(marker);
		}
	}

	/// <summary>Replaces a marker with a changed copy, and keeps the selection and the order of the markers.</summary>
	private void Replace(LevelMarker oldMarker, LevelMarker newMarker)
	{
		int index = Markers.IndexOf(oldMarker);
		if (index < 0)
		{
			return;
		}
		Markers[index] = newMarker;
		if (Selection.Remove(oldMarker))
		{
			Selection.Add(newMarker);
		}
	}

	private bool Change(LevelMarker marker, LevelMarker newMarker)
	{
		Editor.RecordHistory();
		Replace(marker, newMarker);
		Editor.MarkMarkersChanged();
		return true;
	}

	private static LevelMarker WithBounds(LevelMarker marker, Rectanglei bounds) => new LevelMarker(marker.Name, marker.Tag, bounds, marker.Layer);

	#endregion

	#region Properties

	public override List<EditorPropertyRow> GetPropertyRows()
	{
		List<EditorPropertyRow> rows = new List<EditorPropertyRow>();
		if (Selection.Count != 1)
		{
			if (Selection.Count > 1)
			{
				rows.Add(new EditorPropertyRow { Label = "Selected", Value = $"{Selection.Count} markers", IsHeading = true });
			}
			return rows;
		}
		LevelMarker marker = Selection.First();
		Rectanglei bounds = marker.Bounds;
		rows.Add(new EditorPropertyRow { Label = "Marker", Value = IsArea(marker) ? "area" : "point", IsHeading = true });
		rows.Add(new EditorPropertyRow
		{
			Label = "Name",
			Value = marker.Name ?? "",
			Commit = text => Change(marker, new LevelMarker(string.IsNullOrWhiteSpace(text) ? null : text.Trim(), marker.Tag, bounds, marker.Layer))
		});
		rows.Add(new EditorPropertyRow
		{
			Label = "Tag",
			Value = marker.Tag,
			Commit = text => Change(marker, new LevelMarker(marker.Name, text.Trim(), bounds, marker.Layer))
		});
		rows.Add(BoundsRow("X", bounds.X, value => new Rectanglei(value, bounds.Y, bounds.Width, bounds.Height), marker));
		rows.Add(BoundsRow("Y", bounds.Y, value => new Rectanglei(bounds.X, value, bounds.Width, bounds.Height), marker));
		rows.Add(BoundsRow("Width", bounds.Width, value => new Rectanglei(bounds.X, bounds.Y, Math.Max(0, value), bounds.Height), marker));
		rows.Add(BoundsRow("Height", bounds.Height, value => new Rectanglei(bounds.X, bounds.Y, bounds.Width, Math.Max(0, value)), marker));
		int layerIndex = marker.Layer == null ? -1 : Layers.IndexOf(marker.Layer);
		rows.Add(new EditorPropertyRow
		{
			Label = "Layer",
			Value = layerIndex < 0 ? "none" : $"{layerIndex}: {marker.Layer.Name}",
			Step = direction =>
			{
				// The values are no layer (-1), then each layer of the map
				int count = Layers.Count + 1;
				int index = ((layerIndex + 1 + direction) % count + count) % count - 1;
				Change(marker, new LevelMarker(marker.Name, marker.Tag, bounds, index < 0 ? null : Layers[index]));
			}
		});
		return rows;
	}

	private EditorPropertyRow BoundsRow(string label, int value, Func<int, Rectanglei> change, LevelMarker marker)
	{
		return new EditorPropertyRow
		{
			Label = label,
			Value = value.ToString(),
			Commit = text => int.TryParse(text, out int newValue) && Change(marker, WithBounds(marker, change(newValue)))
		};
	}

	#endregion

	#region Drawing

	public override void DrawOverlay(Renderer renderer, I2dRenderer g)
	{
		LevelMarker hover = _dragAction == DragAction.None && Editor.IsMouseOverLevel && _adding == AddMode.None ? HitTest() : null;
		Rectangle view = Editor.Camera.Bounds.Inflate(new Vector2(256.0, 256.0));
		foreach (LevelMarker marker in Markers)
		{
			Rectanglei bounds = marker.Bounds;
			if (_dragAction != DragAction.None && _dragStartBounds.TryGetValue(marker, out Rectanglei start))
			{
				bounds = GetDraggedBounds(start, _dragAction);
			}
			Rectangle world = new Rectangle(bounds.X, bounds.Y, bounds.Width, bounds.Height);
			if (!view.IntersectsWith(world))
			{
				continue;
			}
			bool selected = Selection.Contains(marker);
			Colour colour = selected ? LevelEditor.SelectionColour : (marker == hover ? LevelEditor.HoverColour : (IsArea(marker) ? AreaColour : PointColour));
			Rectangle screen = Editor.ToScreen(world);
			if (IsArea(marker))
			{
				g.RenderQuad(selected ? LevelEditor.SelectionFillColour : AreaFillColour, screen);
				g.RenderRectangle(colour, screen, selected ? 3.0 : 2.0);
				if (selected)
				{
					g.RenderQuad(LevelEditor.SelectionColour, GetHandle(screen));
				}
				Editor.DrawLabel(renderer, g, GetLabel(marker), screen.X + 4.0, screen.Y + 4.0, colour);
			}
			else
			{
				Vector2 point = new Vector2(screen.X, screen.Y);
				LevelEditor.DrawCross(g, point, 18.0, LevelEditor.ShadowColour, 7.0);
				LevelEditor.DrawCross(g, point, 18.0, colour, 3.0);
				Editor.DrawLabel(renderer, g, GetLabel(marker), point.X + 10.0, point.Y + 8.0, colour);
			}
		}
	}

	public override string GetCursorLabel(I2dRenderer g)
	{
		if (_adding == AddMode.None)
		{
			return null;
		}
		Vector2 position = Editor.ToScreen((Vector2)Editor.Snap(Editor.MouseWorld));
		LevelEditor.DrawCross(g, position, 16.0, LevelEditor.SelectionColour, 2.0);
		return _adding == AddMode.Point ? "+ point marker" : "+ area marker";
	}

	#endregion

	#region Markers

	private static bool IsArea(LevelMarker marker) => marker.Bounds.Width > 0 || marker.Bounds.Height > 0;

	private static string GetLabel(LevelMarker marker)
	{
		if (!string.IsNullOrEmpty(marker.Name))
		{
			return string.IsNullOrEmpty(marker.Tag) ? marker.Name : $"{marker.Name} [{marker.Tag}]";
		}
		return string.IsNullOrEmpty(marker.Tag) ? "(no name)" : $"[{marker.Tag}]";
	}

	private static Rectangle GetWorldBounds(LevelMarker marker)
	{
		Rectanglei bounds = marker.Bounds;
		return new Rectangle(bounds.X, bounds.Y, bounds.Width, bounds.Height);
	}

	private Rectangle GetHandle(LevelMarker marker) => GetHandle(Editor.ToScreen(GetWorldBounds(marker)));

	private static Rectangle GetHandle(Rectangle screen) => new Rectangle(screen.Right - HandleSize / 2.0, screen.Bottom - HandleSize / 2.0, HandleSize, HandleSize);

	/// <summary>Finds the marker under the mouse. A point marker comes first, then the smallest area.</summary>
	private LevelMarker HitTest()
	{
		LevelMarker point = Markers
			.Where(m => !IsArea(m))
			.Select(m => (Marker: m, Distance: (Editor.ToScreen((Vector2)m.Position) - Editor.Mouse).Length))
			.Where(x => x.Distance <= PointHitRadius)
			.OrderBy(x => x.Distance)
			.Select(x => x.Marker)
			.FirstOrDefault();
		if (point != null)
		{
			return point;
		}
		return Markers
			.Where(m => IsArea(m) && GetWorldBounds(m).Contains(Editor.MouseWorld))
			.OrderBy(m => (long)m.Bounds.Width * m.Bounds.Height)
			.FirstOrDefault();
	}

	#endregion
}
