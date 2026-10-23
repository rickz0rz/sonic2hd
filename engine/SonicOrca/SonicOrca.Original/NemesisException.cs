using System;

namespace SonicOrca.Original;

/// <summary>
/// The exception that occurs when Nemesis data is not correct.
/// </summary>
[Serializable]
public class NemesisException : Exception
{
	public NemesisException()
	{
	}

	public NemesisException(string message)
		: base(message)
	{
	}

	public NemesisException(string message, Exception inner)
		: base(message, inner)
	{
	}
}
