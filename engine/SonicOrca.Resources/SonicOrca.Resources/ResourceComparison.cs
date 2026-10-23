using System;
using System.Collections.Generic;
using System.IO;

namespace SonicOrca.Resources;

public static class ResourceComparison
{
	/// <summary>
	/// Returns a tree with the resources of <paramref name="tree"/> that are not in <paramref name="baseTree"/>, or
	/// that have a different type or different uncompressed bytes there. Written to the mods folder, this tree
	/// changes only those resources. It cannot remove resources.
	/// </summary>
	public static ResourceTree GetChanges(ResourceTree tree, ResourceTree baseTree)
	{
		IDictionary<string, Resource> baseResources = baseTree.GetResourceListing();
		ResourceTree changes = new ResourceTree();
		foreach (var (key, resource) in tree.GetResourceListing())
		{
			if (!baseResources.TryGetValue(key, out Resource baseResource) || !HaveSameContent(resource, baseResource))
			{
				changes.SetOrAdd(key, resource);
			}
		}
		return changes;
	}

	public static bool HaveSameContent(Resource a, Resource b)
	{
		if (a.Identifier != b.Identifier)
		{
			return false;
		}
		using Stream streamA = a.Source.ReadUncompressed();
		using Stream streamB = b.Source.ReadUncompressed();
		byte[] bufferA = new byte[81920];
		byte[] bufferB = new byte[81920];
		while (true)
		{
			int countA = streamA.ReadAtLeast(bufferA, bufferA.Length, throwOnEndOfStream: false);
			int countB = streamB.ReadAtLeast(bufferB, bufferB.Length, throwOnEndOfStream: false);
			if (countA != countB || !bufferA.AsSpan(0, countA).SequenceEqual(bufferB.AsSpan(0, countB)))
			{
				return false;
			}
			if (countA == 0)
			{
				return true;
			}
		}
	}
}
