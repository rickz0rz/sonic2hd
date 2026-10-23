using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using SonicOrca.Resources;

namespace SonicOrca.Graphics.V2.Video;

/// <summary>
/// Loads a film group from a ".film.xml" file. Each "video" node gives the name of a video resource.
/// </summary>
public class FilmGroupResourceType : ResourceType
{
	public override string Name => "film, xml";

	public override string DefaultExtension => ".film.xml";

	public FilmGroupResourceType()
		: base(ResourceTypeIdentifier.FilmGroup)
	{
	}

	public override async Task<ILoadedResource> LoadAsync(ResourceLoadArgs e, CancellationToken ct = default)
	{
		List<string> filmResourceKeys = new List<string>();
		List<Film> films = new List<Film>();
		XmlDocument xmlDocument = new XmlDocument();
		await Task.Run(() =>
		{
			xmlDocument.Load(e.InputStream);
		});
		IEnumerable<XmlNode> videoNodes = xmlDocument.SelectSingleNode("root").SelectNodes("video").OfType<XmlNode>();
		List<string> videoNames = new List<string>();
		foreach (XmlNode videoNode in videoNodes)
		{
			videoNames.Add(videoNode.InnerText);
		}
		// The videos are in the parent folder of the folder that holds this resource.
		string fullKeyPath = e.Resource.FullKeyPath;
		string parentKeyPath = fullKeyPath.Remove(fullKeyPath.LastIndexOf("/"));
		parentKeyPath = parentKeyPath.Remove(parentKeyPath.LastIndexOf("/"));
		foreach (string videoName in videoNames)
		{
			string filmResourceKey = parentKeyPath + "/" + videoName.ToUpper();
			filmResourceKeys.Add(filmResourceKey);
			films.Add(new Film(filmResourceKey));
		}
		e.PushDependencies(filmResourceKeys);
		return new FilmGroup(e.ResourceTree, filmResourceKeys, films)
		{
			Resource = e.Resource
		};
	}
}
