
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

	if (det < EPSILON)
		return 0;

	/* calculate distance from vert0 to ray origin */
	float4 tvec = orig - vert0;				//SUB(tvec, orig, vert0);

	/* calculate U parameter and test bounds */
	*u = dot(tvec, pvec);					//*u = DOT(tvec, pvec);
	if (*u < 0.0 || *u > det)
		return 0;

	/* prepare to test V parameter */
	float4 qvec = cross(tvec, edge1);		//CROSS(qvec, tvec, edge1);

	/* calculate V parameter and test bounds */
	*v = dot(dir, qvec);					//*v = DOT(dir, qvec);
	if (*v < 0.0 || *u + *v > det)
		return 0;

	/* calculate t, scale parameters, ray intersects triangle */
	float t = dot(edge2, qvec);				//*t = DOT(edge2, qvec);
	float inv_det = 1.0f / det;
	t *= inv_det;
	*u *= inv_det;
	*v *= inv_det;

	*collisionPoint = ray->origin + t * ray->direction;
	*surfaceNormal = fast_normalize( cross(edge1, edge2) );

	return t;

	/*
	float4 e1 = tri.p1 - tri.p0;
	float4 e2 = tri.p2 - tri.p0;
	e2.w=0;
	float4 q = cross(d,e2);
	float a = dot(e1,q);
	if(a > -eps && a < eps) return HUGE_VALF;
	float f = 1/a;
	float4 s = o-tri.p0;
	*u = f*dot(s,q);
	if(*u<0.0f) return HUGE_VALF;
	float4 r = cross(s,e1);
	*v = f*dot(d,r);
	if(*v<0.0f || *u+*v > 1.0f) return HUGE_VALF;
	float t = f*dot(e2,q);

	(*collisionPoint) = ray->origin + t * ray->direction;
	(*surfaceNormal) = fast_normalize( cross(e1,e2) );

	return t;
	*/
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
