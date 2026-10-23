using System.Collections.Generic;
using System.Linq;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SonicOrca.Core.Debugging;

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
		int num = (int)(Context.Font.Height * 0.5);
		Rectangle rectangle = new Rectangle(0.0, 0.0, 1888.0, num);
		foreach (IEnumerable<KeyValuePair<string, object>> item in GetInformation())
		{
			Draw(renderer, rectangle, item.ToArray());
			rectangle.Y += num + 16;
		}
		return (int)rectangle.Y - 16;
	}

	private void Draw(Renderer renderer, Rectanglei bounds, KeyValuePair<string, object>[] horizontalInfos)
	{
		if (horizontalInfos.Length != 0)
		{
			int num = bounds.Width / horizontalInfos.Length;
			for (int i = 0; i < horizontalInfos.Length; i++)
			{
				int num2 = ((i != horizontalInfos.Length - 1) ? 32 : 0);
				int num3 = ((i != 0) ? 32 : 0);
				Rectanglei bounds2 = new Rectanglei(bounds.X + num * i + num3, bounds.Y, num - num3 - num2, bounds.Height);
				Draw(renderer, bounds2, horizontalInfos[i]);
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
