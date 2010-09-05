using System;

using OpenTK;

//using MuxEngine.Utility;

/* 
  Matrix3 uses a right-handed coordinate system (RHS). 
  Basis vectors (Right, Up, and Backward) are stored by row.
  Methods take a forward vector instead of backward, but  
    negate it when it is get or set. 
  This is consistent with XNA Matrix.
*/

namespace MuxEngine.LinearAlgebra
{
    public struct Matrix3
    {
        float m00, m01, m02, m10, m11, m12, m20, m21, m22;

        static Matrix3 ms_identity;

        /************************************************************/

        static Matrix3 ()
        {
            ms_identity = new Matrix3 (true);
        }

        public Matrix3 (float m00, float m01, float m02,
                        float m10, float m11, float m12,
                        float m20, float m21, float m22)
        {
            this.m00 = m00; this.m01 = m01; this.m02 = m02;
            this.m10 = m10; this.m11 = m11; this.m12 = m12;
            this.m20 = m20; this.m21 = m21; this.m22 = m22;
        }

        // Init to identity regardless of "makeIdentity"
        public Matrix3 (bool makeIdentity)
        {
            m00 = m11 = m22 = 1.0f;
            m01 = m02 = m10 = m12 = m20 = m21 = 0.0f;
        }

        // Init with forward vector and right vector orthogonal to the plane  
        //   formed from forward and up
        public Matrix3 (Vector3 forward, Vector3 up)
        {
            // To maintain RHS we store the backward vector in row 2
            Vector3 backward = -forward;
            Vector3 right = Vector3.Cross (up, backward);
            up = Vector3.Cross (backward, right);
            m00 = right.X;
            m01 = right.Y;
            m02 = right.Z;
            m10 = up.X;
            m11 = up.Y;
            m12 = up.Z;
            m20 = backward.X;
            m21 = backward.Y;
            m22 = backward.Z;
        }

        // Normalize the height regardless of "ensureOrthonormal"
        public Matrix3 (Vector3 forward, Vector3 up, bool ensureOrthonormal)
            : this (Vector3.Cross (up, -forward), up, forward, true)
        {
        }

        public Matrix3 (Vector3 right, Vector3 up, Vector3 forward)
        {
            m00 = right.X;
            m01 = right.Y;
            m02 = right.Z;
            m10 = up.X;
            m11 = up.Y;
            m12 = up.Z;
            m20 = -forward.X;
            m21 = -forward.Y;
            m22 = -forward.Z;
        }

        public Matrix3 (EVector3 right, EVector3 up, EVector3 forward)
        {
            m00 = right.X;
            m01 = right.Y;
            m02 = right.Z;
            m10 = up.X;
            m11 = up.Y;
            m12 = up.Z;
            m20 = -forward.X;
            m21 = -forward.Y;
            m22 = -forward.Z;
        }

        // Normalize the parameters regardless of "makeUnit"
        public Matrix3 (Vector3 right, Vector3 up, Vector3 forward, bool ensureOrthonormal)
            : this (right, up, forward)
        {
            makeOrthonormalCross ();
        }

		//public Matrix3 (OpenTK.Matrix4 m)
		//    : this (m.Right, m.Up, m.Forward)
		//{
		//}

        // Normalize the height of "m" regardless of "ensureOrthonormal"
        public Matrix3 (Matrix4 m, bool ensureOrthonormal)
            : this (m.Right, m.Up, m.Forward, true)
        {
        }

        public Matrix3 (Quaternion q)
            : this ()
        {
            setFromQuaternion (q);
        }

        public void setFromQuaternion (Quaternion q)
        {
            float x2 = q.X * q.X;
            float y2 = q.Y * q.Y;
            float z2 = q.Z * q.Z;

            m00 = 1 - 2 * (y2 + z2);
            m01 = 2 * (q.X * q.Y + q.W * q.Z);
            m02 = 2 * (q.X * q.Z - q.W * q.Y);

            m10 = 2 * (q.X * q.Y - q.W * q.Z);
            m11 = 1 - 2 * (x2 + z2);
            m12 = 2 * (q.Y * q.Z + q.W * q.X);

            m20 = 2 * (q.X * q.Z + q.W * q.Y);
            m21 = 2 * (q.Y * q.Z - q.W * q.X);
            m22 = 1 - 2 * (x2 + y2);
        }

