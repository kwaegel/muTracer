using System;

namespace Raytracing.Driver
{
	static class Program
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		static void Main(string[] args)
		{
			using (RayTracingDriver driver = new RayTracingDriver())
			{
				driver.Run();
			}
		}
	}
}

