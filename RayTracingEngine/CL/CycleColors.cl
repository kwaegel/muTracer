kernel void
cycleColors(	const		float		time,
				const		float4		backgroundColor,
				write_only	image2d_t	outputImage)
{
	int2 coord = (int2)(get_global_id(0), get_global_id(1));

	int2 size = get_image_dim(outputImage);

	float4 color = backgroundColor;

	color.x = (((float)coord.x) / size.x) * native_sin(((float)coord.x)/20.0f + time*10.0f);
	color.y = ((float)coord.y) / size.y * native_sin(time*2.0f);
	color.z = backgroundColor.z;
	color.w = 0.0f;
	
	float factor = 1.5f-(color.x + color.y) /2.0f;
	
	color.x *= copysign(native_sin(time), 1.0f) * factor;

	// Oddly enough, the number of digits of PI in this line can cause an InvalidBinaryException...
	// For example, 3.141592 works, but 3.14159 and 3.14159263 do not.
	color.y *= copysign(native_sin(time+3.141592f), 1.0f) * factor;

	write_imagef(outputImage, coord, color);
}