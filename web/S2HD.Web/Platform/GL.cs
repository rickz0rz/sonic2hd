using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.JavaScript;
using System.Text.RegularExpressions;
using OpenTK.Graphics.OpenGL;

namespace SonicOrca.SDL2;

/// <summary>
/// WebGL2 implementation of the OpenGL calls used by the shared GL backend (engine/SonicOrca.SDL2/GL*.cs).
/// Those files are compiled into the web build unchanged; inside namespace SonicOrca.SDL2 this class takes
/// precedence over OpenTK's GL. WebGL2 uses the same enum values as OpenGL, so OpenTK's enums pass straight through.
/// GL objects are JavaScript objects in WebGL; webgl.js maps them to integer handles.
/// </summary>
internal static unsafe partial class GL
{
	private const string Module = "webgl";

	// GLSL 3.30 -> GLSL ES 3.00. The shaders avoid implicit int->float conversions, so only the header changes.
	private static readonly Regex VersionDirective = new Regex(@"^﻿?\s*#version\s+330[^\n]*", RegexOptions.Compiled);

	private const string EsHeader = "#version 300 es\nprecision highp float;\nprecision highp int;\nprecision highp sampler2D;";

	// State

	public static void Enable(EnableCap cap) => Js.Enable((int)cap);

	public static void Disable(EnableCap cap) => Js.Disable((int)cap);

	public static bool IsEnabled(EnableCap cap) => Js.IsEnabled((int)cap);

	public static void BlendFunc(BlendingFactor source, BlendingFactor destination) => Js.BlendFunc((int)source, (int)destination);

	public static void Viewport(int x, int y, int width, int height) => Js.Viewport(x, y, width, height);

	public static void ClearColor(float red, float green, float blue, float alpha) => Js.ClearColor(red, green, blue, alpha);

	public static void Clear(ClearBufferMask mask) => Js.Clear((int)mask);

	/// <summary>The desktop code clears colour attachments with integer zeros; WebGL requires the float variant.</summary>
	public static void ClearBuffer(OpenTK.Graphics.OpenGL.ClearBuffer buffer, int drawBuffer, int[] value) => Js.ClearBufferColor(drawBuffer);

	/// <summary>Wireframe debug mode; WebGL has no polygon modes.</summary>
	public static void PolygonMode(TriangleFace face, OpenTK.Graphics.OpenGL.PolygonMode mode)
	{
	}

	public static void StencilFunc(StencilFunction function, int reference, int mask) => Js.StencilFunc((int)function, reference, mask);

	public static void StencilOp(OpenTK.Graphics.OpenGL.StencilOp stencilFail, OpenTK.Graphics.OpenGL.StencilOp depthFail, OpenTK.Graphics.OpenGL.StencilOp pass) => Js.StencilOp((int)stencilFail, (int)depthFail, (int)pass);

	public static void StencilMask(int mask) => Js.StencilMask(mask);

	public static void GetInteger(GetPName name, out int data)
	{
		data = name == GetPName.PolygonMode ? (int)OpenTK.Graphics.OpenGL.PolygonMode.Fill : Js.GetInteger((int)name);
	}

	public static string GetString(StringName name) => Js.GetString((int)name);

	public static void Finish() => Js.Finish();

	// Buffers

	public static int GenBuffer() => Js.CreateBuffer();

	public static void GenBuffers(int count, int[] buffers)
	{
		for (int i = 0; i < count; i++)
			buffers[i] = Js.CreateBuffer();
	}

	public static void DeleteBuffer(int buffer) => Js.DeleteBuffer(buffer);

	public static void DeleteBuffers(int count, int[] buffers)
	{
		for (int i = 0; i < count; i++)
			Js.DeleteBuffer(buffers[i]);
	}

	public static void BindBuffer(BufferTarget target, int buffer) => Js.BindBuffer((int)target, buffer);

	public static void BufferData(BufferTarget target, IntPtr size, IntPtr data, BufferUsageHint usage)
	{
		Js.BufferData((int)target, new Span<byte>((void*)data, (int)size), (int)usage);
	}

	public static void BufferData(BufferTarget target, IntPtr size, float[] data, BufferUsageHint usage)
	{
		Js.BufferData((int)target, MemoryMarshal.AsBytes(data.AsSpan()).Slice(0, (int)size), (int)usage);
	}

	public static void BufferData(BufferTarget target, int size, uint[] data, BufferUsageHint usage)
	{
		Js.BufferData((int)target, MemoryMarshal.AsBytes(data.AsSpan()).Slice(0, size), (int)usage);
	}

