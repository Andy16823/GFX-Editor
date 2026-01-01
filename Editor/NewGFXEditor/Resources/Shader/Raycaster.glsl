#version 450
layout(local_size_x = 1) in;

uniform vec3 rayOrigin;
uniform vec3 rayDir;

uniform mat4 invModelMatrix;

struct HitResult
{
    int triangleIndex;
    vec4 normal;
    vec4 position;
};

layout(std430, binding = 0) readonly buffer Positions
{
    vec3 positions[];
};

layout(std430, binding = 1) readonly buffer IndexBuffer
{
    int indices[];
};

layout(std430, binding = 2) writeonly buffer Result
{
    HitResult result;
};

bool intersectTriangle(vec3 ro, vec3 rd, vec3 v0, vec3 v1, vec3 v2, out float t, out vec3 normal)
{
    const float EPS = 1e-6;

    vec3 e1 = v1 - v0;
    vec3 e2 = v2 - v0;

    vec3 p = cross(rd, e2);
    float det = dot(e1, p);
    if (abs(det) < EPS) return false;

    float invDet = 1.0 / det;
    vec3 s = ro - v0;

    float u = dot(s, p) * invDet;
    if (u < 0.0 || u > 1.0) return false;

    vec3 q = cross(s, e1);
    float v = dot(rd, q) * invDet;
    if (v < 0.0 || u + v > 1.0) return false;

    t = dot(e2, q) * invDet;
    if(t <= EPS) return false;

    normal = normalize(cross(e1, e2));
    return true;
}

void main()
{
    float closestT = 1e20;
    result.triangleIndex = -1;

    vec3 ro = (invModelMatrix * vec4(rayOrigin, 1.0)).xyz;
    vec3 rd = normalize((invModelMatrix * vec4(rayDir, 0.0)).xyz);

    for (int i = 0; i < indices.length(); i += 3)
    {
        vec3 v0 = positions[indices[i]];
        vec3 v1 = positions[indices[i + 1]];
        vec3 v2 = positions[indices[i + 2]];

        float t;
        vec3 normal;
        if (intersectTriangle(ro, rd, v0, v1, v2, t, normal))
        {
            if (t < closestT)
            {
                closestT = t;
                result.triangleIndex = i / 3;
                result.normal = vec4(normal, t);
                result.position = vec4(ro + rd * t, 1.0);
            }
        }
    }
}
