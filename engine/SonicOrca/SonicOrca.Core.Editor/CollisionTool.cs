using System;
using System.Collections.Generic;
using System.Linq;
using SonicOrca.Core.Collision;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SonicOrca.Core.Editor;

/// <summary>
/// Selects, moves, draws and deletes the collision lines of a map, and changes their paths and flags. A line is solid
/// on its right side, from A to B with Y down. Path 1 and path 2 are the two collision paths that the layer switches
/// select. When the zone has tile collision, the tool can also trace the lines again from the tiles.
/// </summary>
internal sealed class CollisionTool : EditorTool
{
	/// <summary>The distance in screen pixels from a line where a click selects it.</summary>
	private const double HitDistance = 8.0;

	/// <summary>The radius in screen pixels of the handle at each end of a selected line.</summary>
	private const double HandleRadius = 8.0;

	private const uint PathMask = 3;

	private static readonly Colour Path1Colour = new Colour(1.0, 0.3, 1.0, 0.3);

	private static readonly Colour Path2Colour = new Colour(1.0, 0.3, 0.75, 1.0);

	private static readonly Colour BothPathsColour = new Colour(1.0, 0.95, 0.95, 0.95);

	private static readonly Colour OtherPathsColour = new Colour(0.6, 0.6, 0.6, 0.6);

	private static readonly string[] PathNames = { "", "path 1", "path 2", "paths 1 and 2" };

	private static readonly List<CollisionVector> Clipboard = new List<CollisionVector>();

	private readonly Dictionary<CollisionVector, (Vector2i A, Vector2i B)> _dragStart = new Dictionary<CollisionVector, (Vector2i, Vector2i)>();

	private bool _drawing;

	/// <summary>The first point of the next line while the user draws lines, or null.</summary>
	private Vector2i? _drawStart;

	private uint _newPaths = PathMask;

	private DragAction _dragAction;

	/// <summary>The point that a drag of a handle moves. All the line ends at this point move.</summary>
	private Vector2i _dragPoint;

	private Vector2i _dragDelta;

	private enum DragAction
	{
		None,
		Move,
		MovePoint
	}

	public CollisionTool(LevelEditor editor)
		: base(editor)
	{
	}

	public override string Name => "Collision";

	public override string Hint => _drawing
		? "Click to set the points of the lines, from left to right for a floor. Esc to stop."
		: "Drag: move lines.  Drag the end of a selected line: move the point.  The solid side is to the right of the arrow.";

	public override string Status => $"Lines {Vectors.Count}   Selected {Selection.Count}";

	public override string ListHeading => "Collision (lines are solid to the right)";

	public override string PropertiesHeading => Selection.Count == 0 ? "Properties (select a line)" : "Properties (click a value to change it)";

	public override bool HasSelection => Selection.Count > 0;

	private IList<CollisionVector> Vectors => Editor.Vectors;

	private HashSet<CollisionVector> Selection => Editor.VectorSelection;

	public override void Begin()
	{
		_drawing = false;
		_drawStart = null;
		_dragAction = DragAction.None;
		Selection.IntersectWith(Vectors);
	}

	#region List

	public override IReadOnlyList<EditorListRow> GetListRows()
	{
		List<EditorListRow> rows = new List<EditorListRow>
		{
			new EditorListRow { Text = "+ Draw lines", Active = _drawing },
			new EditorListRow { Text = "Paths of new lines", Detail = PathNames[_newPaths] }
		};
		if (Editor.TileCollision != null)
		{
			rows.Add(new EditorListRow { Text = "Trace paths 1 and 2 again from the tiles" });
		}
		else
		{
			rows.Add(new EditorListRow { Text = "(This zone has no tile collision to trace)", Dim = true });
		}
		rows.Add(new EditorListRow { Text = "Path 1", Detail = "green", Dim = true });
		rows.Add(new EditorListRow { Text = "Path 2", Detail = "blue", Dim = true });
		rows.Add(new EditorListRow { Text = "Paths 1 and 2", Detail = "white", Dim = true });
		rows.Add(new EditorListRow { Text = "Other paths (layout changes)", Detail = "grey", Dim = true });
		return rows;
	}

