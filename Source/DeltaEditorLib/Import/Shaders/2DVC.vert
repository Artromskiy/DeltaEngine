#version 450

#include "VertexInput.glsl"

struct Camera
{
    mat4 projView;
    mat4 proj;
    mat4 view;
    vec4 position;
    vec4 rotation;
};

layout (set = 0, binding = 0) buffer InsTRS { mat4 insTRS[ ]; };
layout (set = 0, binding = 1) buffer InsIds { int insIds[ ]; };
layout (set = 2, binding = 0) buffer ScnCam { Camera camera; };

layout(location = 0) out vec4 fragColor;

void main()
{
    // should be done in compute, so all accesses with gl_InstanceIndex are valid
    int id = insIds[gl_InstanceIndex];
    mat4 model = insTRS[id];
    // camera.proj * camera.view * model should be done with compute in predraw stage
    gl_Position = camera.proj * camera.view * model * vec4(Pos2, 0.0, 1.0);
    fragColor = Color;
}