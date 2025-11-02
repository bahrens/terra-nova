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
    // Sample the texture
    vec4 texColor = texture(blockTexture, texCoord);

    // Define border thickness (in texture coordinate space, 0.0 to 1.0)
    float borderWidth = 0.009;
    float aaWidth = 0.004; // Antialiasing transition width

    // Calculate distance from edge
    float distFromEdgeX = min(texCoord.x, 1.0 - texCoord.x);
    float distFromEdgeY = min(texCoord.y, 1.0 - texCoord.y);
    float distFromEdge = min(distFromEdgeX, distFromEdgeY);

    // Smooth transition from border to texture
    float borderMix = smoothstep(borderWidth - aaWidth, borderWidth, distFromEdge);

    // Mix between border color (black) and textured vertex color
    vec4 borderColor = vec4(0.0, 0.0, 0.0, 1.0);
    vec4 blockColor = texColor * vec4(vertexColor, 1.0);
    FragColor = mix(borderColor, blockColor, borderMix);
}
