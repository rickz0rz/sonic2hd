using System;
using System.IO;

namespace SonicOrca;

/// <summary>
/// Locates the game's read-only content (data, shaders, films).
/// </summary>
public static class ContentDirectory
{
	private static string _path;

	/// <summary>
	/// <c>Contents/Resources</c> when running from a macOS app bundle; otherwise the executable's directory,
	/// unless a host has set it with <see cref="Override"/>.
	/// </summary>
	public static string Path => _path ??= Resolve();

	/// <summary>
	/// Sets the content location explicitly, e.g. the web build's in-memory file system.
	/// </summary>
	public static void Override(string path)
	{
		_path = path;
	}

	public static string Combine(string relativePath) => System.IO.Path.Combine(Path, relativePath);

	private static string Resolve()
	{
		string baseDirectory = AppContext.BaseDirectory;
		if (OperatingSystem.IsMacOS())
		{
			string resources = System.IO.Path.GetFullPath(System.IO.Path.Combine(baseDirectory, "..", "Resources"));
			if (Directory.Exists(System.IO.Path.Combine(resources, "data")))
				return resources;
		}
		return baseDirectory;
	}
}
