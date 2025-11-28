#version 300 es

// Fragment shaders REQUIRE precision qualifier in WebGL
precision highp float;

in vec3 vNormal;
in vec2 vTexCoord;

out vec4 FragColor;

void main()
{
    vec3 lightDir = normalize(vec3(0.5, 1.0, 0.3));
    vec3 normal = normalize(vNormal);
    float diffuse = max(dot(normal, lightDir), 0.0);
    float ambient = 0.3;
    float lighting = ambient + (1.0 - ambient) * diffuse;

    vec3 baseColor = vec3(0.4, 0.7, 0.3);
    FragColor = vec4(baseColor * lighting, 1.0);
}
