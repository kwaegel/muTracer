using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

//using Microsoft.Xna.Framework;
//using Microsoft.Xna.Framework.Graphics;

using Raytracing.Primitives;

namespace Raytracing
{
	class RayCastingUtils
	{

		//private static float getShadowFactor(Vector3 point)
		//{
		//    int lightsHit = _lights.Count;
		//    Vector3 cp = new Vector3();
		//    Vector3 sn = new Vector3();
		//    Material m = new Material();

		//    foreach (PointLight pl in _lights)
		//    {
		//        Vector3 directionToLight = Vector3.Subtract(pl.Position, point);
		//        directionToLight.Normalize();
		//        Ray rayToLight = new Ray(point, directionToLight);

		//        float? distance = getNearestCollision(ref rayToLight, ref cp, ref sn, ref m);

		//        if (distance.HasValue)
		//        {
		//            lightsHit--;
		//        }
		//    }

		//    return (float)lightsHit / _lights.Count;

		//}

		///// <summary>
		///// if n is not specified, assume it is 1 (start point in air or vacum)
		///// </summary>
		///// <param name="r"></param>
		///// <param name="recursiveLevel"></param>
		///// <returns></returns>
		//private Color castRay(Ray r, int recursiveLevel)
		//{
		//    return castRay(r, recursiveLevel, 1);
		//}

		//private static Color castRay(ref Scene scene, Ray r, int recursiveLevel, float startN)
		//{
		//    // find the nearest intersection to see if the ray has hit anything
		//    Vector3 collisionPoint = new Vector3();
		//    Vector3 surfaceNormal = new Vector3();
		//    Material mat = new Material();
		//    float? nearestIntersection = scene.getNearestIntersection(ref r, ref collisionPoint, ref surfaceNormal, mat);


		//    // if we have hit somthing, calculate the color to return
		//    // else just return the background color
		//    if (nearestIntersection.HasValue)
		//    {

		//        Vector3 accumulatedColor = Vector3.Zero;
		//        Vector3 diffuseColor = Vector3.Zero;
		//        Vector3 reflectedColorVec = Vector3.Zero;
		//        Vector3 refractedColorVec = Vector3.Zero;

		//        // calculate diffuse shading
		//        foreach (PointLight pl in _lights)
		//        {
		//            Vector3 pointToLight = Vector3.Subtract(pl.Position, collisionPoint);
		//            pointToLight.Normalize();

		//            float shade = Vector3.Dot(surfaceNormal, pointToLight);

		//            if (shade > 0)
		//                diffuseColor = mat.diffusion * shade * mat.color.ToVector3();
		//        }

		//        // calculate shadows on the diffuse light
		//        float shadowFactor = getShadowFactor(collisionPoint);
		//        diffuseColor *= shadowFactor;

		//        // precalculate this as both reflection and refraction need to use it
		//        float cosTheta = Vector3.Dot(r.Direction, surfaceNormal);

		//        // calculate reflections
		//        if (recursiveLevel > 0 && mat.reflectivity > 0)
		//        {
		//            Vector3 reflectedRayDirection = r.Direction - 2.0f * Vector3.Dot(r.Direction, surfaceNormal) * surfaceNormal;

		//            Ray reflectedRay = new Ray(collisionPoint, reflectedRayDirection);

		//            Color reflectedColor = RayCastingUtils.castRay(reflectedRay, recursiveLevel - 1);

		//            reflectedColorVec = mat.reflectivity * reflectedColor.ToVector3();
		//        }


		//        // calculate refraction
		//        if (recursiveLevel > 0 && mat.transparency > 0)
		//        {
		//            // if we are moving from a dense medium to a less dense one, reverse the surface normal
		//            if (startN > 1)
		//            {
		//                surfaceNormal = -surfaceNormal;
		//                // cosTheta is based on the surface normal and must also be negated
		//                cosTheta = -cosTheta;
		//            }

		//            float n = startN / mat.n;

		//            float sinThetaSquared = n * n * (1 - cosTheta * cosTheta);

		//            Vector3 transDir = n * r.Direction - ((float)(n * cosTheta + Math.Sqrt(1 - sinThetaSquared))) * surfaceNormal;

		//            // calculate ray direction

		//            Ray refractedRay = new Ray(collisionPoint, transDir);

		//            Color refractedColor = castRay(refractedRay, recursiveLevel - 1, mat.n);

		//            refractedColorVec = mat.transparency * refractedColor.ToVector3();
		//        }


		//        accumulatedColor = reflectedColorVec + refractedColorVec + diffuseColor;

		//        return new Color(accumulatedColor);
		//    }
		//    else
		//    {
		//        return SimpleScene.BackgroundColor;
		//    }

		//}

		//// casts all rays from startIndex to endIndex, non inclusive
		//public static void castRays(ref Scene scene, ref Ray[] rays, ref Color[] buffer, int startIndex, int endIndex, int recursiveDepth)
		//{
		//    int bufferIndex = startIndex;
		//    while (bufferIndex < endIndex)
		//    {
		//        Ray r = rays[bufferIndex];

		//        buffer[bufferIndex] = RayCastingUtils.castRay(scene, r, recursiveDepth);

		//        ++bufferIndex;
		//    }
		//}


	}
}
