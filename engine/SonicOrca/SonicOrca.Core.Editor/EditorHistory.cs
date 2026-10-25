using System;
using System.Collections.Generic;
using System.Linq;

namespace SonicOrca.Core.Editor;

/// <summary>
/// The undo and redo history of the level editor. Each step keeps the list of placements and the selection before a
/// change. The editor never changes a placement. It replaces the placement with a copy, so a step can keep the old
/// placements.
/// </summary>
public sealed class EditorHistory
{
	/// <summary>The history removes the oldest step when it has more steps than this value.</summary>
	public const int MaxSteps = 200;

	private readonly List<Step> _undo = new List<Step>();

	private readonly List<Step> _redo = new List<Step>();

	private sealed class Step
	{
		public ObjectPlacement[] Placements;

		public Guid[] Selection;
	}

	public bool CanUndo => _undo.Count > 0;

	public bool CanRedo => _redo.Count > 0;

	/// <summary>Keeps the placements and the selection as they are before a change. A new change removes the redo steps.</summary>
	public void Record(IEnumerable<ObjectPlacement> placements, IEnumerable<Guid> selection)
	{
		_undo.Add(new Step { Placements = placements.ToArray(), Selection = selection.ToArray() });
		if (_undo.Count > MaxSteps)
		{
			_undo.RemoveAt(0);
		}
		_redo.Clear();
	}

	/// <summary>Puts back the placements and the selection of the last step.</summary>
	/// <returns><c>true</c> if there was a step to undo.</returns>
	public bool Undo(IList<ObjectPlacement> placements, ISet<Guid> selection) => Move(_undo, _redo, placements, selection);

	/// <summary>Puts back the placements and the selection of the last undone step.</summary>
	/// <returns><c>true</c> if there was a step to redo.</returns>
	public bool Redo(IList<ObjectPlacement> placements, ISet<Guid> selection) => Move(_redo, _undo, placements, selection);

	private static bool Move(List<Step> from, List<Step> to, IList<ObjectPlacement> placements, ISet<Guid> selection)
	{
		if (from.Count == 0)
		{
			return false;
		}
		to.Add(new Step { Placements = placements.ToArray(), Selection = selection.ToArray() });
		Step step = from[from.Count - 1];
		from.RemoveAt(from.Count - 1);
		placements.Clear();
		foreach (ObjectPlacement placement in step.Placements)
		{
			placements.Add(placement);
		}
		selection.Clear();
		selection.UnionWith(step.Selection);
		return true;
	}
}