	public override void OnListClick(int index, int direction)
	{
		switch (index)
		{
			case 0:
				_drawing = !_drawing;
				_drawStart = null;
				break;
			case 1:
				_newPaths = (uint)((_newPaths - 1 + (direction > 0 ? 1 : 2)) % 3 + 1);
				break;
			case 2 when Editor.TileCollision != null:
				Editor.CompleteTraces();
				EditorSnapshot before = Editor.CaptureSnapshot();
				if (Editor.RetraceAll())
				{
					Editor.RecordHistory(before);
					Editor.ShowMessage("The lines of paths 1 and 2 are traced again from the tiles.");
				}
				else
				{
					Editor.ShowMessage("The lines are the same as the tiles give.");
				}
				break;
		}
	}

	#endregion

	#region Level

	public override void OnLevelPress()
	{
		if (_drawing)
		{
			Vector2i point = Editor.Snap(Editor.MouseWorld);
			if (_drawStart is Vector2i start && start != point)
			{
				AddLine(start, point);
			}
			_drawStart = point;
			return;
		}
		CollisionVector handleOwner = Selection.FirstOrDefault(v => IsNear(v.RelativeA) || IsNear(v.RelativeB));
		if (handleOwner != null)
		{
			_dragPoint = IsNear(handleOwner.RelativeA) ? handleOwner.RelativeA : handleOwner.RelativeB;
			BeginDrag(DragAction.MovePoint, Vectors.Where(v => v.Owner == null && (v.RelativeA == _dragPoint || v.RelativeB == _dragPoint)));
			return;
		}
		CollisionVector hit = HitTest();
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
		BeginDrag(DragAction.Move, Selection.ToArray());
	}

	private void BeginDrag(DragAction action, IEnumerable<CollisionVector> vectors)
	{
		_dragAction = action;
		_dragDelta = new Vector2i(0, 0);
		_dragStart.Clear();
		foreach (CollisionVector vector in vectors)
		{
			_dragStart[vector] = (vector.RelativeA, vector.RelativeB);
		}
		Editor.BeginToolDrag();
	}

	public override void OnDrag()
	{
		if (_dragAction == DragAction.None)
		{
			return;
		}
		Vector2 origin = _dragAction == DragAction.MovePoint
			? (Vector2)_dragPoint
			: (Vector2)_dragStart.Values.First().A;
		_dragDelta = Editor.Snap(origin + (Editor.MouseWorld - Editor.DragStartWorld)) - (Vector2i)origin;
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
		foreach (KeyValuePair<CollisionVector, (Vector2i A, Vector2i B)> item in _dragStart)
		{
			(Vector2i a, Vector2i b) = GetDraggedPoints(item.Value, action);
			Replace(item.Key, new CollisionVector(a, b, item.Key.Paths, item.Key.Flags));
		}
		Editor.MarkCollisionChanged();
	}

	private (Vector2i A, Vector2i B) GetDraggedPoints((Vector2i A, Vector2i B) start, DragAction action)
	{
		if (action == DragAction.MovePoint)
		{
			return (start.A == _dragPoint ? start.A + _dragDelta : start.A, start.B == _dragPoint ? start.B + _dragDelta : start.B);
		}
		return (start.A + _dragDelta, start.B + _dragDelta);
	}

	public override void SelectInBox(Rectangle box)
	{
		foreach (CollisionVector vector in Vectors.Where(v => v.Owner == null && box.Contains((Vector2)(v.RelativeA + v.RelativeB) * 0.5)))
		{
			Selection.Add(vector);
		}
	}

	public override bool Cancel()
	{
		if (!_drawing)
		{
			return false;
		}
		_drawing = false;
		_drawStart = null;
		return true;
	}

	public override void ClearSelection() => Selection.Clear();

