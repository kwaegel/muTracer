float4 
transformVector16(	const	float16		transform, 
					const	float4		vector)
{
	
	// swizzle to get each part of the transform

	// Dot product the vector by the columns of a row-major matrix
	float4 result;
	result.x = dot(vector, transform.s02468ace.s0246);	// even even	-> first column
	result.y = dot(vector, transform.s13579bdf.s0246);	// odd even		-> second column
	result.z = dot(vector, transform.s02468ace.s1357);	// even odd		-> third column
	result.w = dot(vector, transform.s13579bdf.s1357);	// odd odd		-> fourth column

	return result;
}

kernel
void
hostTransform(	const		float16	transform,
				const		float4	vector,
				__global write_only	float *	vectorOut,
				__global write_only float * matrixOut)
{
	float4 result = transformVector16(transform, vector);

	vectorOut[0] = result.x;
	vectorOut[1] = result.y;
	vectorOut[2] = result.z;
	vectorOut[3] = result.w;

	matrixOut[0] = transform.s0;
	matrixOut[1] = transform.s1;
	matrixOut[2] = transform.s2;
	matrixOut[3] = transform.s3;
	matrixOut[4] = transform.s4;
	matrixOut[5] = transform.s5;
	matrixOut[6] = transform.s6;
	matrixOut[7] = transform.s7;
	matrixOut[8] = transform.s8;
	matrixOut[9] = transform.s9;
	matrixOut[10] = transform.sa;
	matrixOut[11] = transform.sb;
	matrixOut[12] = transform.sc;
	matrixOut[13] = transform.sd;
	matrixOut[14] = transform.se;
	matrixOut[15] = transform.sf;
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

	float sqrtBC = native_sqrt(b * b - c);
	// if sqrtBC < 0, ray misses sphere

	float tPos = -b + sqrtBC;
	float tNeg = -b - sqrtBC;

	float minT = min(tPos, tNeg);

	return minT;
}

kernel
void
render (	const		float4		cameraPosition,
			const		float16		unprojectionMatrix,
			write_only	image2d_t	outputImage)
{
	int2 coord = (int2)(get_global_id(0), get_global_id(1));
	int2 size = get_image_dim(outputImage);

	// convert to normilized device coordinates
	float2 screenPoint2d = (float2)(2.0f, 2.0f) * convert_float2(coord) / convert_float2(size) - (float2)(1.0f, 1.0f);

	// unproject screen point to world
	float4 screenPoint = (float4)(screenPoint2d.x, screenPoint2d.y, 0.0f, 1.0f);
	float4 rayOrigin = transformVector16(unprojectionMatrix, screenPoint);
	rayOrigin.w = 0.001f;
	float4 rayDirection = fast_normalize(rayOrigin - cameraPosition);

	// create test sphere
	float radius = 1;
	float4 spherePosition = (float4)(0.0f, 0.0f, 0.0f, 1.0f);

	// cast ray
	float4 color;
	float t = raySphereIntersect(rayOrigin, rayDirection, spherePosition, radius);

	if (t > 0)
	{
		float4 collisionPoint = rayOrigin + t * rayDirection;
		float4 surfaceNormal = collisionPoint - spherePosition;
		surfaceNormal = normalize(surfaceNormal);
		
		color = (float4)(0.0f, 0.5f, 0.0f, 0.0f);
	}
	else
	{
		color = (float4)(0.0f, 0.0f, 0.0f, 0.0f);
	}

	write_imagef(outputImage, coord, color);
}