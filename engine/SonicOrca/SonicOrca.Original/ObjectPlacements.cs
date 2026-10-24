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
	/// <param name="priorityLayer">
	/// True when the map has a layer for the high-priority tiles after the layer of the objects. A layer switch then
	/// also moves a character to that layer (in front of the tiles) or back, as the original sets the sprite priority.
	/// </param>
	/// <param name="zone">The zone (for example "ARZ"), for objects that the original draws differently in each zone.</param>
	public static ObjectPlacement ConvertOriginalObjectPlacement(OriginalObjectPlacement originalPlacement, bool priorityLayer = false, string zone = null)
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
					int sideA = (originalPlacement.SubType & 0x10) != 0 ? 1 : 0;
					int sideB = (originalPlacement.SubType & 8) != 0 ? 1 : 0;
					if (priorityLayer)
					{
						// Bit 6 gives the sprite priority on the left (or upper) side, bit 5 on the right (or lower)
						// side. A flipped switch changes only the priority.
						int layerA = (originalPlacement.SubType & 0x40) != 0 ? 1 : 0;
						int layerB = (originalPlacement.SubType & 0x20) != 0 ? 1 : 0;
						behaviour = (originalPlacement.SubType & 4) == 0
							? new
							{
								AllowAirborne = (originalPlacement.SubType & 0x80) == 0,
								Height = layerSwitchSizes[originalPlacement.SubType & 3],
								Left = sideA,
								Right = sideB,
								LeftLayer = layerA,
								RightLayer = layerB,
								LayerOnly = originalPlacement.FlipX
							}
							: new
							{
								AllowAirborne = (originalPlacement.SubType & 0x80) == 0,
								Width = layerSwitchSizes[originalPlacement.SubType & 3],
								Above = sideA,
								Below = sideB,
								AboveLayer = layerA,
								BelowLayer = layerB,
								LayerOnly = originalPlacement.FlipX
							};
						break;
					}
					behaviour = (((originalPlacement.SubType & 4) == 0) ? (new
					{
						AllowAirborne = ((originalPlacement.SubType & 0x80) == 0),
						Height = layerSwitchSizes[originalPlacement.SubType & 3],
						Left = sideA,
						Right = sideB
					}) : (new
					{
						AllowAirborne = ((originalPlacement.SubType & 0x80) == 0),
						Width = layerSwitchSizes[originalPlacement.SubType & 3],
						Above = sideA,
						Below = sideB
					}));
					break;
				}
			case 6 when zone == "MTZ" && (originalPlacement.SubType & 0x80) != 0:
				key = "SONICORCA/OBJECTS/MTZCYLINDER";
				break;
			case 28 when zone == "MTZ":
				key = "SONICORCA/OBJECTS/MTZSCENERY";
				behaviour = new { Subtype = (int)originalPlacement.SubType, FlipX = originalPlacement.FlipX, FlipY = originalPlacement.FlipY };
				break;
			case 45 when zone == "MTZ":
				key = "SONICORCA/OBJECTS/MTZDOOR";
				behaviour = new { FlipX = originalPlacement.FlipX };
				break;
			case 49 when zone == "MTZ":
				key = "SONICORCA/OBJECTS/MTZLAVA";
				behaviour = new { Subtype = (int)originalPlacement.SubType };
				break;
			case 66:
				key = "SONICORCA/OBJECTS/MTZSTEAMSPRING";
				behaviour = new { Subtype = (int)originalPlacement.SubType };
				break;
			case 71:
				// Subtype bits 0 to 3 give the trigger. Bit 6 sets bit 7 of the trigger in place of bit 0.
				key = "SONICORCA/OBJECTS/MTZBUTTON";
				behaviour = new { Trigger = originalPlacement.SubType & 0xF, HighBit = (originalPlacement.SubType & 0x40) != 0 };
				break;
			case 108:
				key = "SONICORCA/OBJECTS/MTZPULLEYPLATFORM";
				behaviour = new { Subtype = (int)originalPlacement.SubType, FlipX = originalPlacement.FlipX };
				break;
			case 109:
				key = "SONICORCA/OBJECTS/MTZFLOORSPIKE";
				behaviour = new { Phase = (int)originalPlacement.SubType };
				break;
			case 110:
				key = "SONICORCA/OBJECTS/MTZMACHINE";
				behaviour = new { Subtype = (int)originalPlacement.SubType };
				break;
			case 112:
				key = "SONICORCA/OBJECTS/MTZCOG";
				behaviour = new { FlipX = originalPlacement.FlipX };
				break;
			case 113 when zone == "MTZ":
				key = "SONICORCA/OBJECTS/MTZLAVABUBBLE";
				break;
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
			case 24 when zone == "ARZ":
				// Aquatic Ruin draws this platform from its level art. The object reads the subtype itself.
				key = "SONICORCA/OBJECTS/ARZPLATFORM";
				behaviour = new
				{
					Subtype = (int)originalPlacement.SubType
				};
				break;
			case 21 when zone == "MCZ":
				key = "SONICORCA/OBJECTS/MCZSWINGPLATFORM";
				behaviour = new
				{
					Links = originalPlacement.SubType & 0xF,
					Kind = ((originalPlacement.SubType >> 4) & 7) switch { 1 => "Left", 3 => "Right", 4 => "Trap", _ => "Full" },
					FlipX = originalPlacement.FlipX
				};
				break;
			case 31 when zone == "MCZ":
				key = "SONICORCA/OBJECTS/MCZCOLLAPSEPLATFORM";
				behaviour = new { FlipX = originalPlacement.FlipX };
				break;
			case 42:
				key = "SONICORCA/OBJECTS/MCZSTOMPER";
				break;
			case 118:
				key = "SONICORCA/OBJECTS/MCZSLIDINGSPIKES";
				behaviour = new { FlipX = originalPlacement.FlipX };
				break;
			case 122 when zone == "MCZ":
				key = "SONICORCA/OBJECTS/MCZSIDESTEPPLATFORM";
				behaviour = new { StartRight = originalPlacement.SubType == 0x12 };
				break;
			case 158:
				key = "SONICORCA/OBJECTS/CRAWLTON";
				break;
			case 159:
				key = "SONICORCA/OBJECTS/SHELLCRACKER";
				behaviour = new { FlipX = originalPlacement.FlipX };
				break;
			case 161:
				key = "SONICORCA/OBJECTS/SLICER";
				behaviour = new { FlipX = originalPlacement.FlipX };
				break;
			case 164:
				key = "SONICORCA/OBJECTS/ASTERON";
				break;
			case 163:
				key = "SONICORCA/OBJECTS/FLASHER";
				break;
			case 21 when zone == "OOZ":
				// The fallen platform stops at y $720 of the act. FloatDepth is that line measured from the pivot.
				key = "SONICORCA/OBJECTS/OOZSWINGPLATFORM";
				behaviour = new
				{
					Links = originalPlacement.SubType & 0xF,
					FallOff = (originalPlacement.SubType & 0x80) != 0,
					FlipX = originalPlacement.FlipX,
					FloatDepth = (0x720 - originalPlacement.Position.Y) * 4
				};
				break;
			case 45 when zone == "DEZ":
				key = "SONICORCA/OBJECTS/DEZDOOR";
				behaviour = new { FlipX = originalPlacement.FlipX, Frame = (int)originalPlacement.SubType };
				break;
			case 198 when zone == "DEZ" && originalPlacement.SubType == 0xA6:
				key = "SONICORCA/OBJECTS/DEZEGGMAN";
				break;
			case 199 when zone == "DEZ":
				// Death Egg Zone starts at the origin of the map, so the origin of the robot is 0
				key = "SONICORCA/OBJECTS/DEATHEGGROBOT";
				behaviour = new { OriginX = 0, OriginY = 0 };
				break;
			case 25 when zone == "WFZ":
				key = "SONICORCA/OBJECTS/WFZPLATFORM";
				behaviour = new { Subtype = (int)originalPlacement.SubType, FlipX = originalPlacement.FlipX };
				break;
			case 45 when zone == "WFZ":
				key = "SONICORCA/OBJECTS/WFZBARRIER";
				behaviour = new { Frame = (int)originalPlacement.SubType, FlipX = originalPlacement.FlipX };
				break;
			case 128 when zone == "WFZ":
				key = "SONICORCA/OBJECTS/WFZHOOK";
				behaviour = new { ShortTravel = (originalPlacement.SubType & 0xF) != 0, StartDown = (originalPlacement.SubType & 0x70) != 0 };
				break;
			case 192:
				key = "SONICORCA/OBJECTS/WFZLAUNCHER";
				behaviour = new { Distance = (int)originalPlacement.SubType, FlipX = originalPlacement.FlipX };
				break;
			case 193:
				key = "SONICORCA/OBJECTS/WFZPLATING";
				behaviour = new { Seconds = (int)originalPlacement.SubType };
				break;
			case 194:
				key = "SONICORCA/OBJECTS/WFZRIVET";
				break;
			case 217:
				key = "SONICORCA/OBJECTS/WFZGRAB";
				break;
			case 25 when zone == "OOZ":
				key = "SONICORCA/OBJECTS/OOZPLATFORM";
				behaviour = new { Subtype = (int)originalPlacement.SubType, FlipX = originalPlacement.FlipX };
				break;
			case 28 when zone == "OOZ" && originalPlacement.SubType >= 0x0A && originalPlacement.SubType <= 0x14:
				// Falling oil: $A to $F are narrow streams, $10 to $14 wide streams
				key = "SONICORCA/OBJECTS/OOZSCENERY";
				behaviour = new { Subtype = (int)originalPlacement.SubType, FlipX = originalPlacement.FlipX };
				break;
			case 31 when zone == "OOZ":
				key = "SONICORCA/OBJECTS/OOZCOLLAPSEPLATFORM";
				behaviour = new { FlipX = originalPlacement.FlipX };
				break;
			case 51:
				key = "SONICORCA/OBJECTS/OOZBURNER";
				behaviour = new { WaitForPlayer = originalPlacement.SubType != 0 };
				break;
			case 61:
				key = "SONICORCA/OBJECTS/OOZLAUNCHERBLOCK";
				behaviour = new { Horizontal = originalPlacement.SubType != 0 };
				break;
			case 63:
				key = "SONICORCA/OBJECTS/OOZFAN";
				behaviour = new
				{
					Vertical = (originalPlacement.SubType & 0x80) != 0,
					Reverse = (originalPlacement.SubType & 1) != 0,
					AlwaysOn = (originalPlacement.SubType & 2) != 0,
					FlipX = originalPlacement.FlipX
				};
				break;
			case 72:
				key = "SONICORCA/OBJECTS/OOZLAUNCHBALL";
				behaviour = new { Subtype = (int)originalPlacement.SubType, FlipX = originalPlacement.FlipX };
				break;
			case 74:
				key = "SONICORCA/OBJECTS/OCTUS";
				behaviour = new { FlipX = originalPlacement.FlipX };
				break;
			case 80:
				key = "SONICORCA/OBJECTS/AQUIS";
				break;
			case 173:
				// The base of a Clucker: a solid block
				key = "SONICORCA/OBJECTS/CLUCKER/BASE";
				behaviour = new { FlipX = originalPlacement.FlipX };
				break;
			case 174:
				key = "SONICORCA/OBJECTS/CLUCKER";
				behaviour = new { FlipX = originalPlacement.FlipX };
				break;
			case 182:
				// Bits 1 and 2 give the kind of tilting platform, bits 4 to 7 the time
				key = "SONICORCA/OBJECTS/WFZTILTPLATFORM";
				behaviour = new
				{
					Kind = new[] { "Timed", "Spin", "Stand", "Vertical" }[(originalPlacement.SubType >> 1) & 3],
					Time = originalPlacement.SubType & 0xF0
				};
				break;
			case 184:
				key = "SONICORCA/OBJECTS/WFZTURRET";
				break;
			case 185:
				key = "SONICORCA/OBJECTS/WFZLASER";
				break;
			case 186:
				key = "SONICORCA/OBJECTS/WFZWHEEL";
				break;
			case 188:
				key = "SONICORCA/OBJECTS/WFZTHRUST";
				break;
			case 189:
				// Subtype $80 moves the platforms down, $7E up. A flipped object moves them for longer.
				key = "SONICORCA/OBJECTS/WFZBELTPLATFORM";
				behaviour = new
				{
					Down = originalPlacement.SubType == 0x80,
					Long = originalPlacement.FlipX
				};
				break;
			case 190:
				key = "SONICORCA/OBJECTS/WFZGUNPLATFORM";
				behaviour = new { Time = originalPlacement.SubType & 0xF0 };
				break;
			case 153:
				key = "SONICORCA/OBJECTS/NEBULA";
				break;
			case 154:
				key = "SONICORCA/OBJECTS/TURTLOID";
				break;
			case 172:
				// A Balkiry flipped vertically flies at $500 in place of $300
				key = "SONICORCA/OBJECTS/BALKIRY";
				behaviour = new { Fast = originalPlacement.FlipY };
				break;
			case 178:
				// $50: Sky Chase Zone, $52: start of Wing Fortress Zone, $54: end of Wing Fortress Zone
				key = "SONICORCA/OBJECTS/TORNADO";
				behaviour = new { Subtype = (int)originalPlacement.SubType };
				break;
			case 179:
				// Subtypes $5E, $60 and $62: big, middle and small
				key = "SONICORCA/OBJECTS/SCZCLOUD";
				behaviour = new { Kind = (originalPlacement.SubType - 0x5E) / 2 };
				break;
			case 180:
				// A vertical propeller flipped vertically does not hurt
				key = "SONICORCA/OBJECTS/PROPELLER";
				behaviour = new { Horizontal = false, Harmless = originalPlacement.FlipY };
				break;
			case 181:
				// Subtype $66 (Wing Fortress Zone) lifts the players, $68 (Sky Chase Zone) only turns
				key = "SONICORCA/OBJECTS/PROPELLER";
				behaviour = new { Horizontal = true, Lift = originalPlacement.SubType == 0x66 };
				break;
			case 67:
				key = "SONICORCA/OBJECTS/OOZSPIKEBALL";
				behaviour = new { Subtype = (int)originalPlacement.SubType };
				break;
			case 69:
				key = "SONICORCA/OBJECTS/OOZSPRING";
				behaviour = new { Subtype = (int)originalPlacement.SubType, FlipX = originalPlacement.FlipX };
				break;
			case 100 when zone == "MTZ":
				key = "SONICORCA/OBJECTS/MTZSTOMPER";
				behaviour = new { Subtype = (int)originalPlacement.SubType, FlipX = originalPlacement.FlipX, FlipY = originalPlacement.FlipY };
				break;
			case 101 when zone == "MTZ":
				key = "SONICORCA/OBJECTS/MTZLONGPLATFORM";
				behaviour = new { Subtype = (int)originalPlacement.SubType, FlipX = originalPlacement.FlipX, OriginalX = originalPlacement.Position.X };
				break;
			case 102 when zone == "MTZ":
				key = "SONICORCA/OBJECTS/MTZSPRINGWALL";
				behaviour = new { Subtype = (int)originalPlacement.SubType, FlipX = originalPlacement.FlipX };
				break;
			case 103 when zone == "MTZ":
				key = "SONICORCA/OBJECTS/MTZSPINTUBE";
				behaviour = new
				{
					Subtype = (int)originalPlacement.SubType,
					FlipX = originalPlacement.FlipX,
					OriginalX = originalPlacement.Position.X,
					OriginalY = originalPlacement.Position.Y
				};
				break;
			case 104 when zone == "MTZ":
				key = "SONICORCA/OBJECTS/MTZSPIKEBLOCK";
				behaviour = new { Subtype = (int)originalPlacement.SubType };
				break;
			case 105 when zone == "MTZ":
				key = "SONICORCA/OBJECTS/MTZNUT";
				behaviour = new { Subtype = (int)originalPlacement.SubType };
				break;
			case 106 when zone == "MTZ":
				key = "SONICORCA/OBJECTS/MTZPLATFORM";
				behaviour = new { Subtype = (int)originalPlacement.SubType, FlipX = originalPlacement.FlipX };
				break;
			case 107 when zone == "MTZ":
				key = "SONICORCA/OBJECTS/MTZBLOCK";
				behaviour = new { Subtype = (int)originalPlacement.SubType, FlipX = originalPlacement.FlipX };
				break;
			case 106 when zone == "MCZ":
				key = "SONICORCA/OBJECTS/MCZPLATFORM";
				behaviour = new { Subtype = (int)originalPlacement.SubType, FlipX = originalPlacement.FlipX };
				break;
			case 117:
				key = "SONICORCA/OBJECTS/MCZSPIKEBALLCHAIN";
				behaviour = new
				{
					Links = originalPlacement.SubType & 0xF,
					Speed = (sbyte)(originalPlacement.SubType & 0xF0) << 3,
					StartAngle = (originalPlacement.FlipX ? 0x40 : 0) | (originalPlacement.FlipY ? 0x80 : 0)
				};
				break;
			case 119:
				key = "SONICORCA/OBJECTS/MCZBRIDGE";
				behaviour = new { Trigger = originalPlacement.SubType & 0xF };
				break;
			case 127:
				key = "SONICORCA/OBJECTS/MCZVINESWITCH";
				behaviour = new { Trigger = originalPlacement.SubType & 0xF };
				break;
			case 128 when zone == "MCZ":
				key = "SONICORCA/OBJECTS/MCZVINE";
				behaviour = new
				{
					IsSwitch = (originalPlacement.SubType & 0x80) != 0,
					StartDown = (originalPlacement.SubType & 0x70) != 0,
					Trigger = originalPlacement.SubType & 0xF
				};
				break;
			case 129:
				key = "SONICORCA/OBJECTS/MCZDRAWBRIDGE";
				behaviour = new
				{
					Trigger = originalPlacement.SubType & 0xF,
					FlipX = originalPlacement.FlipX,
					FlipY = originalPlacement.FlipY
				};
				break;
			case 21 when zone == "ARZ":
				key = "SONICORCA/OBJECTS/ARZSWINGPLATFORM";
				behaviour = new
				{
					Links = originalPlacement.SubType & 0xF,
					FallOff = (originalPlacement.SubType & 0x80) != 0,
					FlipX = originalPlacement.FlipX
				};
				break;
			case 31 when zone == "ARZ":
				key = "SONICORCA/OBJECTS/ARZCOLLAPSEPLATFORM";
				break;
			case 34:
				key = "SONICORCA/OBJECTS/ARZARROWSHOOTER";
				behaviour = new
				{
					FlipX = originalPlacement.FlipX
				};
				break;
			case 35:
				key = "SONICORCA/OBJECTS/ARZFALLINGPILLAR";
				break;
			case 36 when (originalPlacement.SubType & 0x80) != 0:
				// Only the bubble generators (bit 7) are in the levels
				key = "SONICORCA/OBJECTS/ARZBUBBLES";
				behaviour = new
				{
					Cycle = originalPlacement.SubType & 0x7F
				};
				break;
			case 43:
				key = "SONICORCA/OBJECTS/ARZRISINGPILLAR";
				break;
			case 44:
				key = "SONICORCA/OBJECTS/ARZLEAVES";
				behaviour = new
				{
					Size = (int)originalPlacement.SubType
				};
				break;
			case 130:
				key = "SONICORCA/OBJECTS/ARZPILLAR";
				behaviour = new
				{
					Subtype = (int)originalPlacement.SubType
				};
				break;
			case 131:
				// The first of the three rotating platforms. The importer adds the other two (arms 1 and 2).
				key = "SONICORCA/OBJECTS/ARZROTATINGPLATFORM";
				behaviour = new
				{
					Arm = 0,
					Speed = (sbyte)(originalPlacement.SubType & 0xF0) << 3,
					StartAngle = (originalPlacement.FlipX ? 0x40 : 0) | (originalPlacement.FlipY ? 0x80 : 0)
				};
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
				{
					key = "SONICORCA/OBJECTS/SPIKES";
					// Bits 4-7 give the number of spike groups (1 to 4) and the orientation (4 and up is sideways).
					// A y-flip makes ceiling spikes and an x-flip makes sideways spikes face left. The directions
					// are the SpikesDirection values: 0 up, 1 right, 2 down, 3 left. Bits 0-3 give the movement:
					// 1 is vertical and 2 is horizontal.
					int size = originalPlacement.SubType >> 4;
					bool sideways = size >= 4;
					int direction = sideways ? (originalPlacement.FlipX ? 3 : 1) : (originalPlacement.FlipY ? 2 : 0);
					behaviour = new
					{
						Direction = direction,
						Count = (size & 3) + 1,
						MovementAxis = originalPlacement.SubType & 0xF
					};
					break;
				}
			case 62:
				key = "SONICORCA/OBJECTS/CAPSULE";
				break;
			case 64:
				key = "SONICORCA/OBJECTS/SPRINGBOARD";
				if (originalPlacement.FlipX)
				{
					// The name is the state property of SpringBoardInstance
					behaviour = new
					{
						FlipX = true
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
					// Bit 0 makes the character do somersaults: up and down springs give 2 (1 for a yellow spring),
					// the other springs 4 (2 for a yellow spring). Bits 2-3 set the collision path: 4 sets the
					// primary path (0) and 8 the secondary path (1). Bit 7 clears the velocity of the other axis.
					bool yellow = (originalPlacement.SubType & 2) != 0;
					bool vertical = direction == 0 || direction == 4;
					int twirlTurns = 0;
					if ((originalPlacement.SubType & 1) != 0)
						twirlTurns = vertical ? (yellow ? 1 : 2) : (yellow ? 2 : 4);
					int path = (originalPlacement.SubType & 0xC) switch
					{
						4 => 0,
						8 => 1,
						_ => -1
					};
					behaviour = new
					{
						Strength = yellow ? 40 : 64,
						Direction = direction,
						TwirlTurns = twirlTurns,
						Path = path,
						ClearOtherVelocity = (originalPlacement.SubType & 0x80) != 0
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
			case 114:
				// Conveyor belt: the half width is (subtype & $7F) * 16 in a byte, so it wraps like the original
				key = "SONICORCA/OBJECTS/CNZCONVEYOR";
				behaviour = new
				{
					HalfWidth = (((originalPlacement.SubType & 0x7F) << 4) & 0xFF) * 4,
					Height = ((originalPlacement.SubType & 0x80) != 0 ? 0x70 : 0x30) * 4,
					Left = originalPlacement.FlipX
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
			case 200:
				key = "SONICORCA/OBJECTS/CRAWL";
				behaviour = new
				{
					FacingRight = originalPlacement.FlipX
				};
				break;
			case 210:
				key = "SONICORCA/OBJECTS/CNZFLASHINGBLOCK";
				behaviour = new
				{
					Delay = (int)originalPlacement.SubType,
					FlipX = originalPlacement.FlipX
				};
				break;
			case 212:
				key = "SONICORCA/OBJECTS/CNZBIGBLOCK";
				behaviour = new
				{
					Vertical = originalPlacement.SubType != 0,
					StartOnOtherSide = originalPlacement.SubType != 0 ? originalPlacement.FlipY : originalPlacement.FlipX
				};
				break;
			case 213:
				key = "SONICORCA/OBJECTS/CNZELEVATOR";
				behaviour = new
				{
					Distance = originalPlacement.SubType * 4,
					StartAtBottom = originalPlacement.FlipX
				};
				break;
			case 214:
				key = "SONICORCA/OBJECTS/CNZSLOTCAGE";
				behaviour = new
				{
					SlotMachine = originalPlacement.SubType != 0
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
			case 140:
				key = "SONICORCA/OBJECTS/WHISP";
				break;
			case 141:
			case 142:
				// $8D waits in a wall, $8E walks from the start. Both run the same code in the original.
				key = "SONICORCA/OBJECTS/GROUNDER";
				behaviour = new
				{
					InWall = originalPlacement.Type == 141
				};
				break;
			case 145:
				key = "SONICORCA/OBJECTS/CHOPCHOP";
				behaviour = new
				{
					StartRight = originalPlacement.FlipX
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