	// Vertex arrays and drawing

	public static int GenVertexArray() => Js.CreateVertexArray();

	public static void GenVertexArrays(int count, out int array)
	{
		array = Js.CreateVertexArray();
	}

	public static void DeleteVertexArray(int array) => Js.DeleteVertexArray(array);

	public static void DeleteVertexArrays(int count, int[] arrays)
	{
		for (int i = 0; i < count; i++)
			Js.DeleteVertexArray(arrays[i]);
	}

	public static void BindVertexArray(int array) => Js.BindVertexArray(array);

	public static void VertexAttribPointer(int index, int size, VertexAttribPointerType type, bool normalized, int stride, int offset)
	{
		Js.VertexAttribPointer(index, size, (int)type, normalized, stride, offset);
	}

	public static void EnableVertexAttribArray(int index) => Js.EnableVertexAttribArray(index);

	public static void DrawArrays(PrimitiveType mode, int first, int count) => Js.DrawArrays((int)mode, first, count);

	public static void DrawElements(PrimitiveType mode, int count, DrawElementsType type, IntPtr offset)
	{
		Js.DrawElements((int)mode, count, (int)type, (int)offset);
	}

	// Textures

	public static int GenTexture() => Js.CreateTexture();

	public static void DeleteTexture(int texture) => Js.DeleteTexture(texture);

	public static void BindTexture(TextureTarget target, int texture) => Js.BindTexture((int)target, texture);

	public static void ActiveTexture(TextureUnit unit) => Js.ActiveTexture((int)unit);

	public static void TexParameter(TextureTarget target, TextureParameterName name, int value)
	{
		Js.TexParameter((int)target, (int)name, value);
	}

	/// <summary>
	/// Uploads 8-bit pixels. BGR(A) data is swizzled and RGB expanded to RGBA in webgl.js; compressed internal
	/// formats (the desktop asks the driver to compress some textures) are uploaded uncompressed.
	/// </summary>
	public static void TexImage2D(TextureTarget target, int level, PixelInternalFormat internalFormat, int width, int height, int border, PixelFormat format, PixelType type, byte[] pixels)
	{
		Js.TexImage2D((int)target, level, width, height, (int)format, pixels != null, pixels ?? Array.Empty<byte>());
	}

	/// <summary>Reading textures back is only used by debug tools; WebGL2 has no glGetTexImage.</summary>
	public static void GetTexImage(TextureTarget target, int level, PixelFormat format, PixelType type, byte[] pixels)
	{
		throw new NotSupportedException("Reading textures back is not supported in WebGL.");
	}

	// Framebuffers

	public static void GenFramebuffers(int count, out int framebuffer)
	{
		framebuffer = Js.CreateFramebuffer();
	}

	public static void GenFramebuffers(int count, out uint framebuffer)
	{
		framebuffer = (uint)Js.CreateFramebuffer();
	}

	public static void DeleteFramebuffer(int framebuffer) => Js.DeleteFramebuffer(framebuffer);

	public static void DeleteFramebuffer(uint framebuffer) => Js.DeleteFramebuffer((int)framebuffer);

	public static void BindFramebuffer(FramebufferTarget target, int framebuffer) => Js.BindFramebuffer((int)target, framebuffer);

	public static void BindFramebuffer(FramebufferTarget target, uint framebuffer) => Js.BindFramebuffer((int)target, (int)framebuffer);

	public static FramebufferErrorCode CheckFramebufferStatus(FramebufferTarget target) => (FramebufferErrorCode)Js.CheckFramebufferStatus((int)target);

	/// <summary>glFramebufferTexture is desktop GL 3.2; WebGL2 attaches 2D textures with framebufferTexture2D.</summary>
	public static void FramebufferTexture(FramebufferTarget target, FramebufferAttachment attachment, int texture, int level)
	{
		Js.FramebufferTexture2D((int)target, (int)attachment, texture, level);
	}

	public static void DrawBuffers(int count, DrawBuffersEnum[] buffers)
	{
		Span<int> values = stackalloc int[count];
		for (int i = 0; i < count; i++)
			values[i] = (int)buffers[i];
		Js.DrawBuffers(MemoryMarshal.AsBytes(values));
	}

	public static int GenRenderbuffer() => Js.CreateRenderbuffer();

	public static void BindRenderbuffer(RenderbufferTarget target, int renderbuffer) => Js.BindRenderbuffer((int)target, renderbuffer);

