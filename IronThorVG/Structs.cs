using System.Drawing;

namespace IronThorVG;

public partial struct Point : IEquatable<Point>
{
	public Point()
	{
	}
	
	public Point(float x, float y)
	{
		X = x;
		Y = y;
	}

	public bool Equals(Point other)
	{
		return X.Equals(other.X) && Y.Equals(other.Y);
	}

	public override bool Equals(object? obj)
	{
		return obj is Point other && Equals(other);
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(X, Y);
	}
	
	public static Point operator +(Point left, Point right)
	{
		return new Point(left.X + right.X, left.Y + right.Y);
	}

	public static Point operator -(Point left, Point right)
	{
		return new Point(left.X - right.X, left.Y - right.Y);
	}

	public static Point operator *(Point point, float scalar)
	{
		return new Point(point.X * scalar, point.Y * scalar);
	}

	public static Point operator *(float scalar, Point point)
	{
		return new Point(point.X * scalar, point.Y * scalar);
	}

	public static Point operator /(Point point, float scalar)
	{
		return new Point(point.X / scalar, point.Y / scalar);
	}

	public static bool operator ==(Point left, Point right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(Point left, Point right)
	{
		return !left.Equals(right);
	}
}

public partial struct ColorStop
{
	public Color Color
	{
		get => new(R, G, B, A);
		set
		{
			R = value.R;
			G = value.G;
			B = value.B;
			A = value.A;
		}
	}
	
	public ColorStop()
	{
	}

	public ColorStop(float offset, Color color)
	{
		Offset = offset;
		Color = color;
	}
}

/// <summary>
/// The elements e11, e12, e21 and e22 represent the rotation matrix, including the scaling factor.
/// The elements e13 and e23 determine the translation of the object along the x and y-axis, respectively.
/// The elements e31 and e32 are set to 0, e33 is set to 1.
/// </summary>
public partial struct Matrix : IEquatable<Matrix>
{
	public static readonly Matrix Identity = new()
	{
		E11 = 1, E12 = 0, E13 = 0,
		E21 = 0, E22 = 1, E23 = 0,
		E31 = 0, E32 = 0, E33 = 1
	};

	public static Matrix CreateRotationX(float radians)
	{
		var matrix = Identity;
		
		matrix.E22 = MathF.Cos(radians);
		matrix.E33 = matrix.E11;
		
		matrix.E23 = -MathF.Sin(radians);
		matrix.E32 = -matrix.E12;
		
		return matrix;
	}

	public static Matrix CreateRotationY(float radians)
	{
		var matrix = Identity;
		
		matrix.E11 = MathF.Cos(radians);
		matrix.E33 = matrix.E11;
		
		matrix.E31 = -MathF.Sin(radians);
		matrix.E13 = -matrix.E12;
		
		return matrix;
	}

	public static Matrix CreateRotationZ(float radians)
	{
		var matrix = Identity;
		
		matrix.E11 = MathF.Cos(radians);
		matrix.E22 = matrix.E11;
		
		matrix.E12 = -MathF.Sin(radians);
		matrix.E21 = -matrix.E12;
		
		return matrix;
	}
	
	public static Matrix CreateTranslation(float x, float y)
	{
		var matrix = Identity;
		matrix.E13 = x;
		matrix.E23 = y;
		return matrix;
	}
	
	public static Matrix CreateScale(float w, float h)
	{
		var matrix = Identity;
		matrix.E11 = w;
		matrix.E22 = h;
		return matrix;
	}
	
	public static Matrix CreateShearX(float radians)
	{
		var matrix = Identity;
		matrix.E12 = MathF.Tan(radians);
		return matrix;
	}
	
	public static Matrix CreateShearY(float radians)
	{
		var matrix = Identity;
		matrix.E21 = MathF.Tan(radians);
		return matrix;
	}
	
	public static Matrix operator *(Matrix left, Matrix right)
	{
		return new Matrix
		{
			E11 = left.E11 * right.E11 + left.E12 * right.E21 + left.E13 * right.E31,
			E12 = left.E11 * right.E12 + left.E12 * right.E22 + left.E13 * right.E32,
			E13 = left.E11 * right.E13 + left.E12 * right.E23 + left.E13 * right.E33,

			E21 = left.E21 * right.E11 + left.E22 * right.E21 + left.E23 * right.E31,
			E22 = left.E21 * right.E12 + left.E22 * right.E22 + left.E23 * right.E32,
			E23 = left.E21 * right.E13 + left.E22 * right.E23 + left.E23 * right.E33,

			E31 = left.E31 * right.E11 + left.E32 * right.E21 + left.E33 * right.E31,
			E32 = left.E31 * right.E12 + left.E32 * right.E22 + left.E33 * right.E32,
			E33 = left.E31 * right.E13 + left.E32 * right.E23 + left.E33 * right.E33
		};
	}
	
	public static Point operator *(Matrix matrix, PointF point)
	{
		return new Point
		{
			X = matrix.E11 * point.X + matrix.E12 * point.Y + matrix.E13,
			Y = matrix.E21 * point.X + matrix.E22 * point.Y + matrix.E23
		};
	}

	public bool Equals(Matrix other)
	{
		return E11.Equals(other.E11) && E12.Equals(other.E12) && E13.Equals(other.E13) && E21.Equals(other.E21) && E22.Equals(other.E22) && E23.Equals(other.E23) && E31.Equals(other.E31) && E32.Equals(other.E32) && E33.Equals(other.E33);
	}

	public override bool Equals(object? obj)
	{
		return obj is Matrix other && Equals(other);
	}

	public override int GetHashCode()
	{
		var hashCode = new HashCode();
		hashCode.Add(E11);
		hashCode.Add(E12);
		hashCode.Add(E13);
		hashCode.Add(E21);
		hashCode.Add(E22);
		hashCode.Add(E23);
		hashCode.Add(E31);
		hashCode.Add(E32);
		hashCode.Add(E33);
		return hashCode.ToHashCode();
	}

	public static bool operator ==(Matrix left, Matrix right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(Matrix left, Matrix right)
	{
		return !left.Equals(right);
	}
}