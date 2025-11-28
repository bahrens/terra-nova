#version 330 core

// === INPUTS (from your vertex buffer) ===
// These match ChunkMeshData's interleaved format
// "layout(location = X)" tells OpenGL which attribute slot to use
layout(location = 0) in vec3 aPosition;  // 3 floats: x, y, z
layout(location = 1) in vec3 aNormal;    // 3 floats: nx, ny, nz (surface direction)
layout(location = 2) in vec2 aTexCoord;  // 2 floats: u, v (texture coordinates)

// === OUTPUTS (passed to fragment shader) ===
// "out" variables are interpolated across the triangle
out vec3 vNormal;
out vec2 vTexCoord;

// === UNIFORMS (constant for all vertices in a draw call) ===
// These are set from C# before drawing
uniform mat4 uModel;       // Object space -> World space (position/rotation/scale of the object)
uniform mat4 uView;        // World space -> Camera space (where the camera is looking)
uniform mat4 uProjection;  // Camera space -> Clip space (perspective or orthographic)

void main()
{
    // gl_Position is a built-in output - the final 2D screen position
    // We multiply position through all 3 matrices: Model -> View -> Projection
    // Order matters! Matrix multiplication is right-to-left
    gl_Position = uProjection * uView * uModel * vec4(aPosition, 1.0);

    // Transform normal to world space
    // We use mat3(uModel) to ignore translation (normals are directions, not positions)
    // Note: This is simplified. For non-uniform scaling, you'd need the inverse transpose
    vNormal = mat3(uModel) * aNormal;

    // Pass texture coordinates through unchanged
    vTexCoord = aTexCoord;
}
