using OpenTK;
using OpenTK.Graphics;

namespace Raytracing
{
    public class PointLight : Light
    {

        public Vector3 Position;

        public PointLight(Vector3 position, float intensity, Color4 color)
        {
            this.Position = position;
            this.Color = new Color4();

            Color.R = color.R * intensity;
            Color.G = color.G * intensity;
            Color.B = color.B * intensity;
        }

		public override Vector3 getDirection(Vector3 from)
		{
			return Position - from;
		}
    }
}
