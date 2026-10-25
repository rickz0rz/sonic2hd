using System;
using System.Text;

namespace SonicOrca.Geometry;

/// <summary>
/// A 4 x 4 transformation matrix with double precision.
/// </summary>
/// <remarks>
/// The indexer and the Row properties use the field Mxy as the element in column x and row y.
/// <see cref="CreateTranslation(double, double, double)"/> puts the translation in M14, M24 and M34.
/// </remarks>
public struct Matrix4 : IEquatable<Matrix4>
{
	private const int Dimension = 4;

	public double M11;

	public double M21;

	public double M31;

	public double M41;

	public double M12;

	public double M22;

	public double M32;

	public double M42;

	public double M13;

	public double M23;

	public double M33;

	public double M43;

	public double M14;

	public double M24;

	public double M34;

	public double M44;

	public static Matrix4 Identity = new Matrix4(Vector4.UnitX, Vector4.UnitY, Vector4.UnitZ, Vector4.UnitW);

	public Vector4 Row1
	{
		get
		{
			return new Vector4(M11, M21, M31, M41);
		}
		set
		{
			M11 = value.X;
			M21 = value.Y;
			M31 = value.Z;
			M41 = value.W;
		}
	}

	public Vector4 Row2
	{
		get
		{
			return new Vector4(M12, M22, M32, M42);
		}
		set
		{
			M12 = value.X;
			M22 = value.Y;
			M32 = value.Z;
			M42 = value.W;
		}
	}

	public Vector4 Row3
	{
		get
		{
			return new Vector4(M13, M23, M33, M43);
		}
		set
		{
			M13 = value.X;
			M23 = value.Y;
			M33 = value.Z;
			M43 = value.W;
		}
	}

	public Vector4 Row4
	{
		get
		{
			return new Vector4(M14, M24, M34, M44);
		}
		set
		{
			M14 = value.X;
			M24 = value.Y;
			M34 = value.Z;
			M44 = value.W;
		}
	}

