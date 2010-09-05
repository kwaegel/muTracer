using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using OpenTK;
using OpenTK.Graphics;

using Raytracing.Primitives;

namespace Raytracing
{
	/// <summary>
	/// Searches the scene space using a simple linear search. Very slow for complex scenes.
	/// </summary>
	class LinearScene : Scene
	{

		/// <summary>
		/// This field ensures that the ray does not collide with same place it is being 
		/// cast from.
		/// </summary>
		private static float MinimumCollisionDistance = 0.001f;

		List<PointLight> _lightList;
		List<AbstractPrimitive> _sceneObjects;


		public LinearScene(Color4 backgroundColor)
		{
			BackgroundColor = backgroundColor;
			_lightList = new List<PointLight>();
			_sceneObjects = new List<AbstractPrimitive>();
		}

		public override void add(AbstractPrimitive sObject)
		{
			_sceneObjects.Add(sObject);
		}

		public override void add(PointLight light)
		{
			_lightList.Add(light);
		}


		// if return value of float? is null, out variables are null
		public override float getNearestIntersection(ref Ray r, ref Vector3 collisionPoint,
			ref Vector3 surfaceNormal, ref Material material)
		{

			float nearestIntersection = float.PositiveInfinity;
			AbstractPrimitive hitObject = null;
			Vector3 nearestCP = new Vector3();
			Vector3 nearestSN = new Vector3();

			int sceneCount = _sceneObjects.Count;
			for (int i = 0; i < sceneCount; i++)
			{
				AbstractPrimitive si = _sceneObjects[i];
				float? distance = si.intersects(ref r, ref nearestCP, ref nearestSN);
				if (distance.HasValue && distance < nearestIntersection && distance > MinimumCollisionDistance)
				{
					nearestIntersection = (float)distance;
					hitObject = si;
					collisionPoint = nearestCP;
					surfaceNormal = nearestSN;
				}
			}
			if (nearestIntersection < Single.PositiveInfinity)
			{
				material = hitObject.Material;
				return nearestIntersection;
			}
			return -1;
		}
	}
}
