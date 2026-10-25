using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Geometry;

namespace SonicOrca.Core.Editor;

/// <summary>
/// A state variable of an object type that the level editor shows. The editor reads the value from the behaviour
/// of a placement, and writes a changed value back to the behaviour. A value that is the same as the default value
/// is not written.
/// </summary>
public sealed class EditorProperty
{
	private static readonly ConcurrentDictionary<Type, IReadOnlyList<EditorProperty>> Cache = new ConcurrentDictionary<Type, IReadOnlyList<EditorProperty>>();

	public string Name { get; }

	public Type Type { get; }

	public object DefaultValue { get; }

	/// <summary>The editor can change values of this type. Other values, for example references to objects, are shown only.</summary>
	public bool IsEditable => Type == typeof(int) || Type == typeof(uint) || Type == typeof(double) || Type == typeof(bool) ||
		Type == typeof(string) || Type.IsEnum || Type == typeof(Vector2i) || Type == typeof(Vector2);

	private EditorProperty(string name, Type type, object defaultValue)
	{
		Name = name;
		Type = type;
		DefaultValue = defaultValue;
	}

	/// <summary>Gives the state variables of the instances of an object type, in the order of the instance class.</summary>
	public static IReadOnlyList<EditorProperty> For(ObjectType objectType)
	{
		ObjectInstanceAttribute instanceAttribute = ObjectInstanceAttribute.FromObject(objectType);
		if (instanceAttribute == null)
		{
			return Array.Empty<EditorProperty>();
		}
		return Cache.GetOrAdd(instanceAttribute.ObjectInstanceType, instanceType =>
			StateVariableAttribute.GetStateVariables(instanceType)
				.Select(x => new EditorProperty(x.Item2.Name, x.Item2.Type, x.Item2.DefaultValue))
				.ToArray());
	}

	/// <summary>Gives the value of the property in the behaviour of a placement, or the default value.</summary>
	public object GetValue(ObjectPlacement placement)
	{
		KeyValuePair<string, object> pair = placement.Behaviour.FirstOrDefault(x => x.Key == Name);
		if (pair.Key == null)
		{
			return DefaultValue;
		}
		if (pair.Value is IEnumerable<KeyValuePair<string, object>> components)
		{
			// A vector is stored as X and Y values
			string x = Convert.ToString(components.FirstOrDefault(c => c.Key == "X").Value, CultureInfo.InvariantCulture);
			string y = Convert.ToString(components.FirstOrDefault(c => c.Key == "Y").Value, CultureInfo.InvariantCulture);
			return LevelBindingResourceType.ParseBehaviourValue($"{{X={x}, Y={y}}}", Type);
		}
		try
		{
			return LevelBindingResourceType.ParseBehaviourValue(Convert.ToString(pair.Value, CultureInfo.InvariantCulture), Type);
		}
		catch (ArgumentException)
		{
			return DefaultValue;
		}
	}

	/// <summary>Makes a copy of the placement with a new value for this property.</summary>
	public ObjectPlacement SetValue(ObjectPlacement placement, object value)
	{
		List<KeyValuePair<string, object>> behaviour = placement.Behaviour.Where(x => x.Key != Name).ToList();
		if (!ValueEquals(value, DefaultValue))
		{
			behaviour.Add(new KeyValuePair<string, object>(Name, ToBehaviourValue(value)));
		}
		return placement.With(placement.Uid, placement.Position, placement.Layer, behaviour);
	}

	/// <summary>Gives the text that the editor shows for a value.</summary>
	public static string Format(object value)
	{
		return value switch
		{
			null => "-",
			double d => d.ToString("0.###", CultureInfo.InvariantCulture),
			Vector2i v => $"{v.X}, {v.Y}",
			Vector2 v => $"{v.X.ToString("0.###", CultureInfo.InvariantCulture)}, {v.Y.ToString("0.###", CultureInfo.InvariantCulture)}",
			bool b => b ? "Yes" : "No",
			ActiveObject => "(object)",
			_ => Convert.ToString(value, CultureInfo.InvariantCulture),
		};
	}

	/// <summary>Reads a value from the text that the user typed.</summary>
	public bool TryParse(string text, out object value)
	{
		value = null;
		text = text.Trim();
		if (Type == typeof(int) && int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int i))
		{
			value = i;
		}
		else if (Type == typeof(uint) && uint.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out uint u))
		{
			value = u;
		}
		else if (Type == typeof(double) && double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double d))
		{
			value = d;
		}
		else if (Type == typeof(string))
		{
			value = text;
		}
		else if (Type.IsEnum && Enum.TryParse(Type, text, ignoreCase: true, out object e) && Enum.IsDefined(Type, e))
		{
			value = e;
		}
		else if (Type == typeof(bool))
		{
			if (bool.TryParse(text, out bool b))
			{
				value = b;
			}
			else if (text.Equals("yes", StringComparison.OrdinalIgnoreCase) || text.Equals("no", StringComparison.OrdinalIgnoreCase))
			{
				value = text.Equals("yes", StringComparison.OrdinalIgnoreCase);
			}
		}
		else if (Type == typeof(Vector2i) || Type == typeof(Vector2))
		{
			string[] parts = text.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
			if (parts.Length == 2 &&
				double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out double x) &&
				double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double y))
			{
				value = Type == typeof(Vector2i) ? (object)new Vector2i((int)Math.Round(x), (int)Math.Round(y)) : (object)new Vector2(x, y);
			}
		}
		return value != null;
	}

	/// <summary>
	/// Gives the next value for a click on the property: the opposite value of a yes or no, and the next or previous
	/// value of an enumeration. Other types return null, and the user types the value.
	/// </summary>
	public object Step(object value, int direction)
	{
		if (Type == typeof(bool))
		{
			return !(value is bool b && b);
		}
		if (Type.IsEnum)
		{
			Array values = Enum.GetValues(Type);
			int index = Array.IndexOf(values, value);
			return values.GetValue(((index + direction) % values.Length + values.Length) % values.Length);
		}
		return null;
	}

	private static object ToBehaviourValue(object value)
	{
		return value switch
		{
			Vector2i v => new List<KeyValuePair<string, object>>
			{
				new KeyValuePair<string, object>("X", v.X),
				new KeyValuePair<string, object>("Y", v.Y)
			},
			Vector2 v => new List<KeyValuePair<string, object>>
			{
				new KeyValuePair<string, object>("X", v.X),
				new KeyValuePair<string, object>("Y", v.Y)
			},
			_ => value,
		};
	}

	private static bool ValueEquals(object a, object b)
	{
		if (a == null || b == null)
		{
			return a == null && b == null;
		}
		return a.Equals(b) || (a.GetType() == b.GetType() && Format(a) == Format(b));
	}

	public override string ToString() => $"{Name} : {Type.Name}";
}
