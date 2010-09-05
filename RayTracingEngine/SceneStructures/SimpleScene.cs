using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using OpenTK;
using OpenTK.Graphics;

using Raytracing.Primitives;

namespace Raytracing.SceneStructures
{

	// TODO: implement an octree to handle more complex scenes.
	public class SimpleScene : Scene
	{

		/// <summary>
		/// This field ensures that the ray does not collide with same place it is being 
		/// cast from.
		/// </summary>
		private static float MinimumCollisionDistance = 0.001f;

		List<PointLight> _lights;
		List<AbstractPrimitive> _primitives;

		enum ShapeHit {none, box, sphere, model};

		public SimpleScene()
		{
			_lights = new List<PointLight>();
			_primitives = new List<AbstractPrimitive>();
		}

		public override void add(AbstractPrimitive sObject)
		{
			_primitives.Add(sObject);
		}

		public override void add(PointLight light)
		{
			_lights.Add(light);
		}

		// if return value of float? is null, out variables are null
		public override float getNearestIntersection(ref Ray r, ref Vector3 collisionPoint,
			ref Vector3 surfaceNormal, ref Material material)
		{
			
			float nearestIntersection = float.PositiveInfinity;
			AbstractPrimitive hitObject = null;
			Vector3 nearestCP = new Vector3();
			Vector3 nearestSN = new Vector3();

			int sceneCount = _primitives.Count;
			for (int i = 0; i < sceneCount; i++)
			{
				AbstractPrimitive si = _primitives[i];
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

		private bool getAnyCollision(ref Ray r)
		{
			Vector3 nearestCP = new Vector3();
			Vector3 nearestSN = new Vector3();

			int sceneCount = _primitives.Count;
			for (int i = 0; i < sceneCount; i++)
			{
				AbstractPrimitive si = _primitives[i];
				float? distance = si.intersects(ref r, ref nearestCP, ref nearestSN);
				if (distance.HasValue && distance > MinimumCollisionDistance)
				{
					return true;
				}
			}
			return false;
		}

		private float getShadowFactor(Vector3 point)
		{
			int lightsHit = _lights.Count;

			foreach (PointLight pl in _lights)
			{
				Vector3 directionToLight = Vector3.Subtract(pl.Position, point);
				directionToLight.Normalize();
				Ray rayToLight = new Ray(point, directionToLight);

				if (getAnyCollision(ref rayToLight))
				{
					--lightsHit;
				}
			}

			return (float)lightsHit / _lights.Count;

		}

		/// <summary>
		/// if n is not specified, assume it is 1 (start point in air or vacum)
		/// </summary>
		/// <param name="r"></param>
		/// <param name="recursiveLevel"></param>
		/// <returns></returns>
		private Color4 castRay(Ray r, int recursiveLevel)
		{
			return castRay(r, recursiveLevel, 1);
		}

		private Color4 castRay(Ray r, int recursiveLevel, float startN)
		{
			// find the nearest intersection to see if the ray has hit anything
			Vector3 collisionPoint = new Vector3();
			Vector3 surfaceNormal = new Vector3();
			Material mat = new Material();
			float? nearestIntersection = getNearestIntersection(ref r, ref collisionPoint, ref surfaceNormal, ref mat);


			// if we have hit somthing, calculate the color to return
			// else just return the background color
			if (nearestIntersection.HasValue)
			{

				Vector3 accumulatedColor = Vector3.Zero;
				Vector3 diffuseColor = Vector3.Zero;
				Vector3 reflectedColorVec = Vector3.Zero;
				Vector3 refractedColorVec = Vector3.Zero;

				// calculate diffuse shading
				float shade = 1;
				foreach (PointLight pl in _lights)
				{
					Vector3 pointToLight = Vector3.Subtract(pl.Position, collisionPoint);
					pointToLight.Normalize();

					shade = Vector3.Dot(surfaceNormal, pointToLight);

					if (shade > 0)
						diffuseColor = shade * mat.color.ToVector3();
				}

				// calculate shadows on the diffuse light
				float shadowFactor = getShadowFactor(collisionPoint);
				diffuseColor *= shadowFactor;

				// precalculate this as both reflection and refraction need to use it
				float cosTheta = Vector3.Dot(r.Direction, surfaceNormal);

				// calculate reflections
				if (recursiveLevel > 0 && mat.reflectivity > 0)
				{
					Vector3 reflectedRayDirection = r.Direction - 2.0f * Vector3.Dot(r.Direction, surfaceNormal) * surfaceNormal;

					Ray reflectedRay = new Ray(collisionPoint, reflectedRayDirection);

					Color reflectedColor = castRay(reflectedRay, recursiveLevel - 1);

					reflectedColorVec = reflectedColor.ToVector3();
				}


				// calculate refraction
				if (recursiveLevel > 0 && mat.transparency > 0)
				{
					// if we are moving from a dense medium to a less dense one, reverse the surface normal
					if (startN > 1)
					{
						surfaceNormal = -surfaceNormal;
						// cosTheta is based on the surface normal and must also be negated
						cosTheta = -cosTheta;
					}

					float n = startN / mat.n;

					float sinThetaSquared = n * n * (1 - cosTheta * cosTheta);
					
					Vector3 transDir = n * r.Direction - ((float)(n * cosTheta + System.Math.Sqrt(1 - sinThetaSquared)))*surfaceNormal;

					// calculate ray direction
					Ray refractedRay = new Ray(collisionPoint, transDir);

					Color refractedColor = castRay(refractedRay, recursiveLevel - 1, mat.n);

					refractedColorVec = refractedColor.ToVector3();
				}


				accumulatedColor += mat.reflectivity * reflectedColorVec;
				accumulatedColor += mat.transparency * refractedColorVec;
				if (startN == 1)
					accumulatedColor += shadowFactor * diffuseColor * (1 - mat.reflectivity - mat.transparency);
				else
					accumulatedColor += diffuseColor * (1 - mat.reflectivity - mat.transparency);

				return new Color(accumulatedColor);
			}
			else
			{
				return SimpleScene.BackgroundColor;
			}

		}

		// casts all rays from startIndex to endIndex, non inclusive
		public void castRays(ref Ray[] rays, ref Color4[] buffer, int startIndex, int endIndex, int recursiveDepth)
		{
			int bufferIndex = startIndex;
			while (bufferIndex < endIndex)
			{
				Ray r = rays[bufferIndex];

				buffer[bufferIndex] = this.castRay(r, recursiveDepth);

				++bufferIndex;
			}
		}
	}
}
