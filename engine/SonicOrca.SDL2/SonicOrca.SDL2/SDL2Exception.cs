using System;
using SDL2;

namespace SonicOrca.SDL2;

[Serializable]
public class SDL2Exception : Exception
{
	public SDL2Exception()
	{
	}

	public SDL2Exception(string message)
		: base(message)
	{
	}

	public SDL2Exception(string message, Exception inner)
		: base(message, inner)
	{
	}

	public static SDL2Exception FromError()
	{
		return new SDL2Exception(SDL.SDL_GetError());
	}

	public static SDL2Exception FromError(string basicMessage)
	{
		return new SDL2Exception(basicMessage + Environment.NewLine + SDL.SDL_GetError());
	}
}
