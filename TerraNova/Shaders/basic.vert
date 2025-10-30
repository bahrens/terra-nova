#version 330 core

// Input: vertex position, texture coordinates, and color from our C# code
layout (location = 0) in vec3 aPosition;  // Vertex position in 3D space
layout (location = 1) in vec2 aTexCoord;  // Texture coordinates (UV)
layout (location = 2) in vec3 aColor;     // Vertex color (for tinting/lighting)

// Output: data to pass to fragment shader
out vec2 texCoord;
out vec3 vertexColor;

// Uniforms: transformation matrices passed from C# code
uniform mat4 model;       // Model matrix: object space -> world space
uniform mat4 view;        // View matrix: world space -> camera space
uniform mat4 projection;  // Projection matrix: camera space -> screen space

void main()
{
    // Transform vertex position through all transformation matrices
    // This converts from 3D object coordinates to 2D screen coordinates
    gl_Position = projection * view * model * vec4(aPosition, 1.0);

    // Pass texture coordinates and color to the fragment shader
    texCoord = aTexCoord;
    vertexColor = aColor;
}
