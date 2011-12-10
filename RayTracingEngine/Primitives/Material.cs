using System;
using System.Runtime.InteropServices;

using OpenTK;
using OpenTK.Graphics;

namespace Raytracing.Primitives
{
	[StructLayout(LayoutKind.Sequential)]
	public struct Material
	{
		public Color4 Color;
		public float Reflectivity;
		public float Transparency;
		public float RefractiveIndex;
		public float phongExponent;

		public Material(Color4 c)
			: this(c, 0, 0, 0, 0)
		{ }

		public Material(Color4 c, float reflectivity)
			: this(c, reflectivity, 0, 0, 0)
		{
		}

		public Material(Color4 c, float reflectivity, float transparency, float n, float phongExponent)
		{
			this.Color = c;
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
			// If parameter is null return false.
			if (obj == null)
			{
				return false;
			}

			if (obj is Material)
			{
				Material m2 = (Material)obj;
				return this.Color == m2.Color
					&& this.Reflectivity == m2.Reflectivity
					&& this.Transparency == m2.Transparency
					&& this.RefractiveIndex == m2.RefractiveIndex;
			}
			else
			{
				return false;
			}
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
