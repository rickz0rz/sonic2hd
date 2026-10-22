using System;

namespace SonicOrca.SDL2;

/// <summary>
/// Graphics errors thrown by the shared GL backend. The desktop type adds SDL's error text; the web build has no SDL.
/// </summary>
public class SDL2Exception : Exception
{
	public SDL2Exception(string message)
		: base(message)
	{
	}

	public SDL2Exception(string message, Exception inner)
		: base(message, inner)
	{
	}
}
