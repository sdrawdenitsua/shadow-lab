shader_type spatial;
render_mode blend_mix, depth_draw_opaque, cull_back, diffuse_burley, specular_schlick_ggx, unshaded;

// Nova's Violet Eye shader — emissive, pulsing, alive.

uniform vec4 emission_color : source_color = vec4(0.54, 0.0, 1.0, 1.0);
uniform float emission_intensity : hint_range(0.0, 8.0) = 2.0;
uniform float fresnel_power : hint_range(0.0, 8.0) = 3.0;
uniform float pulse_speed : hint_range(0.0, 10.0) = 1.5;
uniform sampler2D iris_texture : hint_default_white, filter_linear_mipmap;
uniform float iris_distortion : hint_range(0.0, 0.5) = 0.08;
uniform float scanline_density : hint_range(0.0, 100.0) = 40.0;
uniform float scanline_opacity : hint_range(0.0, 1.0) = 0.15;

varying vec3 world_normal;
varying vec3 world_position;

void vertex() {
    world_normal = (MODEL_MATRIX * vec4(NORMAL, 0.0)).xyz;
    world_position = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
}

void fragment() {
    vec2 uv = UV;
    
    // Iris distortion — subtle warping like a real eye
    float distortion = sin(uv.x * 12.0 + TIME * 2.0) * iris_distortion * 0.5;
    uv.y += distortion;
    
    vec4 iris = texture(iris_texture, uv);
    
    // Fresnel rim — glows brighter at edges
    vec3 view_dir = normalize(CAMERA_POSITION_WORLD - world_position);
    float fresnel = pow(1.0 - max(dot(normalize(world_normal), view_dir), 0.0), fresnel_power);
    
    // Pulse
    float pulse = (sin(TIME * pulse_speed * TAU) + 1.0) * 0.5;
    float intensity = emission_intensity * (0.7 + pulse * 0.6) * (1.0 + fresnel * 1.5);
    
    // Scanlines — subtle CRT/tech overlay
    float scanline = sin(uv.y * scanline_density) * 0.5 + 0.5;
    float scan_mask = 1.0 - scanline * scanline_opacity;
    
    // Final color
    vec3 base_col = iris.rgb * emission_color.rgb;
    vec3 emissive = base_col * intensity * scan_mask;
    
    ALBEDO = vec3(0.0);
    EMISSION = emissive;
    ALPHA = 1.0;
}
