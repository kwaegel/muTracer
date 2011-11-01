using System;

using OpenTK;

using Raytracing.BoundingVolumes;

namespace Raytracing.Primitives
{
	// Uses clockwise winding like XNA
	[StructLayout(LayoutKind.Sequential)]
	public struct Triangle
	{
		private static float epsilon = 0.001f;

		public Vector3 Point1;
		public Vector3 Point2;
		public Vector3 Point3;
		public int MaterialIndex;

		public Triangle(Vector3 point1, Vector3 point2, Vector3 point3, int materialIndex)
		{
			Point1 = point1;
			Point2 = point2;
			Point3 = point3;
		}

		//public Vector3 computeNormal()
		//{
		//    Vector3 segment1;
		//    Vector3.Subtract(ref Point2, ref Point1, out segment1);
		//    Vector3 segment2;
		//    Vector3.Subtract(ref Point3, ref Point1, out segment2);
		//    Vector3 normal;
		//    // Right-handed cross product
		//    Vector3.Cross(ref segment2, ref segment1, out normal);
		//    normal.Normalize();

		//    return (normal);
		//}

		//public float? intersects(ref Ray r, ref Vector3 collisionPoint,
		//    ref Vector3 surfaceNormal)
		//{
		//    return null;
		//}

		//private bool rayTriIntersect(ref Vector3 o, ref Vector3 d, 
		//    ref Vector3 p0, ref Vector3 p1, ref Vector3 p2,
		//    out float u, out float v, out float t)
		//{
		//    u=v=t=0;
		//    Vector3 e1 = p1 - p0;
		//    Vector3 e2 = p2 - p0;
		//    Vector3 q = Vector3.Cross(d, e2);
		//    float a = Vector3.Dot(e1, q);
		//    if (a > -epsilon && a < epsilon)
		//        return false;
		//    float f = 1 / a;
		//    Vector3 s = o - p0;	// s = o - v0

		//}

		//public AxisAlignedBoundingBox getBoundingBox()
		//{
		//    throw new NotImplementedException("Not implemented for this type yet.");
		//}
	}
}
