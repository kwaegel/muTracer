using System;
using System.Runtime.InteropServices;
using OpenTK;
using OpenTK.Graphics;

using Raytracing.BoundingVolumes;

using float4 = OpenTK.Vector4;

namespace Raytracing.Primitives
{
	[StructLayout(LayoutKind.Sequential)]
	unsafe public struct Triangle
	{
		public float4 p0;
		public float4 p1;
		public float4 p2;
		public Color4 c0, c1, c2;
		public float4 n0, n1, n2;

		public Triangle(Vector3 point0, Vector3 point1, Vector3 point2)
			:this(new Vector4(point0, 1.0f), new Vector4(point1, 1.0f), new Vector4(point2, 1.0f))
		{ }

		public Triangle(Vector4 point0, Vector4 point1, Vector4 point2)
		{
			p0 = point0;
			p1 = point1;
			p2 = point2;

			// Use default vertex color
			c0 = c1 = c2 = Color4.HotPink;

			// Use cross probuct to compute default normals
			Vector3 edge1 = new Vector3(point1 - point0);
			Vector3 edge2 = new Vector3(point2 - point0);
			Vector3 norm = Vector3.Cross(edge1, edge2);
			norm.Normalize();
			n0 = n1 = n2 = new float4(norm);
		}
		public Triangle(Vector3 point0, Vector3 point1, Vector3 point2,
			Color4 color0, Color4 color1, Color4 color2)
		{
			p0 = new Vector4(point0, 1.0f);
			p1 = new Vector4(point1, 1.0f);
			p2 = new Vector4(point2, 1.0f);

			c0 = color0;
			c1 = color1;
			c2 = color2;

			// Use cross probuct to compute default normals
			Vector3 edge1 = new Vector3(point1 - point0);
			Vector3 edge2 = new Vector3(point2 - point0);
			Vector3 norm = Vector3.Cross(edge1, edge2);
			norm.Normalize();
			n0 = n1 = n2 = new float4(norm);
		}

		public Triangle(Vector3 point0, Vector3 point1, Vector3 point2,
			Color4 color0, Color4 color1, Color4 color2,
			Vector3 norm0, Vector3 norm1, Vector3 norm2)
			: this(new Vector4(point0, 1.0f), new Vector4(point1, 1.0f), new Vector4(point2, 1.0f),
				color0, color1, color2,
				new Vector4(norm0, 1.0f), new Vector4(norm1, 1.0f), new Vector4(norm2, 1.0f))
		{ }

		public Triangle(Vector4 point0, Vector4 point1, Vector4 point2,
			Color4 color0, Color4 color1, Color4 color2,
			float4 norm0, float4 norm1, float4 norm2)
		{
			p0 = point0;
			p1 = point1;
			p2 = point2;
			c0 = color0;
			c1 = color1;
			c2 = color2;
			n0 = norm0;
			n1 = norm1;
			n2 = norm2;
		}

		public Triangle(Vector3 point1, Vector3 point2, Vector3 point3, int materialIndex)
			:this(new Vector4(point1, 1.0f), new Vector4(point2, 1.0f), new Vector4(point3, 1.0f), materialIndex)
		{ }

		public Triangle(Vector4 point1, Vector4 point2, Vector4 point3, int materialIndex)
			:this(point1, point2, point3)
		{
			// Since all the vectors have w=1, we can pack the material index into the last w coordinate
			p2.W = BitConverter.ToSingle(BitConverter.GetBytes(materialIndex), 0);
		}

		public BBox getBounds()
		{
			float minX = Math.Min(p0.X, Math.Min(p1.X, p2.X));
			float minY = Math.Min(p0.Y, Math.Min(p1.Y, p2.Y));
			float minZ = Math.Min(p0.Z, Math.Min(p1.Z, p2.Z));
			float maxX = Math.Max(p0.X, Math.Max(p1.X, p2.X));
			float maxY = Math.Max(p0.Y, Math.Max(p1.Y, p2.Y));
			float maxZ = Math.Max(p0.Z, Math.Max(p1.Z, p2.Z));
			return new BBox(minX, maxX, minY, maxY, minZ, maxZ);
		}

		// Code from: http://web.archive.org/web/20040629174917/http://www.acm.org/jgt/papers/MollerTrumbore97/code.html
		public float rayTriIntersect(Ray ray)
		{
			float EPSILON = 10e-5f;
			Vector3 dir = ray.Direction;
			Vector3 orig = ray.Origin;
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

			Vector3 collisionPoint = ray.Origin + t * ray.Direction;
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
