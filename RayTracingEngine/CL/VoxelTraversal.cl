
typedef struct {
	float4 rayOrigin;
	float4 rayDirection;
	float4 gridSpaceCoordinates;
	float4 frac;
	float4 tMax;
	float4 tDelta;
	float4 cellData;
	float4 index;
	float4 step;
} debugStruct;

typedef struct{
	float4 position;
	float4 colorAndIntensity;
} pointLight;

float4
myRemquo(float4 x, float4 y, int4* quo)
{
	float4 n = floor(x/y);
	(*quo) = convert_int4(n);
	return (x - n * y);
}

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
					private float	radius,
					private float4*	collisionPoint,
					private float4* surfaceNormal)
{
	float4 originSubCenter = origin - center;

	float b = dot(direction, originSubCenter);
	float c = dot(originSubCenter, originSubCenter) - radius * radius;
	
	float bSqrSubC = fma(b,b,-c);	// bSqrSUbC = b * b - c;
	// if (b*b-c) < 0, ray misses sphere
	if (bSqrSubC < 0)
		return HUGE_VALF;

	float sqrtBC = native_sqrt(bSqrSubC);

	float tPos = -b + sqrtBC;
	float tNeg = -b - sqrtBC;

	float distence = HUGE_VALF;
	if (tPos < tNeg && tPos > 0)
	{
		distence = tPos;
	}
	else if (tNeg > 0)
	{
		distence  = tNeg;
	}
	else
	{
		return HUGE_VALF;	// Error condition: ray misses sphere.
	}

	(*collisionPoint) = origin + distence * direction;
	(*surfaceNormal) = fast_normalize( (*collisionPoint) - center );
	return distence;
}

/*
	Find the nearest intersection with geometry in a voxel. Returns HUGE_VALF if
	no intersection is found.
*/
float
intersectCellContents(			float4		rayOrigin, 
								float4		rayDirection,
						const	int			vectorsPerVoxel,
								int			geometryIndex,
		__global	read_only	float4*		geometryArray,
						private float4*		collisionPoint,
						private float4*		surfaceNormal)
{
	float minDistence = HUGE_VALF;
	for (int i=0; i<vectorsPerVoxel; i++)
	{

		float4 sphere = geometryArray[geometryIndex+i];

		// Unpack sphere data.
		float4 center = sphere;
		center.w=1;
		float radius = sphere.w;

		// calculate intersection distance. Returns HUGE_VALF if ray misses sphere.
		float distence = raySphereIntersect(rayOrigin, rayDirection, center, radius, collisionPoint, surfaceNormal);

		if (distence < minDistence)
		{
			minDistence = distence;
		}
	}

	return minDistence;
}


