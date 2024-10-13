#version 450

#include "SDF.glsl"

layout(location = 0) in flat int instanceIndex;
layout(location = 1) in vec4 color;
layout(location = 2) in vec4 borderColor;
layout(location = 3) in vec2 uv;

layout(location = 0) out vec4 outColor;

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

const float edgeSoftness = 0.0f;
const float borderSoftness = 0.0f;

void main()
{
    vec2 fragCoord = gl_FragCoord.xy;
    fragCoord.y = scene.windowSize.y - fragCoord.y;

    // should be done in compute, so all accesses with gl_InstanceIndex are valid
    int id = insIds[instanceIndex];

    // This should be filled with compute in predraw stage
    vec4 pixelMinMax = (insBorder[id].minMax + 1.0f) / 2.0f * vec4(scene.windowSize.xyxy);
    vec2 halfSize = (pixelMinMax.zw - pixelMinMax.xy) / 2.0f;
    vec2 pixelCenter = pixelMinMax.xy + halfSize;

    float borderThickness = insBorder[id].borderThickness.x;
    float sdf = roundedBoxSDF(fragCoord - pixelCenter, halfSize, insBorder[id].cornerRadius);
    
    float smoothedAlpha =  1.0f - smoothstep(0.0f, edgeSoftness, sdf);
    float borderAlpha = 1.0f - smoothstep(borderThickness - borderSoftness, borderThickness, abs(sdf));

    vec4 rectColor = vec4(color.rgb, 0);
    rectColor = mix(rectColor, color, min(color.a, smoothedAlpha));
    rectColor = mix(rectColor, borderColor, min(borderColor.a, min(borderAlpha, smoothedAlpha)));
    outColor = rectColor;
}