        /// Get the quaternion representing this rotation
        public Quaternion getQuaternion ()
        {
            Quaternion q = new Quaternion ();
            // Examine combinations of matrix's trace elements
            //   where trace = m00 + m11 + m22
            // Each expression "traceL" computes 4L^2 - 1
            float traceW = m00 + m11 + m22;
            float traceX = m00 - m11 - m22;
            float traceY = -m00 + m11 - m22;
            float traceZ = -m00 - m11 + m22;

            // Find largest magnitute component (w, x, y, or z) 
            float traceLargest = traceW;
            int largest = 0;
            if (traceX > traceLargest)
            {
                traceLargest = traceX;
                largest = 1;
            } 
            if (traceY > traceLargest)
            {
                traceLargest = traceY;
                largest = 2;
            } 
            if (traceZ > traceLargest)
            {
                traceLargest = traceZ;
                largest = 3;
            }

            float largestComponent = (float)System.Math.Sqrt (traceLargest + 1.0f) * 0.5f;
            float factor = 0.25f / largestComponent;
            switch (largest)
            {
                case 0:
                    q.W = largestComponent;
                    q.X = (m12 - m21) * factor;
                    q.Y = (m20 - m02) * factor;
                    q.Z = (m01 - m10) * factor;
                    break;
                case 1:
                    q.X = largestComponent;
                    q.W = (m12 - m21) * factor;
                    q.Y = (m01 + m10) * factor;
                    q.Z = (m20 + m02) * factor;
                    break;
                case 2:
                    q.Y = largestComponent;
                    q.W = (m20 - m02) * factor;
                    q.X = (m01 + m10) * factor;
                    q.Z = (m12 + m21) * factor;
                    break;
                case 3:
                    q.Z = largestComponent;
                    q.W = (m01 - m10) * factor;
                    q.X = (m20 + m02) * factor;
                    q.Y = (m12 + m21) * factor;
                    break;
            }
            return (q);
        }

        public static Matrix3 Identity
        {
            get { return (ms_identity); }
        }

        public void setIdentity ()
        {
            m00 = m11 = m22 = 1.0f;
            m01 = m02 = m10 = m12 = m20 = m21 = 0.0f;
        }

		//public Boolean isIdentity ()
		//{
		//    bool isIdentity = MuxMath.equals (this.Right, Vector3.Right)
		//                        && MuxMath.equals (this.Up, Vector3.Up)
		//                        && MuxMath.equals (this.Backward, Vector3.Backward); 
		//    return (isIdentity);
		//}

		//public Boolean isUniformScaling ()
		//{
		//    // GMZ: Test this
		//    float xLengthSq = m00 * m00 + m01 * m01 + m02 * m02;
		//    float yLengthSq = m10 * m10 + m11 * m11 + m12 * m12;
		//    float zLengthSq = m20 * m20 + m21 * m21 + m22 * m22;
		//    bool isUniform = MuxMath.approximatelyEquals (xLengthSq, yLengthSq)
		//                       && MuxMath.approximatelyEquals (yLengthSq, zLengthSq);
		//    return (isUniform);
		//}

        public void setZero ()
        {
            m00 = m01 = m02 = 0.0f;
            m10 = m11 = m12 = 0.0f;
            m20 = m21 = m22 = 0.0f;
        }

        public void transpose ()
        {
			OpenTK.MathHelper.Swap(ref m01, ref m10);
			OpenTK.MathHelper.Swap(ref m02, ref m20);
			OpenTK.MathHelper.Swap(ref m12, ref m21);
        }

        public void invert ()
        {
            float minor00, minor01, minor02;
            float minor10, minor11, minor12;
            float minor20, minor21, minor22;
            minor00 = m11 * m22 - m12 * m21;
            minor01 = m10 * m22 - m12 * m20;
            minor02 = m10 * m21 - m11 * m20;
            minor10 = m01 * m22 - m02 * m21;
            minor11 = m00 * m22 - m02 * m20;
            minor12 = m00 * m21 - m01 * m20;
            minor20 = m01 * m12 - m02 * m11;
            minor21 = m00 * m12 - m02 * m10;
            minor22 = m00 * m11 - m01 * m10;

            // Determinant could be zero for singular matrix
            float det = m00 * minor00 - m01 * minor01 + m02 * minor02;
            System.Diagnostics.Debug.Assert (det != 0.0f, "Matrix3.invert: determinant is zero");
            float detInv = 1.0f / det;

            m00 = minor00 * detInv;
            m01 = -minor10 * detInv;
            m02 = minor20 * detInv;
            m10 = -minor01 * detInv;
            m11 = minor11 * detInv;
            m12 = -minor21 * detInv;
            m20 = minor02 * detInv;
            m21 = -minor12 * detInv;
            m22 = minor22 * detInv;
        }

