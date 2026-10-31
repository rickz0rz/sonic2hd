using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Hjg.Pngcs;
using SonicOrca.Resources;

namespace SonicOrca.Core.Editor;

/// <summary>The kinds of new object. Each kind has a script that works, for the user to change.</summary>
public enum NewObjectKind
{
	/// <summary>Draws its image and does nothing more.</summary>
	Scenery,

	/// <summary>A solid box that the characters can stand on.</summary>
	SolidBlock,

	/// <summary>Gives rings to the character that touches it, then goes away.</summary>
	Collectible,

	/// <summary>Walks on the ground. A character that rolls or jumps into it breaks it, and a touch hurts the character.</summary>
	Enemy
}

/// <summary>
/// Makes the files of a new Lua object: the script <c>_.object.lua</c>, the animation group with one frame, and the
/// image. The files go into a mod folder, as <c>s2hd-data unpack</c> writes them, so the user can change them in
/// other programs. The game loads the script again when its file changes.
/// </summary>
public static class NewObject
{
	public const string ObjectKeyPrefix = "SONICORCA/OBJECTS/";

	/// <summary>The most characters in the key of a new object.</summary>
	public const int MaxKeyLength = 24;

	/// <summary>The mod folder for new objects when the open level has no mod folder of its own.</summary>
	public const string DefaultModName = "objects";

	private static readonly byte[] PngSignature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

	/// <summary>Gives the problem of a key, or null when the key is correct. The key is the last part of the resource key.</summary>
	public static string CheckKey(string key)
	{
		if (string.IsNullOrEmpty(key))
			return "The key is empty.";
		if (key.Length > MaxKeyLength)
			return $"The key can have a maximum of {MaxKeyLength} characters.";
		// A file name gives the key. An underscore in a file name divides the key, so the key has none.
		if (!key.All(c => char.IsAsciiLetterUpper(c) || char.IsAsciiDigit(c)))
			return "The key can have only capital letters and digits.";
		return null;
	}

	/// <summary>The key from a name: the letters and digits, in capitals.</summary>
	public static string MakeKey(string name)
	{
		string key = new string((name ?? "").Where(char.IsAsciiLetterOrDigit).Select(char.ToUpperInvariant).ToArray());
		return key.Length > MaxKeyLength ? key.Substring(0, MaxKeyLength) : key;
	}

	/// <summary>The text of a kind in the editor.</summary>
	public static string GetKindName(NewObjectKind kind) => kind switch
	{
		NewObjectKind.SolidBlock => "solid block",
		NewObjectKind.Collectible => "collectible",
		NewObjectKind.Enemy => "enemy",
		_ => "scenery"
	};

	/// <summary>The size of a PNG image, from its header, or null when the data is not a PNG image.</summary>
	public static (int Width, int Height)? GetPngSize(byte[] data)
	{
		if (data == null || data.Length < 24 || !data.AsSpan(0, PngSignature.Length).SequenceEqual(PngSignature))
			return null;
		int width = data[16] << 24 | data[17] << 16 | data[18] << 8 | data[19];
		int height = data[20] << 24 | data[21] << 16 | data[22] << 8 | data[23];
		return width > 0 && height > 0 ? (width, height) : null;
	}

	/// <summary>
	/// Makes the files of the object in <paramref name="modFolder"/>, and adds their resources to the tree, so the
	/// object can load before the game starts again.
	/// </summary>
	/// <param name="image">The image of the object, or null for a placeholder image.</param>
	/// <returns>The path of the script.</returns>
	public static string Create(ResourceTree tree, string modFolder, string key, string name, NewObjectKind kind, byte[] image)
	{
		if (CheckKey(key) is string problem)
			throw new InvalidOperationException(problem);
		if (string.IsNullOrWhiteSpace(name))
			throw new InvalidOperationException("The name is empty.");
		string objectKey = ObjectKeyPrefix + key;
		if (tree[objectKey]?.Resource != null)
			throw new InvalidOperationException($"The object {key} exists.");
		image ??= MakePlaceholder(kind);
		if (GetPngSize(image) is not (int width, int height))
			throw new InvalidOperationException("The image is not a PNG file.");
		string objectFolder = Path.Combine(new[] { modFolder }.Concat(objectKey.Split('/')).ToArray());
		if (Directory.Exists(objectFolder))
			throw new InvalidOperationException($"The folder {objectFolder} exists.");

		ResourceTree files = new ResourceTree();
		void Add(string resourceKey, ResourceTypeIdentifier identifier, byte[] data) =>
			files.SetOrAdd(resourceKey, new Resource(resourceKey, identifier, new MemoryResourceSource(data)));
		Add(objectKey, ResourceTypeIdentifier.LuaObject, Encoding.UTF8.GetBytes(MakeScript(kind, name.Trim(), width, height)));
		Add(objectKey + "/ANIGROUP", ResourceTypeIdentifier.AnimationGroup, Encoding.UTF8.GetBytes(MakeAnimationGroup(width, height)));
		Add(objectKey + "/TEXTURE", ResourceTypeIdentifier.TexturePNG, image);
		ResourceDirectory.Export(files, modFolder);

		// The game loads the mod folders when it starts. The new files load now, from the disk, so that the
		// script reloads when the user changes its file.
		foreach (KeyValuePair<string, Resource> item in files.GetResourceListing())
		{
			string path = item.Key == objectKey
				? Path.Combine(objectFolder, "_.object.lua")
				: Path.Combine(objectFolder, item.Key.Substring(objectKey.Length + 1) + ResourceType.FromIdentifier(item.Value.Identifier).DefaultExtension);
			tree.SetOrAdd(item.Key, new Resource(item.Key, item.Value.Identifier, new FileResourceSource(path, 0, new FileInfo(path).Length)));
		}
		return Path.Combine(objectFolder, "_.object.lua");
	}

