using OpenTK;

/* 
  See Matrix3.cs for matrix conventions.
  (RHS and basis vectors stored by row.)
*/

/***************************************************************************/

namespace MuxEngine.LinearAlgebra
{
    public struct Matrix4
    {
        static Matrix4 ms_identity;

        Matrix3 M;
        OpenTK.Vector3 t;

        static Matrix4 ()
        {
            ms_identity = new Matrix4 (true);
        }

        // Init to identity regardless of "makeIdentity"
        public Matrix4 (bool makeIdentity)
        {
            M = new Matrix3 ();
            M.setIdentity ();
            t = Vector3.Zero;
        }

        // this = translation matrix by "t"
        public Matrix4 (Vector3 t)
        {
            M = new Matrix3 (true);
            this.t = t;
        }

        public Matrix4 (Matrix3 m)
        {
            M = m;
            t = Vector3.Zero;
        }

        public Matrix4 (Vector3 forward, Vector3 up)
        {
            M = new Matrix3 (forward, up);
            t = Vector3.Zero;
        }

        // Make orthonormal regardless of "ensureOrthonormal"
        public Matrix4 (Vector3 forward, Vector3 up, bool ensureOrthonormal)
        {
            M = new Matrix3 (forward, up, true);
            t = Vector3.Zero;
        }

        public Matrix4 (Vector3 forward, Vector3 up, Vector3 translation)
        {
            M = new Matrix3 (forward, up);
            this.t = translation;
        }

        public Matrix4 (Vector3 forward, Vector3 up, Vector3 translation, bool ensureOrthonormal)
        {
            M = new Matrix3 (forward, up, true);
            this.t = translation;
        }

        public Matrix4 (Quaternion q)
        {
            M = new Matrix3 (q);
            t = Vector3.Zero;
        }

        public Matrix4 (Matrix3 m, Vector3 translation)
        {
            M = m;
            this.t = translation;
        }

        public Matrix4 (Matrix3 m, Vector3 translation, bool ensureOrthonormal)
        {
            M = m;
            m.makeOrthonormalCross ();
            this.t = translation;
        }

        public Matrix4 (Matrix3 m, Vector3 scale, Vector3 translation)
        {
            M = m;
            M.Right *= scale.X;
            M.Up *= scale.Y;
            M.Forward *= scale.Z;
            this.t = translation;
        }

        public Matrix4 (Quaternion q, Vector3 translation)
        {
            M = new Matrix3 (q);
            this.t = translation;
        }

        public Matrix4 (Quaternion q, Vector3 scale, Vector3 translation)
        {
            M = new Matrix3 (q);
            M.Right *= scale.X;
            M.Up *= scale.Y;
            M.Forward *= scale.Z;
            this.t = translation;
        }

		//public Matrix4 (Matrix m)
		//    : this (m.Forward, m.Up, m.Translation)
		//{
		//}

		//public Matrix4 (Matrix m, bool ensureOrthonormal)
		//    : this (m.Forward, m.Up, m.Translation, true)
		//{
		//}

        public static Matrix4 Identity
        {
            get { return (ms_identity); }
        }

        public void setIdentity ()
        {
            M.setIdentity ();
            t = Vector3.Zero;
        }

        public void setZero ()
        {
            M.setZero ();
            t = Vector3.Zero;
        }

        // rotate about "axis" through "p"
        public static Matrix4 createRotationAboutPoint (Vector3 p, Vector3 axis, float angleDeg)
        {
            Matrix3 rotate = new Matrix3 ();
            rotate.setRotateAxis (axis, angleDeg);
            Vector3 translation = -p * rotate + p;
            return (new Matrix4 (rotate, translation));
        }

        public void setBillboard (Vector3 objectPosition, Vector3 cameraPosition, Vector3 cameraUpVector)
        {
            t = objectPosition;
            Vector3 backward = objectPosition - cameraPosition;
            backward.Normalize ();
            M.Backward = backward;
            Vector3 right = Vector3.Cross (cameraUpVector, backward);
            right.Normalize ();
            M.Right = right;
            M.Up = Vector3.Cross (backward, right);
        }

        public void setTranslation (float x, float y, float z)
        {
            t.X = x;
            t.Y = y;
            t.Z = z;
        }

        public void invert ()
        {
            M.invert ();
            // compute t = -t * M;
            t = -t.X * M.Right + -t.Y * M.Up + -t.Z * M.Forward;
        }

        public static void invert (ref Matrix4 m, out Matrix4 inverse)
        {
            Matrix4 copy = m;
            copy.invert ();
            inverse = copy;
        }

        public void decompose (out Vector3 scale, out Quaternion rotation, out Vector3 translation)
        {
            M.decompose (out scale, out rotation);
            translation = t;
        }

        public static Matrix4 operator * (Matrix4 m1, Matrix4 m2)
        {
            Matrix4 m;
            m.t = m1.t * m2.M;
            m.t += m2.t;
            m.M = m1.M * m2.M;
            return (m);
        }

        // Post-multiply by matrix
        public static Vector3 operator * (Vector3 v, Matrix4 m)
        {
            Vector3 r;
            Vector3 c0 = m.M.Column0;
            Vector3 c1 = m.M.Column1;
            Vector3 c2 = m.M.Column2;
            r.X = v.X * c0.X + v.Y * c0.Y + v.Z * c0.Z + m.t.X;
            r.Y = v.X * c1.X + v.Y * c1.Y + v.Z * c1.Z + m.t.Y;
            r.Z = v.X * c2.X + v.Y * c2.Y + v.Z * c2.Z + m.t.Z;
            return (r);
        }

        public static Matrix4 operator + (Matrix4 m1, Matrix4 m2)
        {
            Matrix4 m;
            m.M = m1.M + m2.M;
            m.t = m1.t + m2.t;
            return (m);
        }

        public static Matrix4 operator - (Matrix4 m1, Matrix4 m2)
        {
            Matrix4 m;
            m.M = m1.M - m2.M;
            m.t = m1.t - m2.t;
            return (m);
        }

        public static Matrix4 operator * (Matrix4 m, float s)
        {
            Matrix4 r;
            r.M = m.M * s;
            r.t = m.t * s;
            return (r);
        }

        public static Matrix4 operator * (float s, Matrix4 m)
        {
            return (m * s);
        }

        public Vector3 Right
        {
            get { return (M.Right); }
            set { M.Right = value; }
        }

        public Vector3 Up
        {
            get { return (M.Up); }
            set { M.Up = value; }
        }

        public Vector3 Forward
        {
            get { return (M.Forward); }
            set { M.Forward = value; }
        }

        // Only for internal use
        internal Vector3 Backward
        {
            get { return (M.Backward); }
            set { M.Backward = value; }
        }

        public Matrix3 Rotation
        {
            get { return (M); }
            set { M = value;  }
        }

        public Vector3 Translation
        {
            get { return (t); }
            set { t = value; }
        }

		//public Matrix Transform
		//{
		//    get
		//    {
		//        Matrix m = new Matrix ();
		//        m.M44 = 1.0f;
		//        m.Right = this.M.Right;
		//        m.Up = this.M.Up;
		//        m.Backward = this.M.Backward;
		//        m.Translation = this.t;
		//        return (m);
		//    }
		//}

        public Vector3 this[int rowNumber]
        {
            get
            {
                if (rowNumber <= 2)
                    return (M[rowNumber]);
                return (t);
            }
        }

    }
}
