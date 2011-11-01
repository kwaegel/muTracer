
float
rayTriIntersect(	private Ray*		ray,
					private Triangle	tri,
					private float*		u,
					private float*		v,
					private float4*		collisionPoint,
					private float4*		surfaceNormal)
{
	float EPSILON = 10e-5;	// epsilon

	float4 orig = ray->origin;
	float4 dir = ray->direction;

	float4 vert0 = tri.p0;
	float4 vert1 = tri.p1;
	float4 vert2 = tri.p2;

	vert0.w=1;
	vert1.w=1;
	vert2.w=1;


	/* find vectors for two edges sharing vert0 */
	float4 edge1 = vert1 - vert0;			//SUB(edge1, vert1, vert0);
	float4 edge2 = vert2 - vert0;			//SUB(edge2, vert2, vert0);

	/* begin calculating determinant - also used to calculate U parameter */
	float4 pvec = cross(dir, edge2);		//CROSS(pvec, dir, edge2);

	/* if determinant is near zero, ray lies in plane of triangle */
	float det = dot(edge1, pvec);			//det = DOT(edge1, pvec);

	if (det > -EPSILON && det < EPSILON)
		return HUGE_VALF;

	float inv_det = 1.0f / det;

	/* calculate distance from vert0 to ray origin */
	float4 tvec = orig - vert0;					//SUB(tvec, orig, vert0);

	/* calculate U parameter and test bounds */
	*u = dot(tvec, pvec) * inv_det;	//*u = DOT(tvec, pvec) * inv_det;
	if (*u < 0.0 || *u > 1.0)
		return HUGE_VALF;

	/* prepare to test V parameter */
	float4 qvec = cross(tvec, edge1);		//CROSS(qvec, tvec, edge1);

	/* calculate V parameter and test bounds */
	*v = dot(dir, qvec) * inv_det;				//*v = DOT(dir, qvec) * inv_det;
	if (*v < 0.0 || *u + *v > 1.0f)
		return HUGE_VALF;

	/* calculate t, ray intersects triangle */
	float t = dot(edge2, qvec) * inv_det;				//*t = DOT(edge2, qvec) * inv_det;

	if (t < 0)
		return HUGE_VALF;

	*collisionPoint = ray->origin + t * ray->direction;
	*surfaceNormal = fast_normalize( cross(edge1, edge2) );

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
