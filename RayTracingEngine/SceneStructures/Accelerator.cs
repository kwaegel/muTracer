﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Raytracing.Primitives;

namespace Raytracing
{
	abstract class Accelerator
	{
		public bool PrintDebugMessages = false;

		public abstract float getNearestIntersection(ref Ray ray, out Sphere primHit);
	}
}
