using OpenTK;
using MuxEngine.LinearAlgebra;

namespace MuxEngine.Movables
{
    public interface IMovable
    {
        void reset ();
        void setPosition (float x, float y, float z);

        void moveWorld (Vector3 dir, float units);
        void moveLocal (Vector3 dir, float units);
        void move (Direction dir, float units);

        void rotateWorld (Vector3 axis, float angleDeg);
        void rotateAboutPoint (Vector3 p, Vector3 axis, float angleDeg);

        Quaternion getRotation ();
        void setRotation (Quaternion rotation);

        void pitch (float angleDeg);
        void yaw (float angleDeg);
        void roll (float angleDeg);
        void alignWithWorldY ();

        // uniform scaling
        void scaleWorld (float s);
        void scaleLocal (float s);

        // non-uniform scaling (only local)
        void scaleLocal (float sx, float sy, float sz);

        // make axes mutually perpendicular; correct for drift
        void makeOrthonormal ();

        // common to both Movable implementations
        Vector3 Right { get; }
        Vector3 Up { get; }
        Vector3 Forward { get; }
        Vector3 Position { get; set; }
        Vector3 Scale { get; }

        MuxEngine.LinearAlgebra.Matrix4 Transform4 { get; set; }
        //OpenTK.Matrix4 Transform { get; set; }
    }
}
