using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Raytracing.Primitives;

using OpenTK;

namespace Raytracing.Primitives
{
	class Plane : AbstractPrimitive
	{
		public Vector3 Normal;
		public float Distance;


		public Plane(Vector3 normal, float d)
		{
			Normal = normal;
			Distance = d;
		}
	}
}
