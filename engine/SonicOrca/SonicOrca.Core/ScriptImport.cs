using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.CSharp;

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
		IReadOnlyList<string> referenceAssemblyNames = new string[1] { "SonicOrca" };
		CompilerParameters compilerParameters = new CompilerParameters((from x in AppDomain.CurrentDomain.GetAssemblies()
			where referenceAssemblyNames.Contains(x.GetName().Name)
			select x.Location).ToArray());
		compilerParameters.GenerateInMemory = true;
		CompilerResults compilerResults = new CSharpCodeProvider().CompileAssemblyFromSource(compilerParameters, sourceCode);
		if (compilerResults.Errors.Count > 0)
		{
			throw new Exception("Compile errors:\n" + string.Join(Environment.NewLine, from x in compilerResults.Errors.OfType<CompilerError>()
				select $"  Line {x.Line}, {x.ErrorText}"));
		}
		return compilerResults.CompiledAssembly.GetTypes();
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
