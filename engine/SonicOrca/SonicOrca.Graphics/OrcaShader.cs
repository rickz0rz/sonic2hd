using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SonicOrca.Core.Debugging;

namespace SonicOrca.Graphics;

/// <summary>
/// Builds shader programs from Orca shader files. An Orca shader file holds the vertex shader and the fragment
/// shader together, in blocks that start with a line such as "@vertex:". A line such as
/// "@include "file.glsl"" puts the content of another file in its place.
/// </summary>
public static class OrcaShader
{
	private const string IncludeDirective = "@include";

	/// <summary>
	/// The blocks that make the vertex shader, in order. The empty name is the text before the first block.
	/// </summary>
	private static readonly string[] VertexBlockNames = new[] { string.Empty, "uniform", "vertex_input", "vertex_output", "vertex" };

	/// <summary>
	/// The blocks that make the fragment shader, in order. The empty name is the text before the first block.
	/// </summary>
	private static readonly string[] FragmentBlockNames = new[] { string.Empty, "uniform", "fragment_input", "fragment_output", "fragment" };

	public static ManagedShaderProgram CreateFromFile(IGraphicsContext context, string path)
	{
		long start = FrameProfiler.Timestamp();
		Parse(ParseShaderFileWithIncludes(path), out string vertexOutput, out string fragmentOutput);
		ManagedShaderProgram program = new ManagedShaderProgram(context, vertexOutput, fragmentOutput);
		FrameProfiler.Note("Shader " + path, start);
		return program;
	}

	public static ManagedShaderProgram Create(IGraphicsContext context, string input)
	{
		Parse(input, out string vertexOutput, out string fragmentOutput);
		return new ManagedShaderProgram(context, vertexOutput, fragmentOutput);
	}

	private static string ParseShaderFileWithIncludes(string path)
	{
		if (!File.Exists(path))
		{
			path = ContentDirectory.Combine(path);
		}
		StringBuilder stringBuilder = new StringBuilder();
		using StreamReader streamReader = new StreamReader(path);
		string line;
		while ((line = streamReader.ReadLine()) != null)
		{
			string directive = line.TrimStart();
			if (directive.StartsWith(IncludeDirective))
			{
				string includePath = directive.Substring(IncludeDirective.Length);
				if (includePath.Length > 0 && char.IsWhiteSpace(includePath[0]))
				{
					includePath = includePath.Trim();
					if (!includePath.StartsWith("\"") || !includePath.EndsWith("\""))
					{
						throw new InvalidDataException("include syntax error");
					}
					includePath = includePath.Substring(1, includePath.Length - 2);
					includePath = Path.Combine(Path.GetDirectoryName(path), includePath);
					if (!File.Exists(includePath))
					{
						throw new FileNotFoundException(includePath + " not found.", includePath);
					}
					stringBuilder.AppendLine(ParseShaderFileWithIncludes(includePath));
					continue;
				}
			}
			stringBuilder.AppendLine(line);
		}
		return stringBuilder.ToString();
	}

	public static void Parse(string input, out string vertexOuput, out string fragmentOutput)
	{
		IDictionary<string, string> blocks = GetBlocks(input).ToDictionary(x => x.Key, x => x.Value);
		vertexOuput = ConcatenateBlocks(blocks, VertexBlockNames);
		fragmentOutput = ConcatenateBlocks(blocks, FragmentBlockNames);
	}

	private static string ConcatenateBlocks(IDictionary<string, string> blocks, IEnumerable<string> blockNames)
	{
		StringBuilder stringBuilder = new StringBuilder();
		foreach (string blockName in blockNames)
		{
			if (blocks.TryGetValue(blockName, out string blockText))
			{
				if (!string.IsNullOrEmpty(blockName))
				{
					stringBuilder.AppendLine("//" + blockName + ":");
				}
				stringBuilder.Append(blockText);
			}
		}
		return stringBuilder.ToString();
	}

	private static IReadOnlyCollection<KeyValuePair<string, string>> GetBlocks(string input)
	{
		string blockName = string.Empty;
		StringBuilder stringBuilder = new StringBuilder();
		List<KeyValuePair<string, string>> blocks = new List<KeyValuePair<string, string>>();
		StringReader stringReader = new StringReader(input);
		string line;
		while ((line = stringReader.ReadLine()) != null)
		{
			if (line.TrimStart().StartsWith("@"))
			{
				string header = line.Substring(line.IndexOf("@") + 1);
				int colonIndex = header.IndexOf(":");
				if (colonIndex != -1)
				{
					blocks.Add(new KeyValuePair<string, string>(blockName, stringBuilder.ToString()));
					blockName = header.Remove(colonIndex);
					stringBuilder.Clear();
					continue;
				}
			}
			stringBuilder.AppendLine(line);
		}
		blocks.Add(new KeyValuePair<string, string>(blockName, stringBuilder.ToString()));
		return blocks.ToArray();
	}
}
