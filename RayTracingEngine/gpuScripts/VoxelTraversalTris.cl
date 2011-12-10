
/*
	Find the nearest intersection with geometry in a voxel. Returns HUGE_VALF if
	no intersection is found.
*/
float
intersectCellContents(			Ray*		ray,
						const	int			vectorsPerVoxel,
								int			geometryBaseIndex,
		__global	read_only	Triangle*	geometryArray,
						private float4*		collisionPoint,
						private float4*		surfaceNormal,
								int*		materialIndex)
{
	float minDistence = HUGE_VALF;
	float4 tempCP, tempSN;
	float tempU, tempV;
	for (int i=0; i<vectorsPerVoxel; i++)
	{
		// Get the triangle data.
		Triangle tri = geometryArray[geometryBaseIndex+i];

		// Unpack triangle data.
		//int materialIndex = as_int(tri.p2.w);

		// calculate intersection distance. Returns HUGE_VALF if ray misses triangle.
		float distence = rayTriIntersect(ray, tri, &tempU, &tempV, &tempCP, &tempSN);

		if (distence < minDistence)
		{
			minDistence = distence;
			*collisionPoint = tempCP;
			*surfaceNormal = tempSN;
			*materialIndex = as_int(tri.p2.w);
		}
	}
	
	return minDistence;
}


float
findNearestIntersectionSimple(	Ray*		ray,
						const	int			numTris,
		__global	read_only	Triangle*	geometryArray,
						private float4*		collisionPoint,
						private float4*		surfaceNormal,
								int*		materialIndex)
{
	float minDistence = HUGE_VALF;
	float4 tempCP, tempSN;
	float tempU, tempV;
	for (int i=0; i<numTris; i++)
	{
		// Get the triangle data.
		Triangle tri = geometryArray[i];

		// calculate intersection distance. Returns HUGE_VALF if ray misses triangle.
		float distence = rayTriIntersect(ray, tri, &tempU, &tempV, &tempCP, &tempSN);

		if (distence < minDistence)
		{
			minDistence = distence;
			*collisionPoint = tempCP;
			*surfaceNormal = tempSN;
			*materialIndex = as_int(tri.p2.w);
		}
	}
	
	return minDistence;
}


