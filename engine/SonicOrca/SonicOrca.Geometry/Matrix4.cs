using System;
using System.Text;

namespace SonicOrca.Geometry;

public struct Matrix4 : IEquatable<Matrix4>
{
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

	public double this[int x, int y]
	{
		get
		{
			if (x == 0 && y == 0)
			{
				return M11;
			}
			if (x == 1 && y == 0)
			{
				return M21;
			}
			if (x == 2 && y == 0)
			{
				return M31;
			}
			if (x == 3 && y == 0)
			{
				return M41;
			}
			if (x == 0 && y == 1)
			{
				return M12;
			}
			if (x == 1 && y == 1)
			{
				return M22;
			}
			if (x == 2 && y == 1)
			{
				return M32;
			}
			if (x == 3 && y == 1)
			{
				return M42;
			}
			if (x == 0 && y == 2)
			{
				return M13;
			}
			if (x == 1 && y == 2)
			{
				return M23;
			}
			if (x == 2 && y == 2)
			{
				return M33;
			}
			if (x == 3 && y == 2)
			{
				return M43;
			}
			if (x == 0 && y == 3)
			{
				return M14;
			}
			if (x == 1 && y == 3)
			{
				return M24;
			}
			if (x == 2 && y == 3)
			{
				return M34;
			}
			if (x == 3 && y == 3)
			{
				return M44;
			}
			throw new ArgumentOutOfRangeException();
		}
		set
		{
			if (x == 0 && y == 0)
			{
				M11 = value;
				return;
			}
			if (x == 1 && y == 0)
			{
				M21 = value;
				return;
			}
			if (x == 2 && y == 0)
			{
				M31 = value;
				return;
			}
			if (x == 3 && y == 0)
			{
				M41 = value;
				return;
			}
			if (x == 0 && y == 1)
			{
				M12 = value;
				return;
			}
			if (x == 1 && y == 1)
			{
				M22 = value;
				return;
			}
			if (x == 2 && y == 1)
			{
				M32 = value;
				return;
			}
			if (x == 3 && y == 1)
			{
				M42 = value;
				return;
			}
			if (x == 0 && y == 2)
			{
				M13 = value;
				return;
			}
			if (x == 1 && y == 2)
			{
				M23 = value;
				return;
			}
			if (x == 2 && y == 2)
			{
				M33 = value;
				return;
			}
			if (x == 3 && y == 2)
			{
				M43 = value;
				return;
			}
			if (x == 0 && y == 3)
			{
				M14 = value;
				return;
			}
			if (x == 1 && y == 3)
			{
				M24 = value;
				return;
			}
			if (x == 2 && y == 3)
			{
				M34 = value;
				return;
			}
			if (x == 3 && y == 3)
			{
				M44 = value;
				return;
			}
			throw new ArgumentOutOfRangeException();
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
		Func<Vector4, string> func = (Vector4 v) => $"({v.X},{v.Y},{v.Z},{v.W})";
		return string.Join(Environment.NewLine, func(Row1), func(Row2), func(Row3), func(Row4));
	}

	public string GetFormattedString()
	{
		string[,] array = new string[4, 4];
		int[] array2 = new int[4];
		for (int i = 0; i < 4; i++)
		{
			for (int j = 0; j < 4; j++)
			{
				array[j, i] = this[j, i].ToString();
				array2[j] = Math.Max(array2[j], array[j, i].Length);
			}
		}
		StringBuilder stringBuilder = new StringBuilder();
		for (int k = 0; k < 4; k++)
		{
			for (int l = 0; l < 4; l++)
			{
				int count = array2[l] - array[l, k].Length;
				stringBuilder.Append(' ');
				stringBuilder.Append(new string(' ', count));
				stringBuilder.Append(array[l, k]);
				stringBuilder.Append(' ');
			}
			if (k < 3)
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
		return new Vector4(vec.X * mat.Row1.X + vec.Y * mat.Row2.X + vec.Z * mat.Row3.X + vec.W * mat.Row4.X, vec.X * mat.Row1.Y + vec.Y * mat.Row2.Y + vec.Z * mat.Row3.Y + vec.W * mat.Row4.Y, vec.X * mat.Row1.Z + vec.Y * mat.Row2.Z + vec.Z * mat.Row3.Z + vec.W * mat.Row4.Z, vec.X * mat.Row1.W + vec.Y * mat.Row2.W + vec.Z * mat.Row3.W + vec.W * mat.Row4.W);
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
		double num = 1.0 / (right - left);
		double num2 = 1.0 / (top - bottom);
		double num3 = 1.0 / (zFar - zNear);
		result.M11 = 2.0 * num;
		result.M22 = 2.0 * num2;
		result.M33 = -2.0 * num3;
		result.M14 = (0.0 - (right + left)) * num;
		result.M24 = (0.0 - (top + bottom)) * num2;
		result.M34 = (0.0 - (zFar + zNear)) * num3;
		result.M44 = 1.0;
		return result;
	}

	public static Matrix4 LookAt(Vector3 eye, Vector3 target, Vector3 up)
	{
		Vector3 normalised = (eye - target).Normalised;
		Vector3 normalised2 = up.Cross(normalised).Normalised;
		Vector3 normalised3 = normalised.Cross(normalised2).Normalised;
		Matrix4 matrix = new Matrix4(new Vector4(normalised2.X, normalised3.X, normalised.X, 0.0), new Vector4(normalised2.Y, normalised3.Y, normalised.Y, 0.0), new Vector4(normalised2.Z, normalised3.Z, normalised.Z, 0.0), Vector4.UnitW);
		Matrix4 matrix2 = CreateTranslation(-eye);
		return matrix * matrix2;
	}

	public static Matrix4 LookAt(double eyeX, double eyeY, double eyeZ, double targetX, double targetY, double targetZ, double upX, double upY, double upZ)
	{
		return LookAt(new Vector3(eyeX, eyeY, eyeZ), new Vector3(targetX, targetY, targetZ), new Vector3(upX, upY, upZ));
	}

	public static Matrix4 Frustum(double left, double right, double bottom, double top, double near, double far)
	{
		double num = 1.0 / (right - left);
		double num2 = 1.0 / (top - bottom);
		double num3 = 1.0 / (far - near);
		return new Matrix4(new Vector4(2.0 * near * num, 0.0, 0.0, 0.0), new Vector4(0.0, 2.0 * near * num2, 0.0, 0.0), new Vector4((right + left) * num, (top + bottom) * num2, (0.0 - (far + near)) * num3, -1.0), new Vector4(0.0, 0.0, -2.0 * far * near * num3, 0.0));
	}

	public static Matrix4 Perspective(double fovy, double aspect, double near, double far)
	{
		double num = near * Math.Tan(0.5 * fovy);
		double num2 = 0.0 - num;
		double left = num2 * aspect;
		double right = num * aspect;
		return Frustum(left, right, num2, num, near, far);
	}

	public static Matrix4 CreateTranslation(Vector2 value)
	{
		Matrix4 identity = Identity;
		identity.M14 = value.X;
		identity.M24 = value.Y;
		return identity;
	}

	public static Matrix4 CreateTranslation(Vector3 value)
	{
		Matrix4 identity = Identity;
		identity.M14 = value.X;
		identity.M24 = value.Y;
		identity.M34 = value.Z;
		return identity;
	}

	public static Matrix4 CreateTranslation(double x, double y, double z = 0.0)
	{
		Matrix4 identity = Identity;
		identity.M14 = x;
		identity.M24 = y;
		identity.M34 = z;
		return identity;
	}

	public static Matrix4 CreateScale(double x, double y, double z = 1.0)
	{
		Matrix4 identity = Identity;
		identity.M11 = x;
		identity.M22 = y;
		identity.M33 = z;
		return identity;
	}

	public static Matrix4 CreateRotationX(double angle)
	{
		double m = Math.Cos(angle);
		double num = Math.Sin(angle);
		Matrix4 identity = Identity;
		identity.M21 = m;
		identity.M31 = num;
		identity.M22 = 0.0 - num;
		identity.M32 = num;
		return identity;
	}

	public static Matrix4 CreateRotationY(double angle)
	{
		double num = Math.Cos(angle);
		double num2 = Math.Sin(angle);
		Matrix4 identity = Identity;
		identity.M11 = num;
		identity.M31 = 0.0 - num2;
		identity.M13 = num2;
		identity.M33 = num;
		return identity;
	}

	public static Matrix4 CreateRotationZ(double angle)
	{
		double num = Math.Cos(angle);
		double num2 = Math.Sin(angle);
		Matrix4 identity = Identity;
		identity.M11 = num;
		identity.M21 = num2;
		identity.M12 = 0.0 - num2;
		identity.M22 = num;
		return identity;
	}
}
