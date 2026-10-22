using System;
using SonicOrca.Geometry;

namespace SonicOrca.Graphics;

public interface IShaderProgram : IDisposable
{
	void Activate();

	int GetAttributeLocation(string name);

	void SetUniform(string name, int value);

	void SetUniform(string name, float value);

	void SetUniform(string name, double value);

	void SetUniform(string name, Vector2 value);

	void SetUniform(string name, Vector3 value);

	void SetUniform(string name, Vector4 value);

	void SetUniform(string name, Matrix4 value);

	void SetUniform(string name, Colour value);
}
