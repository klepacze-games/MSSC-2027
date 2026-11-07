shader_type spatial;

uniform vec3 baseColor: source_color = vec3(0.2, 0.2, 0.2);
uniform sampler2D baseTexture: source_color, repeat_enable;

uniform float snowMinimum: hint_range(0.0, 1.0, 0.01) = 0.4;
uniform float snowOptimum: hint_range(0.0, 1.0, 0.01) = 0.7;

uniform vec3 snowColorMain: source_color = vec3(0.9, 0.9, 1.0);
uniform vec3 snowColorSecondary: source_color;
uniform sampler2D snowNoise : hint_default_white, repeat_enable;
uniform sampler2D snowTexture : hint_default_white, repeat_enable;
uniform float noiseScale : hint_range(0.0, 2.0, 0.05) = 0.3;

// Funkcja Triplanar do pobierania tekstur bez szwów
vec4 triplanar_texture(sampler2D p_sampler, vec3 p_weights, vec3 p_triplanar_pos) {
    vec4 samp_x = texture(p_sampler, p_triplanar_pos.zy);
    vec4 samp_y = texture(p_sampler, p_triplanar_pos.xz);
    vec4 samp_z = texture(p_sampler, p_triplanar_pos.xy);
    return samp_x * p_weights.x + samp_y * p_weights.y + samp_z * p_weights.z;
}

void fragment() {
    vec3 world_normal = normalize(mat3(INV_VIEW_MATRIX) * NORMAL);
    vec3 world_pos = (INV_VIEW_MATRIX * vec4(VERTEX, 1.0)).xyz;

    vec3 triplanar_weights = abs(world_normal);
    triplanar_weights = triplanar_weights / (triplanar_weights.x + triplanar_weights.y + triplanar_weights.z);

    float upness = dot(world_normal, vec3(0.0, 1.0, 0.0));

    float noise = triplanar_texture(snowNoise, triplanar_weights, world_pos * 0.2).r;
    float modifiedUpness = upness - (noise * noiseScale);

    float details = triplanar_texture(snowTexture, triplanar_weights, world_pos * 0.01).r;
    float mask = smoothstep(snowMinimum, snowOptimum, modifiedUpness);
    mask = mix(mask, smoothstep(0.2, 0.5, details), 0.1);

    vec3 snowColorFinal = mix(snowColorMain, snowColorSecondary, details);

    vec3 albedoBase = texture(baseTexture, UV).rgb * baseColor;

    ALBEDO = mix(albedoBase, snowColorFinal, mask);
}
