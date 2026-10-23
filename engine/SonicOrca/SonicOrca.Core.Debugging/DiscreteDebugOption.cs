using System.Collections.Generic;
using System.Linq;
using SonicOrca.Graphics;

namespace SonicOrca.Core.Debugging;

/// <summary>
/// A debug option with a fixed list of values. The left and right keys select a value.
/// </summary>
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
		Context.DrawText(renderer, _name, FontAlignment.Left, 0.0, 0.0, DebugContext.DebugTextXSmall, (Context.CurrentOption == this) ? DebugContext.DebugTextGold : DebugContext.DebugTextSilver);
		// Draw the values from right to left, with the last value at the right edge.
		int right = 1888;
		int valueIndex = _values.Length - 1;
		foreach (KeyValuePair<string, T> item in _values.Reverse())
		{
			bool isSelected = valueIndex == _selectedIndex;
			Context.DrawText(renderer, item.Key, FontAlignment.Right, right, 0.0, DebugContext.DebugTextXSmall, isSelected ? DebugContext.DebugTextGold : DebugContext.DebugTextSilver);
			right += -(int)(Context.Font.MeasureString(item.Key).Width * 0.5) - 16;
			valueIndex--;
		}
		return (int)(Context.Font.Height * DebugContext.DebugTextXSmall);
	}
}
