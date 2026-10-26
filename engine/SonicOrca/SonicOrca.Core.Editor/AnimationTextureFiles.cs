using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using SonicOrca.Resources;

namespace SonicOrca.Core.Editor;

/// <summary>
/// The texture files of the animation editor. The editor writes a texture as a PNG file into its folder, in the layout
/// of "s2hd-data unpack", where the game loads it as a mod. The artist edits this file, and the editor reloads it.
/// </summary>
public static class AnimationTextureFiles
{
	private static readonly byte[] PngSignature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

	/// <summary>
	/// The file of a texture in the folder: SONICORCA/OBJECTS/RING/TEXTURE is SONICORCA/OBJECTS/RING/TEXTURE.png, or
	/// SONICORCA/OBJECTS/RING/TEXTURE/_.png when the key has children (see ResourceDirectory).
	/// </summary>
	public static string GetPath(string folder, ResourceTree tree, string key)
	{
		string[] parts = key.Split('/');
		bool hasChildren = tree?[key]?.Children.Count > 0;
		string path = Path.Combine(new[] { folder }.Concat(parts).ToArray());
		return hasChildren ? Path.Combine(path, "_.png") : path + ".png";
	}

	/// <summary>True when the data starts with the signature of a PNG file.</summary>
	public static bool IsPng(byte[] data) => data.Length >= PngSignature.Length && data.AsSpan(0, PngSignature.Length).SequenceEqual(PngSignature);

	/// <summary>A name for a new texture from a file name: upper case letters, digits and underscores, for example "SHEET_2".</summary>
	public static string MakeTextureName(string fileName)
	{
		string name = Path.GetFileNameWithoutExtension(fileName).ToUpperInvariant();
		StringBuilder clean = new StringBuilder();
		foreach (char c in name)
		{
			clean.Append(char.IsAsciiLetterOrDigit(c) ? c : '_');
		}
		return clean.Length > 0 ? clean.ToString() : "TEXTURE";
	}

	/// <summary>Reads a file that an image editor may still write: the read shares the file, and an error gives null.</summary>
	public static byte[] TryRead(string path)
	{
		try
		{
			using FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
			using MemoryStream memory = new MemoryStream();
			stream.CopyTo(memory);
			return memory.ToArray();
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
			return null;
		}
	}

	/// <summary>Writes a file through a temporary file, so that a reader never sees half of it.</summary>
	public static void Write(string path, byte[] data)
	{
		Directory.CreateDirectory(Path.GetDirectoryName(path));
		string temporary = path + ".tmp";
		File.WriteAllBytes(temporary, data);
		File.Move(temporary, path, overwrite: true);
	}

	/// <summary>Shows a file in the file manager of the system: Finder, Explorer or the default file manager. Returns false in a browser.</summary>
	public static bool ShowInFileManager(string path)
	{
		if (OperatingSystem.IsBrowser())
		{
			return false;
		}
		try
		{
			ProcessStartInfo startInfo;
			if (OperatingSystem.IsMacOS())
			{
				startInfo = new ProcessStartInfo("open") { ArgumentList = { "-R", path } };
			}
			else if (OperatingSystem.IsWindows())
			{
				startInfo = new ProcessStartInfo("explorer.exe") { ArgumentList = { "/select," + path } };
			}
			else
			{
				startInfo = new ProcessStartInfo("xdg-open") { ArgumentList = { Path.GetDirectoryName(path) } };
			}
			startInfo.UseShellExecute = false;
			Process.Start(startInfo)?.Dispose();
			return true;
		}
		catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
		{
			return false;
		}
	}
}
