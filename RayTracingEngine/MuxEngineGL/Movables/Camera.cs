
using System;

using OpenTK;

using MuxEngine.LinearAlgebra;

namespace MuxEngine.Movables
{
    // Camera uses a RHS for consistency with other Movable objects
    public abstract class Camera : Movable
    {
        /************************************************************/

		protected System.Drawing.Rectangle ClientBounds
		{
			get;
			private set;
		}

        protected OpenTK.Matrix4 m_view;

		OpenTK.Matrix4 m_projection;

        bool m_projectionChanged;

        float m_verticalFov;
        float m_aspectRatio;
        float m_nearPlane;
        float m_farPlane;

        /************************************************************/

		public Camera(System.Drawing.Rectangle clientBounds)
			: this(clientBounds, MuxEngine.LinearAlgebra.Matrix4.Identity)
        {
        }

		public Camera(System.Drawing.Rectangle clientBounds, MuxEngine.LinearAlgebra.Matrix4 xform)
            : base (xform)
        {
            ClientBounds = clientBounds;

            initCommon ();
        }

		public Camera(System.Drawing.Rectangle clientBounds, Vector3 forward,
                       Vector3 up, Vector3 position)
            : base (forward, up, position)
        {
            ClientBounds = clientBounds;

            initCommon ();
        }

        private void initCommon ()
        {
            // By default view is aligned with world axes
			m_view = OpenTK.Matrix4.Identity;

            m_verticalFov = MathHelper.PiOver4;
            m_aspectRatio = (float)ClientBounds.Width / ClientBounds.Height;
            m_nearPlane = 1.0f;
            m_farPlane = 1.0e5f;
            m_projectionChanged = true;
            computeProjection ();
        }

		public void setClientBounds(System.Drawing.Rectangle clientBounds)
        {
            ClientBounds = clientBounds;
			AspectRatio = (float)ClientBounds.Width / ClientBounds.Height;
        }

        public virtual void computeView ()
        {
            MuxEngine.LinearAlgebra.Matrix4 world = base.Transform4;
            computeViewMatrix (ref world);
        }

        // Only computes if FOV, aspect ratio, or near/far planes have changed
        public void computeProjection ()
        {
            if (m_projectionChanged)
            {
				//CreatePerspectiveFieldOfView
                m_projection = OpenTK.Matrix4.CreatePerspectiveFieldOfView (m_verticalFov,
                                 m_aspectRatio, m_nearPlane, m_farPlane);
                m_projectionChanged = false;
            }
        }

        protected void computeViewMatrix (ref MuxEngine.LinearAlgebra.Matrix4 world)
        {
            // Compute view matrix directly for speed
            //   To invert the world matrix we transpose it
            //   We ensure it's orthonormal first
            Vector3 zDir = world.Backward;
            Vector3.Normalize (ref zDir, out zDir);
            Vector3 xDir = Vector3.Cross (world.Up, zDir);
            Vector3.Normalize (ref xDir, out xDir);
            Vector3 yDir = Vector3.Cross (zDir, xDir);

            // Column four shouldn't deviate from [ 0 0 0 1 ]
            m_view.M11 = xDir.X;
            m_view.M12 = yDir.X;
            m_view.M13 = zDir.X;
            m_view.M21 = xDir.Y;
            m_view.M22 = yDir.Y;
            m_view.M23 = zDir.Y;
            m_view.M31 = xDir.Z;
            m_view.M32 = yDir.Z;
            m_view.M33 = zDir.Z;
            Vector3 eye = world.Translation;
            m_view.M41 = -Vector3.Dot (eye, xDir);
            m_view.M42 = -Vector3.Dot (eye, yDir);
            m_view.M43 = -Vector3.Dot (eye, zDir);
        }

        public OpenTK.Matrix4 View
        {
            get { return (m_view); }
        }

        public float VerticalFieldOfView
        {
            get { return (MathHelper.RadiansToDegrees(m_verticalFov)); }
            set
            {
                m_verticalFov = MathHelper.DegreesToRadians (value);
                m_projectionChanged = true;
            }
        }

        public float AspectRatio
        {
            get { return (m_aspectRatio); }
            set
            {
                m_aspectRatio = value;
                m_projectionChanged = true;
            }
        }

        public float NearPlane
        {
            get { return (m_nearPlane); }
            set
            {
                m_nearPlane = value;
                m_projectionChanged = true;
            }
        }

        public float FarPlane
        {
            get { return (m_farPlane); }
            set
            {
                m_farPlane = value;
                m_projectionChanged = true;
            }
        }

        public OpenTK.Matrix4 Projection
        {
            get
            {
                // Only computes if necessary
                computeProjection ();
                return (m_projection);
            }
        }

    }
}

/***************************************************************************/
