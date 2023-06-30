
using Msdfgen;
using Typography.OpenFont;

namespace Typography;

internal class MsdfgenShapeAssembler : IGlyphTranslator
{

    public readonly Shape OutputShape;

    private Contour? _currentContour;
    private float _contourStartX;
    private float _contourStartY;
    private float _curX;
    private float _curY;

    public MsdfgenShapeAssembler()
    {
        OutputShape = new Shape();
    }

    private Contour GetContour()
    {
        if (_currentContour == null)
            _currentContour = new Contour();
        return _currentContour;
    }

    public void BeginRead(int contourCount)
    {
    }

    public void MoveTo(float x0, float y0)
    {
        _curX = _contourStartX = x0;
        _curY = _contourStartY = y0;
    }

    public void LineTo(float x1, float y1)
    {
        GetContour().AddLine(_curX, _curY, x1, y1);
        _curX = x1;
        _curY = y1;
    }

    public void Curve3(float x1, float y1, float x2, float y2)
    {
        GetContour().AddQuadraticSegment(_curX, _curY, x1, y1, x2, y2);
        _curX = x2;
        _curY = y2;
    }

    public void Curve4(float x1, float y1, float x2, float y2, float x3, float y3)
    {
        GetContour().AddCubicSegment(_curX, _curY, x1, y1, x2, y2, x3, y3);
        _curX = x3;
        _curY = y3;
    }

    public void CloseContour()
    {
        if (_currentContour != null)
        {
            if (_curX != _contourStartX || _curY != _contourStartY)
                LineTo(_contourStartX, _contourStartY);
            OutputShape.contours.Add(_currentContour);
            _currentContour = null;
        }
    }

    public void EndRead() { }
}