float
findNearestIntersection(	
							float4		rayOrigin,
							float4		rayDirection,
							float4*		collisionPoint,		// Output collision point and surface normal
							float4*		surfaceNormal,		// and return the distence.
			
		global	read_only	image3d_t	voxelGrid,			// Voxel data
				const		float		cellSize,
			
		global	read_only	float4*		geometryArray,		// Geometry data
				const		int			vectorsPerVoxel)
{
	const sampler_t smp = 
		CLK_NORMALIZED_COORDS_FALSE | //Natural coordinates
		CLK_ADDRESS_CLAMP | //Clamp to zeros
		CLK_FILTER_NEAREST; //Don't interpolate

/**** Traverse the grid and find the nearest occupied cell ****/

	// setup up traversel variables

	// get grid size from the texture file. Assume identical sides.
	int gridWidth = get_image_width(voxelGrid);

	// traversel values

	// Center the grid at 0,0,0
	float4 halfGridWidth = (gridWidth * cellSize) / 2.0f;
	float4 gridOrigin = -halfGridWidth;

	// convert the ray start position to grid space
	float4 gridSpaceCoordinates = rayOrigin - gridOrigin;

	// Need to use cellSize as a vector, so only expand it once.
	float4 cellSizeVec = (float4)(cellSize);

	// get the current grid cell index and the distance to the next cell boundary
	// index = gridCoords / cellSize (integer division).
	//  frac = gridCoords % cellSize
	int4 index;	// index of the current voxel
	float4 lowerFraction = myRemquo(gridSpaceCoordinates, cellSizeVec, &index);
	float4 upperFraction = cellSizeVec - lowerFraction;
	
	// Don't draw anything if the camera is outside the grid.
	// This prevents indexOutOfBounds exceptions during testing.
	// TODO: change to a box intersection test and allow drawing outside the grid.
	if (index.x < 0 || index.x >= gridWidth ||
		index.y < 0 || index.y >= gridWidth ||
		index.z < 0 || index.z >= gridWidth)
	{
		return;
	}

	// MSB of a float is the sign bit, so the select call can switch based on the sign bit.
	// value = MSBset ? b : a;
	// first if positive, second if negitive
	int4 out =		select((int4)gridWidth,		  (int4)-1,		as_int4(rayDirection));
	int4 step =		select((int4)		 1,		  (int4)-1,		as_int4(rayDirection));
	float4 frac =	select(  upperFraction,  -lowerFraction,	as_int4(rayDirection));

	// tMax: min distance to move before crossing a gird boundary
	float4 tMax = frac / rayDirection;

	// tDelta: distance (in t) between cell boundaries
	float4 tDelta = cellSizeVec / rayDirection;	// compute projections onto the coordinate axes.
	tDelta = copysign(tDelta, (float4)1.0f);	// ensure tDelta is positive.

	// begin grid traversel
	/*
	 * Might want to change this to a while() loop to test the current voxel first,
	 * before moving to the next one. I am not sure why this seems to be working in
	 * the C# version.
	* */
	bool containsGeometry = false;
	bool rayHalted = false;
	float minDistence = HUGE_VALF;
	int4 cellData;

	// Check grid data at origional index
	cellData = read_imagei(voxelGrid, smp, index);
	containsGeometry = cellData.x > 0 || cellData.y > 0 || cellData.z > 0 || cellData.w > 0;

	while (!rayHalted)
	{
		if (tMax.x < tMax.y)
		{
			if (tMax.x < tMax.z)
			{
				index.x += step.x;			// step to next voxel along this axis
				if (index.x == out.x)		// outside grid
					break; 
				tMax.x = tMax.x + tDelta.x;	// increment max distence to next voxel
			}
			else
			{
				index.z += step.z;
				if (index.z == out.z)
					break;
				tMax.z = tMax.z + tDelta.z;
			}
		}
		else
		{
			if (tMax.y < tMax.z)
			{
				index.y += step.y;
				if (index.y == out.y)
					break;
				tMax.y = tMax.y + tDelta.y;
			}
			else
			{
				index.z += step.z;
				if (index.z == out.z)
					break;
				tMax.z = tMax.z + tDelta.z;
			}
		}

		// get grid data at index
		cellData = read_imagei(voxelGrid, smp, index);
		containsGeometry = cellData.x > 0 || cellData.y > 0 || cellData.z > 0 || cellData.w > 0;

		if (containsGeometry)
		{
			// check for intersection with geometry in the current cell
			int geometryIndex = (index.x * gridWidth * gridWidth + index.y * gridWidth + index.z) * vectorsPerVoxel;
			
			minDistence = intersectCellContents(rayOrigin, rayDirection, cellData.x, geometryIndex, geometryArray, collisionPoint, surfaceNormal);

			// Halt ray progress if it collides with anything.
			if (minDistence < HUGE_VALF)
			{
				rayHalted = true;
			}

		} // End checking geometry.
	} // End voxel traversel loop

	return minDistence;
}

