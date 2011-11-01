﻿using System;
using System.Runtime.InteropServices;

using OpenTK;

using Raytracing.BoundingVolumes;

namespace Raytracing.Primitives
{
	// Uses clockwise winding like XNA
	[StructLayout(LayoutKind.Sequential)]
	unsafe public struct Triangle
	{
		public Vector4 p0;
		public Vector4 p1;
		public Vector4 p2;

		public Triangle(Vector3 point1, Vector3 point2, Vector3 point3, int materialIndex)
		{
			// Since all the vectors have w=1, we can pack the material index into the last w coordinate
			float matAsFloat = BitConverter.ToSingle(BitConverter.GetBytes(materialIndex), 0);

			p0 = new Vector4(point1, 1.0f);
			p1 = new Vector4(point2, 1.0f);
			p2 = new Vector4(point3, matAsFloat);
		}

		public Triangle(Vector4 point1, Vector4 point2, Vector4 point3, int materialIndex)
		{
			p0 = point1;
			p1 = point2;
			p2 = point3;

			// Since all the vectors have w=1, we can pack the material index into the last w coordinate
			p2.W = BitConverter.ToSingle(BitConverter.GetBytes(materialIndex), 0);
		}

		// Code from: http://web.archive.org/web/20040629174917/http://www.acm.org/jgt/papers/MollerTrumbore97/code.html
		public float rayTriIntersect(Ray ray)
		{
			bool testCull = false;
			float EPSILON = 10e-5f;
			Vector3 dir = ray.Direction;
			Vector3 orig = ray.Position;
			Vector3 vert0 = new Vector3(p0);
			Vector3 vert1 = new Vector3(p1);
			Vector3 vert2 = new Vector3(p2);
			//double edge1[3], edge2[3], tvec[3], pvec[3], qvec[3];
			//double det,inv_det;

			/* find vectors for two edges sharing vert0 */
			Vector3 edge1 = vert1 - vert0;					//SUB(edge1, vert1, vert0);
			Vector3 edge2 = vert2 - vert0;					//SUB(edge2, vert2, vert0);

			/* begin calculating determinant - also used to calculate U parameter */
			Vector3 pvec = Vector3.Cross(dir, edge2);		//CROSS(pvec, dir, edge2);

			/* if determinant is near zero, ray lies in plane of triangle */
			float det = Vector3.Dot(edge1, pvec);			//det = DOT(edge1, pvec);

			if (det > -EPSILON && det < EPSILON)
				return 0;

			float inv_det = 1.0f / det;

			/* calculate distance from vert0 to ray origin */
			Vector3 tvec = orig - vert0;					//SUB(tvec, orig, vert0);

			/* calculate U parameter and test bounds */
			float u = Vector3.Dot(tvec, pvec) * inv_det;	//*u = DOT(tvec, pvec) * inv_det;
			if (u < 0.0 || u > 1.0)
				return 0;

			/* prepare to test V parameter */
			Vector3 qvec = Vector3.Cross(tvec, edge1);		//CROSS(qvec, tvec, edge1);

			/* calculate V parameter and test bounds */
			float v = Vector3.Dot(dir, qvec) * inv_det;				//*v = DOT(dir, qvec) * inv_det;
			if (v < 0.0 || u + v > 1.0)
				return 0;

			/* calculate t, ray intersects triangle */
			float t = Vector3.Dot(edge2, qvec) * inv_det;				//*t = DOT(edge2, qvec) * inv_det;

			Vector3 collisionPoint = ray.Position + t * ray.Direction;
			Vector3 surfaceNormal = Vector3.Cross(edge1, edge2);
			surfaceNormal.Normalize();

			return t;
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
