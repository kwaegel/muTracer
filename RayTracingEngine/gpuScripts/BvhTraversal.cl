
#define x_axis 0
#define y_axis 1
#define z_axis 2

// BVH specific data structures
typedef struct {
	BBox	bounds;
	int		primitivesOffset;
	int		secondChildOffset;
	int		nPrimitives;	// 0 -> interior node
	int		axis;
	
} BvhNode;

float
getIntersection(	
							Ray*		ray,				// In: ray being cast
	__global	read_only	BvhNode*	nodes,				// In: array of BVH nodes
	__global	read_only	Triangle*	primitives,			// In: array of primitives

							float4*		collisionPoint,		// Out: collision point and surface normal
							float4*		surfaceNormal,		// Out: surface normal and return the distence.
							int*		materialIndex)		// Out: index of the material to use
{

	float minT = INFINITY;
	int primHit = 0;
	bool hit = false;
	
	// Temp for intersection. Move to calling function.
	float u=0, v=0;
	float4 tempCP, tempSN;

	float4 invDir = 1.0f/ray->direction;
	// select(a,b,c) => memberwise MSB[C] ? b : a
	// May not need dirIsNeg if we can directly use invDir in a select...
	int4 dirIsNeg = select( (int4)0, (int4)-1, as_int4(invDir) );
	int dirIsNegArray[3];
	dirIsNegArray[x_axis] = dirIsNeg.x;
	dirIsNegArray[y_axis] = dirIsNeg.y;
	dirIsNegArray[z_axis] = dirIsNeg.z;

	// Create bvh traversal stack
	int todoOffset = 0;
	int nodeNum = 0;
	int todo[32];

	// Why is primitivesOffset holding the values for nPrimitives?!?
	//return (nodes[0].primitivesOffset == 1) ? 0.8f : 0.0f;
	//return (nodes[0].secondChildOffset == 1) ? 0.8f : 0.0f;
	//return (nodes[0].nPrimitives == 1) ? 0.8f : 0.0f;
	//return (nodes[0].axis == 1) ? 0.8f : 0.0f;

	// Traverse tree
	while(true)
	{
		// Check for intersection with the current BBox
		BvhNode node = nodes[nodeNum];
		bool hitNode = rayBBoxIntersectP(ray, node.bounds, invDir, dirIsNeg);

		if(hitNode)
		{
			if (node.nPrimitives > 0)
			{
				// Intersect primitives
				for (int i=0; i < node.nPrimitives; ++i)
				{
					int primIndex = node.primitivesOffset+i;
					Triangle tri = primitives[primIndex];

					float t = rayTriIntersect(ray, tri,
											&u, &v,
											&tempCP, &tempSN);
					if (t < minT)
					{
						hit = true;
						minT = t;
						*collisionPoint = tempCP;
						*surfaceNormal = tempSN;
						*materialIndex = (int)tri.p2.w;// Packed value
					}
				}
				if (todoOffset == 0) break;	// No more tests to be done
				nodeNum = todo[--todoOffset];
			}
			else
			{
				// Recurse down tree
				if(dirIsNegArray[node.axis] != 0)
				{
					todo[todoOffset++] = nodeNum+1;
					nodeNum = node.secondChildOffset;
				}
				else
				{
					todo[todoOffset++] = node.secondChildOffset;
					nodeNum++;
				}
			}
		}
		else
		{
			// Node not hit. Check next node
			if (todoOffset == 0) break;
			nodeNum = todo[--todoOffset];
		}
	}

	return minT;
}


kernel
void
render (	const		float4		cameraPosition,
			const		float16		unprojectionMatrix,

			const		float4		backgroundColor,
			write_only	image2d_t	outputImage,

			// BVH data
__global	read_only	BvhNode*	nodes,				// In: array of BVH nodes
__global	read_only	Triangle*	primitives,			// In: array of primitives
__global	read_only	Material*	materials,

			// Lights
__global	read_only	float8*		pointLights,
			const		int			pointLightCount)
{
	int2 coord = (int2)(get_global_id(0), get_global_id(1));
	int2 size = get_image_dim(outputImage);

	// Create a local stack to handle recursive rays
	Ray rayStack[4];
	float rayWeights[4];
	int stackHeight = 0;
	int raysCast = 0;	// used to prevent infinate recursion.

	// Create the primary ray in world coordinates
	rayStack[0] = unprojectPrimaryRay(coord, size, cameraPosition, unprojectionMatrix);
	rayWeights[0] = 1.0f;
	rayStack[0].tMin = 0;
	rayStack[0].tMax = INFINITY;
	stackHeight++;

	// Vector to hold the final output color.
	float4 color;
	
	float4 collisionPoint, surfaceNormal;
	
	while (stackHeight > 0 && raysCast < 4)
	{
		stackHeight--;
		Ray currentRay = rayStack[stackHeight];
		float currentRayWeight = rayWeights[stackHeight];

		int materialIndex;
		float distence = getIntersection(&currentRay, nodes, primitives, &collisionPoint, &surfaceNormal, &materialIndex);
		
		// If the ray has hit somthing, draw the color of that object.
		if (distence < INFINITY)
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
				rayWeights[stackHeight] = mat.reflectivity;		// FIXME: this could be a problem with transparency...
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
				float4 tempCP, tempSN;
				float shadowRayDistence = getIntersection(&shadowRay, nodes, primitives, &tempCP, &tempSN, &materialIndex);

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