	#endregion

	#region Commands

	private void AddLine(Vector2i a, Vector2i b)
	{
		CollisionVector vector = new CollisionVector(a, b, _newPaths);
		Editor.RecordHistory();
		Vectors.Add(vector);
		Selection.Clear();
		Selection.Add(vector);
		Editor.MarkCollisionChanged();
	}

	public override void DeleteSelection()
	{
		if (Selection.Count == 0)
		{
			return;
		}
		Editor.RecordHistory();
		foreach (CollisionVector vector in Selection)
		{
			Vectors.Remove(vector);
		}
		Selection.Clear();
		Editor.MarkCollisionChanged();
	}

	public override void Nudge(Vector2i delta)
	{
		Editor.RecordHistory();
		foreach (CollisionVector vector in Selection.ToArray())
		{
			Replace(vector, new CollisionVector(vector.RelativeA + delta, vector.RelativeB + delta, vector.Paths, vector.Flags));
		}
		Editor.MarkCollisionChanged();
	}

	public override void Copy()
	{
		if (Selection.Count == 0)
		{
			return;
		}
		Clipboard.Clear();
		Clipboard.AddRange(Vectors.Where(Selection.Contains));
		Editor.ShowMessage($"Copied {Clipboard.Count} lines.");
	}

	public override void Paste(Vector2 position)
	{
		if (Clipboard.Count == 0)
		{
			return;
		}
		Vector2 centre = new Vector2(Clipboard.Average(v => (double)v.RelativeA.X), Clipboard.Average(v => (double)v.RelativeA.Y));
		AddCopies(Clipboard, Editor.Snap(position) - Editor.Snap(centre));
	}

	public override void Duplicate()
	{
		CollisionVector[] selected = Vectors.Where(Selection.Contains).ToArray();
		if (selected.Length > 0)
		{
			AddCopies(selected, new Vector2i(Editor.GridSize * 2, Editor.GridSize * 2));
		}
	}

	private void AddCopies(IReadOnlyList<CollisionVector> sources, Vector2i offset)
	{
		Editor.RecordHistory();
		Selection.Clear();
		foreach (CollisionVector source in sources)
		{
			CollisionVector copy = new CollisionVector(source.RelativeA + offset, source.RelativeB + offset, source.Paths, source.Flags);
			Vectors.Add(copy);
			Selection.Add(copy);
		}
		Editor.MarkCollisionChanged();
	}

	public override void SelectAllInView()
	{
		Rectangle view = Editor.Camera.Bounds;
		foreach (CollisionVector vector in Vectors.Where(v => v.Owner == null && view.Contains((Vector2)(v.RelativeA + v.RelativeB) * 0.5)))
		{
			Selection.Add(vector);
		}
	}

	/// <summary>Replaces a line with a changed copy, and keeps the selection and the order of the lines.</summary>
	private void Replace(CollisionVector oldVector, CollisionVector newVector)
	{
		int index = Vectors.IndexOf(oldVector);
		if (index < 0)
		{
			return;
		}
		Vectors[index] = newVector;
		if (Selection.Remove(oldVector))
		{
			Selection.Add(newVector);
		}
	}

	private bool Change(CollisionVector vector, Vector2i a, Vector2i b, uint paths, CollisionFlags flags)
	{
		Editor.RecordHistory();
		Replace(vector, new CollisionVector(a, b, paths, flags));
		Editor.MarkCollisionChanged();
		return true;
	}

	#endregion

	#region Properties

