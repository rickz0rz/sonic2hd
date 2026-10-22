using System.Collections.Generic;

namespace SonicOrca.Core.Extensions;

public static class LevelLayerExtensions
{
	public static IEnumerable<ILevelLayerTreeNode> GetDescendantsOrdered(this ILevelLayerTreeNode node)
	{
		IList<ILevelLayerTreeNode> children = node.Children;
		if (children == null)
		{
			yield break;
		}
		foreach (ILevelLayerTreeNode child in children)
		{
			yield return child;
			foreach (ILevelLayerTreeNode item in child.GetDescendantsOrdered())
			{
				yield return item;
			}
		}
	}
}