	/// <summary>The animation group: one animation with one frame that covers the image.</summary>
	public static string MakeAnimationGroup(int width, int height)
	{
		return $"""
			<?xml version="1.0"?>
			<anigroup>
			  <textures>
			    <texture>//TEXTURE</texture>
			  </textures>
			  <animations>
			    <!-- 0: the image -->
			    <animation>
			      <frame texture="0" x="0" y="0" w="{width}" h="{height}" />
			    </animation>
			  </animations>
			</anigroup>

			""";
	}

	/// <summary>The script of a kind. The sizes are the size of the image: one pixel of the image is one unit.</summary>
	public static string MakeScript(NewObjectKind kind, string name, int width, int height)
	{
		string halfWidth = (width / 2).ToString(CultureInfo.InvariantCulture);
		string halfHeight = (height / 2).ToString(CultureInfo.InvariantCulture);
		string quotedName = QuoteLua(name);
		StringBuilder script = new StringBuilder();
		script.Append($"-- {name.Replace('\n', ' ')}. The level editor made this script. Change it and save the file: the game\n");
		script.Append("-- loads it again. The README of examples/mods/lua-item-block describes the functions.\n");
		switch (kind)
		{
			case NewObjectKind.SolidBlock:
				script.Append($$"""
					object {
					  api = 1,
					  name = {{quotedName}},
					  description = "A solid block",
					  classification = "general",
					  dependencies = { animations = "/ANIGROUP" },
					}

					local HALF_WIDTH, HALF_HEIGHT = {{halfWidth}}, {{halfHeight}}

					function start(self)
					  self:set_design_bounds(-HALF_WIDTH, -HALF_HEIGHT, HALF_WIDTH * 2, HALF_HEIGHT * 2)
					  self.sprite = self:animation("animations", 0)
					  self:set_solid_box(-HALF_WIDTH, -HALF_HEIGHT, HALF_WIDTH * 2, HALF_HEIGHT * 2)
					end

					function draw(self)
					  self:draw(self.sprite)
					end

					""");
				break;
			case NewObjectKind.Collectible:
				script.Append($$"""
					object {
					  api = 1,
					  name = {{quotedName}},
					  description = "Gives rings to the character that touches it, then goes away",
					  classification = "general",
					  dependencies = {
					    animations = "/ANIGROUP",
					    sound = "SONICORCA/SOUND/RING",
					  },
					  -- The level editor shows these values
					  state = { rings = 1 },
					}

					local HALF_WIDTH, HALF_HEIGHT = {{halfWidth}}, {{halfHeight}}

					function start(self)
					  self:set_design_bounds(-HALF_WIDTH, -HALF_HEIGHT, HALF_WIDTH * 2, HALF_HEIGHT * 2)
					  self.sprite = self:animation("animations", 0)
					  self:add_hitbox(1, -HALF_WIDTH, -HALF_HEIGHT, HALF_WIDTH * 2, HALF_HEIGHT * 2)
					end

					function collision(self, event)
					  local character = event.character
					  if character == nil or character.is_debug then
					    return
					  end
					  character:gain_rings(self.state.rings)
					  self:play_sound("sound")
					  self:finish_forever()
					end

					function draw(self)
					  self:draw(self.sprite)
					end

					""");
				break;
			case NewObjectKind.Enemy:
				script.Append($$"""
					object {
					  api = 1,
					  name = {{quotedName}},
					  description = "Walks on the ground and turns at edges and walls",
					  classification = "badnik",
					  dependencies = { animations = "/ANIGROUP" },
					  -- The level editor shows these values
					  state = { speed = 4, start_right = false },
					}

					local HALF_WIDTH, HALF_HEIGHT = {{halfWidth}}, {{halfHeight}}

					function start(self)
					  self:set_design_bounds(-HALF_WIDTH, -HALF_HEIGHT, HALF_WIDTH * 2, HALF_HEIGHT * 2)
					  self.sprite = self:animation("animations", 0)
					  self.direction = self.state.start_right and 1 or -1
					  self:add_hitbox(1, -HALF_WIDTH, -HALF_HEIGHT, HALF_WIDTH * 2, HALF_HEIGHT * 2)
					end

					function update(self)
					  local feet = self.y + HALF_HEIGHT
					  local ahead = self:find_floor(self.x + self.direction * (HALF_WIDTH + 4), feet - 32, 96)
					  -- No floor ahead is an edge. A floor far above the feet is a wall.
					  if ahead == nil or ahead < feet - 24 then
					    self.direction = -self.direction
					    return
					  end
					  self.x = self.x + self.direction * self.state.speed
					  local floor = self:find_floor(self.x, feet - 32, 96)
					  if floor ~= nil then
					    self.y = floor - HALF_HEIGHT
					  end
					end

					function collision(self, event)
					  local character = event.character
					  if character == nil or character.is_debug then
					    return
					  end
					  -- A character that rolls or jumps breaks the enemy. A touch hurts the character.
					  if character.is_deadly then
					    self:destroy_enemy(character)
					  else
					    character:hurt(character.x < self.x and -1 or 1)
					  end
					end

					function draw(self)
					  -- The image looks to the left, so it is mirrored when the enemy walks to the right
					  self:draw(self.sprite, self.direction > 0)
					end

					""");
				break;
			default:
				script.Append($$"""
					object {
					  api = 1,
					  name = {{quotedName}},
					  description = "An image in the level",
					  classification = "scenery",
					  dependencies = { animations = "/ANIGROUP" },
					}

					local HALF_WIDTH, HALF_HEIGHT = {{halfWidth}}, {{halfHeight}}

					function start(self)
					  self:set_design_bounds(-HALF_WIDTH, -HALF_HEIGHT, HALF_WIDTH * 2, HALF_HEIGHT * 2)
					  self.sprite = self:animation("animations", 0)
					end

					function draw(self)
					  self:draw(self.sprite)
					end

					""");
				break;
		}
		return script.ToString();
	}

