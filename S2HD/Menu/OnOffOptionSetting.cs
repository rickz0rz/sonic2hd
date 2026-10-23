using System;
using System.Collections.Generic;

namespace S2HD.Menu;

/// <summary>
/// A spinner setting with the values OFF and ON.
/// </summary>
internal class OnOffOptionSetting : ISpinnerSetting, ISetting
{
	private static readonly string[] ValueStrings = new string[2]
	{
		"OFF",
		"ON"
	};
	private readonly Func<bool> _getter;
	private readonly Action<bool> _setter;

	public string Name { get; }

	public IReadOnlyList<string> Values => ValueStrings;

	public int SelectedIndex
	{
		get => Value ? 1 : 0;
		set => Value = value != 0;
	}

	public bool Value
	{
		get => _getter();
		set => _setter(value);
	}

	public OnOffOptionSetting(string name, Func<bool> getter, Action<bool> setter)
	{
		Name = name;
		_getter = getter;
		_setter = setter;
	}

	public override string ToString() => $"{Name}: {Values[SelectedIndex]}";
}
