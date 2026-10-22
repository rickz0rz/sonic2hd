namespace SonicOrca.Graphics.V2.Animation;

public class CompositionAsset
{
	private string _id;

	private int _width;

	private int _height;

	private string _path;

	private string _fileName;

	public string ID => _id;

	public int Width => _width;

	public int Height => _height;

	public string Path => _path;

	public string FileName => _fileName;

	public CompositionAsset(string id, int width, int height, string path, string fileName)
	{
		_id = id;
		_width = width;
		_height = height;
		_path = path;
		_fileName = fileName;
	}
}
