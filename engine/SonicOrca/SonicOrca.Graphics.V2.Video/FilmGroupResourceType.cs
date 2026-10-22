using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using SonicOrca.Resources;

namespace SonicOrca.Graphics.V2.Video;

public class FilmGroupResourceType : ResourceType
{
	public override string Name => "film, xml";

	public override string DefaultExtension => ".film.xml";

	public FilmGroupResourceType()
		: base(ResourceTypeIdentifier.FilmGroup)
	{
	}

	public override async Task<ILoadedResource> LoadAsync(ResourceLoadArgs e, CancellationToken ct = default(CancellationToken))
	{
		List<string> filmResourceKeys = new List<string>();
		List<Film> films = new List<Film>();
		XmlDocument xmlDocument = new XmlDocument();
		await Task.Run(() =>
		{
			xmlDocument.Load(e.InputStream);
		});
		IEnumerable<XmlNode> enumerable = xmlDocument.SelectSingleNode("root").SelectNodes("video").OfType<XmlNode>();
		List<string> list = new List<string>();
		foreach (XmlNode item in enumerable)
		{
			string innerText = item.InnerText;
			list.Add(innerText);
		}
		string fullKeyPath = e.Resource.FullKeyPath;
		string text = fullKeyPath.Remove(fullKeyPath.LastIndexOf("/"));
		text = text.Remove(text.LastIndexOf("/"));
		foreach (string item2 in list)
		{
			string text2 = text + "/" + item2.ToUpper();
			filmResourceKeys.Add(text2);
			films.Add(new Film(text2));
		}
		e.PushDependencies(filmResourceKeys);
		return new FilmGroup(e.ResourceTree, filmResourceKeys, films)
		{
			Resource = e.Resource
		};
	}
}
