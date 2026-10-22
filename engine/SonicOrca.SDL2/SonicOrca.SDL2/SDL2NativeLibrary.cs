using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using SDL2;

namespace SonicOrca.SDL2;

/// <summary>
/// SDL2-CS imports "SDL2.dll"; maps that to the platform's SDL2 library name outside Windows.
/// </summary>
internal static class SDL2NativeLibrary
{
	private static readonly string[] MacOSNames = { "libSDL2.dylib", "libSDL2-2.0.0.dylib" };

	private static readonly string[] LinuxNames = { "libSDL2-2.0.so.0", "libSDL2.so" };

	private static bool _registered;

	public static void Register()
	{
		if (_registered || OperatingSystem.IsWindows())
			return;
		NativeLibrary.SetDllImportResolver(typeof(SDL).Assembly, Resolve);
		_registered = true;
	}

	private static IntPtr Resolve(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
	{
		if (libraryName != "SDL2.dll" && libraryName != "SDL2")
			return IntPtr.Zero;
		string[] names = OperatingSystem.IsMacOS() ? MacOSNames : LinuxNames;
		foreach (string name in names)
		{
			// Prefer the copy bundled next to the executable, then the system search path
			if (NativeLibrary.TryLoad(Path.Combine(AppContext.BaseDirectory, name), out IntPtr handle) ||
				NativeLibrary.TryLoad(name, assembly, searchPath, out handle))
				return handle;
		}
		return IntPtr.Zero;
	}
}