        public static void invert (ref Matrix3 m, out Matrix3 inverse)
        {
            Matrix3 copy = m;
            copy.invert ();
            inverse = copy;
        }

        public void decompose (out Vector3 scale, out Quaternion rotation)
        {
            EVector3 right = new EVector3 (this.Right);
            scale.X = right.normalize ();
            EVector3 up = new EVector3 (this.Up);
            scale.Y = up.normalize ();
            EVector3 forward = new EVector3 (this.Forward);
            scale.Z = forward.normalize ();
            Matrix3 m = new Matrix3 (right, up, forward);
            rotation = m.getQuaternion ();
        }

        public static Matrix3 createYawPitchRoll (float yawDeg, float pitchDeg, float rollDeg)
        {
            Matrix3 rotate = new Matrix3 ();
            rotate.setYawPitchRoll (yawDeg, pitchDeg, rollDeg);
            return (rotate);
        }

        internal static Matrix3 createYawPitchRollRad (float yaw, float pitch, float roll)
        {
            Matrix3 rotate = new Matrix3 ();
            rotate.setYawPitchRollRad (yaw, pitch, roll);
            return (rotate);
        }

        public void setBillboardRotation (Vector3 lookAtVector, Vector3 cameraUpVector)
        {
            Vector3 backward = -lookAtVector;
            backward.Normalize ();
            this.Backward = backward;
            Vector3 right = Vector3.Cross (cameraUpVector, backward);
            right.Normalize ();
            this.Right = right;
            this.Up = Vector3.Cross (backward, right);
        }

        // this = matrix to yaw, pitch, and roll about origin
        public void setYawPitchRoll (float yawDeg, float pitchDeg, float rollDeg)
        {
            float yawRad = MathHelper.DegreesToRadians (yawDeg);
            float pitchRad = MathHelper.DegreesToRadians (pitchDeg);
            float rollRad = MathHelper.DegreesToRadians (rollDeg);
            setYawPitchRollRad (yawRad, pitchRad, rollRad);
        }

        internal void setYawPitchRollRad (float yaw, float pitch, float roll)
        {
            float cosYaw = (float)Math.Cos (yaw);
            float sinYaw = (float)Math.Sin (yaw);
            float cosPitch = (float)Math.Cos (pitch);
            float sinPitch = (float)Math.Sin (pitch);
            float cosRoll = (float)Math.Cos (roll);
            float sinRoll = (float)Math.Sin (roll);

            m00 = cosYaw * cosRoll + sinYaw * sinPitch * sinRoll;
            m01 = sinRoll * cosPitch;
            m02 = -sinYaw * cosRoll + cosYaw * sinPitch * sinRoll;

            m10 = -cosYaw * sinRoll + sinYaw * sinPitch * cosRoll;
            m11 = cosRoll * cosPitch;
            m12 = sinRoll * sinYaw + cosYaw * sinPitch * cosRoll;

            m20 = sinYaw * cosPitch;
            m21 = -sinPitch;
            m22 = cosYaw * cosPitch;
        }

        // this = rotation matrix about axis parallel to "axis" through origin
        // "axis" must be a unit vector
        public void setRotateAxis (Vector3 axis, float angleDeg)
        {
            float angleRad = MathHelper.DegreesToRadians (angleDeg);
            float cosV = (float)Math.Cos (angleRad);
            float oneMC = 1.0f - cosV;
            float sinV = (float)Math.Sin (angleRad);

            m00 = oneMC * axis.X * axis.X + cosV;
            m01 = oneMC * axis.X * axis.Y + axis.Z * sinV;
            m02 = oneMC * axis.X * axis.Z - axis.Y * sinV;

            m10 = oneMC * axis.X * axis.Y - axis.Z * sinV;
            m11 = oneMC * axis.Y * axis.Y + cosV;
            m12 = oneMC * axis.Y * axis.Z + axis.X * sinV;

            m20 = oneMC * axis.X * axis.Z + axis.Y * sinV;
            m21 = oneMC * axis.Y * axis.Z - axis.X * sinV;
            m22 = oneMC * axis.Z * axis.Z + cosV;
        }

