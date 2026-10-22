using System.Collections.Generic;

namespace SonicOrca.Core;

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
