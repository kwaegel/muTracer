using OpenTK;

using MuxEngine.LinearAlgebra;
//using MuxEngine.Utility;

namespace MuxEngine.Movables
{
    public enum Direction
    {
        Right, Up, Forward, Left, Down, Backward
    }

    // Implementation of Movable interface using a Matrix3 (3x3)
    // Better-suited for frequent access to local direction vectors (right, up, forward)
    public class Movable : IMovable
    {
        Matrix3 m_rotation;
        Vector3 m_position;
        Vector3 m_scale;

        /************************************************************/

        public Movable ()
        {
            m_rotation = Matrix3.Identity;
            m_position = Vector3.Zero;
            m_scale = new Vector3 (1,1,1);
        }

        public Movable (Vector3 forward, Vector3 up, Vector3 position)
        {
            m_rotation = new Matrix3 (forward, up, true);
            m_position = position;
            m_scale = new Vector3 (1,1,1);
        }

        public Movable (Matrix3 rotation)
        {
            m_rotation = rotation;
            m_position = Vector3.Zero;
            m_scale = new Vector3 (1,1,1);
        }

        public Movable (MuxEngine.LinearAlgebra.Matrix4 xform)
        {
            m_rotation = xform.Rotation;
            m_position = xform.Translation;
            m_scale = new Vector3 (1,1,1);
        }

		// construct from toolkit (XNA or OpenTK) matrix
		//public Movable(OpenTK.Matrix4 xform)
		//{
		//    m_rotation.Right = xform.Right;
		//    m_rotation.Up = xform.Up;
		//    m_rotation.Backward = xform.Backward;
		//    m_position = xform.Translation;
		//    m_scale = new Vector3 (1,1,1);
		//}

        /// Move to origin, looking down -Z axis
        public void reset ()
        {
			m_rotation.Right = Vector3.UnitX;	// right = X
			m_rotation.Up = Vector3.UnitY;	// up = Y
			m_rotation.Backward = -Vector3.UnitZ;	// backward = -Z
            m_position = Vector3.Zero;
            m_scale = new Vector3 (1,1,1);
        }

        // Set world position to (x, y, z)
        public void setPosition (float x, float y, float z)
        {
            m_position.X = x;
            m_position.Y = y;
            m_position.Z = z;
        }

        // Translate "units" in world direction "dir"
        public void moveWorld (Vector3 dir, float units)
        {
            m_position += dir * units;
        }

        // Translate "units" in local direction "dir"
        public void moveLocal (Vector3 dir, float units)
        {
            Vector3 worldDir = dir * m_rotation;
            m_position += worldDir * units;
        }

        // Translate "units" in local direction "dir"
        //   where "dir" is Right, Up, Forward, etc.
        public void move (Direction dir, float units)
        {
            Vector3 moveVec;
            if (dir == Direction.Right)
            {
                moveVec = m_rotation.Right;
            }
            else if (dir == Direction.Up)
            {
                moveVec = m_rotation.Up;
            }
            else if (dir == Direction.Forward)
            {
                moveVec = m_rotation.Forward;
            }
            else if (dir == Direction.Left)
            {
                moveVec = -m_rotation.Right;
            }
            else if (dir == Direction.Down)
            {
                moveVec = -m_rotation.Up;
            }
            else // Backward
            {
                moveVec = -m_rotation.Forward;
            }
            m_position += moveVec * units;
        }

        /// Translate "units" in local direction "dir"
        ///   where "dir" is Right, Up, Forward, etc.
        /// The local move vector is projected onto the XZ plane 
        public void moveLocalXZ (Direction dir, float units)
        {
            Vector3 moveVec;
            if (dir == Direction.Right)
            {
                moveVec = m_rotation.Right;
            }
            else if (dir == Direction.Up)
            {
                moveVec = m_rotation.Up;
            }
            else if (dir == Direction.Forward)
            {
                moveVec = m_rotation.Forward;
            }
            else if (dir == Direction.Left)
            {
                moveVec = -m_rotation.Right;
            }
            else if (dir == Direction.Down)
            {
                moveVec = -m_rotation.Up;
            }
            else // Backward
            {
                moveVec = -m_rotation.Forward;
            }
            moveVec.Y = 0.0f;
            m_position += moveVec * units;
        }

        // Rotate about "axis" through origin
        public void rotateWorld (Vector3 axis, float angleDeg)
        {
            Matrix3 rotate = new Matrix3 ();
            rotate.setRotateAxis (axis, angleDeg);
            m_rotation *= rotate;
            m_position *= rotate;
        }

        /// Rotate about world Y axis, preserving position
        public void rotateWorldY (float angleDeg)
        {
            Matrix3 rotate = new Matrix3 ();
            rotate.setRotateY (angleDeg);
            m_rotation *= rotate;
        }

        /// Rotate about "axis" through "p"
        public void rotateAboutPoint (Vector3 p, Vector3 axis, float angleDeg)
        {
            Matrix3 rotate = new Matrix3 ();
            rotate.setRotateAxis (axis, angleDeg);
            m_rotation *= rotate;
            m_position *= rotate;
            m_position += -p * rotate + p;
        }

        /// Rotate about the local right axis
        public void pitch (float angleDeg)
        {
            Matrix3 rotate = new Matrix3 ();
            rotate.setRotateAxis (this.Right, angleDeg);
            m_rotation.Up *= rotate;
            m_rotation.Forward *= rotate;
        }

