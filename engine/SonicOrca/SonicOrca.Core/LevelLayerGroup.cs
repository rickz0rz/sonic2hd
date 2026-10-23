using System.Collections.Generic;

namespace SonicOrca.Core;

/// <summary>
/// A named group of level layers in the layer tree.
/// </summary>
public class LevelLayerGroup : ILevelLayerTreeNode
{
	private readonly List<ILevelLayerTreeNode> _children = new List<ILevelLayerTreeNode>();

	public string Name { get; set; }

	public bool Editing { get; set; }

	public bool Visible { get; set; }

	public IList<ILevelLayerTreeNode> Children => _children;

	public LevelLayerGroup(string name)
	{
		Name = name;
		Visible = true;
	}

	public override string ToString()
	{
		return $"[{Name}]";
	}
}
