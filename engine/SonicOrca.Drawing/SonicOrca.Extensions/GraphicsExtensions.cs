using System;
using System.Reflection;
using System.Runtime.InteropServices;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SonicOrca.Extensions;

/// <summary>
/// Gives helper methods for framebuffers, buffers and vertex arrays.
/// </summary>
public static class GraphicsExtensions
{
	public static Matrix4 CreateOrthographic(this IFramebuffer renderTarget)
	{
		return Matrix4.CreateOrthographicOffCenter(0.0, renderTarget.Width, renderTarget.Height, 0.0, 0.0, 1.0);
	}

	public static void SetData<T>(this IBuffer vbo, T[] data)
	{
		vbo.SetData(data, 0, data.Length);
	}

	public static void DefineAttributes(this IVertexArray vao, IShaderProgram program, IBuffer vbo, Type vertexType)
	{
		vao.Bind();
		vbo.Bind();
		int stride = Marshal.SizeOf(vertexType);
		foreach (FieldInfo fieldInfo in vertexType.GetFields())
		{
			VertexAttributeAttribute vertexAttribute = fieldInfo.GetCustomAttribute<VertexAttributeAttribute>();
			if (vertexAttribute == null)
			{
				continue;
			}
			Type fieldType = fieldInfo.FieldType;
			// Use the type attribute of the field type. Use a default for float and int fields.
			VertexAttributeTypeAttribute attributeType = fieldType.GetCustomAttribute<VertexAttributeTypeAttribute>();
			if (attributeType == null)
			{
				if (fieldType == typeof(float))
				{
					attributeType = new VertexAttributeTypeAttribute(VertexAttributePointerType.Float, 1);
				}
				else if (fieldType == typeof(int))
				{
					attributeType = new VertexAttributeTypeAttribute(VertexAttributePointerType.Int, 1);
				}
			}
			int offset = (int)Marshal.OffsetOf(vertexType, fieldInfo.Name);
			vao.DefineAttribute(program.GetAttributeLocation(vertexAttribute.Name), attributeType.Type, attributeType.Size, stride, offset);
		}
	}

	public static void Render<T>(this IVertexArray vao, PrimitiveType type, T[] vertices)
	{
		vao.Render(type, 0, vertices.Length);
	}
}
