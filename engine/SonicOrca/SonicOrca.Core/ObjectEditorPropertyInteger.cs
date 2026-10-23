namespace SonicOrca.Core;

/// <summary>
/// An editor property that holds an integer between a minimum and a maximum value.
/// </summary>
public class ObjectEditorPropertyInteger : ObjectEditorProperty
{
	private readonly int _minValue;

	private readonly int _maxValue;

	public int MinValue => _minValue;

	public int MaxValue => _maxValue;

	public ObjectEditorPropertyInteger(string name, string key, int minValue, int maxValue, int defaultValue = 0, string description = null)
		: base(name, key, typeof(int), defaultValue, description)
	{
		_minValue = minValue;
		_maxValue = maxValue;
	}

	public override bool Validate(ref object value)
	{
		int result;
		if (value is string text)
		{
			if (!int.TryParse(text, out result))
			{
				return false;
			}
		}
		else if (value is int integer)
		{
			result = integer;
		}
		else
		{
			return false;
		}
		value = MathX.Clamp(_minValue, result, _maxValue);
		return true;
	}
}