        /// Rotate about the local up axis
        public void yaw (float angleDeg)
        {
            Matrix3 rotate = new Matrix3 ();
            rotate.setRotateAxis (this.Up, angleDeg);
            m_rotation.Right *= rotate;
            m_rotation.Forward *= rotate;
        }

        /// Rotate about the local forward axis
        public void roll (float angleDeg)
        {
            Matrix3 rotate = new Matrix3 ();
            rotate.setRotateAxis (this.Forward, angleDeg);
            m_rotation.Right *= rotate;
            m_rotation.Up *= rotate;
        }

        public void alignWithWorldY ()
        {
            this.Up = Vector3.UnitY;
            Vector3 backward = this.Backward;
            // Project onto XZ plane
            if (backward.X != 0.0f || backward.Z != 0.0f)
            {
                backward.Y = 0.0f;
                backward.Normalize ();
            }
            else
            {
                // Was looking up or down
                backward = Vector3.UnitZ;
            }
            this.Backward = backward;
            this.Right = Vector3.Cross (this.Up, this.Backward);
        }

        /// Perform spherical linear interpolation from this rotation to destination rotation 
        ///   by factor of destinationWeight (range 0.0 to 1.0)
        public void slerpRotation (Quaternion destinationRotation, float destinationWeight)
        {
            Quaternion source = m_rotation.getQuaternion ();
            //Quaternion.Slerp (ref source, ref destinationRotation, destinationWeight, out source);
			source = Quaternion.Slerp(source, destinationRotation, destinationWeight);

            m_rotation.setFromQuaternion (source);
        }

		///// Perform linear interpolation from this rotation to destination rotation 
		/////   by factor of destinationWeight (range 0.0 to 1.0)
		//public void lerpRotation (Quaternion destinationRotation, float destinationWeight)
		//{
		//    Quaternion source = m_rotation.getQuaternion ();
		//    Quaternion.Lerp (ref source, ref destinationRotation, destinationWeight, out source);
		//    m_rotation.setFromQuaternion (source);
		//}

        /// Linearly interpolate from this position to destination position
        ///   by factor of destinationWeight (range 0.0 to 1.0)
		public void lerpPosition(OpenTK.Vector3 destinationPosition, float destinationWeight)
        { 
            Vector3.Lerp (ref m_position, ref destinationPosition, destinationWeight, out m_position);
        }

        /// Uniform scaling
        public void scaleWorld (float s)
        {
            m_scale *= s;
            m_position *= s;
        }

        public void scaleLocal (float s)
        {
            m_scale *= s;
        }

        /// Non-uniform scaling (only local)
        public void scaleLocal (float sx, float sy, float sz)
        {
            m_scale.X *= sx;
            m_scale.Y *= sy;
            m_scale.Z *= sz;
        }

		//public Boolean hasUniformScaling ()
		//{
		//    bool hasUniformScale = this.Rotation.isUniformScaling ()
		//                             && MuxMath.equals (m_scale.X, m_scale.Y)
		//                             && MuxMath.equals (m_scale.Y, m_scale.Z);
		//    return (hasUniformScale);
		//}

        /// Make axes mutually perpendicular; correct for drift
        public void makeOrthonormal ()
        {
            m_rotation.makeOrthonormal ();
        }

        /// Make axes mutually perpendicular; correct for drift
        /// Slightly faster but less accurate
        public void makeOrthonormalCross ()
        {
            m_rotation.makeOrthonormalCross ();
        }

		public OpenTK.Vector3 Scale
        {
            get { return (m_scale); }
            set { m_scale = value; }
        }

		public OpenTK.Vector3 Right
        {
            get { return (m_rotation.Right); }
            set { m_rotation.Right = value; }
        }

		public OpenTK.Vector3 Up
        {
            get { return (m_rotation.Up); }
            set { m_rotation.Up = value;  }
        }

        public OpenTK.Vector3 Forward
        {
            get { return (m_rotation.Forward); }
            set { m_rotation.Forward = value;  }
        }

        internal OpenTK.Vector3 Backward
        {
            get { return (m_rotation.Backward); }
            set { m_rotation.Backward = value; }
        }

        public Matrix3 Rotation
        {
            get { return (m_rotation); }
            set { m_rotation = value;  }
        }

        public Quaternion getRotation ()
        {
            return (m_rotation.getQuaternion ());
        }

        public void setRotation (Quaternion rotation)
        {
            m_rotation.setFromQuaternion (rotation);
        }

        public OpenTK.Vector3 Position
        {
            get { return (m_position); }
            set { m_position = value;  }
        }

        public MuxEngine.LinearAlgebra.Matrix4 Transform4
        {
            get
            {
				return (new MuxEngine.LinearAlgebra.Matrix4(m_rotation, m_scale, m_position));
            }
            set
            {
                m_rotation = value.Rotation;
                m_position = value.Translation;
            }
        }

		//public Matrix Transform
		//{
		//    // row 1 = right
		//    // row 2 = up
		//    // row 3 = forward (backward?)
		//    get
		//    {
		//        Matrix m = new Matrix();
		//        m.M44 = 1.0f;
		//        m.Row0 = m_rotation.Right * m_scale.X;
		//        m.Up = m_rotation.Up * m_scale.Y;
		//        m.Backward = m_rotation.Backward * m_scale.Z;
		//        m.Translation = m_position;
		//        return (m);
		//    }
		//    set
		//    {
		//        m_rotation.Right = value.Right;
		//        m_rotation.Up = value.Up;
		//        m_rotation.Backward = value.Backward;
		//        m_position = value.Translation;
		//    }
		//}

    }
}
