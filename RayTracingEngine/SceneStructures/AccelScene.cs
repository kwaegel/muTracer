﻿#define BVH
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using OpenTK.Graphics;
using OpenTK;

using SimpleTracer.RayTracing;

namespace KDTreeTracer.RayTracing
{
	/**
	 * An accelerated scene class using a KD-Tree
	 * */
	class AccelScene : SimpleTracer.RayTracing.Scene
	{
		public bool UseTree = true;

		int maxPrims = 3;
		Accelerator _tree;

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
