
typedef struct{
	float4 position;
	float4 colorAndIntensity;
} pointLight;


typedef struct {
	float4 origin;
	float4 direction;
	float currentN;	// The index of refraction of the material the ray is currently in.
} Ray;


typedef struct {
	float4 color;
	float reflectivity;
	float transparency;
	float refractiveIndex;
	float padding;
} Material;


typedef struct {
	float4 center;
	float radius;
	int material;
	int pad1;
	int pad2
} Sphere;
