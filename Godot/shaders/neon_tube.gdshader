shader_type spatial;
render_mode blend_add, depth_draw_never, cull_disabled, unshaded;

// Industrial neon tube — violet glow with subtle flicker.

uniform vec4 neon_color : source_color = vec4(0.54, 0.0, 1.0, 1.0);
uniform float base_intensity : hint_range(0.5, 6.0) = 2.5;
uniform float flicker_speed : hint_range(0.0, 20.0) = 8.0;
uniform float flicker_strength : hint_range(0.0, 0.5) = 0.06;
uniform float tube_core_sharpness : hint_range(1.0, 16.0) = 4.0;

void fragment() {
    // Tube core — bright center, falloff toward edges
    float core = pow(1.0 - abs(UV.x - 0.5) * 2.0, tube_core_sharpness);
    
    // Flicker — random-ish noise using sin harmonics
    float t = TIME * flicker_speed;
    float flicker = 1.0 + (sin(t) * sin(t * 1.7) * sin(t * 0.3)) * flicker_strength;
    
    float intensity = base_intensity * flicker;
    
    ALBEDO = vec3(0.0);
    EMISSION = neon_color.rgb * core * intensity;
    ALPHA = core * neon_color.a;
}
