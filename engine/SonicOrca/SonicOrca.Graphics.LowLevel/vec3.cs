using System.Runtime.InteropServices;

namespace SonicOrca.Graphics.LowLevel;

/// <summary>
/// A vector of 3 floats with the memory layout of a GLSL vec3. The components have more than one name:
/// x, y and z, or s, t and p.
/// </summary>
[StructLayout(LayoutKind.Explicit, Pack = 1)]
[VertexAttributeType(VertexAttributePointerType.Float, 3)]
public struct vec3
{
	[FieldOffset(0)]
	public float x;

	[FieldOffset(4)]
	public float y;

	[FieldOffset(8)]
	public float z;

	[FieldOffset(0)]
	public float s;

	[FieldOffset(4)]
	public float t;

	[FieldOffset(8)]
	public float p;
}
