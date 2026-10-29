using System.Collections.Generic;

namespace SonicOrca.Core;

/// <summary>
/// An active object that keeps its state variables in a table, not in C# members. A script object does this. The
/// engine then gives the values of a placement to the table, and copies the table from the entry state to each new
/// instance.
/// </summary>
public interface IStateTable
{
	/// <summary>Sets a state variable from the text of a placement value.</summary>
	void SetStateValue(string name, string value);

	/// <summary>Takes the state variables from the state of the entry (see <see cref="ObjectEntry.State"/>).</summary>
	void CopyStateFrom(ActiveObject state);
}

/// <summary>An object type whose instances keep their state variables in a table (see <see cref="IStateTable"/>).</summary>
public interface IStateTableType
{
	/// <summary>The state variables that the level editor shows, with their types and default values.</summary>
	IReadOnlyList<ObjectEditorProperty> StateProperties { get; }
}
