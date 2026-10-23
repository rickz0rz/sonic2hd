namespace SonicOrca.Graphics.V2.Video;

/// <summary>
/// Describes one video in a film group by its resource key.
/// </summary>
public class Film
{
	private string _path;

	public string Path => _path;

	public Film(string path)
	{
		_path = path;
	}
}
