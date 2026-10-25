using System;
using System.Collections.Generic;
using System.Linq;
using SonicOrca.Geometry;

namespace SonicOrca.Graphics;

/// <summary>
/// The base class for a vertex buffer. It also maps shader attribute names to attribute locations.
/// </summary>
public abstract class VertexBuffer : IDisposable
{
	private readonly Dictionary<string, int> _attributeLocations = new Dictionary<string, int>();

	public abstract IReadOnlyList<int> VectorCounts { get; }

	/// <summary>
	/// Gets the location of each named attribute from the shader program. Returns the vector counts, ordered by
	/// attribute location.
	/// </summary>
	protected IEnumerable<int> SetAttributeLocations(IShaderProgram shaderProgram, IEnumerable<string> names, IEnumerable<int> vectorCounts)
	{
		string[] nameArray = names.ToArray();
		int[] vectorCountArray = vectorCounts.ToArray();
		foreach (string name in nameArray)
		{
			_attributeLocations[name] = shaderProgram.GetAttributeLocation(name);
		}
		int[] vectorCountsByLocation = new int[vectorCountArray.Length];
		for (int i = 0; i < vectorCountsByLocation.Length; i++)
		{
			vectorCountsByLocation[_attributeLocations[nameArray[i]]] = vectorCountArray[i];
		}
		return vectorCountsByLocation;
	}

	public abstract void Dispose();

	public abstract void SetBufferData(int index, IEnumerable<double> data);

	public abstract void Begin();

	public abstract void End();

	public abstract void Render(PrimitiveType type);

	public abstract void Render(PrimitiveType type, int index, int count);

	public abstract void AddValue(int index, double value);

	public void AddValue(string name, double value)
	{
		AddValue(_attributeLocations[name], value);
	}

	public void AddValue(string name, Vector2 value)
	{
		AddValue(_attributeLocations[name], value);
	}

	public void AddValue(string name, Vector3 value)
	{
		AddValue(_attributeLocations[name], value);
	}

	public void AddValue(string name, Vector4 value)
	{
		AddValue(_attributeLocations[name], value);
	}

	public void AddValue(string name, Colour value)
	{
		AddValue(_attributeLocations[name], value);
	}

	public void AddValue(int index, Vector2 value)
	{
		AddValue(index, value.X);
		AddValue(index, value.Y);
	}

	public void AddValue(int index, Vector3 value)
	{
		AddValue(index, value.X);
		AddValue(index, value.Y);
		AddValue(index, value.Z);
	}

	public void AddValue(int index, Vector4 value)
	{
		AddValue(index, value.X);
		AddValue(index, value.Y);
		AddValue(index, value.Z);
		AddValue(index, value.W);
	}

	public void AddValue(int index, Colour value)
	{
		AddValue(index, value.Red / 255.0);
		AddValue(index, value.Green / 255.0);
		AddValue(index, value.Blue / 255.0);
		AddValue(index, value.Alpha / 255.0);
	}

	/// <summary>
	/// Adds one value to each attribute. The attribute index is the position of the value in the list.
	/// </summary>
	public void AddValues(params object[] values)
	{
		for (int i = 0; i < values.Length; i++)
		{
			if (values[i] is float f)
			{
				AddValue(i, (double)f);
			}
			else if (values[i] is double d)
			{
				AddValue(i, d);
			}
			else if (values[i] is Vector2)
			{
				AddValue(i, (Vector2)values[i]);
			}
			else if (values[i] is Vector3)
			{
				AddValue(i, (Vector3)values[i]);
			}
			else if (values[i] is Vector4)
			{
				AddValue(i, (Vector4)values[i]);
			}
			else if (values[i] is Colour)
			{
				AddValue(i, (Colour)values[i]);
			}
			else
			{
				AddValue(i, (int)values[i]);
			}
		}
	}
}
