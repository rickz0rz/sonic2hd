using System.Collections.Generic;
using System.Linq;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SonicOrca.Core.Debugging;

/// <summary>
/// A debug option that only shows rows of names and values. The user cannot select it.
/// </summary>
internal class InformationDebugOption : DebugOption
{
	public InformationDebugOption(DebugContext context, string page, string category)
		: base(context, page, category, selectable: false)
	{
	}

	protected virtual IEnumerable<IEnumerable<KeyValuePair<string, object>>> GetInformation()
	{
		yield return new KeyValuePair<string, object>[1]
		{
			new KeyValuePair<string, object>("UNDEFINED", "INFORMATION")
		};
	}

	public override int Draw(Renderer renderer)
	{
		int rowHeight = (int)(Context.Font.Height * DebugContext.DebugTextXSmall);
		Rectangle rowBounds = new Rectangle(0.0, 0.0, 1888.0, rowHeight);
		foreach (IEnumerable<KeyValuePair<string, object>> row in GetInformation())
		{
			Draw(renderer, rowBounds, row.ToArray());
			rowBounds.Y += rowHeight + 16;
		}
		return (int)rowBounds.Y - 16;
	}

	/// <summary>
	/// Draws one row. The row width is divided equally between the cells, with a gap between cells.
	/// </summary>
	private void Draw(Renderer renderer, Rectanglei bounds, KeyValuePair<string, object>[] horizontalInfos)
	{
		if (horizontalInfos.Length != 0)
		{
			int cellWidth = bounds.Width / horizontalInfos.Length;
			for (int i = 0; i < horizontalInfos.Length; i++)
			{
				int rightPadding = ((i != horizontalInfos.Length - 1) ? 32 : 0);
				int leftPadding = ((i != 0) ? 32 : 0);
				Rectanglei cellBounds = new Rectanglei(bounds.X + cellWidth * i + leftPadding, bounds.Y, cellWidth - leftPadding - rightPadding, bounds.Height);
				Draw(renderer, cellBounds, horizontalInfos[i]);
			}
		}
	}

	private void Draw(Renderer renderer, Rectanglei bounds, KeyValuePair<string, object> kvp)
	{
		if (!string.IsNullOrEmpty(kvp.Key))
		{
			Context.DrawText(renderer, kvp.Key.ToUpper() + ":", FontAlignment.Left, bounds.Left, bounds.Top, 0.5, 0);
		}
		Context.DrawText(renderer, kvp.Value.ToString().ToUpper(), FontAlignment.Right, bounds.Right, bounds.Top, 0.5, 0);
	}
}
