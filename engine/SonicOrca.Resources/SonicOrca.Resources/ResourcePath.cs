using System.IO;

namespace SonicOrca.Resources;

public static class ResourcePath
{
	public static string GetPathWithoutExtension(string path)
	{
		string directoryName = Path.GetDirectoryName(path);
		string fileName = Path.GetFileName(path);
		int num = fileName.IndexOf('.');
		if (num == -1)
		{
			return string.Empty;
		}
		return Path.Combine(directoryName, fileName.Substring(0, num));
	}

	public static string GetExtension(string path)
	{
		string fileName = Path.GetFileName(path);
		int num = fileName.IndexOf('.');
		if (num == -1)
		{
			return string.Empty;
		}
		return fileName.Substring(num);
	}

	internal static string GetRelativeFileResourceFromAbsolute(string parent, string relative)
	{
		if (parent.StartsWith("$"))
		{
			parent = parent.Substring(1);
		}
		if (relative.StartsWith("/"))
		{
			relative = relative.Substring(1);
		}
		string directoryName = Path.GetDirectoryName(parent);
		string searchPattern = string.Concat(string.Concat(GetPathWithoutExtension(Path.GetFileName(parent)) + "_", relative), ".*");
		string[] files = Directory.GetFiles(directoryName, searchPattern, SearchOption.TopDirectoryOnly);
		if (files.Length == 0)
		{
			throw new ResourceException($"There are one or more resources with the same key in {directoryName}.");
		}
		return "$" + files[0];
	}
}
