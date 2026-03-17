shader_type spatial;
render_mode blend_mix, depth_draw_opaque, cull_back, diffuse_burley, specular_schlick_ggx;

// Mineral oil surface — iridescent, deep, industrial.

uniform sampler2D oil_normal : hint_normal, filter_linear_mipmap;
uniform sampler2D oil_ripple : hint_normal, filter_linear_mipmap;
uniform float ripple_speed : hint_range(0.0, 2.0) = 0.15;
uniform float ripple_scale : hint_range(0.1, 10.0) = 3.0;
uniform float metallic : hint_range(0.0, 1.0) = 0.9;
uniform float roughness : hint_range(0.0, 1.0) = 0.05;
uniform float iridescence_strength : hint_range(0.0, 2.0) = 0.8;
uniform vec4 oil_tint : source_color = vec4(0.05, 0.04, 0.08, 1.0);

void fragment() {
    vec2 uv1 = UV * ripple_scale + vec2(TIME * ripple_speed * 0.4, TIME * ripple_speed * 0.3);
    vec2 uv2 = UV * ripple_scale * 1.4 - vec2(TIME * ripple_speed * 0.2, TIME * ripple_speed * 0.5);
    
    vec3 normal1 = texture(oil_normal, uv1).xyz * 2.0 - 1.0;
    vec3 normal2 = texture(oil_ripple, uv2).xyz * 2.0 - 1.0;
    vec3 combined_normal = normalize(normal1 + normal2);
    
    // Iridescence — thin-film interference effect
    float iridescence = sin(dot(combined_normal, vec3(0.0, 1.0, 0.0)) * 8.0 + TIME * 0.5);
    vec3 irid_color = vec3(
        0.5 + 0.5 * sin(iridescence * TAU),
        0.5 + 0.5 * sin(iridescence * TAU + TAU / 3.0),
        0.5 + 0.5 * sin(iridescence * TAU + 2.0 * TAU / 3.0)
    ) * iridescence_strength;
    
    ALBEDO = oil_tint.rgb + irid_color * 0.3;
    METALLIC = metallic;
    ROUGHNESS = roughness;
    NORMAL_MAP = combined_normal * 0.5 + 0.5;
    SPECULAR = 1.0;
}
