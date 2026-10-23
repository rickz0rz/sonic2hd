using System.Collections.Generic;

namespace SonicOrca.Core;

/// <summary>
/// A node in the tree of level layers and layer groups.
/// </summary>
public interface ILevelLayerTreeNode
{
	string Name { get; set; }

	bool Editing { get; set; }

	bool Visible { get; set; }

	IList<ILevelLayerTreeNode> Children { get; }
}
