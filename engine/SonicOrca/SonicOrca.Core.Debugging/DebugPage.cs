using System.Collections.Generic;
using SonicOrca.Graphics;

namespace SonicOrca.Core.Debugging;

/// <summary>
/// One page of the debug menu. It draws its options in groups, with a heading for each category.
/// </summary>
internal class DebugPage
{
	private readonly DebugContext _context;

	private readonly string _name;

	private readonly List<DebugOption> _options = new List<DebugOption>();

	public string Name => _name;

	public IList<DebugOption> Options => _options;

	public DebugPage(DebugContext context, string name)
	{
		_context = context;
		_name = name;
	}

	public DebugPage(DebugContext context, string name, IEnumerable<DebugOption> options)
	{
		_context = context;
		_name = name;
		_options.AddRange(options);
	}

	public void Draw(Renderer renderer)
	{
		string currentCategory = string.Empty;
		I2dRenderer i2dRenderer = renderer.Get2dRenderer();
		using (i2dRenderer.BeginMatixState())
		{
			foreach (DebugOption option in _options)
			{
				if (option.Category != currentCategory)
				{
					currentCategory = option.Category;
					i2dRenderer.ModelMatrix = i2dRenderer.ModelMatrix.Translate(0.0, 32.0);
					int headingHeight = (int)_context.DrawText(renderer, currentCategory, FontAlignment.Left, 0.0, 0.0, DebugContext.DebugTextSmall, DebugContext.DebugTextSilver);
					i2dRenderer.ModelMatrix = i2dRenderer.ModelMatrix.Translate(0.0, headingHeight + 16);
				}
				i2dRenderer.ModelMatrix = i2dRenderer.ModelMatrix.Translate(0.0, option.Draw(renderer) + 16);
			}
		}
	}
}
