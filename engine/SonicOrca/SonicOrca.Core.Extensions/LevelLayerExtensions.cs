using System.Collections.Generic;

namespace SonicOrca.Core.Extensions;

/// <summary>
/// Extension methods for the level layer tree.
/// </summary>
public static class LevelLayerExtensions
{
	/// <summary>
	/// Returns all descendants of the node in depth-first pre-order.
	/// </summary>
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
			foreach (ILevelLayerTreeNode descendant in child.GetDescendantsOrdered())
			{
				yield return descendant;
			}
		}
	}
}