	public static void RenderbufferStorage(RenderbufferTarget target, OpenTK.Graphics.OpenGL.RenderbufferStorage format, int width, int height)
	{
		Js.RenderbufferStorage((int)target, (int)format, width, height);
	}

	public static void FramebufferRenderbuffer(FramebufferTarget target, FramebufferAttachment attachment, RenderbufferTarget renderbufferTarget, int renderbuffer)
	{
		Js.FramebufferRenderbuffer((int)target, (int)attachment, (int)renderbufferTarget, renderbuffer);
	}

	// Shaders and programs

	public static int CreateShader(ShaderType type) => Js.CreateShader((int)type);

	public static void ShaderSource(int shader, string source)
	{
		Js.ShaderSource(shader, VersionDirective.Replace(source, EsHeader, 1));
	}

	public static void CompileShader(int shader) => Js.CompileShader(shader);

	public static void GetShader(int shader, ShaderParameter name, out int value)
	{
		value = Js.GetShaderParameter(shader, (int)name);
	}

	public static string GetShaderInfoLog(int shader) => Js.GetShaderInfoLog(shader);

	public static void DeleteShader(int shader) => Js.DeleteShader(shader);

	public static int CreateProgram() => Js.CreateProgram();

	public static void AttachShader(int program, int shader) => Js.AttachShader(program, shader);

	public static void LinkProgram(int program) => Js.LinkProgram(program);

	public static void GetProgram(int program, GetProgramParameterName name, out int value)
	{
		value = Js.GetProgramParameter(program, (int)name);
	}

	public static string GetProgramInfoLog(int program) => Js.GetProgramInfoLog(program);

	public static void DeleteProgram(int program) => Js.DeleteProgram(program);

	public static void UseProgram(int program) => Js.UseProgram(program);

	public static int GetAttribLocation(int program, string name) => Js.GetAttribLocation(program, name);

	public static int GetUniformLocation(int program, string name) => Js.GetUniformLocation(program, name);

	public static void Uniform1(int location, int value) => Js.Uniform1i(location, value);

	public static void Uniform1(int location, float value) => Js.Uniform1f(location, value);

	public static void Uniform2(int location, float x, float y) => Js.Uniform2f(location, x, y);

	public static void Uniform3(int location, float x, float y, float z) => Js.Uniform3f(location, x, y, z);

	public static void Uniform4(int location, float x, float y, float z, float w) => Js.Uniform4f(location, x, y, z, w);

	public static void UniformMatrix4(int location, int count, bool transpose, float[] value)
	{
		Js.UniformMatrix4fv(location, transpose, MemoryMarshal.AsBytes(value.AsSpan()));
	}

