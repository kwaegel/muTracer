
float
rayTriIntersect(	private Ray*		ray,
					private Triangle	tri,
					private float*		u,
					private float*		v,
					private float4*		collisionPoint,
					private float4*		surfaceNormal)
{
	float eps = 10e-5;	// epsilon

	float4 o = ray->origin;
	float4 d = ray->direction;
	float4 p0 = tri.p0;
	float4 p1 = tri.p1;
	float4 p2 = tri.p2;

	float4 e1 = p1 - p0;
	float4 e2 = p2 - p0;
	float4 q = cross(d,e2);
	float a = dot(e1,q);
	if(a > -eps && a < eps) return HUGE_VALF;
	float f = 1/a;
	float4 s = o-p0;
	*u = f*dot(s,q);
	if(*u<0.0f) return HUGE_VALF;
	float4 r = cross(s,e1);
	*v = f*dot(d,r);
	if(*v<0.0f || *u+*v > 1.0f) return HUGE_VALF;
	float t = f*dot(e2,q);

	(*collisionPoint) = ray->origin + t * ray->direction;
	(*surfaceNormal) = fast_normalize( cross(e1,e2) );

	return t;
}



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
