
using Typography.OpenFont;

namespace Typography;

/// <summary>
/// Represents a loaded typeface. This is a wrapper around Typography.OpenFont.Typeface
/// </summary>
public class TypefaceWrapper
{

    public static TypefaceWrapper Load(Stream stream, int streamStartOffset = 0, ReadFlags readFlags = ReadFlags.Full)
    {
        Typeface? typeface = new OpenFontReader().Read(stream, streamStartOffset, readFlags);
        if (typeface == null)
            throw new TypographyException("Error load font from stream. That's all we know.");
        return new TypefaceWrapper(typeface);
    }

    public readonly Typeface Typeface;
    private TrueTypeInterpreter? _trueTypeInterpereter;

    public TypefaceWrapper(Typeface typeface)
    {
        Typeface = typeface;
    }

    public GlyphWrapper GetGlyph(int glyphIndex)
    {
        return new GlyphWrapper(this, Typeface.GetGlyph(Typeface.GetGlyphIndex(glyphIndex)));
    }

    public GlyphWrapper GetGlyph(char glyphChar)
    {
        return GetGlyph((int)glyphChar);
    }

    internal TrueTypeInterpreter GetInterpreter()
    {
        if (_trueTypeInterpereter == null)
            _trueTypeInterpereter = new TrueTypeInterpreter(Typeface);
        return _trueTypeInterpereter;
    }
}