using System;
using System.Collections.Generic;
using SonicOrca.Core.Collision;

namespace SonicOrca.Core.Editor;

/// <summary>
/// A change of one tile: the tile values before and after, and for the front layer of a map with tile collision,
/// the solidity before and after.
/// </summary>
public readonly record struct TileEdit(int Layer, int Column, int Row, int OldTile, int NewTile, byte OldSolidity, byte NewSolidity);

/// <summary>
/// The state of the level editor that the undo history keeps: the placements, the markers, the collision lines and
/// the selections. The editor never changes a placement, a marker or a line. It replaces it with a copy, so a
/// snapshot can keep the old ones. The tiles are too large to copy for each step, so a step keeps the tile edits of
/// the change instead (<see cref="TileEdits"/>).
/// </summary>
public sealed record EditorSnapshot
{
	public ObjectPlacement[] Placements { get; init; } = Array.Empty<ObjectPlacement>();

	public Guid[] Selection { get; init; } = Array.Empty<Guid>();

	public LevelMarker[] Markers { get; init; } = Array.Empty<LevelMarker>();

	public LevelMarker[] MarkerSelection { get; init; } = Array.Empty<LevelMarker>();

	public CollisionVector[] Vectors { get; init; } = Array.Empty<CollisionVector>();

	public CollisionVector[] VectorSelection { get; init; } = Array.Empty<CollisionVector>();

	/// <summary>The tile edits of the change after this snapshot. Undo puts back the old values, and redo the new values.</summary>
	public TileEdit[] TileEdits { get; init; } = Array.Empty<TileEdit>();
}

/// <summary>
/// The undo and redo history of the level editor. Each step keeps the snapshot of the editor before a change.
/// </summary>
public sealed class EditorHistory
{
	/// <summary>The history removes the oldest step when it has more steps than this value.</summary>
	public const int MaxSteps = 200;

	private readonly List<EditorSnapshot> _undo = new List<EditorSnapshot>();

	private readonly List<EditorSnapshot> _redo = new List<EditorSnapshot>();

	public bool CanUndo => _undo.Count > 0;

	public bool CanRedo => _redo.Count > 0;

	/// <summary>Keeps the state before a change. A new change removes the redo steps.</summary>
	public void Record(EditorSnapshot before)
	{
		_undo.Add(before);
		if (_undo.Count > MaxSteps)
		{
			_undo.RemoveAt(0);
		}
		_redo.Clear();
	}

	/// <summary>Gives the state of the last step, and keeps the current state for redo.</summary>
	/// <returns>The state to put back, or null if there is no step to undo.</returns>
	public EditorSnapshot Undo(EditorSnapshot current) => Move(_undo, _redo, current);

	/// <summary>Gives the state of the last undone step, and keeps the current state for undo.</summary>
	/// <returns>The state to put back, or null if there is no step to redo.</returns>
	public EditorSnapshot Redo(EditorSnapshot current) => Move(_redo, _undo, current);

	private static EditorSnapshot Move(List<EditorSnapshot> from, List<EditorSnapshot> to, EditorSnapshot current)
	{
		if (from.Count == 0)
		{
			return null;
		}
		EditorSnapshot step = from[from.Count - 1];
		from.RemoveAt(from.Count - 1);
		// The step on the other list does the same tile edits in the other direction
		to.Add(current with { TileEdits = step.TileEdits });
		return step;
	}
}
