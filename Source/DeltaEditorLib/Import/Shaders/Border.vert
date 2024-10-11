#version 450

const int InsSet = 0;
const int ScnSet = 1;

#define ins gl_InstanceIndex
#define vert gl_VertexIndex

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

layout(location = 0) out vec4 color;
layout(location = 1) out vec4 borderColor;
layout(location = 2) out flat int instanceId;

int TrsId() { return insIds[ins]; }
Border Model() { return insBorder[TrsId()]; }

vec4 uintToRGBA(uint color)
{
    float r = float((color >> 24) & 0xFF) / 255.0;
    float g = float((color >> 16) & 0xFF) / 255.0;
    float b = float((color >> 8) & 0xFF) / 255.0;
    float a = float(color & 0xFF) / 255.0;
    return vec4(r, g, b, a);
}

vec4 Pos()
{    
    vec4 minMax = Model().minMax;
    uint xId = (vert / 2) * 2;
    uint yId = ((vert + 1) % 4) / 2 * 2 + 1;
    vec2 pos = vec2(minMax[xId], minMax[yId]);
    return vec4(pos, 0.0f, 1.0f);
}

vec4 Color()
{
    uint uColor = Model().colorsRgba[vert];
    return uintToRGBA(uColor);
}

vec4 BorderColor()
{
    uint uColor = Model().borderColorsRgba[vert];
    return uintToRGBA(uColor);
}

void main()
{
    uint id = TrsId();
    vec4 pos = Pos();
    gl_Position = pos;
    color = Color();
    borderColor = BorderColor();
}