#version 330 core

// Input: texture coordinates, color, brightness from vertex shader
in vec2 texCoord;
in vec3 vertexColor;
in float brightness;

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

    // Output final color (no fog - like modern Minecraft)
    FragColor = litColor;
}
