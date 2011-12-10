#define BVH
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using OpenTK;
using OpenTK.Graphics;

using Raytracing.Primitives;

namespace Raytracing.SceneStructures
{
	/**
	 * An accelerated scene class using a KD-Tree or BVH
	 * */
	public class AccelScene : Scene
	{
		public bool UseTree = true;

		protected int maxPrims = 3;
		protected Accelerator _tree;

		public AccelScene(Color4 backgroundColor)
			: base(backgroundColor)
		{ }

		public AccelScene(List<Sphere> spheres, Color4 backgroundColor) 
			: base(backgroundColor)
		{
			_spheres = spheres;
			rebuildTree();
		}

		public void rebuildTree()
		{
#if BVH
			_tree = new BvhTree(_spheres, maxPrims);
#else
			_tree = new KDTree(_spheres, maxPrims);
#endif
		}

		public override float getNearestIntersection(ref Ray ray, ref Vector3 collisionPoint, ref Vector3 surfaceNormal)
		{
			if(UseTree)
			{
				Sphere primHit;
				_tree.PrintDebugMessages = PrintDebugMessages;
				return _tree.getNearestIntersection(ref ray, out primHit);
			}
			else
			{
				return base.getNearestIntersection(ref ray, ref collisionPoint, ref surfaceNormal);
			}
		}

		public override float getNearestIntersection(ref Ray ray, ref Vector3 collisionPoint, ref Vector3 surfaceNormal, ref Material mat)
		{
			// Use KD-Tree to check for intersection
			Sphere primHit;
			float t = _tree.getNearestIntersection(ref ray, out primHit);

			if ( !float.IsPositiveInfinity(t) )
			{
				ray.Direction.Normalize();
				collisionPoint = ray.Origin + (Vector3)(t * ray.Direction);
				surfaceNormal = Vector3.Subtract(collisionPoint, primHit.Position);
				surfaceNormal.Normalize();

				collisionPoint = primHit.Position + primHit.Radius * surfaceNormal;

				mat = primHit.Material;
			}

			return t;
		}

	}
}
