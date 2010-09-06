using OpenTK;

namespace Raytracing.Primitives
{
	
	public class Ray
	{
		public Vector3 Position;
		public Vector3 Direction;

		public Ray(Vector3 position, Vector3 direction)
		{
			Position = position;
			Direction = direction;
		}

		#region Intersection methods 
		public float? Intersects(AbstractPrimitive primitive)
		{
			return null;
		}

		// the ray does not need to be a unit ray. It needs a distance bound.
		public float? intersects(Sphere sphere, ref Vector3 collisionPoint,
			ref Vector3 surfaceNormal)
		{
			// l = c - o
			Vector3 distanceVector = sphere.Position - Position;

			// project the calculated direction vector onto the ray direction vector
			// s = l * d
			float projectedDistance = Vector3.Dot(distanceVector, this.Direction);

			// l2 = l * l
			float lsquared = Vector3.Dot(distanceVector, distanceVector);

			// test if the sphere is outside and behind the ray
			float radiusSquared = sphere.Radius * sphere.Radius;
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
			this.Direction.Normalize();
			collisionPoint = this.Position + (Vector3)((float)t * this.Direction);
			surfaceNormal = Vector3.Subtract(collisionPoint, sphere.Position);
			surfaceNormal.Normalize();

			collisionPoint = sphere.Position + sphere.Radius * surfaceNormal;

			return (float)t;
		}

		public float Intersects(BoundingVolumes.AxisAlignedBoundingBox box)
		{
			// Return zero to assume the collision has occured.
			return 0;
		}

		#endregion

	}
}
