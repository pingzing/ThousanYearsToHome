shader_type canvas_item;
uniform float percent = 1.0; // value from 0.0 - 1.0 that determines how much to shade the sprite.
uniform bool reverse = false; // If true, will lerp from target_color -> color instead of the other way around
uniform vec4 target_color = vec4(0.0, 0.0, 0.0, 1.0); // Default to pure black.

void fragment() {
	// Get the current color based on the passed-in UV, and the global TEXTURE variable of the 
	// sprite we're attached to
	vec4 color = texture(TEXTURE, UV);
	
	// If the color is not transparent, turn it target_color.
	if (color.a != 0.0) {
		// Change the color to a mix between what it currently is, and the target, based on percent.
		vec4 final_color;
		if (reverse) {
			final_color = mix(target_color, color, percent);
		}
		else {
			final_color = mix(color, target_color, percent);
		}
		color = final_color;
	// Otherwise, leave it transparent.
	} else {
		color = vec4(0.0, 0.0, 0.0, 0.0);
	}
	
	COLOR = color;
}