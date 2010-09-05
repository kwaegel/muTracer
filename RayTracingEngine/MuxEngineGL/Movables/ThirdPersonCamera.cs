
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

using MuxEngine.LinearAlgebra;

namespace MuxEngine.Movables
{
    public class ThirdPersonCamera : Camera
    {
        /************************************************************/

        IMovable m_movableToFollow;
        Vector3 m_offset;
        float m_yaw;
        float m_pitch;

        /************************************************************/

        public ThirdPersonCamera (Rectangle clientBounds)
            : base (clientBounds)
        {
            m_movableToFollow = null;
            // default the offset to slightly up and behind
            m_offset = new Vector3 (0.0f, 4.0f, -20.0f);
        }

        public ThirdPersonCamera (Rectangle clientBounds, IMovable movableToFollow)
            : base (clientBounds, movableToFollow.Transform)
        {
            m_movableToFollow = movableToFollow;
            // default the offset to slightly up and behind
            m_offset = new Vector3 (0.0f, 4.0f, -20.0f);
        }

        public ThirdPersonCamera (Rectangle clientBounds, IMovable movableToFollow,
                                  Vector3 offset)
            : base (clientBounds, movableToFollow.Transform)
        {
            m_movableToFollow = movableToFollow;
            m_offset = offset;
        }

        public IMovable MovableToFollow
        {
            get { return (m_movableToFollow); }
            set { m_movableToFollow = value; }
        }

        public Vector3 Offset
        {
            get { return (m_offset); }
            set { m_offset = value; }
        }

        public Single Yaw
        {
            get { return (m_yaw); }
            set { m_yaw = value; }
        }

        public void adjustYaw (float angleDeg)
        {
            const float MIN_YAW = -179.0f;
            const float MAX_YAW = +180.0f;
            m_yaw = MathHelper.Clamp (m_yaw + angleDeg, MIN_YAW, MAX_YAW);
        }

        public void adjustPitch (float angleDeg)
        {
            const float MIN_PITCH = -89.0f;
            const float MAX_PITCH = +90.0f;
            m_pitch = MathHelper.Clamp (m_pitch + angleDeg, MIN_PITCH, MAX_PITCH);
        }

        public override void computeView ()
        {
            Matrix4 world = m_movableToFollow.Transform4;
            // offset camera from model
            world.Translation += m_offset * world.Rotation;
            // update camera's transform
            this.Transform4 = world;
            // adjust forward vector so camera is looking at model's position
            world.Forward = m_movableToFollow.Position - world.Translation;
            computeViewMatrix (ref world);
        }

        /*
        // Needs fixed
        public void computeView ()
        {
            Matrix4 world = new Matrix4 ();
            //Matrix4 world = m_movableToFollow.Transform4;
            world.Translation = m_movableToFollow.Position;
            Matrix3 yaw = new Matrix3 ();
            yaw.rotateAxis (world.Up, m_yaw);
            Matrix3 pitch = new Matrix3 ();
            pitch.rotateAxis (world.Right, m_pitch);
            world.Rotation = yaw * pitch;
            world.Translation += m_offset * world.Rotation;
            // update camera's transform
            this.Transform4 = world;
            // adjust forward vector so camera is looking at model's position
            //world.Forward = m_movableToFollow.Position - world.Translation;
            computeViewMatrix (ref world);
        }
        */
    }
}
