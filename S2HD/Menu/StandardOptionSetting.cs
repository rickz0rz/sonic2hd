using System;
using System.Collections.Generic;

#nullable disable
namespace S2HD.Menu;

/// <summary>
/// A spinner setting with a list of text values. Functions get and set the selected index.
/// </summary>
internal class StandardOptionSetting : ISpinnerSetting, ISetting
{
	private readonly string[] _valueStrings;
	private readonly Func<int> _getter;
	private readonly Action<int> _setter;

	public string Name { get; }

	public IReadOnlyList<string> Values => _valueStrings;

	public int SelectedIndex
	{
		get => _getter();
		set => _setter(value);
	}

	public StandardOptionSetting(string name, string[] values, Func<int> getter, Action<int> setter)
	{
		Name = name;
		_valueStrings = values;
		_getter = getter;
		_setter = setter;
	}

	public override string ToString() => $"{Name}: {Values[SelectedIndex]}";
}
