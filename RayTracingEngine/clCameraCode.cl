
typedef struct
{
	float4 CenterAndRadius;	// packed into a float4 to maintain alignment.
	float4 Color;
} SphereStruct;

float4
transformVector(	const	float16		transform, 
					const	float4		vector)
{
	// Swizzle to get each part of the transform.
	// Dot product the vector by the columns of a row-major matrix.
	float4 result;
	result.x = dot(vector, transform.s02468ace.s0246);	// even even	-> first column
	result.y = dot(vector, transform.s13579bdf.s0246);	// odd even		-> second column
	result.z = dot(vector, transform.s02468ace.s1357);	// even odd		-> third column
	result.w = dot(vector, transform.s13579bdf.s1357);	// odd odd		-> fourth column

	return result;
}

float
raySphereIntersect(	private float4	origin, 
					private float4	direction, 
					private float4	center, 
					private float	radius)
{
	float4 originSubCenter = origin - center;

	float b = dot(direction, originSubCenter);
	float c = dot(originSubCenter, originSubCenter) - radius * radius;
	
	float bSqrSubC = b * b - c;
	// if (b*b-c) < 0, ray misses sphere
	if (bSqrSubC < 0)
		return -1;

	float sqrtBC = native_sqrt(b * b - c);

	float tPos = -b + sqrtBC;
	float tNeg = -b - sqrtBC;

	if (tPos < tNeg && tPos >= 0)
	{
		return tPos;
	}
	else if (tNeg < tPos && tNeg >= 0)
	{
		return tNeg;
	}

	// Return the minimum positive value of T.
	return min(max(0.0001f, tPos), max(0.0001f, tNeg));
}

kernel
void
render (				const		float4			cameraPosition,
						const		float16			unprojectionMatrix,
						const		float4			backgroundColor,
						write_only	image2d_t		outputImage,
			__global	const		SphereStruct*	sphereArray,
						const		int				sphereCount)
{
	int2 coord = (int2)(get_global_id(0), get_global_id(1));
	int2 size = get_image_dim(outputImage);

	// convert to normilized device coordinates
	float2 screenPoint2d = (float2)(2.0f, 2.0f) * convert_float2(coord) / convert_float2(size) - (float2)(1.0f, 1.0f);

	// unproject screen point to world
	float4 screenPoint = (float4)(screenPoint2d.x, screenPoint2d.y, 0.0f, 0.0f);	// Why does Z need to equal 0??
	float4 rayOrigin = transformVector(unprojectionMatrix, screenPoint);
	float4 rayDirection = fast_normalize(rayOrigin - cameraPosition);

	// create test light
	float4 lightPosition = (float4)(0.0f, 5.0f, 0.0f, 1.0f);

	// set the default background color
	float4 color = backgroundColor;

	float nearestIntersection = INFINITY;

	for (int i=0; i<sphereCount; i++)
	{
		SphereStruct sphere = sphereArray[i];
		float4 center = sphere.CenterAndRadius;
		float radius = center.w;
		center.w=1;

		// cast ray and check for collisions
		float t = raySphereIntersect(rayOrigin, rayDirection, center, radius);

		if (t > 0 && t < nearestIntersection)
		{
			nearestIntersection = t;

			float4 collisionPoint = rayOrigin + t * rayDirection;
			float4 surfaceNormal = collisionPoint - center;
			surfaceNormal = normalize(surfaceNormal);

			// get shading
			float4 lightDirection = lightPosition - collisionPoint;
			lightDirection = normalize(lightDirection);
			float shadeFactor = dot(surfaceNormal, lightDirection);
		
			color = sphere.Color;
			color *= shadeFactor;

			color = (float4)(shadeFactor,shadeFactor,shadeFactor,0.0f);
		}
	}

	write_imagef(outputImage, coord, color);
}
