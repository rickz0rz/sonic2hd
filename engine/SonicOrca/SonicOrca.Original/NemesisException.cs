using System;

namespace SonicOrca.Original;

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
