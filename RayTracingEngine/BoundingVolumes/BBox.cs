using System;
using System.Runtime.InteropServices;

using OpenTK;

using Raytracing.Primitives;

namespace Raytracing
{
	public enum Axis { x, y, z, none}

	[StructLayout(LayoutKind.Sequential)]
	public struct BBox
	{
		public Vector4 pMin;
		public Vector4 pMax;

		public BBox(bool temp)
		{
			pMin = new Vector4(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity,1);
			pMax = new Vector4(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity,1);
		}

		public BBox(Vector3 min, Vector3 max)
		{
			pMin = new Vector4(min,1);
			pMax = new Vector4(max,1);
		}

		public BBox(float minX, float maxX, float minY, float maxY, float minZ, float maxZ)
		{
			pMin = new Vector4(minX,minY,minZ,1);
			pMax = new Vector4(maxX,maxY,maxZ,1);
		}

		public BBox(BBox b)
		{
			pMin = b.pMin;
			pMax = b.pMax;
		}

		public Vector3 this[int i]
		{
			get
			{
				return (i == 0) ? pMin.Xyz : pMax.Xyz;
			}
			set
			{
				if (i == 0)
					pMin.Xyz = value;
				else if (i == 1)
					pMax.Xyz = value;
			}
		}

		public float intersect(ref Ray ray, ref float hitT0, ref float hitT1)
		{
			float t0 = ray.tMin, t1 = ray.tMax;
			
			float invRayDir, tNear, tFar;
			// Check slab x
			invRayDir = 1.0f / ray.Direction.X;
			tNear = (pMin.X - ray.Origin.X) * invRayDir;
			tFar = (pMax.X - ray.Origin.X) * invRayDir;
			if(tNear > tFar) Swap(ref tNear, ref tFar);
			t0 = (tNear > t0) ? tNear : t0;
			t1 = (tFar < t1) ? tFar : t1;
			if (t0 > t1) return float.PositiveInfinity;

			// Check slab y
			invRayDir = 1.0f / ray.Direction.Y;
			tNear = (pMin.Y - ray.Origin.Y) * invRayDir;
			tFar = (pMax.Y - ray.Origin.Y) * invRayDir;
			if (tNear > tFar) Swap(ref tNear, ref tFar);
			t0 = (tNear > t0) ? tNear : t0;
			t1 = (tFar < t1) ? tFar : t1;
			if (t0 > t1) return float.PositiveInfinity;

			// Check slab z
			invRayDir = 1.0f / ray.Direction.Z;
			tNear = (pMin.Z - ray.Origin.Z) * invRayDir;
			tFar = (pMax.Z - ray.Origin.Z) * invRayDir;
			if (tNear > tFar) Swap(ref tNear, ref tFar);
			t0 = (tNear > t0) ? tNear : t0;
			t1 = (tFar < t1) ? tFar : t1;
			if (t0 > t1) return float.PositiveInfinity;

			hitT0 = t0;
			hitT1 = t1;
			return t0;
		}

		public float surfaceArea()
		{
			Vector3 sides = (pMax.Xyz - pMin.Xyz);
			return 2* ((sides.X*sides.Y) + (sides.X*sides.Z) + (sides.Y*sides.Z) );
		}

		public Axis longestAxis()
		{
			Vector3 sides = (pMax.Xyz - pMin.Xyz);

			return (sides.X < sides.Y) ? ( sides.Y < sides.Z ? Axis.z : Axis.y) : (sides.X < sides.Z ? Axis.z : Axis.x);
		}

		public void union(Vector3 p)
		{
			pMin.X = Math.Min(pMin.X, p.X);
			pMin.Y = Math.Min(pMin.Y, p.Y);
			pMin.Z = Math.Min(pMin.Z, p.Z);
			pMax.X = Math.Max(pMax.X, p.X);
			pMax.Y = Math.Max(pMax.Y, p.Y);
			pMax.Z = Math.Max(pMax.Z, p.Z);
		}

		public BBox union(BBox other)
		{
			pMin.Xyz = Vector3.ComponentMin(pMin.Xyz, other.pMin.Xyz);
			pMax.Xyz = Vector3.ComponentMax(pMax.Xyz, other.pMax.Xyz);
			return this;
		}

		public static BBox union(BBox b1, BBox b2)
		{
			return new BBox(b1).union(b2);
		}

		private static void Swap<T>(ref T lhs, ref T rhs)
		{
			T temp;
			temp = lhs;
			lhs = rhs;
			rhs = temp;
		}
	}
}
