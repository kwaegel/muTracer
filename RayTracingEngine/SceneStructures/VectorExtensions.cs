using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using OpenTK;
using SimpleTracer.RayTracing;

namespace KDTreeTracer.RayTracing
{
	public static class VectorExtensions
	{
		public static void axisValue(this Vector3 vec, Axis axis, float value)
		{
			if (axis == Axis.x)
				vec.X = value;
			else if (axis == Axis.y)
				vec.Y = value;
			else
				vec.Z = value;
		}

		public static float axisValue(this Vector3 vec, Axis axis)
		{
			if (axis == Axis.x)
				return vec.X;
			else if (axis == Axis.y)
				return vec.Y;
			return vec.Z;
		}

		public static float axisValue(this Vector3 vec, int axisId)
		{
			if (axisId == (int)Axis.x)
				return vec.X;
			else if (axisId == (int)Axis.y)
				return vec.Y;
			return vec.Z;
		}

	}
}
