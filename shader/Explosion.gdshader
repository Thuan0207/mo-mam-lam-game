shader_type canvas_item;

uniform vec2 centre;
uniform float force;
uniform sampler2D screen_texture : hint_screen_texture, repeat_disable, filter_nearest;
uniform float size;
uniform float outeredgeOffset;
uniform bool monoMode;

void vertex() {
	// Called for every vertex the material is visible on.
}

void fragment() {
	// Called for every pixel the material is visible on.
	float ratio = TEXTURE_PIXEL_SIZE.x / TEXTURE_PIXEL_SIZE.y;
	vec2 scaledUV = (UV - vec2(0.5,0)) / vec2(ratio, 1) + vec2(0.5,0);
	float mask = (1.0 - smoothstep(size - outeredgeOffset, size,length(scaledUV - centre))) 
	* smoothstep(size - (outeredgeOffset * 2.0), size - (outeredgeOffset),length(scaledUV - centre));
	vec2 disp = normalize(scaledUV - centre) * force * mask;
	 COLOR = !monoMode ? texture(TEXTURE, scaledUV - disp) * vec4(vec3(1.0), 0.3) : vec4(vec3(mask), 1);
	//COLOR = vec4(vec3(mask), 1);
}

void light() {
	// Called for every pixel for every light affecting the CanvasItem.
}
