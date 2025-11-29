#version 430 core

// === INPUTS (from vertex shader, interpolated) ===
in vec3 vNormal;
in vec2 vTexCoord;

// === OUTPUT ===
out vec4 FragColor;  // Final pixel color (RGBA, 0.0 to 1.0)

void main()
{
    // Simple directional lighting
    // Imagine a light shining from upper-right-front
    vec3 lightDir = normalize(vec3(0.5, 1.0, 0.3));

    // Normalize the interpolated normal (interpolation can denormalize it)
    vec3 normal = normalize(vNormal);

    // Diffuse lighting: how much the surface faces the light
    // dot product: 1.0 = facing light, 0.0 = perpendicular, -1.0 = facing away
    // max() clamps negative values to 0 (surfaces facing away get no light)
    float diffuse = max(dot(normal, lightDir), 0.0);

    // Ambient light: minimum light level so shadows aren't pure black
    float ambient = 0.3;

    // Combine ambient and diffuse
    float lighting = ambient + (1.0 - ambient) * diffuse;

    // Base color (green-ish for now, will be replaced with textures later)
    vec3 baseColor = vec3(0.4, 0.7, 0.3);

    // Final color = base color * lighting
    // Alpha = 1.0 (fully opaque)
    FragColor = vec4(baseColor * lighting, 1.0);
}
