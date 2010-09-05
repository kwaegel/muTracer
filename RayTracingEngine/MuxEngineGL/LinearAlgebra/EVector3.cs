using System;

using OpenTK;

//using MuxEngine.Utility;

namespace MuxEngine.LinearAlgebra
{
    public static class VectorMethods
    {
        static Random ms_random = new Random ();

        // Project "v" onto "direction" (must be unit vector)
        public static Vector3 project (Vector3 v, Vector3 direction)
        {
            float mag = Vector3.Dot (v, direction);
            return (mag * direction);
        }

        // Project "v" onto "direction"
        // Consider "direction" to be non-normal
        public static Vector3 projectOnNonUnit (Vector3 v, Vector3 direction)
        {
            float mag = Vector3.Dot (v, direction) * (1.0f / direction.LengthSquared);
            return (mag * direction);
        }

        // Remove component from "v" along direction "dir" (must be unit vector)
        public static void removeComponent (ref Vector3 v, Vector3 dir)
        {
            float mag = Vector3.Dot (v, dir);
            v -= mag * dir;
        }

        // Return "v" formatted to 2 decimal places
        public static String formatVector (Vector3 v)
        {
            return (String.Format ("({0:f2}, {1:f2}, {2:f2})", v.X, v.Y, v.Z));
        }

        public static double getRandomInRange (double min, double max)
        {
            return (ms_random.NextDouble () * (max - min) + min);
        }

        public static double getRandomInRange (int min, int max)
        {
            return (ms_random.Next (max - min + 1) + min);
        }

        public static Vector2 getRandomDirVector ()
        {
            double phi = getRandomInRange (0, MathHelper.TwoPi);
            return (new Vector2 ((float)Math.Cos (phi), (float)Math.Sin (phi)));
        }
    }

    /// An enhanced 3-D vector class
    public struct EVector3
    {
        public float X;
        public float Y;
        public float Z;

        static EVector3 ms_zeroVector;
        static EVector3 ms_oneVector;

        /************************************************************/

        static EVector3 ()
        {
            ms_zeroVector = new EVector3 (true);
            ms_oneVector = new EVector3 (1.0f);
        }

		// Init to zero vector regardless of "setZero"
		public EVector3 (bool setZero)
		{
            X = Y = Z = 0.0f;
		}

		public EVector3 (float v)
		{
            X = Y = Z = v;
		}

		public EVector3 (float x, float y, float z)
		{
			X = x;
            Y = y;
            Z = z;
		}

        public EVector3 (Vector3 v)
        {
            X = v.X;
            Y = v.Y;
            Z = v.Z;
        }

        public void setZero ()
		{
			set (0.0f);
		}

		public void	set (float v)
		{
			set (v, v, v);
		}

		public void	set (float x, float y, float z)
		{
			X = x;
			Y = y;
			Z = z;
		}

		public float dot (EVector3 v)
		{
			return (X * v.X + Y * v.Y + Z * v.Z);
		}

		public static float	dot (EVector3 v1, EVector3 v2)
		{
			return (v1.dot (v2));
		}

        public float normSquared ()
		{
			return (dot (this));
		}

		public float norm ()
		{
            return ((float)Math.Sqrt (normSquared ()));
		}

		public float normalize ()
		{
			float normV = norm ();
			float invNorm = 1.0f / normV;
			X *= invNorm;
			Y *= invNorm;
			Z *= invNorm;
			return (normV);
		}

		// "direction" must be a unit vector
        public EVector3 project (EVector3 direction)
		{
			float mag = dot (direction);
			return (mag * direction);
		}

		public EVector3	projectOntoNonUnitVector (EVector3 v)
		{
			float mag = dot (v) / v.normSquared ();
			return (mag * v);
		}

        // Return the perpendicular component of the projection of 
        //   this onto "direction"
        // "direction" must be a unit vector
        public EVector3 computePerpendicularComponentWrt (EVector3 direction)
        {
            return (this - project (direction));
        }

        // consider "v" non-normal regardless of "isNormal"
        public EVector3 computePerpendicularComponentWrt (EVector3 v, bool isNormal)
        {
            return (this - projectOntoNonUnitVector (v));
        }

        public void multiplyComponents (EVector3 v) 
		{
			X *= v.X;
			Y *= v.Y;
			Z *= v.Z;
		}

		public EVector3	multiplyComponents (EVector3 v1, EVector3 v2)
		{
			EVector3 prodV = v1;
            prodV.multiplyComponents (v2);
			return (prodV);
		}

		public EVector3	cross (EVector3 v) 
		{
			float rx, ry, rz;
			rx =  Y * v.Z - Z * v.Y;
			ry = -X * v.Z + Z * v.X;
			rz =  X * v.Y - Y * v.X;
			return (new EVector3 (rx, ry, rz));
		}

		public static EVector3 cross (EVector3 v1, EVector3 v2)
		{
			return (v1.cross (v2));
		}

        public static EVector3 lerp (EVector3 source, EVector3 destination, float destinationWeight)
        {
            EVector3 result = (1 - destinationWeight) * source + destinationWeight * destination;
            return (result);
        }

