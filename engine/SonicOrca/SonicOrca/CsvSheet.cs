using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SonicOrca;

/// <summary>
/// A table of text cells that loads from and saves to comma-separated values (CSV).
/// </summary>
/// <remarks>
/// The indexer uses x for the column and y for the row. A cell outside the table reads as an empty string.
/// </remarks>
public class CsvSheet
{
	private List<List<string>> _rows = new List<List<string>>();

	private int _columns;

	public string this[int x, int y]
	{
		get
		{
			return Get(x, y);
		}
		set
		{
			Set(x, y, value);
		}
	}

	public int Rows => _rows.Count;

	public int Columns => _columns;

	public CsvSheet()
	{
	}

	public CsvSheet(string filename)
	{
		using FileStream stream = new FileStream(filename, FileMode.Open);
		Load(stream);
	}

	public CsvSheet(Stream stream)
	{
		Load(stream);
	}

	private void Load(Stream stream)
	{
		_rows = new List<List<string>>();
		using (StreamReader streamReader = new StreamReader(stream))
		{
			string line;
			while ((line = streamReader.ReadLine()) != null)
			{
				_rows.Add(new List<string>(ProcessLine(line)));
			}
		}
		CalculateColumns();
	}

	public void Save(string filename)
	{
		using FileStream stream = new FileStream(filename, FileMode.Create);
		Save(stream);
	}

	public void Save(Stream stream)
	{
		using StreamWriter streamWriter = new StreamWriter(stream);
		foreach (List<string> row in _rows)
		{
			for (int i = 0; i < row.Count - 1; i++)
			{
				streamWriter.Write("{0},", FormatAsCSV(row[i]));
			}
			if (row.Count > 0)
			{
				streamWriter.Write(FormatAsCSV(row[row.Count - 1]));
			}
			streamWriter.WriteLine();
		}
	}

	private string FormatAsCSV(string cell)
	{
		if (cell.Contains('"'))
		{
			cell = cell.Replace("\"", "\"\"");
		}
		if (cell.Contains(",") || cell.StartsWith("\"") || cell.EndsWith("\""))
		{
			cell = $"\"{cell}\"";
		}
		return cell;
	}

	private string Get(int x, int y)
	{
		if (x < 0 || y < 0)
		{
			throw new ArgumentOutOfRangeException();
		}
		if (y >= _rows.Count)
		{
			return string.Empty;
		}
		List<string> row = _rows[y];
		if (x >= row.Count)
		{
			return string.Empty;
		}
		return row[x];
	}

	private void Set(int x, int y, string value)
	{
		if (x < 0 || y < 0)
		{
			throw new ArgumentOutOfRangeException();
		}
		while (y >= _rows.Count)
		{
			_rows.Add(new List<string>());
		}
		List<string> row = _rows[y];
		while (x >= row.Count)
		{
			row.Add(string.Empty);
		}
		row[x] = value;
		CalculateColumns();
	}

	private void CalculateColumns()
	{
		_columns = _rows.Count == 0 ? 0 : _rows.Max(row => row.Count);
	}

	private string[] ProcessLine(string line)
	{
		List<string> cells = new List<string>();
		using (StringReader sr = new StringReader(line))
		{
			string nextCell;
			while ((nextCell = GetNextCell(sr)) != null)
			{
				cells.Add(nextCell);
			}
		}
		return cells.ToArray();
	}

	/// <summary>
	/// Reads one cell, up to the next comma that is not in quotes.
	/// </summary>
	/// <returns>The cell text, or <c>null</c> at the end of the line.</returns>
	private string GetNextCell(StringReader sr)
	{
		if (sr.Peek() == -1)
		{
			return null;
		}
		// Spaces outside quotes are kept only when more text follows them, so trailing spaces are removed.
		int pendingSpaces = 0;
		bool inQuotes = false;
		StringBuilder stringBuilder = new StringBuilder();
		while (sr.Peek() != -1)
		{
			char c = (char)sr.Read();
			if (char.IsWhiteSpace(c) && !inQuotes && string.IsNullOrEmpty(stringBuilder.ToString()))
			{
				continue;
			}
			if (c == ',' && !inQuotes)
			{
				break;
			}
			if (c == ' ' && !inQuotes)
			{
				pendingSpaces++;
				continue;
			}
			stringBuilder.Append(new string(' ', pendingSpaces));
			pendingSpaces = 0;
			if (c == '"' && sr.Peek() == '"')
			{
				// Two quotes in sequence are one literal quote.
				sr.Read();
				stringBuilder.Append('"');
			}
			else if (c == '"')
			{
				inQuotes = !inQuotes;
			}
			else
			{
				stringBuilder.Append(c);
			}
		}
		return stringBuilder.ToString();
	}
}
