shader_type canvas_item;



void fragment() {
	// Called for every pixel the material is visible on.
	
	// for every second of time, move UV. Since they wrap, it scrolls
	vec4 color = texture(TEXTURE, UV + vec2(0.04 * TIME, 0.2 * TIME));
	COLOR = color;
}