	private static string QuoteLua(string text)
	{
		return "\"" + text.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", " ").Replace("\r", "") + "\"";
	}

	/// <summary>
	/// A placeholder image for a kind: a shape with a dark edge, until the user has art. An enemy has an eye on the
	/// left, because the enemy script mirrors the image when the enemy walks to the right.
	/// </summary>
	public static byte[] MakePlaceholder(NewObjectKind kind)
	{
		(int width, int height, uint colour) = kind switch
		{
			NewObjectKind.SolidBlock => (128, 128, 0xB0703Au),
			NewObjectKind.Collectible => (64, 64, 0xF0C020u),
			NewObjectKind.Enemy => (96, 72, 0xD04040u),
			_ => (128, 128, 0x40A060u)
		};
		const int Edge = 6;
		bool round = kind == NewObjectKind.Collectible;
		byte[] pixels = new byte[width * height * 4];
		for (int y = 0; y < height; y++)
		{
			for (int x = 0; x < width; x++)
			{
				double dx = x + 0.5 - width / 2.0;
				double dy = y + 0.5 - height / 2.0;
				bool inside;
				bool edge;
				if (round)
				{
					double distance = Math.Sqrt(dx * dx + dy * dy);
					inside = distance <= width / 2.0;
					edge = distance > width / 2.0 - Edge;
				}
				else
				{
					inside = true;
					edge = x < Edge || y < Edge || x >= width - Edge || y >= height - Edge;
				}
				if (kind == NewObjectKind.Enemy && Math.Abs(x - width / 4.0) < 9 && Math.Abs(y - height / 3.0) < 9)
				{
					// The eye
					edge = false;
					inside = true;
					SetPixel(pixels, width, x, y, 0xFFFFFF);
					continue;
				}
				if (inside)
				{
					SetPixel(pixels, width, x, y, edge ? Darken(colour) : colour);
				}
			}
		}
		MemoryStream stream = new MemoryStream();
		PngWriter writer = new PngWriter(stream, new ImageInfo(width, height, 8, true), "placeholder.png");
		writer.ShouldCloseStream = false;
		byte[] row = new byte[width * 4];
		for (int y = 0; y < height; y++)
		{
			Array.Copy(pixels, y * width * 4, row, 0, row.Length);
			writer.WriteRowByte(row, y);
		}
		writer.End();
		return stream.ToArray();
	}

	private static void SetPixel(byte[] pixels, int width, int x, int y, uint colour)
	{
		int index = (y * width + x) * 4;
		pixels[index] = (byte)(colour >> 16);
		pixels[index + 1] = (byte)(colour >> 8);
		pixels[index + 2] = (byte)colour;
		pixels[index + 3] = 255;
	}

	private static uint Darken(uint colour)
	{
		return (colour >> 16 & 0xFF) / 2 << 16 | (colour >> 8 & 0xFF) / 2 << 8 | (colour & 0xFF) / 2;
	}
}
