using System.Collections.Generic;
using System.IO;
using System.Linq;
using SonicOrca.Core;
using SonicOrca.Geometry;

namespace SonicOrca.Original;

/// <summary>
/// Reads the object placements of a level from the data format of the original game and converts them to
/// SonicOrca object placements.
/// </summary>
public static class ObjectPlacements
{
	/// <summary>
	/// One 6-byte object entry of the original game: the position, the flags, the object type and the subtype.
	/// </summary>
	public class OriginalObjectPlacement
	{
		private readonly Vector2i _position;

		private readonly bool _respawn;

		private readonly bool _flipX;

		private readonly bool _flipY;

		private readonly byte _type;

		private readonly byte _subtype;

		public Vector2i Position => _position;

		public bool Respawn => _respawn;

		public bool FlipX => _flipX;

		public bool FlipY => _flipY;

		public byte Type => _type;

		public byte SubType => _subtype;

		public OriginalObjectPlacement(byte[] data)
		{
			_position = new Vector2i((data[0] << 8) | data[1], ((data[2] & 0xF) << 8) | data[3]);
			_respawn = (data[2] & 0x80) == 0;
			_flipX = (data[2] & 0x20) != 0;
			_flipY = (data[2] & 0x40) != 0;
			_type = data[4];
			_subtype = data[5];
		}
	}

	public static IReadOnlyCollection<OriginalObjectPlacement> FromFile(string filename)
	{
		using FileStream stream = new FileStream(filename, FileMode.Open, FileAccess.Read);
		return FromStream(stream);
	}

	/// <summary>
	/// Reads 6-byte entries until the end of the stream or an 0xFFFF marker.
	/// </summary>
	public static IReadOnlyCollection<OriginalObjectPlacement> FromStream(Stream stream)
	{
		List<OriginalObjectPlacement> placements = new List<OriginalObjectPlacement>();
		byte[] entry = new byte[6];
		while (stream.Read(entry, 0, 2) == 2 && (entry[0] != byte.MaxValue || entry[1] != byte.MaxValue) && stream.Read(entry, 2, 4) == 4)
		{
			placements.Add(new OriginalObjectPlacement(entry));
		}
		return placements.ToArray();
	}

	public static IEnumerable<ObjectPlacement> ConvertOriginalObjectPlacement(IEnumerable<OriginalObjectPlacement> originalPlacements)
	{
		return from original in originalPlacements
			   select ConvertOriginalObjectPlacement(original) into placement
			   where placement != null
			   select placement;
	}

