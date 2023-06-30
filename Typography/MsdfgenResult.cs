
using Msdfgen;

namespace Typography;

public readonly record struct MsdfgenResult(
    FloatRGBBmp Bmp,
    Vector2 Translation
);