	public override List<EditorPropertyRow> GetPropertyRows()
	{
		List<EditorPropertyRow> rows = new List<EditorPropertyRow>();
		if (Selection.Count != 1)
		{
			if (Selection.Count > 1)
			{
				rows.Add(new EditorPropertyRow { Label = "Selected", Value = $"{Selection.Count} lines", IsHeading = true });
				rows.Add(new EditorPropertyRow
				{
					Label = "Paths",
					Value = "click to set paths 1 and 2 of all",
					Step = direction =>
					{
						Editor.RecordHistory();
						foreach (CollisionVector vector in Selection.ToArray())
						{
							uint paths = (vector.Paths & ~PathMask) | (uint)(((vector.Paths & PathMask) - 1 + (direction > 0 ? 1 : 2)) % 3 + 1);
							Replace(vector, new CollisionVector(vector.RelativeA, vector.RelativeB, paths, vector.Flags));
						}
						Editor.MarkCollisionChanged();
					}
				});
			}
			return rows;
		}
		CollisionVector line = Selection.First();
		Vector2i a = line.RelativeA;
		Vector2i b = line.RelativeB;
		rows.Add(new EditorPropertyRow { Label = "Line", Value = line.IsWall ? "wall" : (b.X > a.X ? "floor" : "ceiling"), IsHeading = true });
		rows.Add(PointRow("A", a, point => Change(line, point, b, line.Paths, line.Flags)));
		rows.Add(PointRow("B", b, point => Change(line, a, point, line.Paths, line.Flags)));
		rows.Add(new EditorPropertyRow
		{
			Label = "Direction",
			Value = "click to swap A and B",
			Step = _ => Change(line, b, a, line.Paths, line.Flags)
		});
		uint basePaths = line.Paths & PathMask;
		rows.Add(new EditorPropertyRow
		{
			Label = "Paths",
			Value = (basePaths == 0 ? "none" : PathNames[basePaths]) + ((line.Paths & ~PathMask) != 0 ? $" (+ bits {line.Paths & ~PathMask})" : ""),
			Step = direction =>
			{
				uint paths = (uint)((basePaths + 3 + (direction > 0 ? 1 : 3)) % 4);
				Change(line, a, b, (line.Paths & ~PathMask) | paths, line.Flags);
			}
		});
		rows.Add(new EditorPropertyRow
		{
			Label = "Flags",
			Value = ((int)line.Flags).ToString(),
			Commit = text => int.TryParse(text, out int flags) && flags >= 0 && Change(line, a, b, line.Paths, (CollisionFlags)flags)
		});
		rows.Add(new EditorPropertyRow { Label = "Flag names", Value = line.Flags == 0 ? "none" : line.Flags.ToString() });
		return rows;
	}

	private static EditorPropertyRow PointRow(string label, Vector2i point, Func<Vector2i, bool> change)
	{
		return new EditorPropertyRow
		{
			Label = label,
			Value = $"{point.X}, {point.Y}",
			Commit = text =>
			{
				string[] parts = text.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
				return parts.Length == 2 && int.TryParse(parts[0], out int x) && int.TryParse(parts[1], out int y) && change(new Vector2i(x, y));
			}
		};
	}

	#endregion

	#region Drawing

	public override void DrawOverlay(Renderer renderer, I2dRenderer g)
	{
		CollisionVector hover = _dragAction == DragAction.None && !_drawing && Editor.IsMouseOverLevel ? HitTest() : null;
		foreach (CollisionVector vector in Vectors)
		{
			if (vector.Owner != null)
			{
				continue;
			}
			(Vector2i a, Vector2i b) = _dragAction != DragAction.None && _dragStart.TryGetValue(vector, out (Vector2i A, Vector2i B) start)
				? GetDraggedPoints(start, _dragAction)
				: (vector.RelativeA, vector.RelativeB);
			Vector2 screenA = Editor.ToScreen((Vector2)a);
			Vector2 screenB = Editor.ToScreen((Vector2)b);
			if (!IsOnScreen(screenA, screenB))
			{
				continue;
			}
			bool selected = Selection.Contains(vector);
			Colour colour = selected ? LevelEditor.SelectionColour : (vector == hover ? LevelEditor.HoverColour : GetPathColour(vector.Paths));
			double thickness = selected ? 4.0 : 2.0;
			// A dark outline, so that a line shows on tiles of its own colour
			DrawArrow(g, screenA, screenB, LevelEditor.ShadowColour, thickness + 3.0);
			DrawArrow(g, screenA, screenB, colour, thickness);
			if (selected)
			{
				DrawHandle(g, screenA);
				DrawHandle(g, screenB);
			}
		}
		if (_drawing && _drawStart is Vector2i drawStart && Editor.IsMouseOverLevel)
		{
			DrawArrow(g, Editor.ToScreen((Vector2)drawStart), Editor.ToScreen((Vector2)Editor.Snap(Editor.MouseWorld)), GetPathColour(_newPaths), 2.0);
		}
	}

