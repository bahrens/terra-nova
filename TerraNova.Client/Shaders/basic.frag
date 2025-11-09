#version 330 core

// Input: texture coordinates, color, brightness from vertex shader
in vec2 texCoord;
in vec3 vertexColor;
in float brightness;
in float fogDistance;  // Distance from camera

// Output: final pixel color
out vec4 FragColor;

// Texture sampler
uniform sampler2D blockTexture;

void main()
{
    // Sample grayscale noise texture and multiply by vertex color for pixelated look
    vec4 texColor = texture(blockTexture, texCoord);
    vec4 baseColor = texColor * vec4(vertexColor, 1.0);

    // Apply directional lighting by multiplying with brightness
    vec4 litColor = baseColor * brightness;

    // Minecraft-like fog (sky blue matching background)
    vec3 fogColor = vec3(0.529, 0.808, 0.922);  // Sky blue (0x87CEEB)
    float fogNear = 96.0;   // Start fog at 6 chunks (96 blocks)
    float fogFar = 200.0;   // Full fog at far plane

    // Calculate linear fog factor (0 = no fog, 1 = full fog)
    float fogFactor = clamp((fogDistance - fogNear) / (fogFar - fogNear), 0.0, 1.0);

    // Mix lit color with fog color
    FragColor = vec4(mix(litColor.rgb, fogColor, fogFactor), litColor.a);
}
