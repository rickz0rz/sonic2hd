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
	Enemy,

	/// <summary>Moves to and fro and carries the characters that stand on it. It can fall when a character stands on it.</summary>
	MovingPlatform,

	/// <summary>A boss: it locks an arena, follows the nearest character, and ends the act after its last hit.</summary>
	Boss,

	/// <summary>An item that gives an ability, and the ability (a second object, the key with "ABILITY" at the end).</summary>
	PowerUp
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
		NewObjectKind.MovingPlatform => "moving platform",
		NewObjectKind.Boss => "boss",
		NewObjectKind.PowerUp => "power-up",
		_ => "scenery"
	};

	/// <summary>The key of the ability that a power-up gives: the key of the power-up with "ABILITY" at the end.</summary>
	public static string GetAbilityKey(string key) => key + "ABILITY";

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
		string abilityKey = kind == NewObjectKind.PowerUp ? ObjectKeyPrefix + GetAbilityKey(key) : null;
		if (abilityKey != null && tree[abilityKey]?.Resource != null)
			throw new InvalidOperationException($"The object {GetAbilityKey(key)} exists.");
		image ??= MakePlaceholder(kind);
		if (GetPngSize(image) is not (int width, int height))
			throw new InvalidOperationException("The image is not a PNG file.");
		string objectFolder = Path.Combine(new[] { modFolder }.Concat(objectKey.Split('/')).ToArray());
		if (Directory.Exists(objectFolder))
			throw new InvalidOperationException($"The folder {objectFolder} exists.");

		ResourceTree files = new ResourceTree();
		void Add(string resourceKey, ResourceTypeIdentifier identifier, byte[] data) =>
			files.SetOrAdd(resourceKey, new Resource(resourceKey, identifier, new MemoryResourceSource(data)));
		Add(objectKey, ResourceTypeIdentifier.LuaObject, Encoding.UTF8.GetBytes(MakeScript(kind, name.Trim(), width, height, key)));
		Add(objectKey + "/ANIGROUP", ResourceTypeIdentifier.AnimationGroup, Encoding.UTF8.GetBytes(MakeAnimationGroup(width, height)));
		Add(objectKey + "/TEXTURE", ResourceTypeIdentifier.TexturePNG, image);
		if (abilityKey != null)
			Add(abilityKey, ResourceTypeIdentifier.LuaObject, Encoding.UTF8.GetBytes(MakeAbilityScript(name.Trim())));
		ResourceDirectory.Export(files, modFolder);

		// The game loads the mod folders when it starts. The new files load now, from the disk, so that the
		// script reloads when the user changes its file.
		foreach (KeyValuePair<string, Resource> item in files.GetResourceListing())
		{
			// The object has its files in its folder, as _.object.lua and the others. The ability of a power-up has no
			// folder: it is one file next to the folder of the power-up.
			string extension = ResourceType.FromIdentifier(item.Value.Identifier).DefaultExtension;
			string path = item.Key == objectKey ? Path.Combine(objectFolder, "_.object.lua")
				: item.Key.StartsWith(objectKey + "/", StringComparison.Ordinal) ? Path.Combine(objectFolder, item.Key.Substring(objectKey.Length + 1) + extension)
				: Path.Combine(Path.GetDirectoryName(objectFolder), item.Key.Substring(item.Key.LastIndexOf('/') + 1) + extension);
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
	public static string MakeScript(NewObjectKind kind, string name, int width, int height, string key = "OBJECT")
	{
		string abilityName = GetAbilityKey(key);
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
			case NewObjectKind.MovingPlatform:
				script.Append($$"""
					object {
					  api = 3,
					  name = {{quotedName}},
					  description = "Moves to and fro and carries the characters on it",
					  classification = "platform",
					  dependencies = { animations = "/ANIGROUP" },
					  -- The level editor shows these values. The distance is in units: four units are one pixel of
					  -- the original game. With fall, the platform falls a short time after a character stands on it.
					  state = { distance = 384, speed = 4, vertical = false, fall = false },
					}

					local HALF_WIDTH, HALF_HEIGHT = {{halfWidth}}, {{halfHeight}}
					-- The delay and the gravity of the fall are the values of the platforms of the game
					local FALL_DELAY, GRAVITY, FALL_DISTANCE = 30, 0.875, 1536

					-- The game moves the platform by its velocity at the start of the next tick, and the characters on
					-- it move too
					local function set_velocity(self)
					  local speed = self.direction * self.state.speed
					  if self.state.vertical then
					    self.velocity_x, self.velocity_y = 0, speed
					  else
					    self.velocity_x, self.velocity_y = speed, 0
					  end
					end

					function start(self)
					  self:set_design_bounds(-HALF_WIDTH, -HALF_HEIGHT, HALF_WIDTH * 2, HALF_HEIGHT * 2)
					  self.sprite = self:animation("animations", 0)
					  -- A line on the top. The characters stand on it and can jump through it from below.
					  self:set_solid_top(-HALF_WIDTH, -HALF_HEIGHT, HALF_WIDTH * 2)
					  self.offset = 0
					  self.direction = 1
					  self.fall_ticks = nil
					  self.fallen = 0
					  set_velocity(self)
					end

					function update(self)
					  if self.fall_ticks ~= nil then
					    self.fall_ticks = self.fall_ticks - 1
					    if self.fall_ticks < 0 then
					      self.velocity_x = 0
					      self.velocity_y = self.velocity_y + GRAVITY
					      self.fallen = self.fallen + self.velocity_y
					      if self.fallen > FALL_DISTANCE then
					        -- The platform comes back when its area comes into view again
					        self:finish()
					      end
					    end
					    return
					  end
					  if self.state.fall and #self:standing_characters() > 0 then
					    self.fall_ticks = FALL_DELAY
					    self.velocity_x, self.velocity_y = 0, 0
					    return
					  end
					  self.offset = self.offset + self.direction * self.state.speed
					  if self.offset >= self.state.distance then
					    self.direction = -1
					  elseif self.offset <= 0 then
					    self.direction = 1
					  end
					  set_velocity(self)
					end

					function draw(self)
					  self:draw(self.sprite)
					end

					""");
				break;
			case NewObjectKind.Boss:
				script.Append($$"""
					object {
					  api = 7,
					  name = {{quotedName}},
					  description = "A boss. The act ends when it is defeated.",
					  classification = "badnik",
					  -- The game gives the object the rules of a boss: health, hits, the flash after a hit, defeat
					  base = "boss",
					  dependencies = {
					    animations = "/ANIGROUP",
					    music = "SONICORCA/MUSIC/BOSS/S2",
					  },
					  -- The level editor shows these values. The speed is in units per tick.
					  state = { health = 8, speed = 3 },
					}

					local HALF_WIDTH, HALF_HEIGHT = {{halfWidth}}, {{halfHeight}}
					-- The ticks before the boss music, of explosions after the defeat, and of the flight before the
					-- act ends
					local MUSIC_DELAY, EXPLODE_TICKS, FLEE_TICKS = 60, 120, 90

					function start(self)
					  self:set_design_bounds(-HALF_WIDTH, -HALF_HEIGHT, HALF_WIDTH * 2, HALF_HEIGHT * 2)
					  self.sprite = self:animation("animations", 0)
					  self:add_hitbox(1, -HALF_WIDTH, -HALF_HEIGHT, HALF_WIDTH * 2, HALF_HEIGHT * 2)
					  self.health = self.state.health
					  -- The fight goes on also when the boss goes out of view
					  self.lock_lifetime = true
					  self.home_y = self.y
					  self.home_x = self.x
					  self.ticks = 0
					  self.after = 0
					end

					-- The character that is nearest to the boss on the x axis, or nil
					local function nearest_character(self)
					  local nearest = nil
					  for _, character in ipairs(self:characters()) do
					    if not character.is_debug and (nearest == nil or math.abs(character.x - self.x) < math.abs(nearest.x - self.x)) then
					      nearest = character
					    end
					  end
					  return nearest
					end

					function update(self)
					  self.ticks = self.ticks + 1
					  if self.ticks == 1 then
					    -- Lock the camera on an arena of one screen around the boss. This is in update, not in
					    -- start, because the level editor also starts the objects.
					    local view = self:view_bounds()
					    local half_screen = (view.right - view.left) / 2
					    self.old_bounds = self:camera_bounds()
					    self:set_camera_bounds(self.home_x - half_screen, view.top, self.home_x + half_screen, view.bottom, 16)
					    self:fade_out_music(MUSIC_DELAY)
					  end
					  if self.ticks == MUSIC_DELAY then
					    self:play_music("music")
					  end
					  if not self.defeated then
					    -- Float over the nearest character, and up and down a little
					    local target = nearest_character(self)
					    if target ~= nil then
					      local dx = target.x - self.x
					      self.x = self.x + math.max(-self.state.speed, math.min(self.state.speed, dx))
					    end
					    self.y = self.home_y + 24 * math.sin(self.ticks / 20)
					    return
					  end
					  self.after = self.after + 1
					  if self.after < EXPLODE_TICKS then
					    if self.after % 8 == 0 then
					      self:explode(math.random(-HALF_WIDTH, HALF_WIDTH), math.random(-HALF_HEIGHT, HALF_HEIGHT))
					    end
					    return
					  end
					  if self.after == EXPLODE_TICKS then
					    -- The camera can go on again, and the level music plays
					    local b = self.old_bounds
					    self:set_camera_bounds(b.left, b.top, b.right, b.bottom)
					    self:play_level_music()
					  end
					  -- Fly away up and to the right, then end the act
					  self.x = self.x + 12
					  self.y = self.y - 6
					  if self.after == EXPLODE_TICKS + FLEE_TICKS then
					    self:complete_level()
					  end
					end

					function hit(self, character)
					  -- The game bounced the character back and played the hit sound. Add more to a hit here.
					end

					function defeat(self)
					  self:explode(0, 0)
					end

					function draw(self)
					  -- After a hit, the boss flashes on every second tick. After the defeat, it is darker.
					  local options = {}
					  if self.flashing then
					    options.additive = { 200, 200, 200 }
					  elseif self.defeated then
					    options.color = { 120, 120, 120 }
					  end
					  self:draw(self.sprite, options)
					end

					""");
				break;
			case NewObjectKind.PowerUp:
				script.Append($$"""
					object {
					  api = 8,
					  name = {{quotedName}},
					  description = "Gives its ability to the character that touches it",
					  classification = "general",
					  dependencies = {
					    animations = "/ANIGROUP",
					    -- The ability is the file next to the folder of this object. Change it there.
					    ability = "SONICORCA/OBJECTS/{{abilityName}}",
					    sound = "SONICORCA/SOUND/RING",
					  },
					}

					local HALF_WIDTH, HALF_HEIGHT = {{halfWidth}}, {{halfHeight}}

					function start(self)
					  self:set_design_bounds(-HALF_WIDTH, -HALF_HEIGHT, HALF_WIDTH * 2, HALF_HEIGHT * 2)
					  self.sprite = self:animation("animations", 0)
					  self:add_hitbox(1, -HALF_WIDTH, -HALF_HEIGHT, HALF_WIDTH * 2, HALF_HEIGHT * 2)
					  self.ticks = 0
					end

					function update(self)
					  self.ticks = self.ticks + 1
					end

					function collision(self, event)
					  local character = event.character
					  if character == nil or character.is_debug then
					    return
					  end
					  -- The character keeps the ability until it gets hurt
					  character:give_ability("ability")
					  self:play_sound("sound")
					  self:finish_forever()
					end

					function draw(self)
					  -- The power-up floats up and down a little
					  self:draw(self.sprite, { y = 8 * math.sin(self.ticks / 15) })
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

	/// <summary>
	/// The ability of a power-up: a higher jump. The tick function makes the jump force of the character larger on
	/// each tick, so the jump goes higher. The character loses the ability when it gets hurt.
	/// </summary>
	public static string MakeAbilityScript(string name)
	{
		string quotedName = QuoteLua(name + " ability");
		StringBuilder script = new StringBuilder();
		script.Append($"-- The ability of {name.Replace('\n', ' ')}. The level editor made this script. Change it and save the file: the game\n");
		script.Append("-- loads it again. The README of examples/mods/lua-item-block describes the functions of an ability.\n");
		script.Append($$"""
			object {
			  api = 8,
			  name = {{quotedName}},
			  description = "A higher jump",
			  classification = "general",
			  -- An item gives the object to a character. It is not for a place in a level.
			  base = "ability",
			}

			-- The jump is this much stronger
			local POWER = 1.25

			-- Each tick, after the character set its speed values. The values are different in the original
			-- physics and the HD physics, so change them with a factor.
			function tick(self, character)
			  character.jump_force = character.jump_force * POWER
			end

			-- More functions of an ability: given, air_jump, jump, land and lost. For example, air_jump makes a
			-- double jump: see LUADOUBLEJUMP in examples/mods/lua-item-block.

			""");
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
			NewObjectKind.MovingPlatform => (128, 32, 0x9A6A3Au),
			NewObjectKind.Boss => (128, 128, 0x8040C0u),
			NewObjectKind.PowerUp => (64, 64, 0x40C0E0u),
			_ => (128, 128, 0x40A060u)
		};
		const int Edge = 6;
		bool round = kind == NewObjectKind.Collectible || kind == NewObjectKind.Boss || kind == NewObjectKind.PowerUp;
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