kernel
void
render (	const		float4		cameraPosition,
			const		float16		unprojectionMatrix,
			const		float4		backgroundColor,
			write_only	image2d_t	outputImage,

			// Voxel data
			read_only	image3d_t	voxelGrid,
			const		float		cellSize,

			// Geometry
__global	read_only	float4 *	geometryArray,
			const		int			vectorsPerVoxel,

			// Lights
__global	read_only	float8*		pointLights,
			const		int			pointLightCount,
__local					float8*		localLightBuffer,

			// Debug structs
__global	write_only	debugStruct* debug,
			const		int			debugSetCount,
			const		int2		debugPixelLocation
			)
{
	// Copy global data to local buffers
	event_t lightsFinishedLoading = async_work_group_copy(localLightBuffer, pointLights, (size_t)(pointLightCount), 0);

	int2 coord = (int2)(get_global_id(0), get_global_id(1));
	int2 size = get_image_dim(outputImage);

	///// DEBUG VALUES /////
	bool debugPixel = coord.x == debugPixelLocation.x && coord.y == debugPixelLocation.y;
	int debugIndex = 0;

	///// Create a ray in world coordinates /////

	// convert to normilized device (screen) coordinates [-1, 1]
	float2 screenPoint2d = (float2)(2.0f) * convert_float2(coord) / convert_float2(size) - (float2)(1.0f);

	// unproject screen point to world
	float4 screenPoint = (float4)(screenPoint2d.x, screenPoint2d.y, -1.0f, 1.0f);
	float4 rayOrigin = transformVector(unprojectionMatrix, screenPoint);
	float4 rayDirection = normalize(rayOrigin - cameraPosition);

	// set the default background color
	float4 color = backgroundColor;

	float4 collisionPoint;
	float4 surfaceNormal;

	// find the nearest intersected object
	float distence = findNearestIntersection(rayOrigin, rayDirection, &collisionPoint, &surfaceNormal,
													voxelGrid, cellSize, geometryArray, vectorsPerVoxel);

	// If the ray has hit somthing, draw the color of that object.
	if (distence < HUGE_VALF)
	{
		color = (float4)(0.0f);
		wait_group_events(1, &lightsFinishedLoading);

		float4 objectColor = (float4)(0.5f, 0.0f, 0.0f, 0.0f);	// Use generic color for testing.

		// Sum up contributions of all light sources
		for (int lightIndex = 0; lightIndex < pointLightCount; lightIndex++)
		{
			// test cosine shading
			// TODO: the async copy is not copying the light buffer correctly.
			//float8 lightData = localLightBuffer[lightIndex];
			float4 lightPosition = pointLights[lightIndex].s0123;
			float4 lightColor = pointLights[lightIndex].s4567;
			//float4 lightPosition = localLightBuffer[lightIndex].s0123;
			//float4 lightColor = localLightBuffer[lightIndex].s4567;
			float lightIntensity = lightColor.w;

			if (debugPixel)
			{
				debug[debugIndex].frac = lightPosition;
				debug[debugIndex].tMax = lightColor;
				debug[debugIndex].index.x = (float)(lightIndex);
				debug[debugIndex].index.y = (float)(pointLightCount);
				debug[debugIndex].index.z = lightIntensity;
				debugIndex++;
			}
			

			float4 lightVector = lightPosition - collisionPoint;
			float lightDistence = length(lightVector);
			float4 lightDirection = fast_normalize(lightVector);

			float shade = clamp(dot(surfaceNormal, lightDirection),0.0f,1.0f);	// Clamped cosine shading

			// Apply shading modifiers.
			float4 lightContrib = objectColor;
			lightContrib *= (float4)(shade);
			lightContrib *= lightIntensity;
			lightContrib *= 1.0f/(lightDistence*lightDistence);	// Inverse square law
			
			// Add light contribution to total color.
			color += lightContrib;
		}
	}

//if (debugPixel)
//{
//	color = (float4)(1.0f);
//}

	// Write the resulting color to the camera texture.
	write_imagef(outputImage, coord, color);
}