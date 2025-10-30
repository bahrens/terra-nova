#version 330 core

// Input: color from vertex shader (interpolated across the triangle)
in vec3 vertexColor;

// Output: final pixel color
out vec4 FragColor;

void main()
{
    // Simply output the color (with full opacity)
    FragColor = vec4(vertexColor, 1.0);
}