	/// <summary>
	/// Converts one object of the original game. The original type selects the SonicOrca object and the subtype
	/// selects its behaviour. Returns null for an object type that has no SonicOrca object.
	/// </summary>
	public static ObjectPlacement ConvertOriginalObjectPlacement(OriginalObjectPlacement originalPlacement)
	{
		string key = null;
		object behaviour = null;
		// SonicOrca levels use four times the resolution of the original game.
		Vector2i position = originalPlacement.Position * 4;
		switch (originalPlacement.Type)
		{
			case 3:
				{
					key = "SONICORCA/OBJECTS/LAYERSWITCH";
					int[] layerSwitchSizes = new int[4] { 256, 512, 1024, 2048 };
					behaviour = (((originalPlacement.SubType & 4) == 0) ? (new
					{
						AllowAirborne = ((originalPlacement.SubType & 0x80) == 0),
						Height = layerSwitchSizes[originalPlacement.SubType & 3],
						Left = (((originalPlacement.SubType & 0x10) != 0) ? 1 : 0),
						Right = (((originalPlacement.SubType & 8) != 0) ? 1 : 0)
					}) : (new
					{
						AllowAirborne = ((originalPlacement.SubType & 0x80) == 0),
						Width = layerSwitchSizes[originalPlacement.SubType & 3],
						Above = (((originalPlacement.SubType & 0x10) != 0) ? 1 : 0),
						Below = (((originalPlacement.SubType & 8) != 0) ? 1 : 0)
					}));
					break;
				}
			case 11:
				key = "SONICORCA/OBJECTS/CPZTRAPDOOR";
				behaviour = new
				{
					TimeOffset = (originalPlacement.SubType & 0xF) * 16
				};
				break;
			case 13:
				key = "SONICORCA/OBJECTS/SIGNPOST";
				break;
			case 20:
				key = "SONICORCA/OBJECTS/HTZSEESAW";
				break;
			case 22:
				key = "SONICORCA/OBJECTS/HTZVINELIFT";
				behaviour = new
				{
					Duration = originalPlacement.SubType * 8,
					Direction = (originalPlacement.FlipX ? "left" : "right")
				};
				position.Y += 152;
				break;
			case 24:
				key = "SONICORCA/OBJECTS/EHZPLATFORM";
				if (((originalPlacement.SubType >> 4) & 1) != 0)
				{
					key = "SONICORCA/OBJECTS/EHZBLOCK";
				}
				switch (originalPlacement.SubType & 3)
				{
					case 1:
						behaviour = new
						{
							RadiusX = 256,
							TimePeriod = 256
						};
						break;
					case 2:
						behaviour = new
						{
							RadiusY = 256,
							TimePeriod = 256
						};
						break;
					case 3:
						behaviour = new
						{
							Falling = true
						};
						break;
				}
				break;
			case 25:
				{
					key = "SONICORCA/OBJECTS/CPZPLATFORM";
					bool isLarge = ((originalPlacement.SubType >> 4) & 1) == 0;
					switch (originalPlacement.SubType & 7)
					{
						case 0:
							behaviour = new
							{
								Size = (isLarge ? "large" : "small"),
								RadiusX = 256,
								TimePeriod = 256
							};
							break;
						case 1:
							position += new Vector2i(-192, 0);
							behaviour = new
							{
								Size = (isLarge ? "large" : "small"),
								RadiusX = 192,
								TimePeriod = 440,
								TimeOffset = 110
							};
							break;
						case 2:
							position += new Vector2i(0, -256);
							behaviour = new
							{
								Size = (isLarge ? "large" : "small"),
								RadiusY = 256,
								TimePeriod = 360,
								TimeOffset = 90
							};
							break;
						case 6:
							position += new Vector2i(0, -384);
							behaviour = new
							{
								Size = (isLarge ? "large" : "small"),
								RadiusY = 384,
								TimePeriod = 312,
								TimeOffset = 78
							};
							break;
						case 7:
							position += new Vector2i(0, -392);
							behaviour = new
							{
								Size = (isLarge ? "large" : "small"),
								RadiusY = 392,
								TimePeriod = 316,
								TimeOffset = 79
							};
							break;
					}
					break;
				}
			case 27:
				key = "SONICORCA/OBJECTS/CPZSPEEDBOOSTER";
				behaviour = new
				{
					Strength = (new int[2] { 64, 40 })[(originalPlacement.SubType & 2) >> 1] * ((!originalPlacement.FlipX) ? 1 : (-1))
				};
				break;
			case 28:
				switch (originalPlacement.SubType)
				{
					case 2:
						key = "SONICORCA/OBJECTS/EHZBRIDGE/STAKE";
						break;
					case 4:
						key = "SONICORCA/OBJECTS/HTZVINELIFT/PILLAR";
						break;
					case 5:
						key = "SONICORCA/OBJECTS/HTZVINELIFT/PILLAR";
						behaviour = new
						{
							Right = true
						};
						break;
				}
				break;
			case 29:
				key = "SONICORCA/OBJECTS/CPZBLOBS";
				behaviour = new
				{
					Count = (originalPlacement.SubType & 0xF),
					Type = (((originalPlacement.SubType & 0xF0) != 0) ? "straight" : "arc")
				};
				break;
			case 30:
				key = "SONICORCA/OBJECTS/CPZTUBEENTRANCE";
				behaviour = new
				{
					Subtype = originalPlacement.SubType
				};
				break;
			case 38:
				key = "SONICORCA/OBJECTS/MONITOR";
				behaviour = new
				{
					Contents = (new string[10] { "none", "life", "life", "robotnik", "ring", "speedshoes", "barrier", "invincibility", "swapplaces", "random" })[originalPlacement.SubType]
				};
				break;
			case 45:
				key = "SONICORCA/OBJECTS/HTZDOOR";
				break;
			case 50:
				key = "SONICORCA/OBJECTS/HTZROCK";
				break;
			case 54:
				key = "SONICORCA/OBJECTS/SPIKES";
				break;
			case 62:
				key = "SONICORCA/OBJECTS/CAPSULE";
				break;
			case 64:
				key = "SONICORCA/OBJECTS/SPRINGBOARD";
				if (originalPlacement.FlipX)
				{
					behaviour = new
					{
						flipX = true
					};
				}
				break;
			case 65:
				{
					key = "SONICORCA/OBJECTS/SPRING";
					int direction = 0;
					switch ((originalPlacement.SubType >> 4) & 7)
					{
						case 1:
							direction = (originalPlacement.FlipX ? 6 : 2);
							break;
						case 2:
							direction = 4;
							break;
						case 3:
							direction = ((!originalPlacement.FlipX) ? 1 : 7);
							break;
						case 4:
							direction = (originalPlacement.FlipX ? 5 : 3);
							break;
					}
					behaviour = new
					{
						Strength = (((originalPlacement.SubType & 2) != 0) ? 40 : 64),
						Direction = direction
					};
					break;
				}
			case 107:
				key = "SONICORCA/OBJECTS/CPZBLOCK";
				behaviour = new
				{
					Type = originalPlacement.SubType
				};
				break;
			case 120:
				key = "SONICORCA/OBJECTS/CPZBLOCK";
				behaviour = new
				{
					Type = "stairs " + originalPlacement.SubType
				};
				break;
			case 121:
				key = "SONICORCA/OBJECTS/STARPOST";
				behaviour = new
				{
					Index = originalPlacement.SubType
				};
				position.Y -= 32;
				break;
			case 123:
				key = "SONICORCA/OBJECTS/CPZTUBESPRING";
				behaviour = new
				{
					Type = originalPlacement.SubType
				};
				break;
			case 68:
				key = "SONICORCA/OBJECTS/CNZBUMPER";
				break;
			case 116:
				// Half width ((subtype & $F0) + $10) / 2, half height ((subtype & $F) + 1) * 8
				key = "SONICORCA/OBJECTS/INVISIBLEBLOCK";
				behaviour = new
				{
					Width = ((originalPlacement.SubType & 0xF0) + 0x10) * 4,
					Height = ((originalPlacement.SubType & 0xF) + 1) * 16 * 4
				};
				break;
			case 133:
				// Spring launcher: subtype 0 is vertical, others diagonal. Bit 7 launches along the ground.
				key = "SONICORCA/OBJECTS/CNZLAUNCHER";
				behaviour = new
				{
					Diagonal = originalPlacement.SubType != 0,
					GroundLaunch = (originalPlacement.SubType & 0x80) != 0
				};
				break;
			case 134:
				// Flipper: subtype 0 sends upward, others are side flippers
				key = "SONICORCA/OBJECTS/CNZFLIPPER";
				behaviour = new
				{
					Sideways = originalPlacement.SubType != 0,
					FlipX = originalPlacement.FlipX
				};
				break;
			case 215:
				key = "SONICORCA/OBJECTS/CNZHEXBUMPER";
				behaviour = new
				{
					Moving = originalPlacement.SubType != 0,
					StartLeft = originalPlacement.FlipX
				};
				break;
			case 216:
				// Bonus block: bits 6-7 of the subtype give the kind, bits 0-5 the group
				key = "SONICORCA/OBJECTS/CNZBONUSBLOCK";
				behaviour = new
				{
					Kind = new[] { "Vertical", "Diagonal", "Horizontal", "Horizontal" }[(originalPlacement.SubType >> 6) & 3],
					Group = originalPlacement.SubType & 0x3F,
					FlipX = originalPlacement.FlipX
				};
				break;
			case 132:
				// Pinball mode: bit 2 makes the line horizontal, bits 0-1 give the half length $20 to $100, and a
				// flipped marker releases a character that crosses to the right (or down) instead
				key = "SONICORCA/OBJECTS/FORCESPINBALL";
				// The names are the state fields of ForceSpinballInstance
				behaviour = new
				{
					_direction = originalPlacement.FlipX ? 3 : 1,
					_radius = (0x20 << (originalPlacement.SubType & 3)) * 4,
					_horizontal = (originalPlacement.SubType & 4) != 0
				};
				break;
			case 146:
				key = "SONICORCA/OBJECTS/SPIKER";
				if (originalPlacement.FlipY)
				{
					position.Y += 64;
				}
				else
				{
					position.Y -= 64;
				}
				behaviour = new
				{
					UpsideDown = originalPlacement.FlipY
				};
				break;
			case 148:
			case 150:
				key = "SONICORCA/OBJECTS/REXON";
				break;
			case 149:
				key = "SONICORCA/OBJECTS/SOL";
				break;
			case 165:
				key = "SONICORCA/OBJECTS/SPINY";
				break;
			case 166:
				key = "SONICORCA/OBJECTS/SPINY";
				behaviour = new
				{
					Wall = true
				};
				break;
			case 167:
				key = "SONICORCA/OBJECTS/GRABBER";
				break;
		}
		if (key == null)
		{
			return null;
		}
		if (behaviour == null)
		{
			return new ObjectPlacement(key, -1, position);
		}
		return new ObjectPlacement(key, -1, position, behaviour);
	}
}
