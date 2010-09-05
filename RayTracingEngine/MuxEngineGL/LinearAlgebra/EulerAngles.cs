
using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;

using MuxEngine.Utility;

namespace MuxEngine.LinearAlgebra
{
    public struct EulerAngles
    {
        // angles in radians
        float m_yaw;
        float m_pitch;
        float m_roll;

        public EulerAngles (bool init)
        {
            m_yaw = 0.0f;
            m_pitch = 0.0f;
            m_roll = 0.0f;
        }

        public EulerAngles (float yawDeg, float pitchDeg, float rollDeg)
        {
            m_yaw = MathHelper.ToRadians (yawDeg);
            m_pitch = MathHelper.ToRadians (pitchDeg);
            m_roll = MathHelper.ToRadians (rollDeg);
            normalize ();
        }

        public EulerAngles (Matrix3 rotation)
        {
            float sineOfPitch = -rotation.Forward.Y;
            if (MuxMath.equals (Math.Abs (sineOfPitch), 1.0f))
            {
                // pitched 90 or -90
                m_pitch = MathHelper.PiOver2 * sineOfPitch;
                m_yaw = (float)Math.Atan2 (-rotation.Right.Z, rotation.Right.X);
                m_roll = 0.0f;
            }
            else
            {
                m_pitch = (float)Math.Asin (sineOfPitch);
                m_yaw = (float)Math.Atan2 (rotation.Forward.X, rotation.Forward.Z);
                m_roll = (float)Math.Atan2 (-rotation.Right.Y, rotation.Up.Y);
            }
        }

        public EulerAngles (Quaternion rotation)
        {
            float sineOfPitch = -2.0f * (rotation.Y * rotation.Z - rotation.W * rotation.X);
            if (MuxMath.equals (Math.Abs (sineOfPitch), 1.0f))
            {
                // pitched 90 or -90
                m_pitch = MathHelper.PiOver2 * sineOfPitch;
                m_yaw = (float)Math.Atan2 (-rotation.X * rotation.Z + rotation.W * rotation.Y,
                                            0.5f - rotation.Y * rotation.Y - rotation.Z * rotation.Z);
                m_roll = 0.0f;
            }
            else
            {
                m_pitch = (float)Math.Asin (sineOfPitch);
                m_yaw = (float)Math.Atan2 (rotation.X * rotation.Z + rotation.W * rotation.Y,
                                           0.5f - rotation.X * rotation.X - rotation.Y * rotation.Y);
                m_roll = (float)Math.Atan2 (rotation.X * rotation.Y + rotation.W * rotation.Z,
                                            0.5f - rotation.X * rotation.X - rotation.Z * rotation.Z);
            }
        }
            
        public void setIdentity ()
        {
            m_yaw = 0.0f;
            m_pitch = 0.0f;
            m_roll = 0.0f;
        }

        // maintain yaw   in [ -180, 180 ]
        //          pitch in [ -90, 90 ]
        //          roll  in [ -180, 180 ]
        void normalize ()
        {
            m_pitch = MathHelper.WrapAngle (m_pitch);
            // limit pitch to [ -90, 90 ]
            if (m_pitch > MathHelper.PiOver2)
            {
                m_pitch -= MathHelper.PiOver2;
                m_yaw += MathHelper.Pi;
                m_roll += MathHelper.Pi;
            }
            else if (m_pitch < -MathHelper.PiOver2)
            {
                m_pitch += MathHelper.PiOver2;
                m_yaw += MathHelper.Pi;
                m_roll += MathHelper.Pi;
            }
            if (MuxMath.equals (Math.Abs (m_pitch), MathHelper.PiOver2))
            {
                // pitching 90 or -90 causes roll to rotate about the y-axis
                m_yaw += m_roll;
                m_roll = 0.0f;
            }
            else
                m_roll = MathHelper.Clamp (m_roll, -MathHelper.Pi, MathHelper.Pi);

            m_yaw = MathHelper.WrapAngle (m_yaw);
        }

        public Matrix3 ToMatrix3 ()
        {
            Matrix3 rotation = Matrix3.createYawPitchRollRad (m_yaw, m_pitch, m_roll);
            return (rotation);
        }

        public Quaternion ToQuaternion ()
        {
            return (Quaternion.CreateFromYawPitchRoll (m_yaw, m_pitch, m_roll));
        }

        public override String ToString ()
        {
            return (String.Format ("Yaw: {0:f2}, Pitch: {1:f2}, Roll: {2:f2}", Yaw, Pitch, Roll));
        }

        public float Yaw
        {
            get { return (MathHelper.ToDegrees (m_yaw)); }
        }

        public float Pitch
        {
            get { return (MathHelper.ToDegrees (m_pitch)); }
        }

        public float Roll
        {
            get { return (MathHelper.ToDegrees (m_roll)); }
        }

    }
}
