using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SonicOrca;

public class CsvSheet
{
	private List<List<string>> mRows = new List<List<string>>();

	private int mColumns;

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

	public int Rows => mRows.Count;

	public int Columns => mColumns;

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
		mRows = new List<List<string>>();
		using (StreamReader streamReader = new StreamReader(stream))
		{
			string line;
			while ((line = streamReader.ReadLine()) != null)
			{
				mRows.Add(new List<string>(ProcessLine(line)));
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
		foreach (List<string> mRow in mRows)
		{
			for (int i = 0; i < mRow.Count - 1; i++)
			{
				streamWriter.Write("{0},", FormatAsCSV(mRow[i]));
			}
			if (mRow.Count > 0)
			{
				streamWriter.Write(FormatAsCSV(mRow[mRow.Count - 1]));
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
		if (y >= mRows.Count)
		{
			return string.Empty;
		}
		List<string> list = mRows[y];
		if (x >= list.Count)
		{
			return string.Empty;
		}
		return list[x];
	}

	private void Set(int x, int y, string value)
	{
		if (x < 0 || y < 0)
		{
			throw new ArgumentOutOfRangeException();
		}
		while (y >= mRows.Count)
		{
			mRows.Add(new List<string>());
		}
		List<string> list = mRows[y];
		while (x >= list.Count)
		{
			list.Add(string.Empty);
		}
		list[x] = value;
		CalculateColumns();
	}

	private void CalculateColumns()
	{
		mColumns = mRows.Max((List<string> row) => row.Count);
	}

	private string[] ProcessLine(string line)
	{
		List<string> list = new List<string>();
		using (StringReader sr = new StringReader(line))
		{
			string nextCell;
			while ((nextCell = GetNextCell(sr)) != null)
			{
				list.Add(nextCell);
			}
		}
		return list.ToArray();
	}

	private string GetNextCell(StringReader sr)
	{
		if (sr.Peek() == -1)
		{
			return null;
		}
		int num = 0;
		bool flag = false;
		StringBuilder stringBuilder = new StringBuilder();
		while (sr.Peek() != -1)
		{
			char c = (char)sr.Read();
			if (char.IsWhiteSpace(c) && !flag && string.IsNullOrEmpty(stringBuilder.ToString()))
			{
				continue;
			}
			if (c == ',' && !flag)
			{
				break;
			}
			if (c == ' ' && !flag)
			{
				num++;
				continue;
			}
			stringBuilder.Append(new string(' ', num));
			num = 0;
			if (c == '"' && (ushort)sr.Peek() == 34)
			{
				sr.Read();
				stringBuilder.Append('"');
			}
			else if (c == '"')
			{
				flag = !flag;
			}
			else
			{
				stringBuilder.Append(c);
			}
		}
		return stringBuilder.ToString();
	}
}
