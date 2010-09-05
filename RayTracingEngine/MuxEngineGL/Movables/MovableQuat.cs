
using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;

using MuxEngine.LinearAlgebra;
using MuxEngine.Utility;

namespace MuxEngine.Movables
{
    // Implementation of Movable interface using quaternions
    //   Better-suited for frequent rotations
    //   Also saves space over matrix-based Movable implementation
    public class MovableQuat : IMovable, ICloneable
    {
        Quaternion m_rotation;
        Vector3 m_position;
        Vector3 m_scale;

        /************************************************************/

        public MovableQuat ()
        {
            m_rotation = Quaternion.Identity;
            m_position = Vector3.Zero;
            m_scale = new Vector3 (1.0f);
        }

        public MovableQuat (Vector3 forward, Vector3 up, Vector3 position)
        {
            Matrix3 rotation = new Matrix3 (forward, up, true);
            m_rotation = rotation.getQuaternion ();
            m_position = position;
            m_scale = new Vector3 (1.0f);
        }

        public MovableQuat (Matrix3 rotation)
        {
            m_rotation = rotation.getQuaternion ();
            m_position = Vector3.Zero;
            m_scale = new Vector3 (1.0f);
        }

        public MovableQuat (Matrix4 transform)
        {
            m_rotation = transform.Rotation.getQuaternion ();
            m_position = transform.Translation;
            m_scale = new Vector3 (1.0f);
        }

        public MovableQuat (Matrix transform)
        {
            Matrix3 rotation = new Matrix3 (transform);
            m_rotation = rotation.getQuaternion ();
            m_position = transform.Translation;
            m_scale = new Vector3 (1.0f);
        }

        /// Move to origin, looking down -Z axis
        public void reset ()
        {
            m_rotation = Quaternion.CreateFromAxisAngle (Vector3.UnitY, MathHelper.Pi);
            m_position = Vector3.Zero;
            m_scale = new Vector3 (1.0f);
        }

        public void setPosition (float x, float y, float z)
        {
            m_position.X = x;
            m_position.Y = y;
            m_position.Z = z;
        }

        public void moveWorld (Vector3 dir, float units)
        {
            m_position += dir * units;
        }

        public void moveLocal (Vector3 dir, float units)
        {
            Vector3 worldDir = Vector3.Transform (dir, m_rotation);
            m_position += worldDir * units;
        }

        /// Translate "units" in local direction "dir"
        ///   where "dir" is Right, Up, Forward, etc.
        public void move (Direction dir, float units)
        {
            Vector3 moveVec;
            if (dir == Direction.Right)
            {
                moveVec = this.Right;
            }
            else if (dir == Direction.Up)
            {
                moveVec = this.Up;
            }
            else if (dir == Direction.Forward)
            {
                moveVec = this.Forward;
            }
            else if (dir == Direction.Left)
            {
                moveVec = -this.Right;
            }
            else if (dir == Direction.Down)
            {
                moveVec = -this.Up;
            }
            else // Backward
            {
                moveVec = -this.Forward;
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
                moveVec = this.Right;
            }
            else if (dir == Direction.Up)
            {
                moveVec = this.Up;
            }
            else if (dir == Direction.Forward)
            {
                moveVec = this.Forward;
            }
            else if (dir == Direction.Left)
            {
                moveVec = -this.Right;
            }
            else if (dir == Direction.Down)
            {
                moveVec = -this.Up;
            }
            else // Backward
            {
                moveVec = -this.Forward;
            }
            moveVec.Y = 0.0f;
            m_position += moveVec * units;
        }

        public void rotateWorld (Vector3 axis, float angleDeg)
        {
            float angleRad = MathHelper.ToRadians (angleDeg);
            Quaternion rotate = Quaternion.CreateFromAxisAngle (axis, angleRad);
            Quaternion.Concatenate (ref m_rotation, ref rotate, out m_rotation);
            m_position = Vector3.Transform (m_position, rotate);
        }

        /// Rotate about world Y axis, preserving position
        public void rotateWorldY (float angleDeg)
        {
            float angleRad = MathHelper.ToRadians (angleDeg);
            Quaternion rotate = Quaternion.CreateFromAxisAngle (Vector3.UnitY, angleRad);
            Quaternion.Concatenate (ref m_rotation, ref rotate, out m_rotation);
        }

        public void rotateAboutPoint (Vector3 p, Vector3 axis, float angleDeg)
        {
            float angleRad = MathHelper.ToRadians (angleDeg);
            Quaternion rotate = Quaternion.CreateFromAxisAngle (axis, angleRad);
            Quaternion.Concatenate (ref m_rotation, ref rotate, out m_rotation);
            m_position = Vector3.Transform (m_position, rotate);
            m_position += Vector3.Transform (-p, rotate) + p;
        }

        public void pitch (float angleDeg)
        {
            float angleRad = MathHelper.ToRadians (angleDeg);
            Quaternion rotate = Quaternion.CreateFromAxisAngle (this.Right, angleRad);
            // Concatenate multiplies in reverse order b/c XNA uses standard multiplication
            Quaternion.Concatenate (ref m_rotation, ref rotate, out m_rotation);
        }

        public void yaw (float angleDeg)
        {
            float angleRad = MathHelper.ToRadians (angleDeg);
            Quaternion rotate = Quaternion.CreateFromAxisAngle (this.Up, angleRad);
            Quaternion.Concatenate (ref m_rotation, ref rotate, out m_rotation);
        }

