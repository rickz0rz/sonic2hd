using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace SonicOrca.Original;

/// <summary>An animation script of the original: its name, the delay of each frame, the mapping frames, and the end.</summary>
/// <param name="Name">The name, written as a comment in the animation group.</param>
/// <param name="Delay">The frames that each mapping frame shows, minus 1, as in the original.</param>
/// <param name="Frames">The mapping frames.</param>
/// <param name="HoldLast">True when the script stays on its last frame ($FE 1 or $FD). False when it starts again ($FF).</param>
public sealed record OriginalScript(string Name, int Delay, int[] Frames, bool HoldLast = false);

/// <summary>
/// Makes the sprites of a character of the original, whose frames load their tiles with DPLC (dynamic pattern load
/// cues). The animation group has one animation for each script, in order.
/// </summary>
public static class DplcSpriteImport
{
	private const int BytesPerTile = 32;

	/// <summary>Makes the texture and the animation group, at four times the size of the original.</summary>
	/// <param name="art">The uncompressed art of the character.</param>
	/// <param name="mappings">The mapping frames.</param>
	/// <param name="dplcs">The DPLC of each mapping frame (see <see cref="SuperSonicArt.ParseDplc"/>).</param>
	/// <param name="paletteLine">The palette line of the sprites.</param>
	/// <param name="textures">The textures of the group, for example "//TEXTURE".</param>
	public static ImportedSprite Import(string objectKey, byte[] art, List<List<SpritePiece>> mappings, List<List<(int Tiles, int Offset)>> dplcs,
		IReadOnlyList<OriginalScript> scripts, uint[] palette, int paletteLine, IReadOnlyList<string> textures)
	{
		// Each frame gets its own tiles: the DPLC of the frame loads them to the start of the video memory of the character
		int[] frameIds = scripts.SelectMany(script => script.Frames).Distinct().OrderBy(id => id).ToArray();
		var frameArt = new List<byte>();
		var frames = new List<List<SpritePiece>>();
		foreach (int id in frameIds)
		{
			int firstTile = frameArt.Count / BytesPerTile;
			foreach ((int tiles, int offset) in dplcs[id])
			{
				frameArt.AddRange(art.AsSpan(offset * BytesPerTile, tiles * BytesPerTile).ToArray());
			}
			frames.Add(mappings[id].Select(piece => piece with { Tile = piece.Tile + firstTile }).ToList());
		}
		ImportedSprite sprite = SpriteImport.Import(objectKey, frameArt.ToArray(), frames, new[] { paletteLine }, Array.Empty<int>(), palette, 4);

		// The group of SpriteImport has one animation for each frame. Put the frames of each script together.
		XElement[] frameElements = XDocument.Parse(sprite.AnimationGroup).Descendants("frame").ToArray();
		var anigroup = new StringBuilder();
		anigroup.AppendLine("<?xml version=\"1.0\"?>");
		anigroup.AppendLine("<anigroup>");
		anigroup.AppendLine("  <textures>");
		foreach (string texture in textures)
		{
			anigroup.AppendLine($"    <texture>{texture}</texture>");
		}
		anigroup.AppendLine("  </textures>");
		anigroup.AppendLine("  <animations>");
		foreach (OriginalScript script in scripts)
		{
			string loop = script.HoldLast ? $" loop=\"{script.Frames.Length - 1}\"" : "";
			anigroup.AppendLine($"    <!-- {script.Name} -->");
			anigroup.AppendLine($"    <animation delay=\"{script.Delay}\"{loop}>");
			foreach (int id in script.Frames)
			{
				XElement frame = frameElements[Array.IndexOf(frameIds, id)];
				anigroup.AppendLine($"      <frame x=\"{frame.Attribute("x").Value}\" y=\"{frame.Attribute("y").Value}\" w=\"{frame.Attribute("w").Value}\" " +
					$"h=\"{frame.Attribute("h").Value}\" offset_x=\"{frame.Attribute("offset_x").Value}\" offset_y=\"{frame.Attribute("offset_y").Value}\" />");
			}
			anigroup.AppendLine("    </animation>");
		}
		anigroup.AppendLine("  </animations>");
		anigroup.AppendLine("</anigroup>");
		return sprite with { AnimationGroup = anigroup.ToString() };
	}
}
