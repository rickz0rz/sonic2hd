using System.Collections.Generic;
using SonicOrca.Extensions;

#nullable disable
namespace S2HD;

/// <summary>
/// Parses the command line of the game.
/// A "--name" argument is an option, and the arguments after it (until the next "-" argument) are its values.
/// A "-abc" argument sets the single-character flags a, b and c.
/// </summary>
internal class CommandLineArguments
{
	private readonly HashSet<char> _flags = new HashSet<char>();
	private readonly Dictionary<string, string[]> _options = new Dictionary<string, string[]>();
	private readonly string _commandLine;

	public CommandLineArguments(IEnumerable<string> args)
	  : this(args.AsArray())
	{
	}

	public CommandLineArguments(string[] args)
	{
		List<string> optionValues = new List<string>();
		_commandLine = string.Join(" ", args);
		for (int index = 0; index < args.Length; ++index)
		{
			string argument = args[index];
			if (argument.StartsWith("--"))
			{
				string key = argument.Substring(2);
				optionValues.Clear();
				for (++index; index < args.Length; ++index)
				{
					string value = args[index];
					if (value.StartsWith("-"))
					{
						--index;
						break;
					}
					optionValues.Add(value);
				}
				_options.Add(key, optionValues.ToArray());
			}
			else if (argument.StartsWith("-"))
			{
				foreach (char flag in argument.Substring(1))
					_flags.Add(flag);
			}
		}
	}

	public bool HasFlag(char c) => _flags.Contains(c);

	public bool HasOption(string option) => _options.ContainsKey(option);

	public string[] GetOptionValues(string option)
	{
		if (!_options.TryGetValue(option, out string[] optionValues))
			optionValues = new string[0];
		return optionValues;
	}

	public override string ToString() => _commandLine;
}
