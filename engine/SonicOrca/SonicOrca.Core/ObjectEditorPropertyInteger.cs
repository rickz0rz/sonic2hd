namespace SonicOrca.Core;

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
		if (value is string)
		{
			if (!int.TryParse((string)value, out result))
			{
				return false;
			}
		}
		else
		{
			if (!(value is int))
			{
				return false;
			}
			result = (int)value;
		}
		value = MathX.Clamp(_minValue, result, _maxValue);
		return true;
	}
}
