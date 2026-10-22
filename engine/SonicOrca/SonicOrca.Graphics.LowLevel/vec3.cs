using System.Runtime.InteropServices;

namespace SonicOrca.Graphics.LowLevel;

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
