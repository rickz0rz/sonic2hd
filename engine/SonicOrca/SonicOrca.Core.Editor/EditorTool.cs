using System;
using System.Collections.Generic;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SonicOrca.Core.Editor;

/// <summary>A row of the list at the top of the side panel, for example an object type.</summary>
internal sealed class EditorListRow
{
	public string Text;

	/// <summary>A short text at the right of the row, for example a count.</summary>
	public string Detail;

	/// <summary>The colour of the detail, or null for the dim text colour.</summary>
	public Colour? DetailColour;

	public bool Active;

	public bool Dim;
}

/// <summary>
/// A row of the properties panel. A row with <see cref="Commit"/> takes a typed value. A row with <see cref="Step"/>
/// changes on a click: a left click gives 1, a right click gives -1.
/// </summary>
internal sealed class EditorPropertyRow
{
	public string Label;

	public string Value;

	public Func<string, bool> Commit;

	public Action<int> Step;

	public bool IsHeading;
}

/// <summary>
/// A tool of the level editor edits one part of the level. The editor gives the mouse clicks in the level, the
/// drags, the keyboard commands and the clicks in the side panel to the current tool.
/// </summary>
internal abstract class EditorTool : IDisposable
{
	protected EditorTool(LevelEditor editor)
	{
		Editor = editor;
	}

	protected LevelEditor Editor { get; }

	/// <summary>The name on the button of the tool.</summary>
	public abstract string Name { get; }

	/// <summary>The help text in the status bar.</summary>
	public abstract string Hint { get; }

	/// <summary>The counts in the status bar, for example the number of objects.</summary>
	public abstract string Status { get; }

	public abstract string ListHeading { get; }

	public abstract string PropertiesHeading { get; }

	public virtual bool HasSelection => false;

	public abstract IReadOnlyList<EditorListRow> GetListRows();

	public virtual void OnListClick(int index, int direction)
	{
	}

	public abstract List<EditorPropertyRow> GetPropertyRows();

	/// <summary>The tool draws the properties panel itself, with <see cref="DrawProperties"/>, in place of the rows.</summary>
	public virtual bool HasCustomProperties => false;

	public virtual void DrawProperties(Renderer renderer, I2dRenderer g, Rectangle area)
	{
	}

	public virtual void OnPropertiesClick(Vector2 mouse, Rectangle area, int direction)
	{
	}

	public virtual void OnPropertiesWheel(double wheel)
	{
	}

	/// <summary>The editor opens.</summary>
	public virtual void Begin()
	{
	}

	/// <summary>Another level loads into the editor. The tool forgets its state of the old level.</summary>
	public virtual void OnNewLevel()
	{
	}

	/// <summary>Called on each frame of the editor.</summary>
	public virtual void Update()
	{
	}

	/// <summary>The left mouse button goes down in the level. The tool can start a drag or a box selection.</summary>
	public abstract void OnLevelPress();

	/// <summary>The mouse moves with the button down, after <see cref="LevelEditor.BeginToolDrag"/>.</summary>
	public virtual void OnDrag()
	{
	}

	/// <summary>The mouse button goes up, after <see cref="LevelEditor.BeginToolDrag"/>.</summary>
	public virtual void OnDragEnd()
	{
	}

	/// <summary>The user dragged a box, after <see cref="LevelEditor.BeginBoxSelect"/>. Add the items in the box to the selection.</summary>
	public virtual void SelectInBox(Rectangle box)
	{
	}

	public virtual void DeleteSelection()
	{
	}

	public virtual void Copy()
	{
	}

	public virtual void Paste(Vector2 position)
	{
	}

	public virtual void Duplicate()
	{
	}

	public virtual void SelectAllInView()
	{
	}

	public virtual void ClearSelection()
	{
	}

	/// <summary>Moves the selection by a distance.</summary>
	public virtual void Nudge(Vector2i delta)
	{
	}

	/// <summary>Esc: stops an action of the tool, for example adding objects.</summary>
	/// <returns><c>true</c> if the tool stopped an action. Otherwise the editor clears the selection.</returns>
	public virtual bool Cancel() => false;

	/// <summary>Draws the items of the tool in the level, under the panels.</summary>
	public abstract void DrawOverlay(Renderer renderer, I2dRenderer g);

	/// <summary>Gives the text next to the mouse, or null. The tool can also draw a cursor.</summary>
	public virtual string GetCursorLabel(I2dRenderer g) => null;

	public virtual void Dispose()
	{
	}
}