float
findNearestIntersection(	
							Ray*		ray,
							float4*		collisionPoint,		// Output collision point and surface normal
							float4*		surfaceNormal,		// and return the distence.

							int*		materialIndex,		// Index of the material to use
			
	__global	read_only	image3d_t	voxelGrid,			// Voxel data
				const		float		cellSize,
			
	__global	read_only	Triangle*	geometryArray,		// Geometry data
				const		int			vectorsPerVoxel)
{
	//Natural coordinates, clamp to zeros, don't interpolate.
	const sampler_t smp = CLK_NORMALIZED_COORDS_FALSE | CLK_ADDRESS_CLAMP | CLK_FILTER_NEAREST;

	// setup up traversel variables

	// get grid size from the texture file. Assume identical sides.
	int gridWidth = get_image_width(voxelGrid);

	// traversel values

	// Center the grid at 0,0,0
	float4 halfGridWidth = (gridWidth * cellSize) / 2.0f;
	float4 gridOrigin = -halfGridWidth;

	// convert the ray start position to grid space
	float4 gridSpaceCoordinates = ray->origin - gridOrigin;

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
	// TODO: change to a box intersection test to allow a camera outside the grid.
	if (index.x < 0 || index.x >= gridWidth ||
		index.y < 0 || index.y >= gridWidth ||
		index.z < 0 || index.z >= gridWidth)
	{
		return HUGE_VALF;
	}

	// MSB of a float is the sign bit, so the select call can switch based on the sign bit.
	// value = MSBset ? b : a;
	// first if positive, second if negitive
	int4 out =		select((int4)gridWidth,		  (int4)-1,		as_int4(ray->direction));
	int4 step =		select((int4)		 1,		  (int4)-1,		as_int4(ray->direction));
	float4 frac =	select(  upperFraction,  -lowerFraction,	as_int4(ray->direction));

	// tMax: min distance to move before crossing a gird boundary
	float4 tMax = frac / ray->direction;

	// tDelta: distance (in t) between cell boundaries
	float4 tDelta = cellSizeVec / ray->direction;	// compute projections onto the coordinate axes.
	tDelta = copysign(tDelta, (float4)1.0f);	// ensure tDelta is positive.

	// begin grid traversel
	bool containsGeometry = false;
	bool rayHalted = false;
	float minDistence = HUGE_VALF;
	int4 cellData;

	// Check grid data at origional index
	cellData = read_imagei(voxelGrid, smp, index);
	containsGeometry = cellData.x > 0 || cellData.y > 0 || cellData.z > 0 || cellData.w > 0;

	if (containsGeometry)
	{
		// check for intersection with geometry in the current cell
		int geometryIndex = (index.x * gridWidth * gridWidth + index.y * gridWidth + index.z) * vectorsPerVoxel;
			
		minDistence = intersectCellContents(ray, cellData.x, geometryIndex, geometryArray, collisionPoint, surfaceNormal, materialIndex);

		// Halt ray progress if it collides with anything.
		rayHalted = minDistence < HUGE_VALF;

	} // End checking geometry.
	
	int4 mask;
	while (!rayHalted)
	{
		mask.x = (tMax.x < tMax.y) && (tMax.x < tMax.z);
		mask.y = (tMax.y <= tMax.x) && (tMax.y < tMax.z);
		mask.z = !mask.x && !mask.y;

		index += step * mask;
		if (mask.x)
			tMax.x += tDelta.x;
		if (mask.y)
			tMax.y += tDelta.y;
		if (mask.z)
			tMax.z += tDelta.z;


		if (index.x == out.x || index.y == out.y || index.z == out.z)
		{
			break;
		}

		// get grid data at index
		cellData = read_imagei(voxelGrid, smp, index);
		containsGeometry = cellData.x > 0 || cellData.y > 0 || cellData.z > 0 || cellData.w > 0;

		if (containsGeometry)
		{

			// check for intersection with geometry in the current cell
			int geometryIndex = (index.x * gridWidth * gridWidth + index.y * gridWidth + index.z) * vectorsPerVoxel;
			
			minDistence = intersectCellContents(ray, cellData.x, geometryIndex, geometryArray, collisionPoint, surfaceNormal, materialIndex);

			// Halt ray progress if it collides with anything.
			rayHalted = minDistence < HUGE_VALF;

		} // End checking geometry.
	} // End voxel traversel loop

	return minDistence;
}

