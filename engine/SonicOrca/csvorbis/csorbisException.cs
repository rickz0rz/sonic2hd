using System;

namespace csvorbis;

public class csorbisException : Exception
{
	public csorbisException()
	{
	}

	public csorbisException(string s)
		: base("csorbis: " + s)
	{
	}
}
