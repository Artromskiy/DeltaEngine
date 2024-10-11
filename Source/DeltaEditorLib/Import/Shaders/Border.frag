#version 450

const int InsSet = 0;
const int ScnSet = 1;

layout(location = 0) in vec4 color;
layout(location = 1) in vec4 borderColor;
layout(location = 2) in flat int ins;

layout(location = 0) out vec4 outColor;

struct Border
{
    vec4 minMax;
    vec4 cornerRadius;
    vec4 borderThickness;
    uvec4 colorsRgba;
    uvec4 borderColorsRgba;
};

struct SceneData
{
    mat4 projView;
    mat4 proj;
    mat4 view;
    vec4 position;
    vec4 rotation;
    vec4 windowSize;
};


layout (set = InsSet, binding = 0) buffer InsTRS { Border insBorder[ ]; };
layout (set = InsSet, binding = 1) buffer InsIds { int insIds[ ]; };
layout (set = ScnSet, binding = 0) buffer ScnCam { SceneData scene; };

int TrsId() { return insIds[ins]; }
Border Model() { return insBorder[TrsId()]; }

void main()
{
    vec2 norm = gl_FragCoord.xy / scene.windowSize.xy;
    outColor = vec4(norm, 1, 1);
}