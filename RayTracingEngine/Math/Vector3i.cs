using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Raytracing.Math
{
	class Vector3i
	{
		public static Vector3i Zero = new Vector3i(0, 0, 0);

		public int X;
		public int Y;
		public int Z;

		public Vector3i(int a)
		{
			X = a;
			Y = a;
			Z = a;
		}

		public Vector3i(int x, int y, int z)
		{
			X = x;
			Y = y;
			Z = z;
		}

		public Vector3i(OpenTK.Vector3 vector)
		{
			X = (int)vector.X;
			Y = (int)vector.Y;
			Z = (int)vector.Z;
		}

		public OpenTK.Vector3 toVector3()
		{
			return new OpenTK.Vector3(X,Y,Z);
		}

		/// <summary>
		/// Componentwise compairison.
		/// </summary>
		/// <param name="?"></param>
		/// <returns>True if all values are less then the coorosponding values in the given vector</returns>
		public bool lessThen(OpenTK.Vector3 vector)
		{
			return (X < vector.X && Y < vector.Y && Z < vector.Z);
		}

		/// <summary>
		/// Componentwise compairison.
		/// </summary>
		/// <param name="?"></param>
		/// <returns></returns>
		public bool greaterThan(OpenTK.Vector3 vector)
		{
			return (X > vector.X && Y > vector.Y && Z > vector.Z);
		}

		public override string  ToString()
		{
			return "(" + X + ", " + Y + ", " + Z +")";
		}

	}
}