Ray
unprojectPrimaryRay(	int2		screenCoords,
						int2		screenSize,
			const		float4		cameraPosition,
			const		float16		unprojectionMatrix)
{
	float4 windowCoords = (float4)(screenCoords.x, screenCoords.y, 0.0f, 1.0f);
	Ray ray;

	// map x and y from window coords
	windowCoords.xy /= convert_float2(screenSize);

	// Convert window to normalized device coordinates in the range [-1, 1]
	// Assume viewport is at zero, so do not need to subtract viewport (x,y)
	float4 ndc = 2.0f * windowCoords  - 1.0f;

	ray.origin = transformVector(unprojectionMatrix, ndc);

	// Convert to homogeneous coordinates.
	// Not sure what this really does, but it is required.
	ray.origin /= (float4)(ray.origin.w);

	ray.direction = normalize(ray.origin - cameraPosition);

	ray.currentN = 1.0f;	// Index of refraction of vacuum.

	return ray;
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
__global	read_only	Triangle*	geometryArray,
			const		int			vectorsPerVoxel,

			// Lights
__global	read_only	float8*		pointLights,
			const		int			pointLightCount,

__global	read_only	Material*	materials)
{
	int2 coord = (int2)(get_global_id(0), get_global_id(1));
	int2 size = get_image_dim(outputImage);

	// Create a local stack to hold recursive rays
	Ray rayStack[4];
	float rayWeights[4];
	int stackHeight = 0;
	int raysCast = 0;	// used to prevent infinate recursion.

	// Create the primary ray in world coordinates
	rayStack[0] = unprojectPrimaryRay(coord, size, cameraPosition, unprojectionMatrix);
	rayWeights[0] = 1.0f;
	stackHeight++;

	float4 debug = (float4)(0);

	// Vector to hold the final output color.
	float4 color = (float4)(0,0,0,0);
	
	float4 collisionPoint, surfaceNormal;
	int materialIndex;
	
	
	while (stackHeight > 0 && raysCast < 4)
	{
		stackHeight--;
		Ray currentRay = rayStack[stackHeight];
		float currentRayWeight = rayWeights[stackHeight];

		int materialIndex;
		float distence = findNearestIntersection(&currentRay, &collisionPoint, &surfaceNormal, &materialIndex, voxelGrid, cellSize, geometryArray, vectorsPerVoxel);
		
		
		// If the ray has hit somthing, draw the color of that object.
		if (distence < HUGE_VALF)
		{
			color = (float4)(0.0f);

			// Get the material properties.
			Material mat = materials[materialIndex];
			float4 objectColor = mat.color;
			float diffusion = 1.0f - mat.reflectivity - mat.transparency;

			// Calculate the cos of theta for both reflecton and refraction
			// TODO: This might need to be dot(normal, -rayDir) for refraction...
			float cosTheta = dot(currentRay.direction, surfaceNormal);


			// Add reflection ray to stack
			if (mat.reflectivity > 0)
			{			
				rayStack[stackHeight].origin = collisionPoint - currentRay.direction * distence*0.0004f;
				rayStack[stackHeight].direction= currentRay.direction - (2 * cosTheta * surfaceNormal);
				rayStack[stackHeight].currentN = currentRay.currentN;
				rayWeights[stackHeight] = mat.reflectivity;
				stackHeight++;
			}


			// Add refracted ray to stack;
			// NOTE: based on C# code from SimpleScene.cs (from initial import, rev 087a9e15)
			if (mat.transparency > 0)
			{
				// if we are moving from a dense medium to a less dense one, reverse the surface normal
				//if (currentRay.currentN > 1)
				if (cosTheta > 0)	// If we intersected the inside of an edge, flip the surface normal.
				{
					surfaceNormal = -surfaceNormal;
					// cosTheta is based on the surface normal and must also be negated
					cosTheta = -cosTheta;
				}

				float n = currentRay.currentN / mat.refractiveIndex;
				float sinThetaSquared = n * n * (1 - cosTheta * cosTheta);

				float4 transDir = n * currentRay.direction - ( n * cosTheta + native_sqrt(1-sinThetaSquared) )*surfaceNormal;
				
				// Create refracted ray to stack
				rayStack[stackHeight].origin = collisionPoint + currentRay.direction * distence*0.0004f;
				rayStack[stackHeight].direction= transDir;
				rayStack[stackHeight].currentN = mat.refractiveIndex;
				rayWeights[stackHeight] = mat.transparency;
				stackHeight++;
			}


			// Sum up contributions of all light sources
			for (int lightIndex = 0; lightIndex < pointLightCount; lightIndex++)
			{
				// test cosine shading
				float4 lightPosition = pointLights[lightIndex].s0123;
				float4 lightColor = pointLights[lightIndex].s4567;
				float lightIntensity = lightColor.w;

				float4 lightVector = lightPosition - collisionPoint;
				float lightDistence = length(lightVector);
				float4 lightDirection = fast_normalize(lightVector);

				float shade = clamp(dot(surfaceNormal, lightDirection), 0.0f, 1.0f);	// Clamped cosine shading

				// check for shadowing. Reuse collisionPoint and surfaceNormal as they are no longer needed.
				Ray shadowRay = {collisionPoint - currentRay.direction * distence*0.0004f, lightDirection};
				float4 shadowCollisionPoint, shadowSurfaceNormal;
				float shadowRayDistence = findNearestIntersection(	&shadowRay, &shadowCollisionPoint, &shadowSurfaceNormal, &materialIndex, 
																	voxelGrid, cellSize, geometryArray, vectorsPerVoxel);

				bool isInShadow = shadowRayDistence < lightDistence;

				// Apply shading modifiers.
				float4 lightContrib = objectColor;
				lightContrib *= (float4)(shade);
				lightContrib *= lightIntensity;
				lightContrib *= native_recip(lightDistence*lightDistence);	// Inverse square law
				lightContrib *= currentRayWeight * diffusion;
			
				// Add light contribution to total color.
				// Multiply by shadow factor to ignore the contribution of hidden lights.
				color += lightContrib * !isInShadow;
			}
		}
		else
		{
			// if the ray hits nothing, add in the background color.
			color += backgroundColor * currentRayWeight;
		}
		raysCast++;
	}
	
	
	// Write the resulting color to the camera texture.
	write_imagef(outputImage, coord, color);
}