	private static partial class Js
	{
		[JSImport("enable", Module)] public static partial void Enable(int cap);
		[JSImport("disable", Module)] public static partial void Disable(int cap);
		[JSImport("isEnabled", Module)] public static partial bool IsEnabled(int cap);
		[JSImport("blendFunc", Module)] public static partial void BlendFunc(int source, int destination);
		[JSImport("viewport", Module)] public static partial void Viewport(int x, int y, int width, int height);
		[JSImport("clearColor", Module)] public static partial void ClearColor(float red, float green, float blue, float alpha);
		[JSImport("clear", Module)] public static partial void Clear(int mask);
		[JSImport("clearBufferColor", Module)] public static partial void ClearBufferColor(int drawBuffer);
		[JSImport("stencilFunc", Module)] public static partial void StencilFunc(int function, int reference, int mask);
		[JSImport("stencilOp", Module)] public static partial void StencilOp(int stencilFail, int depthFail, int pass);
		[JSImport("stencilMask", Module)] public static partial void StencilMask(int mask);
		[JSImport("getInteger", Module)] public static partial int GetInteger(int name);
		[JSImport("getString", Module)] public static partial string GetString(int name);
		[JSImport("finish", Module)] public static partial void Finish();
		[JSImport("createBuffer", Module)] public static partial int CreateBuffer();
		[JSImport("deleteBuffer", Module)] public static partial void DeleteBuffer(int buffer);
		[JSImport("bindBuffer", Module)] public static partial void BindBuffer(int target, int buffer);
		[JSImport("bufferData", Module)] public static partial void BufferData(int target, [JSMarshalAs<JSType.MemoryView>] Span<byte> data, int usage);
		[JSImport("createVertexArray", Module)] public static partial int CreateVertexArray();
		[JSImport("deleteVertexArray", Module)] public static partial void DeleteVertexArray(int array);
		[JSImport("bindVertexArray", Module)] public static partial void BindVertexArray(int array);
		[JSImport("vertexAttribPointer", Module)] public static partial void VertexAttribPointer(int index, int size, int type, bool normalized, int stride, int offset);
		[JSImport("enableVertexAttribArray", Module)] public static partial void EnableVertexAttribArray(int index);
		[JSImport("drawArrays", Module)] public static partial void DrawArrays(int mode, int first, int count);
		[JSImport("drawElements", Module)] public static partial void DrawElements(int mode, int count, int type, int offset);
		[JSImport("createTexture", Module)] public static partial int CreateTexture();
		[JSImport("deleteTexture", Module)] public static partial void DeleteTexture(int texture);
		[JSImport("bindTexture", Module)] public static partial void BindTexture(int target, int texture);
		[JSImport("activeTexture", Module)] public static partial void ActiveTexture(int unit);
		[JSImport("texParameter", Module)] public static partial void TexParameter(int target, int name, int value);
		[JSImport("texImage2D", Module)] public static partial void TexImage2D(int target, int level, int width, int height, int format, bool hasPixels, [JSMarshalAs<JSType.MemoryView>] Span<byte> pixels);
		[JSImport("createFramebuffer", Module)] public static partial int CreateFramebuffer();
		[JSImport("deleteFramebuffer", Module)] public static partial void DeleteFramebuffer(int framebuffer);
		[JSImport("bindFramebuffer", Module)] public static partial void BindFramebuffer(int target, int framebuffer);
		[JSImport("checkFramebufferStatus", Module)] public static partial int CheckFramebufferStatus(int target);
		[JSImport("framebufferTexture2D", Module)] public static partial void FramebufferTexture2D(int target, int attachment, int texture, int level);
		[JSImport("drawBuffers", Module)] public static partial void DrawBuffers([JSMarshalAs<JSType.MemoryView>] Span<byte> buffers);
		[JSImport("createRenderbuffer", Module)] public static partial int CreateRenderbuffer();
		[JSImport("bindRenderbuffer", Module)] public static partial void BindRenderbuffer(int target, int renderbuffer);
		[JSImport("renderbufferStorage", Module)] public static partial void RenderbufferStorage(int target, int format, int width, int height);
		[JSImport("framebufferRenderbuffer", Module)] public static partial void FramebufferRenderbuffer(int target, int attachment, int renderbufferTarget, int renderbuffer);
		[JSImport("createShader", Module)] public static partial int CreateShader(int type);
		[JSImport("shaderSource", Module)] public static partial void ShaderSource(int shader, string source);
		[JSImport("compileShader", Module)] public static partial void CompileShader(int shader);
		[JSImport("getShaderParameter", Module)] public static partial int GetShaderParameter(int shader, int name);
		[JSImport("getShaderInfoLog", Module)] public static partial string GetShaderInfoLog(int shader);
		[JSImport("deleteShader", Module)] public static partial void DeleteShader(int shader);
		[JSImport("createProgram", Module)] public static partial int CreateProgram();
		[JSImport("attachShader", Module)] public static partial void AttachShader(int program, int shader);
		[JSImport("linkProgram", Module)] public static partial void LinkProgram(int program);
		[JSImport("getProgramParameter", Module)] public static partial int GetProgramParameter(int program, int name);
		[JSImport("getProgramInfoLog", Module)] public static partial string GetProgramInfoLog(int program);
		[JSImport("deleteProgram", Module)] public static partial void DeleteProgram(int program);
		[JSImport("useProgram", Module)] public static partial void UseProgram(int program);
		[JSImport("getAttribLocation", Module)] public static partial int GetAttribLocation(int program, string name);
		[JSImport("getUniformLocation", Module)] public static partial int GetUniformLocation(int program, string name);
		[JSImport("uniform1i", Module)] public static partial void Uniform1i(int location, int value);
		[JSImport("uniform1f", Module)] public static partial void Uniform1f(int location, float value);
		[JSImport("uniform2f", Module)] public static partial void Uniform2f(int location, float x, float y);
		[JSImport("uniform3f", Module)] public static partial void Uniform3f(int location, float x, float y, float z);
		[JSImport("uniform4f", Module)] public static partial void Uniform4f(int location, float x, float y, float z, float w);
		[JSImport("uniformMatrix4fv", Module)] public static partial void UniformMatrix4fv(int location, bool transpose, [JSMarshalAs<JSType.MemoryView>] Span<byte> value);
	}
}
