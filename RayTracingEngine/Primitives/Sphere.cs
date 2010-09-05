using System;

//using Microsoft.Xna.Framework;
using OpenTK;

using Raytracing;
using Raytracing.BoundingVolumes;

namespace Raytracing.Primitives
{
	public class Sphere : AbstractPrimitive
	{
		Vector3 _center;
		public override Vector3 Position
		{
			get
			{
				return _center;
			}
			set
			{
				_center = value;
			}
		}

		float _radius;
		public float Radius
		{
			get
			{
				return _radius;
			}
			set
			{
				_radius = value;
			}
		}

		public Sphere(Vector3 position, float radius)
			: this(position, radius, AbstractPrimitive.BasicMaterial)
		{	
		}

		public Sphere(Vector3 position, float radius, Material mat)
		{
			_center = position;
			_radius = radius;
			Material = mat;
		}

		/// <summary>
		/// Get a axis-aligned bounding box that contains this model.
		/// </summary>
		/// <returns></returns>
		public override AxisAlignedBoundingBox getBoundingBox()
		{
			Vector3 min = new Vector3(_center.X - _radius, _center.Y - _radius, _center.Z - _radius);
			Vector3 max = new Vector3(_center.X + _radius, _center.Y + _radius, _center.Z + _radius);

			return new AxisAlignedBoundingBox(min, max);
		}

		// the ray does not need to be a unit ray. It needs a distance bound.
		public override float? intersects(ref Ray r, ref Vector3 collisionPoint,
			ref Vector3 surfaceNormal)
		{
			// l = c - o
			Vector3 distanceVector = _center - r.Position;

			// project the calculated direction vector onto the ray direction vector
			// s = l * d
			float projectedDistance = Vector3.Dot(distanceVector, r.Direction);

			// l2 = l * l
			float lsquared = Vector3.Dot(distanceVector, distanceVector);

			// test if the sphere is outside and behind the ray
			float radiusSquared = _radius * _radius;
			// if s < 0 and l2 > r2
			if (projectedDistance < 0 && lsquared > radiusSquared)
			{
				return null;
			}

			// m2 = l2 - s2
			float rayDistanceFromCenterSquared = lsquared - projectedDistance * projectedDistance;

			// if the ray is pointing away from the sphere
			// if (m2 > r2)
			if (rayDistanceFromCenterSquared > radiusSquared)
			{
				return null;
			}

			// q = sqrt(r2 - m2)
			double q = System.Math.Sqrt(radiusSquared - rayDistanceFromCenterSquared);

			// pick the nearer value of t (the collision distance)
			double t;
			// if l2 > r2
			//	t = s - q
			if (lsquared > radiusSquared)
			{
				t = projectedDistance - q;
			}
			else
			{
				t = projectedDistance + q;
			}

			// get the collision point and surface normal
			r.Direction.Normalize();
			collisionPoint = r.Position + (Vector3)((float)t * r.Direction);
			surfaceNormal = Vector3.Subtract(collisionPoint, _center);
			surfaceNormal.Normalize();

			collisionPoint = _center + _radius * surfaceNormal;

			return (float)t;
		}

		// the ray does not need to be a unit ray. It needs a distance bound.
		public override bool simpleIntersects(ref Ray r)
		{
			// l = c - o
			Vector3 distanceVector = _center - r.Position;

			// project the calculated direction vector onto the ray direction vector
			// s = l * d
			float projectedDistance = Vector3.Dot(distanceVector, r.Direction);

			// l2 = l * l
			float lsquared = Vector3.Dot(distanceVector, distanceVector);

			// test if the sphere is outside and behind the ray
			float radiusSquared = _radius * _radius;
			// if s < 0 and l2 > r2
			if (projectedDistance < 0 && lsquared > radiusSquared)
			{
				return false;
			}

			// m2 = l2 - s2
			float rayDistanceFromCenterSquared = lsquared - projectedDistance * projectedDistance;

			// if the ray is pointing away from the sphere
			// if (m2 > r2)
			if (rayDistanceFromCenterSquared > radiusSquared)
			{
				return false;
			}

			// at this point, the ray must have collided with the sphere
			return true;

			//// q = sqrt(r2 - m2)
			//double q = Math.Sqrt(radiusSquared - rayDistanceFromCenterSquared);

			//// pick the nearer value of t (the collision distance)
			//double t;
			//// if l2 > r2
			////	t = s - q
			//if (lsquared > radiusSquared)
			//{
			//    t = projectedDistance - q;
			//}
			//else
			//{
			//    t = projectedDistance + q;
			//}

			//// get the collision point and surface normal
			//r.Direction.Normalize();
			//collisionPoint = r.Position + (Vector3)((float)t * r.Direction);
			//surfaceNormal = Vector3.Subtract(collisionPoint, _center);
			//surfaceNormal.Normalize();

			//collisionPoint = _center + _radius * surfaceNormal;

			//return (float)t;
		}

		public override string ToString()
		{
			return "Shpere center: " + _center + ", r=" + _radius;
		}
	}


	//class SceneSphere : Primitive
	//{
	//    public override Vector3 Position
	//    {
	//        get
	//        {
	//            return _sphere.Center;
	//        }
	//        set
	//        {
	//            _sphere.Center = value;
	//        }
	//    }

	//    BoundingSphere _sphere;

	//    public SceneSphere(Vector3 position, float radius, Material mat)
	//    {
	//        _sphere = new BoundingSphere(position, radius);
	//        this.Material = mat;
	//    }

	//    // pass the ray and sphere by ref for speed. They will not be modified.
	//    public override float? intersects(ref Ray ray, ref Vector3 collisionPoint,
	//        ref Vector3 surfaceNormal)
	//    {
	//        // call the XNA intersection for now
	//        float? distance = ray.Intersects(_sphere);

	//        // if distance has a value
	//        if (distance.HasValue)
	//        {

	//            collisionPoint = ray.Position + (Vector3)(distance * ray.Direction);
	//            surfaceNormal = Vector3.Subtract(collisionPoint, _sphere.Center);
	//            surfaceNormal.Normalize();

	//            // move the collision point slightly away from the surface of the sphere.
	//            // otherwise it may be inside the surface.
	//            collisionPoint += 0.00005f * surfaceNormal;
	//        }
	//        else
	//        {
	//            collisionPoint = Vector3.Zero;
	//            surfaceNormal = Vector3.Zero;
	//        }
	//        return distance;
	//    }
	//}

}
