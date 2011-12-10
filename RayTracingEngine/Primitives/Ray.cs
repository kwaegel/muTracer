using OpenTK;

namespace Raytracing.Primitives
{

	public class Ray
	{
		public Vector3 Origin;
		public Vector3 Direction;
		public float CurrentRefractiveIndex = 1.0f;
		public float tMin = 0, tMax = 0;	// Parametric range the ray can intersect

		public Ray(Vector3 position, Vector3 direction, float indexOfRefraction)
			: this(position, direction, 0, float.PositiveInfinity)
		{
			CurrentRefractiveIndex = indexOfRefraction;
		}

		public Ray(Vector3 position, Vector3 direction, float minT, float maxT)
		{
			Origin = position;
			Direction = direction;
			this.tMin = minT;
			this.tMax = maxT;
		}

		public float intersects(Sphere sphere, ref Vector3 collisionPoint, ref Vector3 surfaceNormal)
		{
			float t = intersects(sphere);
			if (t < float.PositiveInfinity)
			{
				// get the collision point and surface normal
				this.Direction.Normalize();
				collisionPoint = this.Origin + (Vector3)((float)t * this.Direction);
				surfaceNormal = Vector3.Subtract(collisionPoint, sphere.Position);
				surfaceNormal.Normalize();

				collisionPoint = sphere.Position + sphere.Radius * surfaceNormal;
			}

			return t;
		}

		// the ray does not need to be a unit ray. It needs a distance bound.
		public float intersects(Sphere sphere)
		{
			// l = c - o
			Vector3 distanceVector = sphere.Position - Origin;

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
				return float.PositiveInfinity;
			}

			// m2 = l2 - s2
			float rayDistanceFromCenterSquared = lsquared - projectedDistance * projectedDistance;

			// if the ray is pointing away from the sphere
			// if (m2 > r2)
			if (rayDistanceFromCenterSquared > radiusSquared)
			{
				return float.PositiveInfinity;
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

			return (float)t;
		}

	}
}