        // this = rotation matrix about X axis
        public void setRotateX (float angleDeg)
        {
            float angleRad = MathHelper.DegreesToRadians (angleDeg);
            float cosV = (float)Math.Cos (angleRad);
            float sinV = (float)Math.Sin (angleRad);

            m00 = 1.0f; m01 = 0.0f; m02 = 0.0f;
            m10 = 0.0f; m11 = cosV; m12 = sinV;
            m20 = 0.0f; m21 = -sinV; m22 = cosV;
        }

        // this = rotation matrix about Y axis
        public void setRotateY (float angleDeg)
        {
            float angleRad = MathHelper.DegreesToRadians (angleDeg);
            float cosV = (float)Math.Cos (angleRad);
            float sinV = (float)Math.Sin (angleRad);

            m00 = cosV; m01 = 0.0f; m02 = -sinV;
            m10 = 0.0f; m11 = 1.0f; m12 = 0.0f;
            m20 = sinV; m21 = 0.0f; m22 = cosV;
        }

        // this = rotation matrix about Z axis
        public void setRotateZ (float angleDeg)
        {
            float angleRad = MathHelper.DegreesToRadians (angleDeg);
            float cosV = (float)Math.Cos (angleRad);
            float sinV = (float)Math.Sin (angleRad);

            m00 = cosV; m01 = sinV; m02 = 0.0f;
            m10 = -sinV; m11 = cosV; m12 = 0.0f;
            m20 = 0.0f; m21 = 0.0f; m22 = 1.0f;
        }

        // this = matrix that reflects about plane "p" through origin
        public void setReflect (Vector3 p)
        {
            m00 = 1.0f - 2.0f * p.X * p.X;
            m01 = -2.0f * p.X * p.Y;
            m02 = -2.0f * p.X * p.Z;

            m10 = m01;
            m11 = 1.0f - 2.0f * p.Y * p.Y;
            m12 = -2.0f * p.Y * p.Z;

            m20 = m02;
            m21 = m12;
            m22 = 1.0f - 2.0f * p.Z * p.Z;
        }

        // this = shear matrix to shear X and Y by Z
        public void setShearXY (float shearXZ, float shearYZ)
        {
            m00 = 1.0f; m01 = 0.0f; m02 = 0.0f;

            m10 = 0.0f; m11 = 1.0f; m12 = 0.0f;

            m20 = shearXZ; m21 = shearYZ; m22 = 1.0f;
        }

        // this = shear matrix to shear X and Z by Y
        public void setShearXZ (float shearXY, float shearZY)
        {
            m00 = 1.0f; m01 = 0.0f; m02 = 0.0f;

            m10 = shearXY; m11 = 1.0f; m12 = shearZY;

            m20 = 0.0f; m21 = 0.0f; m22 = 1.0f;
        }

        // this = shear matrix to shear Y and Z by X
        public void setShearYZ (float shearYX, float shearZX)
        {
            m00 = 1.0f; m01 = shearYX; m02 = shearZX;

            m10 = 0.0f; m11 = 1.0f; m12 = 0.0f;

            m20 = 0.0f; m21 = 0.0f; m22 = 1.0f;
        }

        // this = uniform scale matrix
        public void setScale (float s)
        {
            m00 = m11 = m22 = s;
            m01 = m02 = m10 = m12 = m20 = m21 = 0.0f;
        }

        // this = non-uniform scale matrix
        public void setScale (float sx, float sy, float sz)
        {
            m00 = sx;
            m11 = sy;
            m22 = sz;
            m01 = m02 = m10 = m12 = m20 = m21 = 0.0f;
        }

