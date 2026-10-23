using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SonicOrca.Core.Debugging;

namespace SonicOrca.Graphics;

public static class OrcaShader
{
	public static ManagedShaderProgram CreateFromFile(IGraphicsContext context, string path)
	{
		long start = FrameProfiler.Timestamp();
		Parse(ParseShaderFileWithIncludes(path), out var vertexOuput, out var fragmentOutput);
		ManagedShaderProgram program = new ManagedShaderProgram(context, vertexOuput, fragmentOutput);
		FrameProfiler.Note("Shader " + path, start);
		return program;
	}

	public static ManagedShaderProgram Create(IGraphicsContext context, string input)
	{
		Parse(input, out var vertexOuput, out var fragmentOutput);
		return new ManagedShaderProgram(context, vertexOuput, fragmentOutput);
	}

	private static string ParseShaderFileWithIncludes(string path)
	{
		if (!File.Exists(path))
		{
			path = ContentDirectory.Combine(path);
		}
		StringBuilder stringBuilder = new StringBuilder();
		StreamReader streamReader = new StreamReader(path);
		string text;
		while ((text = streamReader.ReadLine()) != null)
		{
			string text2 = text.TrimStart();
			if (text2.StartsWith("@include"))
			{
				text2 = text2.Substring(8);
				if (text2.Length > 0 && char.IsWhiteSpace(text2[0]))
				{
					text2 = text2.Trim();
					if (!text2.StartsWith("\"") || !text2.EndsWith("\""))
					{
						throw new InvalidDataException("include syntax error");
					}
					text2 = text2.Substring(1, text2.Length - 2);
					text2 = Path.Combine(Path.GetDirectoryName(path), text2);
					if (!File.Exists(text2))
					{
						throw new FileNotFoundException(text2 + " not found.", text2);
					}
					stringBuilder.AppendLine(ParseShaderFileWithIncludes(text2));
					continue;
				}
			}
			stringBuilder.AppendLine(text);
		}
		return stringBuilder.ToString();
	}

	public static void Parse(string input, out string vertexOuput, out string fragmentOutput)
	{
		IDictionary<string, string> blocks = GetBlocks(input).ToDictionary((KeyValuePair<string, string> x) => x.Key, (KeyValuePair<string, string> x) => x.Value);
		vertexOuput = ConcatenateBlocks(blocks, new string[5]
		{
			string.Empty,
			"uniform",
			"vertex_input",
			"vertex_output",
			"vertex"
		});
		fragmentOutput = ConcatenateBlocks(blocks, new string[5]
		{
			string.Empty,
			"uniform",
			"fragment_input",
			"fragment_output",
			"fragment"
		});
	}

	private static string ConcatenateBlocks(IDictionary<string, string> blocks, IEnumerable<string> blockNames)
	{
		StringBuilder stringBuilder = new StringBuilder();
		foreach (string blockName in blockNames)
		{
			if (blocks.TryGetValue(blockName, out var value))
			{
				if (!string.IsNullOrEmpty(blockName))
				{
					stringBuilder.AppendLine("//" + blockName + ":");
				}
				stringBuilder.Append(value);
			}
		}
		return stringBuilder.ToString();
	}

	private static IReadOnlyCollection<KeyValuePair<string, string>> GetBlocks(string input)
	{
		string key = string.Empty;
		StringBuilder stringBuilder = new StringBuilder();
		List<KeyValuePair<string, string>> list = new List<KeyValuePair<string, string>>();
		StringReader stringReader = new StringReader(input);
		string text;
		while ((text = stringReader.ReadLine()) != null)
		{
			if (text.TrimStart().StartsWith("@"))
			{
				string text2 = text.Substring(text.IndexOf("@") + 1);
				int num = text2.IndexOf(":");
				if (num != -1)
				{
					list.Add(new KeyValuePair<string, string>(key, stringBuilder.ToString()));
					key = text2.Remove(num);
					stringBuilder.Clear();
					continue;
				}
			}
			stringBuilder.AppendLine(text);
		}
		list.Add(new KeyValuePair<string, string>(key, stringBuilder.ToString()));
		return list.ToArray();
	}
}