	public override string GetCursorLabel(I2dRenderer g)
	{
		if (_drawing)
		{
			Vector2 position = Editor.ToScreen((Vector2)Editor.Snap(Editor.MouseWorld));
			LevelEditor.DrawCross(g, position, 12.0, LevelEditor.SelectionColour, 2.0);
			return _drawStart == null ? "first point" : "next point";
		}
		return null;
	}

	/// <summary>Draws a line with an arrow head at B, and a short mark on the solid side.</summary>
	private static void DrawArrow(I2dRenderer g, Vector2 a, Vector2 b, Colour colour, double thickness)
	{
		g.RenderLine(colour, a, b, thickness);
		Vector2 direction = b - a;
		double length = direction.Length;
		if (length < 12.0)
		{
			return;
		}
		Vector2 unit = direction * (1.0 / length);
		// With Y down, the right side of the direction (x, y) is (-y, x)
		Vector2 right = new Vector2(-unit.Y, unit.X);
		double head = Math.Min(10.0, length / 3.0);
		g.RenderLine(colour, b, b - unit * head + right * (head * 0.5), thickness);
		g.RenderLine(colour, b, b - unit * head - right * (head * 0.5), thickness);
		Vector2 middle = (a + b) * 0.5;
		g.RenderLine(colour, middle, middle + right * 6.0, thickness);
	}

	private static void DrawHandle(I2dRenderer g, Vector2 point)
	{
		g.RenderQuad(LevelEditor.SelectionColour, new Rectangle(point.X - HandleRadius / 2.0, point.Y - HandleRadius / 2.0, HandleRadius, HandleRadius));
	}

	private static Colour GetPathColour(uint paths)
	{
		return (paths & PathMask) switch
		{
			1 => Path1Colour,
			2 => Path2Colour,
			3 => BothPathsColour,
			_ => OtherPathsColour
		};
	}

	private static bool IsOnScreen(Vector2 a, Vector2 b)
	{
		return Math.Max(a.X, b.X) >= 0 && Math.Min(a.X, b.X) <= LevelEditor.ScreenWidth && Math.Max(a.Y, b.Y) >= 0 && Math.Min(a.Y, b.Y) <= LevelEditor.ScreenHeight;
	}

	#endregion

	#region Lines

	private bool IsNear(Vector2i point) => (Editor.ToScreen((Vector2)point) - Editor.Mouse).Length <= HandleRadius;

	/// <summary>Finds the line nearest to the mouse, in screen pixels.</summary>
	private CollisionVector HitTest()
	{
		return Vectors
			.Where(v => v.Owner == null)
			.Select(v => (Vector: v, Distance: DistanceToSegment(Editor.Mouse, Editor.ToScreen((Vector2)v.RelativeA), Editor.ToScreen((Vector2)v.RelativeB))))
			.Where(x => x.Distance <= HitDistance)
			.OrderBy(x => x.Distance)
			.Select(x => x.Vector)
			.FirstOrDefault();
	}

	private static double DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
	{
		Vector2 ab = b - a;
		double lengthSquared = ab.X * ab.X + ab.Y * ab.Y;
		double t = lengthSquared == 0.0 ? 0.0 : Math.Clamp(((p.X - a.X) * ab.X + (p.Y - a.Y) * ab.Y) / lengthSquared, 0.0, 1.0);
		Vector2 closest = a + ab * t;
		return (p - closest).Length;
	}

	#endregion
}
