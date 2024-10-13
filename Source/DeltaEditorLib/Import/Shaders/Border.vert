#version 450

#include "VertexInput.glsl"

struct Border
{
    vec4 minMax;
    vec4 uv;
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

layout (set = 0, binding = 0) buffer InsTRS { Border insBorder[ ]; };
layout (set = 0, binding = 1) buffer InsIds { int insIds[ ]; };
layout (set = 1, binding = 0) buffer ScnCam { SceneData scene; };

layout(location = 0) out flat int instanceIndex;
layout(location = 1) out vec4 color;
layout(location = 2) out vec4 borderColor;
layout(location = 3) out vec2 uv;

vec4 uintToRGBA(uint color)
{
    float r = float((color >> 0 ) & 0xFF);
    float g = float((color >> 8 ) & 0xFF);
    float b = float((color >> 16) & 0xFF);
    float a = float((color >> 24) & 0xFF);
    return vec4(r, g, b, a) / 255.0f;
}

vec4 PosUV()
{
    // should be done in compute, so all accesses with gl_InstanceIndex are valid
    int id = insIds[gl_InstanceIndex];
    vec4 minMaxPos = insBorder[id].minMax;
    vec4 minMaxUV = insBorder[id].uv;
    uint xId = (gl_VertexIndex / 2) * 2;
    uint yId = ((gl_VertexIndex + 1) % 4) / 2 * 2 + 1;
    return vec4(minMaxPos[xId], minMaxPos[yId], minMaxUV[xId], minMaxUV[yId]);
}

vec4 BackColor()
{
    // should be done in compute, so all accesses with gl_InstanceIndex are valid
    int id = insIds[gl_InstanceIndex];
    uint uColor = insBorder[id].colorsRgba[gl_VertexIndex];
    return uintToRGBA(uColor);
}

vec4 BorderColor()
{
    // should be done in compute, so all accesses with gl_InstanceIndex are valid
    int id = insIds[gl_InstanceIndex];
    uint uColor = insBorder[id].borderColorsRgba[gl_VertexIndex];
    return uintToRGBA(uColor);
}

void main()
{
    // should be done in compute, so all accesses with gl_InstanceIndex are valid
    uint id = insIds[gl_InstanceIndex];
    vec4 posUV = PosUV();
    gl_Position = vec4(posUV.xy, 0, 1);
    instanceIndex = gl_InstanceIndex;
    uv = posUV.zw;
    color = BackColor();
    borderColor = BorderColor();
}