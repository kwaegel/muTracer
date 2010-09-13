using System;
using System.Runtime.InteropServices;

using OpenTK;
using OpenTK.Graphics;

namespace Raytracing.Primitives
{
	[StructLayout(LayoutKind.Sequential, Pack=16)]
	unsafe struct SphereStruct
	{
		public Vector3 Center;
		public float Radius;	// In OpenCL, get the radius using Center.w
		public Color4 Color;

		public SphereStruct(Vector3 center, float radius, Color4 color)
		{
			Center = center;
			Radius = radius;
			Color = color;
		}
	};

}
