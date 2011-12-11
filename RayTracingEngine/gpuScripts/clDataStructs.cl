
typedef struct{
	float4 position;
	float4 colorAndIntensity;
} pointLight;


typedef struct {
	float4 origin;
	float4 direction;
	float currentN;	// The index of refraction of the material the ray is currently in.
	float tMin;
	float tMax;
} Ray;


typedef struct {
	float kd, ks, ka;
	float reflectivity;
	float transparency;
	float refractiveIndex;
	float phongExponent;
	float padding;
} Material;


typedef struct {
	float4 center;
	float radius;
	int material;
	int pad1;
	int pad2
} Sphere;

// The material index is packed into p2.w
typedef struct {
	float4 p0, p1, p2; // Last point includes packed material index in w
	float4 c0, c1, c2;
	float4 n0, n1, n2;
} Triangle;

typedef struct {
	//float4 p[2];
	float4 pMin;
	float4 pMax;
} BBox;