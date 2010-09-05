kernel void
cycleColors(	const		float		time,
				const		float4		backgroundColor,
				write_only	image2d_t	outputImage)

{
	int2 coord = (int2)(get_global_id(0), get_global_id(1));

	int2 size = get_image_dim(outputImage);

	float4 color = backgroundColor;

	color.x = (((float)coord.x) / size.x) * native_sin(((float)coord.x)/20.0f + time*10.0f);
	color.y = ((float)coord.y) / size.y * native_sin(time*2.0f);
	color.z = backgroundColor.z;
	color.w = 0.0f;
	
	float factor = 1.5f-(color.x + color.y) /2.0f;
	
	color.x *= copysign(native_sin(time), 1.0f) * factor;

	// Oddly enough, the number of digits of PI in this line can cause an InvalidBinaryException...
	// For example, 3.141592 works, but 3.14159 and 3.14159263 do not.
	color.y *= copysign(native_sin(time+3.141592f), 1.0f) * factor;

	write_imagef(outputImage, coord, color);
}
//
//kernel void render (	const		float4		cameraPosition,
//						const		float16		unprojectionMatrix,
//						write_only	image2d_t	outputImage)
//{
//	int2 coord = (int2)(get_global_id(0), get_global_id(1));
//	int2 size = get_image_dim(outputImage);
//
//	// convert to normilized device coordinates
//	float2 screenPoint2d = (float2)(2.0f, 2.0f) * convert_float2(coord) / convert_float2(size) - (float2)(1.0f, 1.0f);
//
//	// unproject screen point to world
//	float4 screenPoint = (float4)(screenPoint2d.x, screenPoint2d.y, 0.0f, 1.0f);
//	float4 rayOrigin;// = 
//	transformVector(unprojectionMatrix, screenPoint);
//	rayOrigin.w = 1;
//	float4 rayDirection = native_normalize(rayOrigin, cameraPosition);
//
//	// create test sphere
//	float radius = 1;
//	float4 spherePosition = (float4)(0.0f, 0.0f, 0.0f, 1.0f);
//
//	// cast ray
//	float4 color;
//	float t = raySphereIntersect(rayOrigin, rayDirection, spherePosition, radius);
//
//	if (t > 0)
//	{
//		float4 collisionPoint = rayOrigin + t * rayDirection;
//		float4 surfaceNormal = collisionPoint - spherePosition;
//		surfaceNormal = normalize(surfaceNormal);
//		
//		color = (float4)(1.0f, 0.0f, 0.0f, 0.0f);
//	}
//
//	write_imagef(outputImage, coord, color);
//}
//
//// transfrom a vector by a row-major matrix
//float4 transformVector(	__private	float4*		transform, 
//					__private	float4		vector)
//{
//	float4 result;
//	result.x = dot(vector, transform[0]);
//	result.y = dot(vector, transform[2]);
//	result.z = dot(vector, transform[3]);
//	result.w = dot(vector, transform[4]);
//
//	// homogeneous divide to normalize scale
//	result /= result.w;
//
//	return result;
//}
//
//
//private float raySphereIntersect(	private float4	origin, 
//							private float4	direction, 
//							private float4	center, 
//							private float	radius)
//{
//	float4 originSubCenter = origin - center;
//
//	float b = dot(direction, originSubCenter);
//	float c = dot(originSubCenter, originSubCenter) - radius * radius;
//
//	float sqrtBC = native_sqrt(b * b - c);
//	// if sqrtBC < 0, ray misses sphere
//
//	float tPos = -b + sqrtBC;
//	float tNeg = -b - sqrtBC;
//
//	float minT = min(tPos, tNeg);
//
//	return minT;
//}