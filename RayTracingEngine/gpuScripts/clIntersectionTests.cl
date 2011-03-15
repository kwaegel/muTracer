
float
raySphereIntersect(	private	Ray*	ray, 
					private Sphere	sphere,
					//private float4	center, 
					//private float	radius,
					private float4*	collisionPoint,
					private float4* surfaceNormal)
{
	float4 originSubCenter = ray->origin - sphere.center;

	float b = dot(ray->direction, originSubCenter);
	float c = dot(originSubCenter, originSubCenter) - sphere.radius * sphere.radius;
	
	float bSqrSubC = fma(b,b,-c);	// bSqrSubC = b * b - c;
	// if (b*b-c) < 0, ray misses sphere
	if (bSqrSubC < 0)
		return HUGE_VALF;

	float sqrtBC = native_sqrt(bSqrSubC);

	float tPos = -b + sqrtBC;
	float tNeg = -b - sqrtBC;

	float distance = HUGE_VALF;

	if (tPos > 0 && tNeg > 0)
    {
        distance = (tPos < tNeg ? tPos : tNeg);
    }
    else if (tPos > 0 || tNeg > 0)
    {
        distance = (tPos > 0 ? tPos : tNeg);
    }
	else
	{
		return HUGE_VALF;
	}

	(*collisionPoint) = ray->origin + distance * ray->direction;
	(*surfaceNormal) = fast_normalize( (*collisionPoint) - sphere.center );
	return distance;
}
