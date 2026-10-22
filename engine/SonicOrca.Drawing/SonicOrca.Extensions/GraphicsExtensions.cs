using System;
using System.Reflection;
using System.Runtime.InteropServices;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SonicOrca.Extensions;

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
		FieldInfo[] fields = vertexType.GetFields();
		foreach (FieldInfo fieldInfo in fields)
		{
			VertexAttributeAttribute customAttribute = fieldInfo.GetCustomAttribute<VertexAttributeAttribute>();
			if (customAttribute == null)
			{
				continue;
			}
			Type fieldType = fieldInfo.FieldType;
			VertexAttributeTypeAttribute vertexAttributeTypeAttribute = fieldType.GetCustomAttribute<VertexAttributeTypeAttribute>();
			if (vertexAttributeTypeAttribute == null)
			{
				if (fieldType == typeof(float))
				{
					vertexAttributeTypeAttribute = new VertexAttributeTypeAttribute(VertexAttributePointerType.Float, 1);
				}
				else if (fieldType == typeof(int))
				{
					vertexAttributeTypeAttribute = new VertexAttributeTypeAttribute(VertexAttributePointerType.Int, 1);
				}
			}
			int offset = (int)Marshal.OffsetOf(vertexType, fieldInfo.Name);
			vao.DefineAttribute(program.GetAttributeLocation(customAttribute.Name), vertexAttributeTypeAttribute.Type, vertexAttributeTypeAttribute.Size, stride, offset);
		}
	}

	public static void Render<T>(this IVertexArray vao, PrimitiveType type, T[] vertices)
	{
		vao.Render(type, 0, vertices.Length);
	}
}
