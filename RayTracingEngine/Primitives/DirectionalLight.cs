using OpenTK;
using OpenTK.Graphics;

namespace Raytracing.SceneStructures
{
	public class DirectionalLight : Light
	{

		public Vector3 Direction;

		public DirectionalLight(Vector3 direction, float intensity, Color4 color)
		{
			this.Direction = direction;
			this.Color = new Color4();

			Color.R = color.R * intensity;
			Color.G = color.G * intensity;
			Color.B = color.B * intensity;
		}

		public override Vector3 getDirection(Vector3 from)
		{
			return -Direction*50000;
		}
	}
}
