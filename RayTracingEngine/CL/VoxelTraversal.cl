﻿
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

	// Return the minimum positive value of T.
	return min(max(0.0001f, tPos), max(0.0001f, tNeg));
}

kernel
void
render (	const		float4		cameraPosition,
			const		float16		unprojectionMatrix,
			const		float4		backgroundColor,
			write_only	image2d_t	outputImage,
			read_only	image3d_t	voxelGrid,
			const		float		cellSize)
{
	int2 coord = (int2)(get_global_id(0), get_global_id(1));
	int2 size = get_image_dim(outputImage);

	const sampler_t smp = 
		CLK_NORMALIZED_COORDS_FALSE | //Natural coordinates
		CLK_ADDRESS_CLAMP | //Clamp to zeros
		CLK_FILTER_NEAREST; //Don't interpolate

	/**** Create a ray in world coordinates ****/

	// convert to normilized device coordinates
	float2 screenPoint2d = (float2)(2.0f, 2.0f) * convert_float2(coord) / convert_float2(size) - (float2)(1.0f, 1.0f);

	// unproject screen point to world
	float4 screenPoint = (float4)(screenPoint2d.x, screenPoint2d.y, -1.0f, 1.0f);	
	float4 rayOrigin = transformVector(unprojectionMatrix, screenPoint);
	float4 rayDirection = fast_normalize(rayOrigin - cameraPosition);

	/**** Create generic test data ****/

	// create test light
	float4 lightPosition = (float4)(50.0f, 100.0f, 50.0f, 1.0f);

	// set the default background color
	float4 color = backgroundColor;

	float nearestIntersection = INFINITY;

	/**** Traverse the grid and find the nearest occupied cell ****/
	// setup up traversel variables

	// get grid size from the texture file
	int gridWidth = get_image_width(voxelGrid);

	// traversel values
	int4 step;		// cell widths
	float4 tMax;	// min distance to move before crossing a gird boundary
	float4 tDelta;	// distance (in t) between cell boundaries
	int4 index;		// index of the current voxel
	int4 out;		// index of invalid first voxel index.
	float totalT;	// the distance from ray start to the cell intersection

	// convert the ray start position to grid space
	float4 gridOrigin = (float4)(-4.0f, -4.0f, -4.0f, 1);
	float4 gridSpaceCoordinates = rayOrigin - gridOrigin;

	// get the current grid cell index and the distance to the next cell boundary
	//float4 frac = gridSpaceCoordinates % (float)cellSize;
	//index = (int4)(gridSpaceCoordinates / cellSize); // space to gird coords
	float4 frac = remquo(gridSpaceCoordinates, (float4)cellSize, &index);
	

	out = -1;
	step = -1;
	if (rayDirection.x >= 0)
	{
		out.x = gridWidth;
		step.x = 1;
		frac.x = cellSize - frac.x;
	}
	if (rayDirection.y >= 0)
	{
		out.y = gridWidth;
		step.y = 1;
		frac.y = cellSize - frac.y;
	}
	if (rayDirection.z >= 0)
	{
		out.z = gridWidth;
		step.z = 1;
		frac.z = cellSize - frac.z;
	}

	tMax = frac / rayDirection;
	tDelta = cellSize / rayDirection;// compute projections onto the coordinate axes
	tDelta = copysign(tDelta, (float4)1);
	//tDelta *= step;// multiply by step to ensure all deltas are positive.

	// begin grid traversel
	totalT = 0.0f;
	bool containsGeometry = false;
	float4 cellData;
	do
	{
/*
		// Idea: use mask boolean values to aviod conditionals
		int4 mask;
		mask.x = (tMax.x < tMax.y) && (tMax.x < tMax.z);
		mask.y = (tMax.y < tMax.x) && (tMax.y < tMax.z);
		mask.z = (tMax.z < tMax.x) && (tMax.z < tMax.y);
		mask &= 1;	// ensure mask entries are 0 positive 1.

		// Stepping can be done vector-wise using the mask to select the index to increment.
		index = step * mask;

		// Check if the ray has exited the grid
		if (index.x==out.x || index.y==out.y || index.z==out.z)
			break;

		totalT += 
*/

		if (tMax.x < tMax.y)
		{
			if (tMax.x < tMax.z)
			{
				index.x += step.x;	// step to next voxel along this axis
				if (index.x == out.x)	// outside grid
					break; 
				tMax.x = tMax.x + tDelta.x;	// increment max distence to next voxel
				totalT += tMax.x;
			}
			else
			{
				index.z += step.z;
				if (index.z == out.z)
					break;
				tMax.z = tMax.z + tDelta.z;
				totalT += tMax.z;
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
				totalT += tMax.y;
			}
			else
			{
				index.z += step.z;
				if (index.z == out.z)
					break;
				tMax.z = tMax.z + tDelta.z;
				totalT += tMax.z;
			}
		}

		// get grid data at index
		cellData = read_imagef(voxelGrid, smp, index);
		
		containsGeometry = cellData.x > 0 || cellData.y > 0 || cellData.z > 0 || cellData.w > 0;

	} while (!containsGeometry);

	// compute intersection point
	float4 collisionPoint = rayOrigin + (rayDirection * totalT);

	/**** Write output to image ****/
	if (containsGeometry)
	{
		color = cellData;//(float4)(0.3f, 0.7f, 0.0f, 0.0f);
	}

	write_imagef(outputImage, coord, color);
}