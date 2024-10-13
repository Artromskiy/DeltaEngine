

float roundedBoxSDF(vec2 centerPos, vec2 halfSize, vec4 radius4)
{
    radius4.xy = (centerPos.x > 0.0) ? radius4.wz : radius4.xy;
    float radius = (centerPos.y > 0.0) ? radius4.y : radius4.x;
    radius = clamp(radius, 0, min(halfSize.x, halfSize.y));

    vec2 q = abs(centerPos) - halfSize + radius;
    return min(max(q.x, q.y), 0.0f) + length(max(q, 0.0f)) - radius;
}

float roundedBoxSDF(vec2 centerPos, vec2 halfSize, float radius)
{
    vec2 q = abs(centerPos) - halfSize + radius;
    return min(max(q.x, q.y), 0.0f) + length(max(q, 0.0f)) - radius;
}