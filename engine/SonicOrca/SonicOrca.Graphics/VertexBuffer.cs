using System;
using System.Collections.Generic;
using System.Linq;
using SonicOrca.Geometry;

namespace SonicOrca.Graphics;

public abstract class VertexBuffer : IDisposable
{
	private readonly Dictionary<string, int> _attributeLocations = new Dictionary<string, int>();

	public abstract IReadOnlyList<int> VectorCounts { get; }

	protected IEnumerable<int> SetAttributeLocations(IShaderProgram shaderProgram, IEnumerable<string> names, IEnumerable<int> vectorCounts)
	{
		string[] array = names.ToArray();
		int[] array2 = vectorCounts.ToArray();
		string[] array3 = array;
		foreach (string text in array3)
		{
			_attributeLocations[text] = shaderProgram.GetAttributeLocation(text);
		}
		int[] array4 = new int[array2.Length];
		for (int j = 0; j < array4.Length; j++)
		{
			array4[_attributeLocations[array[j]]] = array2[j];
		}
		return array4;
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
		AddValue(index, (double)(int)value.Red / 255.0);
		AddValue(index, (double)(int)value.Green / 255.0);
		AddValue(index, (double)(int)value.Blue / 255.0);
		AddValue(index, (double)(int)value.Alpha / 255.0);
	}

	public void AddValues(params object[] values)
	{
		for (int i = 0; i < values.Length; i++)
		{
			if (values[i] is float || values[i] is double)
			{
				AddValue(i, (double)values[i]);
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
				AddValue(i, (float)(int)values[i]);
			}
		}
	}
}
