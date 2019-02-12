// includes vars and functions for dof

float focalLength;
    // capture.GetAdjustedDistance());
float4 dofParams;
    // x: 1.0 / (dofparams.x - dofparams.y),
    // y: dofparams.y,
    // z: dofparams.z,
    // w: 1.0 / (dofparams.w - dofparams.z)

float linearDepth(float depthSample)
{
    return 
        _ProjectionParams.y * depthSample /
        (depthSample * (_ProjectionParams.y - _ProjectionParams.z) + _ProjectionParams.z);
}

float depthDist(float depthSample)
{
    return
        _ProjectionParams.y + depthSample * 
        (_ProjectionParams.z - _ProjectionParams.y);
}