        // Keep axes mutually perpendicular and normalized
        public void makeOrthonormal ()
        {
            // bias toward forward vector
            this.Backward = Vector3.Normalize (this.Backward);
            this.Up -= VectorMethods.project (this.Up, this.Right);
            this.Up = Vector3.Normalize (this.Up);
            this.Right -= VectorMethods.project (this.Right, this.Backward);
            this.Right -= VectorMethods.project (this.Right, this.Up);
            this.Right = Vector3.Normalize (this.Right);
        }

        // Keep axes mutually perpendicular and normalized
        // Utilize cross products instead of projections
        // Should be faster than "makeOrthonormal", but not as accurate
        public void makeOrthonormalCross ()
        {
            // Bias toward backward vector
            this.Backward = Vector3.Normalize (this.Backward);
            this.Right = Vector3.Cross (this.Up, this.Backward);
            this.Right = Vector3.Normalize (this.Right);
            this.Up = Vector3.Cross (this.Backward, this.Right);
            // Unnecessary to normalize Up
            //this.Up = Vector3.Normalize (this.Up);
        }

        // Pre-multiply by diagonal matrix "d"
        public void preMultiplyDiag (Vector3 d)
        {
            m00 *= d.X;
            m01 *= d.X;
            m02 *= d.X;

            m10 *= d.Y;
            m11 *= d.Y;
            m12 *= d.Y;

            m20 *= d.Z;
            m21 *= d.Z;
            m22 *= d.Z;
        }

        // Post-multiply by diagonal matrix "d"
        public void postMultiplyDiag (Vector3 d)
        {
            m00 *= d.X;
            m01 *= d.Y;
            m02 *= d.Z;

            m10 *= d.X;
            m11 *= d.Y;
            m12 *= d.Z;

            m20 *= d.X;
            m21 *= d.Y;
            m22 *= d.Z;
        }

        // this = "m" * this
        public void preMultiply (Matrix3 m)
        {
            this = m * this;
        }

        // this = this * "m"
        public void postMultiply (Matrix3 m)
        {
            this *= m;
        }

        public static Matrix3 operator * (Matrix3 m1, Matrix3 m2)
        {
            float r0, r1, r2, r3, r4, r5, r6, r7, r8;

            r0 = m1.m00 * m2.m00 + m1.m01 * m2.m10 + m1.m02 * m2.m20;
            r1 = m1.m00 * m2.m01 + m1.m01 * m2.m11 + m1.m02 * m2.m21;
            r2 = m1.m00 * m2.m02 + m1.m01 * m2.m12 + m1.m02 * m2.m22;

            r3 = m1.m10 * m2.m00 + m1.m11 * m2.m10 + m1.m12 * m2.m20;
            r4 = m1.m10 * m2.m01 + m1.m11 * m2.m11 + m1.m12 * m2.m21;
            r5 = m1.m10 * m2.m02 + m1.m11 * m2.m12 + m1.m12 * m2.m22;

            r6 = m1.m20 * m2.m00 + m1.m21 * m2.m10 + m1.m22 * m2.m20;
            r7 = m1.m20 * m2.m01 + m1.m21 * m2.m11 + m1.m22 * m2.m21;
            r8 = m1.m20 * m2.m02 + m1.m21 * m2.m12 + m1.m22 * m2.m22;

            Matrix3 m;
            m.m00 = r0; m.m01 = r1; m.m02 = r2;
            m.m10 = r3; m.m11 = r4; m.m12 = r5;
            m.m20 = r6; m.m21 = r7; m.m22 = r8;
            return (m);
        }

        // Post-multiply by matrix
        public static Vector3 operator * (Vector3 v, Matrix3 m)
        {
            Vector3 r;
            r.X = v.X * m.m00 + v.Y * m.m10 + v.Z * m.m20;
            r.Y = v.X * m.m01 + v.Y * m.m11 + v.Z * m.m21;
            r.Z = v.X * m.m02 + v.Y * m.m12 + v.Z * m.m22;
            return (r);
        }

        public static Matrix3 operator * (Matrix3 m, float s)
        {
            Matrix3 r;
            r.m00 = m.m00 * s;
            r.m01 = m.m01 * s;
            r.m02 = m.m02 * s;

            r.m10 = m.m10 * s;
            r.m11 = m.m11 * s;
            r.m12 = m.m12 * s;

            r.m20 = m.m20 * s;
            r.m21 = m.m21 * s;
            r.m22 = m.m22 * s;
            return (r);
        }

