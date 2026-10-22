using System.Runtime.InteropServices;

namespace SonicOrca.Graphics.LowLevel;

[StructLayout(LayoutKind.Explicit, Pack = 1)]
[VertexAttributeType(VertexAttributePointerType.Float, 2)]
public struct vec2
{
	[FieldOffset(0)]
	public float x;

	[FieldOffset(4)]
	public float y;

	[FieldOffset(0)]
	public float s;

	[FieldOffset(4)]
	public float t;
}
