using System;
using System.Runtime.InteropServices;

using OpenTK;
using OpenTK.Graphics;

namespace Raytracing.Primitives
{
	[StructLayout(LayoutKind.Sequential)]
	unsafe struct SphereStruct
	{
		public Vector4 Center;
		public float Radius;
        public int MaterialIndex;
        int pad1;
        int pad2;

		public SphereStruct(Vector3 center, float radius, int materialIndex)
		{
			Center = new Vector4(center, 1.0f);
			Radius = radius;
			MaterialIndex = materialIndex;
            pad1 = 0;
            pad2 = 0;
		}
	};

}
