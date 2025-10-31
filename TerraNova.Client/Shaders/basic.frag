#version 330 core

// Input: texture coordinates and color from vertex shader
in vec2 texCoord;
in vec3 vertexColor;

// Output: final pixel color
out vec4 FragColor;

// Texture sampler
uniform sampler2D blockTexture;

void main()
{
    // Sample the texture and multiply by vertex color (for lighting/tinting)
    vec4 texColor = texture(blockTexture, texCoord);
    FragColor = texColor * vec4(vertexColor, 1.0);
}
