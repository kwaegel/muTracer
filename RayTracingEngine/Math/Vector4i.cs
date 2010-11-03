using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Raytracing.Math
{
	struct Vector4i
	{
		public static Vector4i Zero = new Vector4i(0, 0, 0, 0);

		public int X;
		public int Y;
		public int Z;
		public int W;

		public Vector4i(int a)
		{
			X = a;
			Y = a;
			Z = a;
			W = a;
		}

		public Vector4i(int x, int y, int z, int w)
		{
			X = x;
			Y = y;
			Z = z;
			W = w;
		}

		public Vector4i(OpenTK.Vector4 vector)
		{
			X = (int)vector.X;
			Y = (int)vector.Y;
			Z = (int)vector.Z;
			W = (int)vector.W;
		}

		public OpenTK.Vector4 toVector4()
		{
			return new OpenTK.Vector4(X,Y,Z,W);
		}

		/// <summary>
		/// Componentwise compairison.
		/// </summary>
		/// <param name="?"></param>
		/// <returns>True if all values are less then the coorosponding values in the given vector</returns>
		public bool lessThen(OpenTK.Vector4 vector)
		{
			return (X < vector.X && Y < vector.Y && Z < vector.Z && W < vector.W);
		}

		/// <summary>
		/// Componentwise compairison.
		/// </summary>
		/// <param name="?"></param>
		/// <returns></returns>
		public bool greaterThan(OpenTK.Vector4 vector)
		{
			return (X > vector.X && Y > vector.Y && Z > vector.Z && W > vector.W);
		}

		public override string  ToString()
		{
			return "(" + X + ", " + Y + ", " + Z + ", " + W +")";
		}

	}
}
