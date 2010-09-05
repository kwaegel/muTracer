
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

/***************************************************************************/

namespace MuxEngine.Movables
{

    public class FirstPersonCamera : Camera
    {
        /************************************************************/

        public FirstPersonCamera (Rectangle clientBounds)
            : base (clientBounds)
        {
        }

        // Assumes "forward" is in the XZ plane, and up is (0, 1, 0)
        public FirstPersonCamera (Rectangle clientBounds, Vector3 forward,
                                  Vector3 position)
            : this (clientBounds, forward, Vector3.Up, position)
        {
        }

        public FirstPersonCamera (Rectangle clientBounds, Vector3 forward,
                                  Vector3 up, Vector3 position)
            : base (clientBounds, forward, up, position)
        {
        }

    }
}

/***************************************************************************/



