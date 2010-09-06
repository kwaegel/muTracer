using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

//using Microsoft.Xna.Framework;
//using Microsoft.Xna.Framework.Graphics;

using OpenTK;
using OpenTK.Graphics;

using Raytracing.BoundingVolumes;


namespace Raytracing.Primitives
{
	public abstract class AbstractPrimitive
	{
		protected static Material BasicMaterial = new Material(Color4.Gray);

		public abstract Vector3 Position
		{
			get;
			set;
		}

		public Material Material
		{
			get;
			set;
		}

		public abstract float intersects(ref Ray r, ref Vector3 collisionPoint,
			ref Vector3 surfaceNormal);

		public virtual AxisAlignedBoundingBox getBoundingBox()
		{
			throw new NotImplementedException("Not implemented for this type yet.");
		}
	}
}
