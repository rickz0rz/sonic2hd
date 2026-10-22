using System.Runtime.InteropServices;

namespace SonicOrca.Graphics.LowLevel;

[StructLayout(LayoutKind.Explicit, Pack = 1)]
[VertexAttributeType(VertexAttributePointerType.Float, 4)]
public struct vec4
{
	[FieldOffset(0)]
	public float x;

	[FieldOffset(4)]
	public float y;

	[FieldOffset(8)]
	public float z;

	[FieldOffset(12)]
	public float w;

	[FieldOffset(0)]
	public float s;

	[FieldOffset(4)]
	public float t;

	[FieldOffset(8)]
	public float p;

	[FieldOffset(12)]
	public float q;

	[FieldOffset(0)]
	public float r;

	[FieldOffset(4)]
	public float g;

	[FieldOffset(8)]
	public float b;

	[FieldOffset(12)]
	public float a;
}
