
typedef struct {
	float4 rayOrigin;
	float4 rayDirection;
	float4 gridSpaceCoordinates;
	float4 frac;
	float4 tMax;
	float4 tDelta;
	float4 cellData;
	float4 index;
} debugStruct;

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
	
	float bSqrSubC = fma(b,b,-c);	// bSqrSUbC = b * b - c;
	// if (b*b-c) < 0, ray misses sphere
	if (bSqrSubC < 0)
		return -1;

	float sqrtBC = native_sqrt(bSqrSubC);

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
}

kernel
void
render (	const		float4		cameraPosition,
			const		float16		unprojectionMatrix,
			const		float4		backgroundColor,
			write_only	image2d_t	outputImage,
			read_only	image3d_t	voxelGrid,
			const		float		cellSize,
			__global write_only		debugStruct * debug)
{
	int2 coord = (int2)(get_global_id(0), get_global_id(1));
	int2 size = get_image_dim(outputImage);

	bool debugPixel = coord.x == 189 && coord.y == 189;

	const sampler_t smp = 
		CLK_NORMALIZED_COORDS_FALSE | //Natural coordinates
		CLK_ADDRESS_CLAMP | //Clamp to zeros
		CLK_FILTER_NEAREST; //Don't interpolate

	/**** Create a ray in world coordinates ****/

	// convert to normilized device (screen) coordinates [-1, 1]
	float2 screenPoint2d = (float2)(2.0f) * convert_float2(coord) / convert_float2(size) - (float2)(1.0f);

	// unproject screen point to world
	float4 screenPoint = (float4)(screenPoint2d.x, screenPoint2d.y, -1.0f, 1.0f);
	float4 rayOrigin = transformVector(unprojectionMatrix, screenPoint);
	float4 rayDirection = normalize(rayOrigin - cameraPosition);

	// create a generic test light
	float4 lightPosition = (float4)(10.0f, 20.0f, 10.0f, 1.0f);

	// set the default background color
	float4 color = backgroundColor;

	

	/**** Traverse the grid and find the nearest occupied cell ****/

	// setup up traversel variables

	// get grid size from the texture file
	//int4 gridSize = (int4)(get_image_width(voxelGrid), get_image_height(voxelGrid), get_image_depth(voxelGrid), 0);
	int gridWidth = get_image_width(voxelGrid);

	// traversel values

	// Center the grid at 0,0,0
	float4 halfGridWidth = (gridWidth * cellSize) / 2.0f;
	float4 gridOrigin = -halfGridWidth;

	// convert the ray start position to grid space
	float4 gridSpaceCoordinates = rayOrigin - gridOrigin;

	// get the current grid cell index and the distance to the next cell boundary
	// index = gridCoords / cellSize (integer division).
	//  frac = gridCoords % cellSize
	int4 index;	// index of the current voxel
	float4 frac = -remquo(gridSpaceCoordinates, (float4)cellSize, &index);
	
	// Don't draw anything if the camera is outside the grid.
	// This prevents indexOutOfBounds exceptions during testing.
	if (index.x < 0 || index.x >= gridWidth ||
		index.y < 0 || index.y >= gridWidth ||
		index.z < 0 || index.z >= gridWidth)
	{
		return;
	}

	int4 step = -1;		// cell direction to step in
	int4 out = -1;		// index of the first positive invalid voxel index.
	if (rayDirection.x >= 0)
	{
		out.x = gridWidth;
		step.x = 1;
		frac.x = cellSize + frac.x;		// frac is negative
	}
	if (rayDirection.y >= 0)
	{
		out.y = gridWidth;
		step.y = 1;
		frac.y = cellSize + frac.y;		// frac is negative
	}
	if (rayDirection.z >= 0)
	{
		out.z = gridWidth;
		step.z = 1;
		frac.z = cellSize + frac.z;		// frac is negative
	}

	// tMax: min distance to move before crossing a gird boundary
	float4 tMax = frac / rayDirection;

	// tDelta: distance (in t) between cell boundaries
	float4 tDelta = ((float4)cellSize) / rayDirection;// compute projections onto the coordinate axes
	tDelta = copysign(tDelta, (float4)1.0f);	// must be positive

	// begin grid traversel
	/*
	 * Might want to change this to a while() loop to test the current voxel first,
	 * before moving to the next one. I am not sure why this seems to be working in
	 * the C# version.
	* */
	bool containsGeometry = false;
	float4 cellData;

	// Check grid data at origional index
	cellData = read_imagef(voxelGrid, smp, index);
	containsGeometry = cellData.x > 0.5f || cellData.y > 0.5f || cellData.z > 0.5f || cellData.w > 0;

	if (debugPixel)
	{
		int debugIndex = 0;
		debug[debugIndex].rayOrigin = rayOrigin;
		debug[debugIndex].rayDirection = rayDirection;
		debug[debugIndex].gridSpaceCoordinates = gridSpaceCoordinates;
		debug[debugIndex].frac = frac;
		debug[debugIndex].tMax = tMax;
		debug[debugIndex].tDelta = tDelta;
		debug[debugIndex].cellData = cellData;
		debug[debugIndex].index = convert_float4(index);
	}

	while (!containsGeometry)
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
		cellData = read_imagef(voxelGrid, smp, index);
		containsGeometry = cellData.x > 0.5f || cellData.y > 0.5f || cellData.z > 0.5f || cellData.w > 0;
	}


	/**** Write output to image ****/
	if (containsGeometry)
	{
		color = cellData;
	}

	if (debugPixel)
	{
		int debugIndex = 1;
		debug[debugIndex].rayOrigin = rayOrigin;
		debug[debugIndex].rayDirection = rayDirection;
		debug[debugIndex].gridSpaceCoordinates = gridSpaceCoordinates;
		debug[debugIndex].frac = frac;
		debug[debugIndex].tMax = tMax;
		debug[debugIndex].tDelta = tDelta;
		debug[debugIndex].cellData = cellData;
		debug[debugIndex].index = convert_float4(index);
	}

	write_imagef(outputImage, coord, color);
}