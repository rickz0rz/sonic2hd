using System.IO;
using System.Linq;
using System.Text;
using SonicOrca.Core;
using SonicOrca.Graphics;
using SonicOrca.Original;
using Xunit;

namespace S2HD.Tests;

/// <summary>Tests the palette cycles of the sprites of the objects: the key colours and the colours of the steps.</summary>
public sealed class ObjectPaletteCyclesTests
{
	[Fact]
	public void GetSwaps_GivesTheColourOfTheStepOfEachKey()
	{
		ObjectPaletteCycles cycles = new ObjectPaletteCycles(new[]
		{
			new ObjectPaletteCycles.Entry(Colour.ParseHex("#FF0001"), new[] { Colour.ParseHex("#F00"), Colour.ParseHex("#FF0"), Colour.ParseHex("#0F0") }, 8)
		}, new[] { "SONICORCA/OBJECTS/CNZBUMPER" });

		Assert.True(cycles.Uses("sonicorca/objects/cnzbumper"));
		Assert.False(cycles.Uses("SONICORCA/OBJECTS/RING"));
		Assert.Equal(Colour.ParseHex("#F00"), cycles.GetSwaps(7)[0].To);
		Assert.Equal(Colour.ParseHex("#FF0"), cycles.GetSwaps(8)[0].To);
		Assert.Equal(Colour.ParseHex("#F00"), cycles.GetSwaps(24)[0].To);
		Assert.Equal(Colour.ParseHex("#FF0001"), cycles.GetSwaps(0)[0].From);
	}

	[Fact]
	public void ToXml_ThenFromXml_KeepsTheEntriesAndTheObjects()
	{
		ObjectPaletteCycles cycles = new ObjectPaletteCycles(new[]
		{
			new ObjectPaletteCycles.Entry(Colour.ParseHex("#FF6D01"), new[] { Colour.ParseHex("#FF6D00"), Colour.ParseHex("#F00") }, 6)
		}, new[] { "SONICORCA/OBJECTS/WFZBOSS" });

		ObjectPaletteCycles copy = ObjectPaletteCycles.FromXml(new MemoryStream(Encoding.UTF8.GetBytes(cycles.ToXml())));

		ObjectPaletteCycles.Entry entry = Assert.Single(copy.Entries);
		Assert.Equal((Colour.ParseHex("#FF6D01"), 6), (entry.Key, entry.Period));
		Assert.Equal(cycles.Entries[0].Colours, entry.Colours);
		Assert.True(copy.Uses("SONICORCA/OBJECTS/WFZBOSS"));
	}

	[Fact]
	public void MakeKeyPalette_GivesEachCycledEntryAColourThatIsNotInThePalette()
	{
		// Entries 1 and 2 have the same colour as entry 0, so their keys must differ from it and from each other
		uint[] palette = new uint[64];
		palette[0] = palette[1] = palette[2] = 0xFF0000FF;
		palette[3] = 0x00FF00FF;
		PaletteCycle cycle = new PaletteCycle(8, new[] { new[] { (1, 0xFFFF00FFu), (2, 0x00FF00FFu) } });

		(uint[] keyPalette, var keys) = SpriteImport.MakeKeyPalette(palette, new[] { cycle });

		Assert.Equal(new[] { 1, 2 }, keys.Keys.OrderBy(k => k));
		Assert.Equal(2, keys.Values.Distinct().Count());
		Assert.All(keys.Values, key => Assert.DoesNotContain(key, palette));
		Assert.Equal(palette[0], keyPalette[0]);
		Assert.Equal(keys[1], keyPalette[1]);
	}
}
