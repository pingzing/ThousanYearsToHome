shader_type particles;

uniform vec3 direction = vec3(0, -1, 0);
uniform float rise_speed = 20.0;
uniform float rise_speed_randomness;
uniform float scale;
uniform float scale_random;
uniform float lifetime_randomness;
uniform vec3 emission_box_extents;
uniform vec4 color_value : hint_color;
uniform int trail_divisor;
uniform float time = 0.0;

uniform sampler2D sprite;


float rand_from_seed(inout uint seed) {
	int k;
	int s = int(seed);
	if (s == 0)
	s = 305420679;
	k = s / 127773;
	s = 16807 * (s - k * 127773) - 2836 * k;
	if (s < 0)
		s += 2147483647;
	seed = uint(s);
	return float(seed % uint(65536)) / 65535.0;
}

float rand_from_seed_m1_p1(inout uint seed) {
	return rand_from_seed(seed) * 2.0 - 1.0;
}

uint hash(uint x) {
	x = ((x >> uint(16)) ^ x) * uint(73244475);
	x = ((x >> uint(16)) ^ x) * uint(73244475);
	x = (x >> uint(16)) ^ x;
	return x;
}

// CUSTOM.x is some crazy transform nonsense?
// CUSTOM.y = is 0.0 - 1.0 normalized value that determines how far along in the particle lifetime we are
// CUSOTM.w is a 0.0 - 1.0 normalized value that is as far as CUSTOM.y is allowed to go. Because
//		lifetimes can be randomized, it might not actually max out at 1.0.
// CUSTOM.z is the rise velocity determined at RESTART, and affected by randomness.

void vertex() {
	uint base_number = NUMBER / uint(trail_divisor);
	uint alt_seed = hash(base_number + uint(1) + RANDOM_SEED);
	float scale_rand = rand_from_seed(alt_seed);
	float pi = 3.14159;

	bool restart = false;
	float tv = 0.0;
	if (CUSTOM.y > CUSTOM.w) {
		restart = true;
		tv = 1.0;
	}

	if (RESTART || restart) {
		float tex_linear_velocity = 0.0;
		float tex_angle = 0.0;
		CUSTOM.y = 0.0;
		CUSTOM.w = (1.0 - lifetime_randomness * rand_from_seed(alt_seed));
		CUSTOM.z = (1.0 - rise_speed_randomness * rand_from_seed(alt_seed));
		TRANSFORM[3].xyz = vec3(rand_from_seed(alt_seed) * 2.0 - 1.0, rand_from_seed(alt_seed) * 2.0 - 1.0, rand_from_seed(alt_seed) * 2.0 - 1.0) * emission_box_extents;
		VELOCITY = (EMISSION_TRANSFORM * vec4(VELOCITY, 0.0)).xyz;
		TRANSFORM = EMISSION_TRANSFORM * TRANSFORM;
		VELOCITY.z = 0.0;
		TRANSFORM[3].z = 0.0;
		
		// --- Dissolve-specific code ---
		
		vec2 particle_position = TRANSFORM[3].xy;
		vec2 tex_size = vec2(textureSize(sprite, 0));
		// (0.5, 0.5) is the UV coordinate of the dissolve origin	
		vec4 sprite_color = texture(sprite, particle_position / tex_size + vec2(0.5, 0.5));
		COLOR = sprite_color; // set this one-pixel particle to the color of the sprite underneath
		
		// Ignore tranpsarent pixels
		if (sprite_color.a == 0.0) {
			ACTIVE = false;
		}
		
	} else {
		CUSTOM.y += DELTA / LIFETIME;
		tv = CUSTOM.y / CUSTOM.w;		
		
		//--- Dissolve code ---
		
		vec2 particle_position = TRANSFORM[3].xy;
		vec2 tex_size = vec2(textureSize(sprite, 0));
		float elapsed = time / LIFETIME;
		
		// Movement
		// x = 0.0 - 1.0 from left to right
		// y = 0.0 - 1.0 from top to bottom
		// Add (0.5, 0.5) because the sprite is centered, not top-left-origin'd
		vec2 uv = (particle_position / tex_size) + vec2(0.5, 0.5);
		if (elapsed > uv.y) {
			float rise_speed_randomized = rise_speed * CUSTOM.z;
			VELOCITY.x = direction.x * rise_speed_randomized;
			VELOCITY.y = direction.y * rise_speed_randomized;
			VELOCITY.z = direction.z * rise_speed_randomized;
		}
		
		// --- End dissolve code ---

	}
	float tex_scale = 1.0;

	TRANSFORM[0] = vec4(cos(CUSTOM.x), -sin(CUSTOM.x), 0.0, 0.0);
	TRANSFORM[1] = vec4(sin(CUSTOM.x), cos(CUSTOM.x), 0.0, 0.0);
	TRANSFORM[2] = vec4(0.0, 0.0, 1.0, 0.0);
	float base_scale = tex_scale * mix(scale, 1.0, scale_random * scale_rand);
	if (base_scale < 0.000001) {
		base_scale = 0.000001;
	}
	TRANSFORM[0].xyz *= base_scale;
	TRANSFORM[1].xyz *= base_scale;
	TRANSFORM[2].xyz *= base_scale;
	VELOCITY.z = 0.0;
	
	// Optional fadeout here?
	
	TRANSFORM[3].z = 0.0;
	if (CUSTOM.y > CUSTOM.w) {
		ACTIVE = false;
	}
}

