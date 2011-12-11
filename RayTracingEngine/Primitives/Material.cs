using System;
using System.Runtime.InteropServices;

using OpenTK;
using OpenTK.Graphics;

namespace Raytracing.Primitives
{
	[StructLayout(LayoutKind.Sequential)]
	public struct Material
	{
		public float kd;	// diffuse reflection value
		public float ks;	// specular reflection value
		public float ka;	// ambient reflection value
		public float Reflectivity;
		public float Transparency;
		public float RefractiveIndex;
		public float phongExponent;

		public Material(float diffuse, float specular, float ambient)
			:this(diffuse, specular, ambient, 0, 0, 1, 0)
		{ }

		public Material(float diffuse, float specular, float ambient, float reflectivity)
			: this(diffuse, specular, ambient, reflectivity, 0, 1, 0)
		{ }

		public Material(float diffuse, float specular, float ambient, float reflectivity, float transparency)
			: this(diffuse, specular, ambient, reflectivity, transparency, 1, 0)
		{ }

		public Material(float reflectivity)
			: this(0,0,0, reflectivity, 0, 1, 0)
		{ }

		public Material(float diffuse, float specular, float ambient,
			float reflectivity, float transparency, float n, float phongExponent)
		{
			kd = diffuse;
			ks = specular;
			ka = ambient;
			this.RefractiveIndex = n;

			float partialSum = reflectivity + transparency;
			if (partialSum > 1.0f)
			{
				this.Reflectivity = reflectivity / partialSum;
				this.Transparency = transparency / partialSum;
			}
			else
			{
				this.Reflectivity = reflectivity;
				this.Transparency = transparency;
			}

			this.phongExponent = phongExponent;
		}

		public override bool Equals(object obj)
		{
			if (obj is Material)
			{
				Material m2 = (Material)obj;
				return kd == m2.kd 
					&& ks == m2.ks 
					&& ka == m2.ka
					&& this.Reflectivity == m2.Reflectivity
					&& this.Transparency == m2.Transparency
					&& this.RefractiveIndex == m2.RefractiveIndex;
			}
			return false;
		}

		// Not used
		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		public static bool operator ==(Material m1, Material m2)
		{
			return m1.Equals(m2);
		}

		public static bool operator !=(Material m1, Material m2)
		{
			return !m1.Equals(m2);
		}
	}
}
