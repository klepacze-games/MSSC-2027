shader_type spatial;
render_mode cull_disabled;
render_mode unshaded;
render_mode blend_mix, depth_draw_opaque;

uniform vec3 ParticleColor: source_color = vec3(1.0);
uniform float Opacity: hint_range(0.0, 1.0, 0.1);

varying float CurrentLifeTime;


float rand(uint seed) {
    seed = (seed ^ 61u) ^ (seed >> 16u);
    seed *= 9u;
    seed = seed ^ (seed >> 4u);
    seed *= 0x27d91955u; 
    seed = seed ^ (seed >> 15u);
    
	return float(seed) * (1.0 / 4294967296.0);
}

void vertex() {
	CurrentLifeTime = INSTANCE_CUSTOM[0];
    vec3 scale = vec3(
        length(MODEL_MATRIX[0].xyz),
        length(MODEL_MATRIX[1].xyz),
        length(MODEL_MATRIX[2].xyz)
    );
    MODELVIEW_MATRIX = VIEW_MATRIX * MODEL_MATRIX;
    MODELVIEW_MATRIX[0] = vec4(1.0, 0.0, 0.0, 0.0) * scale.x;
    MODELVIEW_MATRIX[1] = vec4(0.0, 1.0, 0.0, 0.0) * scale.y; 
    MODELVIEW_MATRIX[2] = vec4(0.0, 0.0, 1.0, 0.0) * scale.z;
}

void fragment() {
	float fade = sin(CurrentLifeTime * PI);
	ALBEDO = COLOR.xyz;
	ALPHA = Opacity * fade;
}