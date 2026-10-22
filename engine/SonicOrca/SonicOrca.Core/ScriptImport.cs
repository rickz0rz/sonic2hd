using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SonicOrca.Core;

internal static class ScriptImport
{
	public static Type[] Compile(string sourceCode)
	{
		if (sourceCode.StartsWith("#use "))
		{
			string useNamespace = ReadToWhitespace(sourceCode.Substring(5));
			return (from x in AppDomain.CurrentDomain.GetAssemblies().SelectMany((Assembly x) => x.GetTypes())
				where x.Namespace == useNamespace
				select x).ToArray();
		}
		// Runtime C# compilation (CodeDom) is not available on modern .NET. All shipped area and object
		// resources use the "#use <namespace>" form, referencing types precompiled into the game assembly.
		throw new NotSupportedException("Compiling script source at runtime is not supported; precompile the type and reference it with \"#use <namespace>\".");
	}

	private static string ReadToWhitespace(string input)
	{
		for (int i = 0; i < input.Length; i++)
		{
			if (char.IsWhiteSpace(input[i]))
			{
				return input.Substring(0, i);
			}
		}
		return input;
	}
}