        public static Matrix3 operator * (float s, Matrix3 m)
        {
            return (m * s);
        }

        public static Matrix3 operator + (Matrix3 m1, Matrix3 m2)
        {
            Matrix3 m;
            m.m00 = m1.m00 + m2.m00;
            m.m01 = m1.m01 + m2.m01;
            m.m02 = m1.m02 + m2.m02;

            m.m10 = m1.m10 + m2.m10;
            m.m11 = m1.m11 + m2.m11;
            m.m12 = m1.m12 + m2.m12;

            m.m20 = m1.m20 + m2.m20;
            m.m21 = m1.m21 + m2.m21;
            m.m22 = m1.m22 + m2.m22;
            return (m);
        }

        public static Matrix3 operator - (Matrix3 m1, Matrix3 m2)
        {
            Matrix3 m;
            m.m00 = m1.m00 - m2.m00;
            m.m01 = m1.m01 - m2.m01;
            m.m02 = m1.m02 - m2.m02;

            m.m10 = m1.m10 - m2.m10;
            m.m11 = m1.m11 - m2.m11;
            m.m12 = m1.m12 - m2.m12;

            m.m20 = m1.m20 - m2.m20;
            m.m21 = m1.m21 - m2.m21;
            m.m22 = m1.m22 - m2.m22;
            return (m);
        }

		//public bool Equals (Matrix3 m)
		//{
		//    return (MuxMath.equals (Right, m.Right)
		//            && MuxMath.equals (Up, m.Up)
		//            && MuxMath.equals (Forward, m.Forward));
		//}

        public override String ToString ()
        {
			System.Text.StringBuilder s = new System.Text.StringBuilder("[ " + VectorMethods.formatVector(this.Right) + " ]\n");
            s.Append ("[ " + VectorMethods.formatVector (this.Up) + " ]\n");
            s.Append ("[ " + VectorMethods.formatVector (this.Backward) + " ]\n");
            return (s.ToString ());
        }

        public Vector3 Column0
        {
            get
            {
                return (new Vector3 (m00, m10, m20));
            }
            set
            {
                m00 = value.X;
                m10 = value.Y;
                m20 = value.Z;
            }
        }

        public Vector3 Column1
        {
            get
            {
                return (new Vector3 (m01, m11, m21));
            }
            set
            {
                m01 = value.X;
                m11 = value.Y;
                m21 = value.Z;
            }
        }

        public Vector3 Column2
        {
            get
            {
                return (new Vector3 (m02, m12, m22));
            }
            set
            {
                m02 = value.X;
                m12 = value.Y;
                m22 = value.Z;
            }
        }

        public Vector3 Right
        {
            get
            {
                return (new Vector3 (m00, m01, m02));
            }
            set
            {
                m00 = value.X;
                m01 = value.Y;
                m02 = value.Z;
            }
        }

        public Vector3 Up
        {
            get
            {
                return (new Vector3 (m10, m11, m12));
            }
            set
            {
                m10 = value.X;
                m11 = value.Y;
                m12 = value.Z;
            }
        }

        // To be consistent with XNA, we store the negation of the forward vector
        public Vector3 Forward
        {
            get
            {
                return (new Vector3 (-m20, -m21, -m22));
            }
            set
            {
                m20 = -value.X;
                m21 = -value.Y;
                m22 = -value.Z;
            }
        }

        // Only for internal use
        internal Vector3 Backward
        {
            get
            {
                return (new Vector3 (m20, m21, m22));
            }
            set
            {
                m20 = value.X;
                m21 = value.Y;
                m22 = value.Z;
            }
        }

        public OpenTK.Matrix4 Transform
        {
            get
            {
				OpenTK.Matrix4 m = new OpenTK.Matrix4(	m00, m01, m02, 0.0f,
														m10,  m11,  m12, 0.0f,
														m20,  m21,  m22, 0.0f,
														0.0f, 0.0f, 0.0f, 1.0f);
                return (m);
            }
        }

        public Vector3 this[int rowNumber]
        {
            get
            {
                if (rowNumber == 0)
                    return (this.Right);
                else if (rowNumber == 1)
                    return (this.Up);
                else
                    return (this.Backward);
            }
        }

    }
}
