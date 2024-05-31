shader_type canvas_item;

uniform bool whitened = false;
uniform float mix_weight = 1.0;

void vertex() {
	// Called for every vertex the material is visible on.
}

void fragment() {
	// Called for every pixel the material is visible on.
	vec4 texture_color = texture(TEXTURE, UV);
	if (whitened) {
		vec3 white = vec3(1,1,1);
		vec3 whitened_texture_rgb = mix(texture_color.rbg, white, mix_weight);
		COLOR = vec4(whitened_texture_rgb, texture_color.a);	
	} else {
		COLOR = texture_color;
	}
}

void light() {
	// Called for every pixel for every light affecting the CanvasItem.
}
