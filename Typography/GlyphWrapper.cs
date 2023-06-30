
using Msdfgen;
using Typography.OpenFont;

namespace Typography;

/// <summary>
/// Represents a glyph from a loaded typeface. This is a wrapper around Typography.OpenFont.Glyph
/// </summary>
public class GlyphWrapper
{

    public readonly TypefaceWrapper Typeface;
    public readonly Glyph Glyph;

    public GlyphWrapper(TypefaceWrapper typeface, Glyph glyph)
    {
        Typeface = typeface;
        Glyph = glyph;
    }

    public MsdfgenResult RenderMSDF(float glyphSizePixels, double range = 5, int paddingPixels = 3)
    {

        (ushort[], GlyphPointF[])? info = Glyph.TtfWoffInfo;

        if (info == null)
            throw new TypographyException("Cannot convert this type of font to MSDF.");

        (ushort[] endPoints, GlyphPointF[] glyphPoints) = info!.Value;

        TrueTypeInterpreter interpreter = Typeface.GetInterpreter();
        GlyphPointF[] newGlyphPoints = interpreter.HintGlyph(Glyph.GlyphIndex, glyphSizePixels);

        MsdfgenShapeAssembler glyphShapeAssembler = new MsdfgenShapeAssembler();
        glyphShapeAssembler.Read(newGlyphPoints, endPoints, 1f);

        Shape glyphShape = glyphShapeAssembler.OutputShape;
        glyphShape.findBounds(out double left, out double bottom, out double right, out double top);

        Vector2 glyphTranslation = new Vector2(-left, -bottom);
        double glyphWidth = right + glyphTranslation.x;
        double glyphHeight = top + glyphTranslation.y;

        Vector2 sdfTranslation = glyphTranslation + new Vector2(paddingPixels, paddingPixels);
        int sdfWidth = (int)(glyphWidth + paddingPixels * 2);
        int sdfHeight = (int)(glyphHeight + paddingPixels * 2);

        FloatRGBBmp bmp = new FloatRGBBmp(sdfWidth, sdfHeight);
        EdgeColoring.edgeColoringSimple(glyphShape, 3);
        MsdfGenerator.generateMSDF(bmp, glyphShape, range, new Vector2(1, 1), sdfTranslation, 1.00000001);

        return new MsdfgenResult(bmp, sdfTranslation);
    }

}