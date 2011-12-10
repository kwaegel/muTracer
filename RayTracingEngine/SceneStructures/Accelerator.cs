using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SimpleTracer.RayTracing;

namespace KDTreeTracer.RayTracing
{
	abstract class Accelerator
	{
		public bool PrintDebugMessages = false;

		public abstract float getNearestIntersection(ref Ray ray, out Sphere primHit);
	}
}
