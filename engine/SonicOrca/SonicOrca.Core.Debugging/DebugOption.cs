using SonicOrca.Graphics;

namespace SonicOrca.Core.Debugging;

/// <summary>
/// The base class for one line in the debug menu.
/// </summary>
public class DebugOption
{
	private readonly DebugContext _context;

	private readonly string _page;

	private readonly string _category;

	private readonly bool _selectable;

	public DebugContext Context => _context;

	public string Page => _page;

	public string Category => _category;

	public bool Selectable => _selectable;

	public DebugOption(DebugContext context, string page, string category, bool selectable = true)
	{
		_context = context;
		_page = page;
		_category = category;
		_selectable = selectable;
	}

	public virtual void OnPressLeft()
	{
	}

	public virtual void OnPressRight()
	{
	}

	public virtual int Draw(Renderer renderer)
	{
		return 32;
	}
}