	/// <summary>
	/// Gets or sets the element in column <paramref name="x"/> and row <paramref name="y"/>, both from 0 to 3.
	/// </summary>
	public double this[int x, int y]
	{
		get
		{
			return (x, y) switch
			{
				(0, 0) => M11,
				(1, 0) => M21,
				(2, 0) => M31,
				(3, 0) => M41,
				(0, 1) => M12,
				(1, 1) => M22,
				(2, 1) => M32,
				(3, 1) => M42,
				(0, 2) => M13,
				(1, 2) => M23,
				(2, 2) => M33,
				(3, 2) => M43,
				(0, 3) => M14,
				(1, 3) => M24,
				(2, 3) => M34,
				(3, 3) => M44,
				_ => throw new ArgumentOutOfRangeException(),
			};
		}
		set
		{
			switch (x, y)
			{
				case (0, 0):
					M11 = value;
					break;
				case (1, 0):
					M21 = value;
					break;
				case (2, 0):
					M31 = value;
					break;
				case (3, 0):
					M41 = value;
					break;
				case (0, 1):
					M12 = value;
					break;
				case (1, 1):
					M22 = value;
					break;
				case (2, 1):
					M32 = value;
					break;
				case (3, 1):
					M42 = value;
					break;
				case (0, 2):
					M13 = value;
					break;
				case (1, 2):
					M23 = value;
					break;
				case (2, 2):
					M33 = value;
					break;
				case (3, 2):
					M43 = value;
					break;
				case (0, 3):
					M14 = value;
					break;
				case (1, 3):
					M24 = value;
					break;
				case (2, 3):
					M34 = value;
					break;
				case (3, 3):
					M44 = value;
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}
	}

	public Matrix4(Vector4 row1, Vector4 row2, Vector4 row3, Vector4 row4)
	{
		this = default;
		Row1 = row1;
		Row2 = row2;
		Row3 = row3;
		Row4 = row4;
	}

	public override bool Equals(object obj)
	{
		return Equals((Matrix4)obj);
	}

	public bool Equals(Matrix4 other)
	{
		if (Row1 == other.Row1 && Row2 == other.Row2 && Row3 == other.Row3)
		{
			return Row4 == other.Row4;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return (((13 * 7 + Row1.GetHashCode()) * 7 + Row2.GetHashCode()) * 7 + Row3.GetHashCode()) * 7 + Row4.GetHashCode();
	}

	public override string ToString()
	{
		static string FormatRow(Vector4 v) => $"({v.X},{v.Y},{v.Z},{v.W})";
		return string.Join(Environment.NewLine, FormatRow(Row1), FormatRow(Row2), FormatRow(Row3), FormatRow(Row4));
	}

	/// <summary>
	/// Gets the matrix as text, with each column aligned to the right.
	/// </summary>
	public string GetFormattedString()
	{
		string[,] cells = new string[Dimension, Dimension];
		int[] columnWidths = new int[Dimension];
		for (int row = 0; row < Dimension; row++)
		{
			for (int column = 0; column < Dimension; column++)
			{
				cells[column, row] = this[column, row].ToString();
				columnWidths[column] = Math.Max(columnWidths[column], cells[column, row].Length);
			}
		}
		StringBuilder stringBuilder = new StringBuilder();
		for (int row = 0; row < Dimension; row++)
		{
			for (int column = 0; column < Dimension; column++)
			{
				int padding = columnWidths[column] - cells[column, row].Length;
				stringBuilder.Append(' ');
				stringBuilder.Append(new string(' ', padding));
				stringBuilder.Append(cells[column, row]);
				stringBuilder.Append(' ');
			}
			if (row < Dimension - 1)
			{
				stringBuilder.AppendLine();
			}
		}
		return stringBuilder.ToString();
	}

	public Matrix4 Translate(double x, double y, double z = 0.0)
	{
		return CreateTranslation(x, y, z) * this;
	}

	public Matrix4 Translate(Vector2 v)
	{
		return CreateTranslation(v) * this;
	}

	public Matrix4 Translate(Vector3 v)
	{
		return CreateTranslation(v) * this;
	}

	public Matrix4 Scale(Vector2 v)
	{
		return CreateScale(v.X, v.Y) * this;
	}

	public Matrix4 Scale(double x, double y, double z = 1.0)
	{
		return CreateScale(x, y, z) * this;
	}

	public Matrix4 RotateX(double angle)
	{
		return this * CreateRotationX(angle);
	}

	public Matrix4 RotateY(double angle)
	{
		return this * CreateRotationY(angle);
	}

	public Matrix4 RotateZ(double angle)
	{
		return this * CreateRotationZ(angle);
	}

	public static Matrix4 operator *(Matrix4 lhs, Matrix4 rhs)
	{
		return new Matrix4
		{
			M11 = lhs.M11 * rhs.M11 + lhs.M12 * rhs.M21 + lhs.M13 * rhs.M31 + lhs.M14 * rhs.M41,
			M12 = lhs.M11 * rhs.M12 + lhs.M12 * rhs.M22 + lhs.M13 * rhs.M32 + lhs.M14 * rhs.M42,
			M13 = lhs.M11 * rhs.M13 + lhs.M12 * rhs.M23 + lhs.M13 * rhs.M33 + lhs.M14 * rhs.M43,
			M14 = lhs.M11 * rhs.M14 + lhs.M12 * rhs.M24 + lhs.M13 * rhs.M34 + lhs.M14 * rhs.M44,
			M21 = lhs.M21 * rhs.M11 + lhs.M22 * rhs.M21 + lhs.M23 * rhs.M31 + lhs.M24 * rhs.M41,
			M22 = lhs.M21 * rhs.M12 + lhs.M22 * rhs.M22 + lhs.M23 * rhs.M32 + lhs.M24 * rhs.M42,
			M23 = lhs.M21 * rhs.M13 + lhs.M22 * rhs.M23 + lhs.M23 * rhs.M33 + lhs.M24 * rhs.M43,
			M24 = lhs.M21 * rhs.M14 + lhs.M22 * rhs.M24 + lhs.M23 * rhs.M34 + lhs.M24 * rhs.M44,
			M31 = lhs.M31 * rhs.M11 + lhs.M32 * rhs.M21 + lhs.M33 * rhs.M31 + lhs.M34 * rhs.M41,
			M32 = lhs.M31 * rhs.M12 + lhs.M32 * rhs.M22 + lhs.M33 * rhs.M32 + lhs.M34 * rhs.M42,
			M33 = lhs.M31 * rhs.M13 + lhs.M32 * rhs.M23 + lhs.M33 * rhs.M33 + lhs.M34 * rhs.M43,
			M34 = lhs.M31 * rhs.M14 + lhs.M32 * rhs.M24 + lhs.M33 * rhs.M34 + lhs.M34 * rhs.M44,
			M41 = lhs.M41 * rhs.M11 + lhs.M42 * rhs.M21 + lhs.M43 * rhs.M31 + lhs.M44 * rhs.M41,
			M42 = lhs.M41 * rhs.M12 + lhs.M42 * rhs.M22 + lhs.M43 * rhs.M32 + lhs.M44 * rhs.M42,
			M43 = lhs.M41 * rhs.M13 + lhs.M42 * rhs.M23 + lhs.M43 * rhs.M33 + lhs.M44 * rhs.M43,
			M44 = lhs.M41 * rhs.M14 + lhs.M42 * rhs.M24 + lhs.M43 * rhs.M34 + lhs.M44 * rhs.M44
		};
	}

	public static Vector2 operator *(Matrix4 mat, Vector2 vec)
	{
		return (mat * new Vector4(vec.X, vec.Y, 1.0, 1.0)).XY;
	}

	public static Vector3 operator *(Matrix4 mat, Vector3 vec)
	{
		return (mat * new Vector4(vec.X, vec.Y, vec.Z, 1.0)).XYZ;
	}

	public static Vector4 operator *(Matrix4 mat, Vector4 vec)
	{
		return new Vector4(
			vec.X * mat.Row1.X + vec.Y * mat.Row2.X + vec.Z * mat.Row3.X + vec.W * mat.Row4.X,
			vec.X * mat.Row1.Y + vec.Y * mat.Row2.Y + vec.Z * mat.Row3.Y + vec.W * mat.Row4.Y,
			vec.X * mat.Row1.Z + vec.Y * mat.Row2.Z + vec.Z * mat.Row3.Z + vec.W * mat.Row4.Z,
			vec.X * mat.Row1.W + vec.Y * mat.Row2.W + vec.Z * mat.Row3.W + vec.W * mat.Row4.W);
	}

	public static bool operator ==(Matrix4 lhs, Matrix4 rhs)
	{
		return lhs.Equals(rhs);
	}

	public static bool operator !=(Matrix4 lhs, Matrix4 rhs)
	{
		return !lhs.Equals(rhs);
	}

	public static Matrix4 CreateOrthographic(double width, double height, double zNear, double zFar)
	{
		return CreateOrthographicOffCenter((0.0 - width) / 2.0, width / 2.0, (0.0 - height) / 2.0, height / 2.0, zNear, zFar);
	}

	public static Matrix4 CreateOrthographicOffCenter(double left, double right, double bottom, double top, double zNear, double zFar)
	{
		Matrix4 result = default;
		double inverseWidth = 1.0 / (right - left);
		double inverseHeight = 1.0 / (top - bottom);
		double inverseDepth = 1.0 / (zFar - zNear);
		result.M11 = 2.0 * inverseWidth;
		result.M22 = 2.0 * inverseHeight;
		result.M33 = -2.0 * inverseDepth;
		result.M14 = (0.0 - (right + left)) * inverseWidth;
		result.M24 = (0.0 - (top + bottom)) * inverseHeight;
		result.M34 = (0.0 - (zFar + zNear)) * inverseDepth;
		result.M44 = 1.0;
		return result;
	}

	public static Matrix4 LookAt(Vector3 eye, Vector3 target, Vector3 up)
	{
		Vector3 forward = (eye - target).Normalised;
		Vector3 right = up.Cross(forward).Normalised;
		Vector3 cameraUp = forward.Cross(right).Normalised;
		Matrix4 rotation = new Matrix4(
			new Vector4(right.X, cameraUp.X, forward.X, 0.0),
			new Vector4(right.Y, cameraUp.Y, forward.Y, 0.0),
			new Vector4(right.Z, cameraUp.Z, forward.Z, 0.0),
			Vector4.UnitW);
		Matrix4 translation = CreateTranslation(-eye);
		return rotation * translation;
	}

	public static Matrix4 LookAt(double eyeX, double eyeY, double eyeZ, double targetX, double targetY, double targetZ, double upX, double upY, double upZ)
	{
		return LookAt(new Vector3(eyeX, eyeY, eyeZ), new Vector3(targetX, targetY, targetZ), new Vector3(upX, upY, upZ));
	}

	public static Matrix4 Frustum(double left, double right, double bottom, double top, double near, double far)
	{
		double inverseWidth = 1.0 / (right - left);
		double inverseHeight = 1.0 / (top - bottom);
		double inverseDepth = 1.0 / (far - near);
		return new Matrix4(
			new Vector4(2.0 * near * inverseWidth, 0.0, 0.0, 0.0),
			new Vector4(0.0, 2.0 * near * inverseHeight, 0.0, 0.0),
			new Vector4((right + left) * inverseWidth, (top + bottom) * inverseHeight, (0.0 - (far + near)) * inverseDepth, -1.0),
			new Vector4(0.0, 0.0, -2.0 * far * near * inverseDepth, 0.0));
	}

	public static Matrix4 Perspective(double fovy, double aspect, double near, double far)
	{
		double top = near * Math.Tan(0.5 * fovy);
		double bottom = 0.0 - top;
		double left = bottom * aspect;
		double right = top * aspect;
		return Frustum(left, right, bottom, top, near, far);
	}

	public static Matrix4 CreateTranslation(Vector2 value)
	{
		Matrix4 matrix = Identity;
		matrix.M14 = value.X;
		matrix.M24 = value.Y;
		return matrix;
	}

	public static Matrix4 CreateTranslation(Vector3 value)
	{
		Matrix4 matrix = Identity;
		matrix.M14 = value.X;
		matrix.M24 = value.Y;
		matrix.M34 = value.Z;
		return matrix;
	}

	public static Matrix4 CreateTranslation(double x, double y, double z = 0.0)
	{
		Matrix4 matrix = Identity;
		matrix.M14 = x;
		matrix.M24 = y;
		matrix.M34 = z;
		return matrix;
	}

	public static Matrix4 CreateScale(double x, double y, double z = 1.0)
	{
		Matrix4 matrix = Identity;
		matrix.M11 = x;
		matrix.M22 = y;
		matrix.M33 = z;
		return matrix;
	}

	public static Matrix4 CreateRotationX(double angle)
	{
		double cos = Math.Cos(angle);
		double sin = Math.Sin(angle);
		Matrix4 matrix = Identity;
		matrix.M22 = cos;
		matrix.M23 = 0.0 - sin;
		matrix.M32 = sin;
		matrix.M33 = cos;
		return matrix;
	}

	public static Matrix4 CreateRotationY(double angle)
	{
		double cos = Math.Cos(angle);
		double sin = Math.Sin(angle);
		Matrix4 matrix = Identity;
		matrix.M11 = cos;
		matrix.M31 = 0.0 - sin;
		matrix.M13 = sin;
		matrix.M33 = cos;
		return matrix;
	}

	public static Matrix4 CreateRotationZ(double angle)
	{
		double cos = Math.Cos(angle);
		double sin = Math.Sin(angle);
		Matrix4 matrix = Identity;
		matrix.M11 = cos;
		matrix.M21 = sin;
		matrix.M12 = 0.0 - sin;
		matrix.M22 = cos;
		return matrix;
	}
}