        public void roll (float angleDeg)
        {
            float angleRad = MathHelper.ToRadians (angleDeg);
            Quaternion rotate = Quaternion.CreateFromAxisAngle (this.Forward, angleRad);
            Quaternion.Concatenate (ref m_rotation, ref rotate, out m_rotation);
        }

        public void billboard (Camera camera)
        {
            Vector3 forward = camera.Position - m_position;
            forward.Normalize ();
            Matrix3 billboard = new Matrix3 (forward, camera.Up);
            m_rotation = billboard.getQuaternion ();
        }

        public void alignWithWorldY ()
        {
            Matrix transform = this.Transform; 
            transform.Up = Vector3.UnitY;
            Vector3 backward = transform.Backward;
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
            transform.Backward = backward;
            transform.Right = Vector3.Cross (transform.Up, transform.Backward); 
            Quaternion.CreateFromRotationMatrix (ref transform, out m_rotation);
        }

        /// Perform spherical linear interpolation from this rotation to destination rotation 
        ///   by factor of destinationWeight (range 0.0 to 1.0)
        public void slerpRotation (Quaternion destinationRotation, float destinationWeight)
        {
            Quaternion.Slerp (ref m_rotation, ref destinationRotation, destinationWeight, out m_rotation);
        }

        /// Perform linear interpolation from this rotation to destination rotation 
        ///   by factor of destinationWeight (range 0.0 to 1.0)
        public void lerpRotation (Quaternion destinationRotation, float destinationWeight)
        {
            Quaternion.Lerp (ref m_rotation, ref destinationRotation, destinationWeight, out m_rotation);
        }

        /// Linearly interpolate from this position to destination position
        ///   by factor of destinationWeight (range 0.0 to 1.0)
        public void lerpPosition (Vector3 destinationPosition, float destinationWeight)
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

        public Boolean hasUniformScaling ()
        {
            bool hasUniformScale = MuxMath.equals (m_scale.X, m_scale.Y)
                                     && MuxMath.equals (m_scale.Y, m_scale.Z);
            return (hasUniformScale);
        }

        /// Ensure rotation is valid
        public void makeOrthonormal ()
        {
            m_rotation.Normalize ();
        }

        Object ICloneable.Clone ()
        {
            return (this.Clone ());
        }

        public MovableQuat Clone ()
        {
            return ((MovableQuat)this.MemberwiseClone ());
        }

        public Vector3 Scale
        {
            get { return (m_scale); }
            set { m_scale = value; }
        }

        // Right, Up, and Forward are somewhat costly when using quaternions
        public Vector3 Right
        {
            get
            {
                float qw = m_rotation.W;
                float qx = m_rotation.X;
                float qy = m_rotation.Y;
                float qz = m_rotation.Z;
                float rx = 1 - 2 * (qy * qy + qz * qz);
                float ry = 2 * (qx * qy + qw * qz);
                float rz = 2 * (qx * qz - qw * qy);
                return (new Vector3 (rx, ry, rz));
            }
        }

        public Vector3 Up
        {
            get
            {
                float qw = m_rotation.W;
                float qx = m_rotation.X;
                float qy = m_rotation.Y;
                float qz = m_rotation.Z;

                float rx = 2 * (qx * qy - qw * qz);
                float ry = 1 - 2 * (qx * qx + qz * qz);
                float rz = 2 * (qy * qz + qw * qx);
                return (new Vector3 (rx, ry, rz));
            }
        }

        public Vector3 Forward
        {
            get
            {
                float qw = m_rotation.W;
                float qx = m_rotation.X;
                float qy = m_rotation.Y;
                float qz = m_rotation.Z;

                float rx = 2 * (qx * qz + qw * qy);
                float ry = 2 * (qy * qz - qw * qx);
                float rz = 1 - 2 * (qx * qx + qy * qy);
                return (new Vector3 (-rx, -ry, -rz));
            }
        }

        internal Vector3 Backward
        {
            get
            {
                return (-this.Forward);
            }
        }

        public Quaternion Rotation
        {
            get { return (m_rotation); }
            set { m_rotation = value;  }
        }

        public Matrix3 RotationMatrix
        {
            get
            {
                // Matrix3 ctor will invert Forward
                return (new Matrix3 (this.Right, this.Up, this.Forward));
            }
            set
            {
                m_rotation = value.getQuaternion ();
            }
        }

        public Quaternion getRotation ()
        {
            return (m_rotation);
        }

        public void setRotation (Quaternion rotation)
        {
            m_rotation = rotation;
        }

        public Vector3 Position
        {
            get { return (m_position); }
            set { m_position = value;  }
        }

        public Matrix4 Transform4
        {
            get
            {
                return (new Matrix4 (m_rotation, m_scale, m_position));
            }
            set
            {
                Matrix m = value.Transform;
                m_rotation = Quaternion.CreateFromRotationMatrix (m);
                m_position = value.Translation;
            }
        }

        public Matrix Transform
        {
            get
            {
                Matrix m = Matrix.CreateFromQuaternion (m_rotation);
                m.Right *= m_scale.X;
                m.Up *= m_scale.Y;
                m.Backward *= m_scale.Z;
                m.Translation = m_position;
                return (m);
            }
            set
            {
                value.Decompose (out m_scale, out m_rotation, out m_position);
            }
        }
    }
}
