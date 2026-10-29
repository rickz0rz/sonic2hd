using System;
using System.Collections.Generic;
using System.Linq;

namespace SonicOrca.Core.Editor;

/// <summary>
/// Adds and removes the layers of a map. The collision paths and the shadows of the map refer to a layer by its
/// index, so a change of the layers also changes these indices. Each change gives the new index of each old index,
/// so that the editor can change the indices of the placements and of the tile collision.
/// </summary>
public static class EditorLayerList
{
	/// <summary>Makes an empty layer with the size and the scroll settings of a layer, and a name that the map does not use.</summary>
	public static LevelLayer CreateLayerLike(LevelMap map, LevelLayer model)
	{
		LevelLayer layer = new LevelLayer(map)
		{
			Name = Enumerable.Range(1, int.MaxValue).Select(n => $"layer_{n}").First(name => map.FindLayer(name) == null)
		};
		layer.Resize(Math.Max(1, model.Columns), Math.Max(1, model.Rows));
		EditorLayerScroll.Of(model).ApplyTo(layer);
		return layer;
	}

	/// <summary>Puts a layer at an index of the map. The game draws it in front of the layer before it.</summary>
	/// <returns>The new index of each old index.</returns>
	public static int[] Insert(LevelMap map, int index, LevelLayer layer)
	{
		LevelLayer[] oldLayers = map.Layers.ToArray();
		// The layer goes in the group of the layer next to it
		LevelLayer neighbour = index > 0 ? oldLayers[index - 1] : oldLayers.FirstOrDefault();
		IList<ILevelLayerTreeNode> siblings = neighbour == null ? map.LayerTree.Children : FindParent(map.LayerTree, neighbour)?.Children ?? map.LayerTree.Children;
		int position = neighbour == null ? siblings.Count : siblings.IndexOf(neighbour) + (index > 0 ? 1 : 0);
		siblings.Insert(Math.Clamp(position, 0, siblings.Count), layer);
		map.Layers.Insert(index, layer);
		return FixIndices(map, oldLayers);
	}

	/// <summary>Removes the layer at an index of the map.</summary>
	/// <returns>The new index of each old index, or -1 for the removed layer.</returns>
	public static int[] Remove(LevelMap map, int index)
	{
		LevelLayer[] oldLayers = map.Layers.ToArray();
		LevelLayer layer = oldLayers[index];
		(FindParent(map.LayerTree, layer)?.Children ?? map.LayerTree.Children).Remove(layer);
		map.Layers.RemoveAt(index);
		return FixIndices(map, oldLayers);
	}

	private static ILevelLayerTreeNode FindParent(ILevelLayerTreeNode node, LevelLayer layer)
	{
		if (node.Children == null)
		{
			return null;
		}
		if (node.Children.Contains(layer))
		{
			return node;
		}
		return node.Children.Select(child => FindParent(child, layer)).FirstOrDefault(parent => parent != null);
	}

	/// <summary>Changes the indices of the collision paths and of the shadows after a change of the layers.</summary>
	private static int[] FixIndices(LevelMap map, LevelLayer[] oldLayers)
	{
		int[] newIndices = oldLayers.Select(layer => map.Layers.IndexOf(layer)).ToArray();
		for (int i = 0; i < map.CollisionPathLayers.Count; i++)
		{
			int old = map.CollisionPathLayers[i];
			if (old >= 0 && old < newIndices.Length)
			{
				map.CollisionPathLayers[i] = newIndices[old];
			}
		}
		for (int i = 0; i < oldLayers.Length; i++)
		{
			if (newIndices[i] < 0)
			{
				continue;
			}
			// A shadow refers to the layer that casts it by an offset from the index of its layer
			foreach (LevelLayerShadow shadow in oldLayers[i].Shadows.ToArray())
			{
				int caster = i + shadow.LayerIndexOffset;
				if (caster < 0 || caster >= oldLayers.Length || newIndices[caster] < 0)
				{
					oldLayers[i].Shadows.Remove(shadow);
				}
				else
				{
					shadow.LayerIndexOffset = newIndices[caster] - newIndices[i];
				}
			}
		}
		for (int i = 0; i < map.Layers.Count; i++)
		{
			map.Layers[i].Index = i;
		}
		return newIndices;
	}
}
