using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using OpenTK;
using OpenTK.Graphics;

using Raytracing.BoundingVolumes;

namespace Raytracing.Primitives
{
	public struct Material
	{
		public Color4 color;
		public float reflectivity;
		public float transparency;
		public float n;

		public Material(Color4 c)
			:this(c, 0, 0, 0)
		{}

		public Material(Color4 c, float reflectivity)
			:this(c,reflectivity, 0,0)
		{
		}

		public Material(Color4 c, float reflectivity, float transparency, float n)
		{
			this.color = c;
			this.n = n;

			float partialSum = reflectivity + transparency;
			if (partialSum > 1.0f)
			{
				this.reflectivity = reflectivity / partialSum;
				this.transparency = transparency / partialSum;
			}
			else
			{
				this.reflectivity = reflectivity;
				this.transparency = transparency;
			}
		}
	}

	public class PointLight
	{

		public Vector3 Position;
		public Color4 Color;

		public PointLight(Vector3 position, float intensity, Color4 color)
		{
			this.Position = position;
			this.Color = new Color4();

			Color.R = color.R * intensity;
			Color.G = color.G * intensity;
			Color.B = color.B * intensity;
		}
	}

	public class SceneBox : AbstractPrimitive
	{
		AxisAlignedBoundingBox _box;

		public override Vector3 Position
		{
			get
			{
				return _box.Min;
			}
			set
			{
				_box.Min = value;
			}
		}

		public SceneBox(Vector3 min, Vector3 max, Material mat)
		{
			_box = new AxisAlignedBoundingBox(min, max);
			this.Material = mat;
		}

		// pass the ray and sphere by ref for speed. They will not be modified.
		public override float intersects(ref Ray r, ref Vector3 collisionPoint,
			ref Vector3 surfaceNormal)
		{
			// call the XNA intersection for now
			float distance = r.Intersects(_box);
			if (!float.IsInfinity(distance))
			{
				// subtract a small ammount from the distance or the collision point will be
				// inside the surface
				distance -=0.000005f;
				collisionPoint = r.Position + (Vector3)(distance * r.Direction);


				surfaceNormal = Vector3.Subtract(collisionPoint, r.Position);
				surfaceNormal.Normalize();

				// move the collision point slightly away from the surface of the sphere.
				// otherwise it may be inside the surface.
				collisionPoint += 0.00005f * surfaceNormal;
			}
			else
			{
				collisionPoint = new Vector3();
				surfaceNormal = new Vector3();
			}
			return distance;
		}

		//// pass the ray and sphere by ref for speed. They will not be modified.
		//public override bool simpleIntersects(ref Ray r)
		//{
		//    // call the XNA intersection for now
		//    float distance = r.Intersects(_box);
		//    if (!float.IsInfinity(distance))
		//        return true;
		//    return false;
		//}
	}


	//public class ScenePlane : AbstractPrimitive
	//{
	//    Plane _plane;

	//    public override Vector3 Position
	//    {
	//        get
	//        {
	//            throw new NotSupportedException();
	//        }
	//        set
	//        {
	//            throw new NotSupportedException();
	//        }
	//    }

	//    public ScenePlane(Vector4 planeDefination, Material mat)
	//    {
	//        _plane = new Plane(planeDefination);
	//        this.Material = mat;
	//    }

	//    // pass the ray and sphere by ref for speed. They will not be modified.
	//    public override float? intersects(ref Ray r, ref Vector3 collisionPoint,
	//        ref Vector3 surfaceNormal)
	//    {
	//        // call the XNA intersection for now
	//        float? distance = r.Intersects(_plane);
	//        collisionPoint = new Vector3();
	//        surfaceNormal = _plane.Normal;

	//        if (distance.HasValue)
	//        {
	//            collisionPoint = r.Position + (Vector3)(distance * r.Direction);

	//            // move the collision point slightly away from the surface of the sphere.
	//            // otherwise it may be inside the surface.
	//            collisionPoint += 0.00002f * surfaceNormal;
	//        }
	//        return distance;
	//    }

	//    // pass the ray and sphere by ref for speed. They will not be modified.
	//    public override bool simpleIntersects(ref Ray r)
	//    {
	//        // call the XNA intersection for now
	//        if (r.Intersects(_plane).HasValue)
	//            return true;
	//        return false;
	//    }

	//}


	//public class SceneModel : Primitive
	//{
	//    Model _model;
	//    BoundingSphere _bounds;	// shortcut intersection test

	//    public SceneModel(Model model)
	//    {
	//        _model = model;
	//    }

	//    // pass the ray and sphere by ref for speed. They will not be modified.
	//    public override float? intersects(ref Ray r, ref Vector3 collisionPoint,
	//        ref Vector3 surfaceNormal)
	//    {

	//    }

	//}
}
