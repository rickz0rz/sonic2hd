using System.Collections.Generic;
using System.IO;
using System.Linq;
using SonicOrca.Core;
using SonicOrca.Geometry;

namespace SonicOrca.Original;

public static class ObjectPlacements
{
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

	public static IReadOnlyCollection<OriginalObjectPlacement> FromStream(Stream stream)
	{
		List<OriginalObjectPlacement> list = new List<OriginalObjectPlacement>();
		byte[] array = new byte[6];
		while (stream.Read(array, 0, 2) == 2 && (array[0] != byte.MaxValue || array[1] != byte.MaxValue) && stream.Read(array, 2, 4) == 4)
		{
			list.Add(new OriginalObjectPlacement(array));
		}
		return list.ToArray();
	}

	public static IEnumerable<ObjectPlacement> ConvertOriginalObjectPlacement(IEnumerable<OriginalObjectPlacement> originalPlacements)
	{
		return from x in originalPlacements
			   select ConvertOriginalObjectPlacement(x) into x
			   where x != null
			   select x;
	}

	public static ObjectPlacement ConvertOriginalObjectPlacement(OriginalObjectPlacement originalPlacement)
	{
		string text = null;
		object obj = null;
		Vector2i position = originalPlacement.Position * 4;
		switch (originalPlacement.Type)
		{
			case 3:
				{
					text = "SONICORCA/OBJECTS/LAYERSWITCH";
					int[] array = new int[4] { 256, 512, 1024, 2048 };
					obj = (((originalPlacement.SubType & 4) == 0) ? ((object)new
					{
						AllowAirborne = ((originalPlacement.SubType & 0x80) == 0),
						Height = array[originalPlacement.SubType & 3],
						Left = (((originalPlacement.SubType & 0x10) != 0) ? 1 : 0),
						Right = (((originalPlacement.SubType & 8) != 0) ? 1 : 0)
					}) : ((object)new
					{
						AllowAirborne = ((originalPlacement.SubType & 0x80) == 0),
						Width = array[originalPlacement.SubType & 3],
						Above = (((originalPlacement.SubType & 0x10) != 0) ? 1 : 0),
						Below = (((originalPlacement.SubType & 8) != 0) ? 1 : 0)
					}));
					break;
				}
			case 11:
				text = "SONICORCA/OBJECTS/CPZTRAPDOOR";
				obj = new
				{
					TimeOffset = (originalPlacement.SubType & 0xF) * 16
				};
				break;
			case 13:
				text = "SONICORCA/OBJECTS/SIGNPOST";
				break;
			case 20:
				text = "SONICORCA/OBJECTS/HTZSEESAW";
				break;
			case 22:
				text = "SONICORCA/OBJECTS/HTZVINELIFT";
				obj = new
				{
					Duration = originalPlacement.SubType * 8,
					Direction = (originalPlacement.FlipX ? "left" : "right")
				};
				position.Y += 152;
				break;
			case 24:
				text = "SONICORCA/OBJECTS/EHZPLATFORM";
				if (((originalPlacement.SubType >> 4) & 1) != 0)
				{
					text = "SONICORCA/OBJECTS/EHZBLOCK";
				}
				switch (originalPlacement.SubType & 3)
				{
					case 1:
						obj = new
						{
							RadiusX = 256,
							TimePeriod = 256
						};
						break;
					case 2:
						obj = new
						{
							RadiusY = 256,
							TimePeriod = 256
						};
						break;
					case 3:
						obj = new
						{
							Falling = true
						};
						break;
				}
				break;
			case 25:
				{
					text = "SONICORCA/OBJECTS/CPZPLATFORM";
					bool flag = ((originalPlacement.SubType >> 4) & 1) == 0;
					switch (originalPlacement.SubType & 7)
					{
						case 0:
							obj = new
							{
								Size = (flag ? "large" : "small"),
								RadiusX = 256,
								TimePeriod = 256
							};
							break;
						case 1:
							position += new Vector2i(-192, 0);
							obj = new
							{
								Size = (flag ? "large" : "small"),
								RadiusX = 192,
								TimePeriod = 440,
								TimeOffset = 110
							};
							break;
						case 2:
							position += new Vector2i(0, -256);
							obj = new
							{
								Size = (flag ? "large" : "small"),
								RadiusY = 256,
								TimePeriod = 360,
								TimeOffset = 90
							};
							break;
						case 6:
							position += new Vector2i(0, -384);
							obj = new
							{
								Size = (flag ? "large" : "small"),
								RadiusY = 384,
								TimePeriod = 312,
								TimeOffset = 78
							};
							break;
						case 7:
							position += new Vector2i(0, -392);
							obj = new
							{
								Size = (flag ? "large" : "small"),
								RadiusY = 392,
								TimePeriod = 316,
								TimeOffset = 79
							};
							break;
					}
					break;
				}
			case 27:
				text = "SONICORCA/OBJECTS/CPZSPEEDBOOSTER";
				obj = new
				{
					Strength = (new int[2] { 64, 40 })[(originalPlacement.SubType & 2) >> 1] * ((!originalPlacement.FlipX) ? 1 : (-1))
				};
				break;
			case 28:
				switch (originalPlacement.SubType)
				{
					case 2:
						text = "SONICORCA/OBJECTS/EHZBRIDGE/STAKE";
						break;
					case 4:
						text = "SONICORCA/OBJECTS/HTZVINELIFT/PILLAR";
						break;
					case 5:
						text = "SONICORCA/OBJECTS/HTZVINELIFT/PILLAR";
						obj = new
						{
							Right = true
						};
						break;
				}
				break;
			case 29:
				text = "SONICORCA/OBJECTS/CPZBLOBS";
				obj = new
				{
					Count = (originalPlacement.SubType & 0xF),
					Type = (((originalPlacement.SubType & 0xF0) != 0) ? "straight" : "arc")
				};
				break;
			case 30:
				text = "SONICORCA/OBJECTS/CPZTUBEENTRANCE";
				obj = new
				{
					Subtype = originalPlacement.SubType
				};
				break;
			case 38:
				text = "SONICORCA/OBJECTS/MONITOR";
				obj = new
				{
					Contents = (new string[10] { "none", "life", "life", "robotnik", "ring", "speedshoes", "barrier", "invincibility", "swapplaces", "random" })[originalPlacement.SubType]
				};
				break;
			case 45:
				text = "SONICORCA/OBJECTS/HTZDOOR";
				break;
			case 50:
				text = "SONICORCA/OBJECTS/HTZROCK";
				break;
			case 54:
				text = "SONICORCA/OBJECTS/SPIKES";
				break;
			case 62:
				text = "SONICORCA/OBJECTS/CAPSULE";
				break;
			case 64:
				text = "SONICORCA/OBJECTS/SPRINGBOARD";
				if (originalPlacement.FlipX)
				{
					obj = new
					{
						flipX = true
					};
				}
				break;
			case 65:
				{
					text = "SONICORCA/OBJECTS/SPRING";
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
					obj = new
					{
						Strength = (((originalPlacement.SubType & 2) != 0) ? 40 : 64),
						Direction = direction
					};
					break;
				}
			case 107:
				text = "SONICORCA/OBJECTS/CPZBLOCK";
				obj = new
				{
					Type = originalPlacement.SubType
				};
				break;
			case 120:
				text = "SONICORCA/OBJECTS/CPZBLOCK";
				obj = new
				{
					Type = "stairs " + originalPlacement.SubType
				};
				break;
			case 121:
				text = "SONICORCA/OBJECTS/STARPOST";
				obj = new
				{
					Index = originalPlacement.SubType
				};
				position.Y -= 32;
				break;
			case 123:
				text = "SONICORCA/OBJECTS/CPZTUBESPRING";
				obj = new
				{
					Type = originalPlacement.SubType
				};
				break;
			case 132:
				text = "SONICORCA/OBJECTS/FORCESPINBALL";
				break;
			case 146:
				text = "SONICORCA/OBJECTS/SPIKER";
				if (originalPlacement.FlipY)
				{
					position.Y += 64;
				}
				else
				{
					position.Y -= 64;
				}
				obj = new
				{
					UpsideDown = originalPlacement.FlipY
				};
				break;
			case 148:
			case 150:
				text = "SONICORCA/OBJECTS/REXON";
				break;
			case 149:
				text = "SONICORCA/OBJECTS/SOL";
				break;
			case 165:
				text = "SONICORCA/OBJECTS/SPINY";
				break;
			case 166:
				text = "SONICORCA/OBJECTS/SPINY";
				obj = new
				{
					Wall = true
				};
				break;
			case 167:
				text = "SONICORCA/OBJECTS/GRABBER";
				break;
		}
		if (text == null)
		{
			return null;
		}
		if (obj == null)
		{
			return new ObjectPlacement(text, -1, position);
		}
		return new ObjectPlacement(text, -1, position, obj);
	}
}
