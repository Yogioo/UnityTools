#ifndef SAMPLE_CULLING
#define SAMPLE_CULLING

bool IsOutsideThePlane(float4 plane, float3 pointPosition)
{
    if (dot(plane.xyz, pointPosition) + plane.w > 0)
        return true;
    return false;
}

void GetBounds(float3 center, float3 extens, out float3 BoundsVerts[8])
{
    float3 minBounds = BoundsVerts[0] = center - extens;
    float3 maxBounds = BoundsVerts[1] = center + extens;
    BoundsVerts[2] = float3(maxBounds.x, maxBounds.y, minBounds.z);
    BoundsVerts[3] = float3(maxBounds.x, minBounds.y, maxBounds.z);
    BoundsVerts[4] = float3(maxBounds.x, minBounds.y, minBounds.z);
    BoundsVerts[5] = float3(minBounds.x, maxBounds.y, maxBounds.z);
    BoundsVerts[6] = float3(minBounds.x, maxBounds.y, minBounds.z);
    BoundsVerts[7] = float3(minBounds.x, minBounds.y, maxBounds.z);
}

// Return is in Frustm
bool FrustmCull(float4 cameraPanels[6], float3 BoundsVerts[8])
{
    for (int i = 0; i < 6; i++)
    {
        for (int j = 0; j < 8; j++)
        {
            if (!IsOutsideThePlane(cameraPanels[i], BoundsVerts[j]))
            {
                break;
            }
            if (j == 7)
            {
                return false;
            }
        }
    }
    return true;
}


#endif