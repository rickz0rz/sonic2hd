using System.Collections.Generic;

namespace SonicOrca.Core;

public interface ILevelLayerTreeNode
{
	string Name { get; set; }

	bool Editing { get; set; }

	bool Visible { get; set; }

	IList<ILevelLayerTreeNode> Children { get; }
}