        public EVector3 getRandomDirVector ()
        {  
            float z = (float)VectorMethods.getRandomInRange (-1, 1);
            Vector2 dir = VectorMethods.getRandomDirVector () * (float)Math.Sqrt (1 - z * z);
            return (new EVector3 (dir.X, dir.Y, z));
        }

		// this' = this * M; right-multiply matrix semantics
		public void	transform (Matrix3 m) 
		{
            float tx = X * m.Right.X + Y * m.Up.X + Z * m.Backward.X;
            float ty = X * m.Right.Y + Y * m.Up.Y + Z * m.Backward.Y;
            float tz = X * m.Right.Z + Y * m.Up.Z + Z * m.Backward.Z;
			X = tx;
            Y = ty;
            Z = tz;
		}

        // this' = this * M^(-1); right-multiply matrix semantics
        public void transformByInverse (Matrix3 m)
        {
            m.invert ();
            transform (m);
        }

        // this' = this * M; right-multiply matrix semantics
		public void	transform (Matrix4 m) 
		{
            float tx = X * m.Right.X + Y * m.Up.X + Z * m.Backward.X + m.Translation.X;
            float ty = X * m.Right.Y + Y * m.Up.Y + Z * m.Backward.Y + m.Translation.Y;
            float tz = X * m.Right.Z + Y * m.Up.Z + Z * m.Backward.Z + m.Translation.Z;
			X = tx;
            Y = ty;
            Z = tz;
		}

        // this' = this * M; right-multiply matrix semantics
        // Ignore translation when transforming a normal
        public void transformNormal (Matrix4 m)
        {
            float tx = X * m.Right.X + Y * m.Up.X + Z * m.Backward.X;
            float ty = X * m.Right.Y + Y * m.Up.Y + Z * m.Backward.Y;
            float tz = X * m.Right.Z + Y * m.Up.Z + Z * m.Backward.Z;
            X = tx;
            Y = ty;
            Z = tz;
        }

        // this' = this * M^(-1); right-multiply matrix semantics
        public void transformByInverse (Matrix4 m)
        {
            m.invert ();
            transform (m);
        }

        // this' = this * M; right-multiply matrix semantics
        public void transform (OpenTK.Matrix4 m)
        {
            float tx = X * m.M11 + Y * m.M21 + Z * m.M31 + m.M41;
            float ty = X * m.M12 + Y * m.M22 + Z * m.M32 + m.M42;
            float tz = X * m.M13 + Y * m.M23 + Z * m.M33 + m.M43;
            X = tx;
            Y = ty;
            Z = tz;
        }

        // this' = this * M; right-multiply matrix semantics
        // Ignore translation when transforming a normal
        public void transformNormal (OpenTK.Matrix4 m)
        {
            float tx = X * m.M11 + Y * m.M21 + Z * m.M31;
            float ty = X * m.M12 + Y * m.M22 + Z * m.M32;
            float tz = X * m.M13 + Y * m.M23 + Z * m.M33;
            X = tx;
            Y = ty;
            Z = tz;
        }

        // this' = this * M^(-1); right-multiply matrix semantics
        public void transformByInverse (OpenTK.Matrix4 m)
        {
			OpenTK.Matrix4 inverse = OpenTK.Matrix4.Invert(m);
            transform (inverse);
        }

        public void transform (Quaternion q)
        {
            Matrix3 m = new Matrix3 (q);
            transform (m);
        }

        public static EVector3 operator + (EVector3 v1, EVector3 v2)
		{
			EVector3 r;
			r.X = v1.X + v2.X;
			r.Y = v1.Y + v2.Y;
			r.Z = v1.Z + v2.Z;
			return (r);
		}

        public static EVector3 operator - (EVector3 v)
        {
			EVector3 r;
			r.X = -v.X;
			r.Y = -v.Y;
			r.Z = -v.Z;
			return (r);
		}

        public static EVector3 operator - (EVector3 v1, EVector3 v2)
		{
			EVector3 r = v1 + (-v2);
			return (r);
		}

        public static EVector3 operator * (EVector3 v, Matrix3 m)
        {
            EVector3 r = v;
            r.transform (m);
            return (r);
        }

        public static EVector3 operator * (EVector3 v, Matrix4 m)
        {
            EVector3 r = v;
            r.transform (m);
            return (r);
        }

        public static EVector3 operator * (EVector3 v, OpenTK.Matrix4 m)
        {
            EVector3 r = v;
            r.transform (m);
            return (r);
        }

        public static EVector3 operator * (EVector3 v, Quaternion q)
        {
            EVector3 r = v;
            r.transform (q);
            return (r);
        }

        public static EVector3 operator * (EVector3 v, float s)
		{
            return (new EVector3 (v.X * s, v.Y * s, v.Z * s));
		}

        public static EVector3 operator * (float s, EVector3 v)
		{
			return (v * s);
		}

        public static EVector3 operator / (EVector3 v, float s)
		{
			return (v * (1 / s));
		}

        public override string ToString ()
        {
            return (String.Format ("({0:f2}, {1:f2}, {2:f2})", X, Y, Z));
        }

        public Vector3 ToVector3 ()
        {
            return (new Vector3 (X, Y, Z));
        }

        public static EVector3 Zero
        {
            get { return (ms_zeroVector); }
        }

        public static EVector3 One
        {
            get { return (ms_oneVector); }
        }

	}

}

/******************************************************************************/        
