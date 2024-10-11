#version 450

layout(location = 0) in vec3 Pos3;
layout(location = 1) in vec2 Pos2;
layout(location = 2) in vec4 Color;
layout(location = 3) in vec2 Tex;
layout(location = 4) in vec3 Norm;
layout(location = 5) in vec3 Tan;
layout(location = 6) in vec3 Binorm;
layout(location = 7) in vec3 Bitan;

const int InsSet = 0;
const int ScnSet = 2;

#define ins gl_InstanceIndex

struct Camera
{
    mat4 projView;

    mat4 proj;
    mat4 view;

    vec4 position;
    vec4 rotation;
};

layout (set = InsSet, binding = 0) buffer InsTRS { mat4 insTRS[ ]; };
layout (set = InsSet, binding = 1) buffer InsIds { int insIds[ ]; };
layout (set = ScnSet, binding = 0) buffer ScnCam { Camera camera; };

int TrsId() { return insIds[ins]; }
mat4 Model() { return insTRS[TrsId()]; }

layout(location = 0) out vec4 fragColor;

void main()
{
    uint id = TrsId();
    mat4 model = insTRS[id];
    gl_Position = camera.proj * camera.view * model * vec4(Pos2, 0.0, 1.0);
    fragColor = Color;
}