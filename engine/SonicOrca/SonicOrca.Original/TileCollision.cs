using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;
using SonicOrca.Core;
using SonicOrca.Geometry;

namespace SonicOrca.Original;

/// <summary>
/// The collision data of the original for the front layer of an imported map: the height maps, the collision number
/// of each block for the two paths, and the solidity of each cell. The HD map keeps only the block and the flips of
/// each cell, so the level editor needs this data to trace the collision again after it changes tiles. The import
/// writes it as the resource TILECOLLISION next to the map. It comes from the original game, so it stays local.
/// </summary>
public sealed class TileCollision
{
	/// <summary>The name of the resource in the folder of the zone.</summary>
	public const string ResourceName = "TILECOLLISION";

	/// <summary>The cell is solid from the top on path 1 (bit 12 of a chunk entry of the original).</summary>
	public const byte TopSolidPath1 = 1;

	/// <summary>The cell is solid from the sides and the bottom on path 1 (bit 13).</summary>
	public const byte SidesSolidPath1 = 2;

	/// <summary>The cell is solid from the top on path 2 (bit 14).</summary>
	public const byte TopSolidPath2 = 4;

	/// <summary>The cell is solid from the sides and the bottom on path 2 (bit 15).</summary>
	public const byte SidesSolidPath2 = 8;

	private const int BlockMask = 0x3FF;

	private const int FlipXFlag = 0x4000;

	private const int FlipYFlag = 0x8000;

	private readonly byte[] _solidity;

	/// <summary>The index of the front layer in the map.</summary>
	public int Layer { get; }

	public int Columns { get; }

	public int Rows { get; }

	/// <summary>The height maps: 16 signed heights for each collision number.</summary>
	public byte[] Heights { get; }

	/// <summary>The collision number of each block for path 1.</summary>
	public byte[] Primary { get; }

	/// <summary>The collision number of each block for path 2.</summary>
	public byte[] Secondary { get; }

	public TileCollision(int layer, int columns, int rows, byte[] heights, byte[] primary, byte[] secondary, byte[] solidity)
	{
		if (solidity.Length != columns * rows)
		{
			throw new ArgumentException("The solidity does not have columns * rows cells.", nameof(solidity));
		}
		Layer = layer;
		Columns = columns;
		Rows = rows;
		Heights = heights;
		Primary = primary;
		Secondary = secondary;
		_solidity = solidity;
	}

	/// <summary>Makes the data from the chunk entries of the original: the block, the flips and the solidity bits.</summary>
	public static TileCollision FromBlocks(int layer, ushort[,] blocks, byte[] heights, byte[] primary, byte[] secondary)
	{
		int columns = blocks.GetLength(0);
		int rows = blocks.GetLength(1);
		byte[] solidity = new byte[columns * rows];
		for (int row = 0; row < rows; row++)
		{
			for (int column = 0; column < columns; column++)
			{
				solidity[row * columns + column] = (byte)(blocks[column, row] >> 12);
			}
		}
		return new TileCollision(layer, columns, rows, heights, primary, secondary, solidity);
	}

	/// <summary>The solidity of a cell: a combination of the path flags. A cell outside the data is not solid.</summary>
	public byte GetSolidity(int column, int row)
	{
		return column >= 0 && row >= 0 && column < Columns && row < Rows ? _solidity[row * Columns + column] : (byte)0;
	}

	public void SetSolidity(int column, int row, byte solidity)
	{
		if (column >= 0 && row >= 0 && column < Columns && row < Rows)
		{
			_solidity[row * Columns + column] = (byte)(solidity & 0xF);
		}
	}

	/// <summary>
	/// Traces the collision of both paths from the tiles of the front layer and the solidity of each cell, as the
	/// import does. The block of a cell is the tile index without the flags.
	/// </summary>
	public List<(Vector2i A, Vector2i B, uint Paths)> Trace(LevelLayer front)
	{
		ushort[,] blocks = new ushort[Columns, Rows];
		for (int row = 0; row < Rows && row < front.Rows; row++)
		{
			for (int column = 0; column < Columns && column < front.Columns; column++)
			{
				int tile = front.Tiles[column, row];
				int entry = (tile & BlockMask) | (GetSolidity(column, row) << 12);
				if ((tile & FlipXFlag) != 0)
				{
					entry |= 0x400;
				}
				if ((tile & FlipYFlag) != 0)
				{
					entry |= 0x800;
				}
				blocks[column, row] = (ushort)entry;
			}
		}
		return LevelImport.GetCollisionLines(blocks, Primary, Secondary, Heights);
	}

	/// <summary>The same data for the front layer at another index, after a change of the layers of the map.</summary>
	public TileCollision WithLayer(int layer) => new TileCollision(layer, Columns, Rows, Heights, Primary, Secondary, _solidity);

	public TileCollision Clone() => new TileCollision(Layer, Columns, Rows, Heights, Primary, Secondary, (byte[])_solidity.Clone());

	/// <summary>Writes the data as XML, with the tables in base 64.</summary>
	public byte[] ToXml()
	{
		using MemoryStream stream = new MemoryStream();
		using (XmlWriter writer = XmlWriter.Create(stream, new XmlWriterSettings { Indent = true, Encoding = new UTF8Encoding(false) }))
		{
			writer.WriteStartElement("tilecollision");
			writer.WriteAttributeString("layer", Layer.ToString(CultureInfo.InvariantCulture));
			writer.WriteAttributeString("columns", Columns.ToString(CultureInfo.InvariantCulture));
			writer.WriteAttributeString("rows", Rows.ToString(CultureInfo.InvariantCulture));
			writer.WriteElementString("heights", Convert.ToBase64String(Heights));
			writer.WriteElementString("primary", Convert.ToBase64String(Primary));
			writer.WriteElementString("secondary", Convert.ToBase64String(Secondary));
			writer.WriteElementString("solidity", Convert.ToBase64String(_solidity));
			writer.WriteEndElement();
		}
		return stream.ToArray();
	}

	public static TileCollision FromXml(Stream stream)
	{
		XmlDocument document = new XmlDocument();
		document.Load(stream);
		XmlElement root = document.DocumentElement ?? throw new InvalidDataException("The tile collision has no root element.");
		int Number(string name) => int.Parse(root.GetAttribute(name), CultureInfo.InvariantCulture);
		byte[] Table(string name) => Convert.FromBase64String(root.SelectSingleNode(name)?.InnerText ?? throw new InvalidDataException("The tile collision has no " + name + "."));
		return new TileCollision(Number("layer"), Number("columns"), Number("rows"), Table("heights"), Table("primary"), Table("secondary"), Table("solidity"));
	}
}
