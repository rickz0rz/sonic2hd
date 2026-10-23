using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SonicOrca.Resources;
using Xunit;

namespace S2HD.Tests;

/// <summary>Reads and rewrites the real sonicorca.dat. Skipped when the game files are not available.</summary>
public sealed class GameDataTests
{
	[Fact]
	public void Scan_FindsAllResourcesOfTheDemo()
	{
		IDictionary<string, Resource> resources = ScanGameData();

		Assert.Equal(841, resources.Count);
		Assert.Equal(523, resources.Values.Count(r => r.Identifier == ResourceTypeIdentifier.TexturePNG));
		Assert.Equal(67, resources.Values.Count(r => r.Identifier == ResourceTypeIdentifier.SampleOGG));
		Assert.Equal(ResourceTypeIdentifier.Map, resources["SONICORCA/LEVELS/EHZ/MAP"].Identifier);
	}

	[Fact]
	public void WriteThenScan_KeepsEveryResource()
	{
		IDictionary<string, Resource> original = ScanGameData();
		var tree = new ResourceTree();
		foreach (var (key, resource) in original)
		{
			tree.SetOrAdd(key, resource);
		}
		string directory = Directory.CreateTempSubdirectory("s2hd-tests-").FullName;
		try
		{
			string copyPath = Path.Combine(directory, "copy.dat");

			new ResourceFile(copyPath).Write(tree);
			IDictionary<string, Resource> copy = new ResourceFile(copyPath).Scan().GetResourceListing();

			Assert.Equal(original.Keys.OrderBy(k => k), copy.Keys.OrderBy(k => k));
			foreach (var (key, resource) in original)
			{
				Assert.Equal(resource.Identifier, copy[key].Identifier);
				Assert.True(
					ResourceFileTests.ReadAll(resource.Source.ReadUncompressed())
						.AsSpan().SequenceEqual(ResourceFileTests.ReadAll(copy[key].Source.ReadUncompressed())),
					"Content changed: " + key);
			}
		}
		finally
		{
			Directory.Delete(directory, recursive: true);
		}
	}

	[Fact]
	public void UnpackThenPack_KeepsEveryResource()
	{
		IDictionary<string, Resource> original = ScanGameData();
		var tree = new ResourceTree();
		foreach (var (key, resource) in original)
		{
			tree.SetOrAdd(key, resource);
		}
		string directory = Directory.CreateTempSubdirectory("s2hd-tests-").FullName;
		try
		{
			string unpacked = Path.Combine(directory, "unpacked");
			Assert.Equal(original.Count, ResourceDirectory.Export(tree, unpacked));
			string packedPath = Path.Combine(directory, "packed.dat");
			new ResourceFile(packedPath).Write(ResourceDirectory.Import(unpacked));
			ResourceTree packed = new ResourceFile(packedPath).Scan();

			Assert.Equal(original.Count, packed.GetResourceListing().Count);
			Assert.Empty(ResourceComparison.GetChanges(packed, tree).GetResourceListing());
		}
		finally
		{
			Directory.Delete(directory, recursive: true);
		}
	}

	private static IDictionary<string, Resource> ScanGameData()
	{
		string path = TestPaths.GameDataFile;
		if (path == null)
		{
			Assert.Skip("Game files not found. Set S2HD_GAME_DIR to the S2HD_2.0.1012-rc2 folder.");
		}
		return new ResourceFile(path).Scan().GetResourceListing();
	}
}
