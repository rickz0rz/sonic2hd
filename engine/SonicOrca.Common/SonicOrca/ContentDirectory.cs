using System;
using System.IO;

namespace SonicOrca;

/// <summary>
/// Locates the game's read-only content (data, shaders, films).
/// </summary>
public static class ContentDirectory
{
	private static readonly Lazy<string> _path = new Lazy<string>(Resolve);

	/// <summary>
	/// <c>Contents/Resources</c> when running from a macOS app bundle; otherwise the executable's directory.
	/// </summary>
	public static string Path => _path.Value;

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
