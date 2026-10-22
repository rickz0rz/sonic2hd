using System.Collections.Generic;
using System.Linq;
using SonicOrca.Graphics;

namespace SonicOrca.Core.Debugging;

internal class DiscreteDebugOption<T> : DebugOption
{
	private readonly string _name;

	private readonly KeyValuePair<string, T>[] _values;

	private int _selectedIndex;

	public T SelectedValue
	{
		get
		{
			return _values[_selectedIndex].Value;
		}
		set
		{
			for (int i = 0; i < _values.Length; i++)
			{
				if (_values[i].Value.Equals(value))
				{
					_selectedIndex = i;
					break;
				}
			}
		}
	}

	public DiscreteDebugOption(DebugContext context, string page, string category, string name, IEnumerable<KeyValuePair<string, T>> values)
		: base(context, page, category)
	{
		_name = name;
		_values = values.ToArray();
	}

	public override void OnPressLeft()
	{
		if (_selectedIndex > 0)
		{
			_selectedIndex--;
			OnChange();
			Context.PlayFocusSound();
		}
	}

	public override void OnPressRight()
	{
		if (_selectedIndex < _values.Length - 1)
		{
			_selectedIndex++;
			OnChange();
			Context.PlayFocusSound();
		}
	}

	public virtual void OnChange()
	{
	}

	public override int Draw(Renderer renderer)
	{
		Context.DrawText(renderer, _name, FontAlignment.Left, 0.0, 0.0, 0.5, (Context.CurrentOption == this) ? 1 : 0);
		int num = 1888;
		int num2 = _values.Length - 1;
		foreach (KeyValuePair<string, T> item in _values.Reverse())
		{
			bool flag = num2 == _selectedIndex;
			Context.DrawText(renderer, item.Key, FontAlignment.Right, num, 0.0, 0.5, flag ? 1 : 0);
			num += -(int)(Context.Font.MeasureString(item.Key).Width * 0.5) - 16;
			num2--;
		}
		return (int)((double)Context.Font.Height * 0.5);
	}
}
