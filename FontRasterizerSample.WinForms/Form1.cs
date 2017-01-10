﻿//MIT, 2016,  WinterDev
using System;
using System.Collections.Generic;

using System.Drawing;
using System.IO;
using System.Windows.Forms;

using NOpenType;
using NOpenType.Extensions;

using PixelFarm.Agg;
using PixelFarm.Agg.VertexSource;

namespace SampleWinForms
{
    public partial class Form1 : Form
    {
        Graphics g;
        AggCanvasPainter p;
        ImageGraphics2D imgGfx2d;
        ActualImage destImg;
        Bitmap winBmp;
        static CurveFlattener curveFlattener = new CurveFlattener();

        public Form1()
        {
            InitializeComponent();
            this.Load += new EventHandler(Form1_Load);

            cmbRenderChoices.Items.Add(RenderChoice.RenderWithMiniAgg);
            cmbRenderChoices.Items.Add(RenderChoice.RenderWithPlugableGlyphRasterizer);
            cmbRenderChoices.Items.Add(RenderChoice.RenderWithTextPrinterAndMiniAgg);
            cmbRenderChoices.SelectedIndex = 0;
            cmbRenderChoices.SelectedIndexChanged += new EventHandler(cmbRenderChoices_SelectedIndexChanged);

            this.txtInputChar.Text = "B";

            lstFontSizes.Items.AddRange(
                new object[]{
                    8, 9,
                    10,11,
                    12,
                    14,
                    16,
                    18,20,22,24,26,28,36,48,72,240,300
                });
        }


        enum RenderChoice
        {
            RenderWithMiniAgg,
            RenderWithPlugableGlyphRasterizer, //new 
            RenderWithTextPrinterAndMiniAgg, //new
        }

        void Form1_Load(object sender, EventArgs e)
        {
            this.Text = "Render with PixelFarm";
            this.lstFontSizes.SelectedIndex = lstFontSizes.Items.Count - 1;//select last one 

        }

        private void button1_Click(object sender, EventArgs e)
        {

            if (g == null)
            {
                destImg = new ActualImage(400, 300, PixelFormat.ARGB32);
                imgGfx2d = new ImageGraphics2D(destImg); //no platform
                p = new AggCanvasPainter(imgGfx2d);
                winBmp = new Bitmap(400, 300, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                g = this.CreateGraphics();
            }
            //ReadAndRender(@"..\..\segoeui.ttf");
            //ReadAndRender(@"..\..\tahoma.ttf");
            ReadAndRender(@"..\..\cambriaz.ttf");
            //ReadAndRender(@"..\..\CompositeMS2.ttf");
        }

        float fontSizeInPoint = 14; //default
        void ReadAndRender(string fontfile)
        {
            if (string.IsNullOrEmpty(this.txtInputChar.Text))
            {
                return;
            }
            var reader = new OpenTypeReader();
            char testChar = txtInputChar.Text[0];//only 1 char 
            int resolution = 96;

            using (var fs = new FileStream(fontfile, FileMode.Open))
            {
                //1. read typeface from font file
                Typeface typeFace = reader.Read(fs);

#if DEBUG
                //-----
                //about typeface 
                //short ascender = typeFace.Ascender;
                //short descender = typeFace.Descender;
                //short lineGap = typeFace.LineGap;

                //NOpenType.Tables.UnicodeLangBits test = NOpenType.Tables.UnicodeLangBits.Thai;
                //NOpenType.Tables.UnicodeRangeInfo rangeInfo = test.ToUnicodeRangeInfo();
                //bool doseSupport = typeFace.DoseSupportUnicode(test); 
                ////-----
                ////string inputstr = "ก่นกิ่น";
                //string inputstr = "ญญู";
                //List<int> outputGlyphIndice = new List<int>();
                //typeFace.Lookup(inputstr.ToCharArray(), outputGlyphIndice);
#endif

                RenderChoice renderChoice = (RenderChoice)this.cmbRenderChoices.SelectedItem;
                switch (renderChoice)
                {
                    case RenderChoice.RenderWithMiniAgg:
                        RenderWithMiniAgg(typeFace, testChar, fontSizeInPoint);
                        break;

                    case RenderChoice.RenderWithPlugableGlyphRasterizer:
                        RenderWithPlugableGlyphRasterizer(typeFace, testChar, fontSizeInPoint, resolution);
                        break;
                    case RenderChoice.RenderWithTextPrinterAndMiniAgg:
                        RenderWithTextPrinterAndMiniAgg(typeFace, this.txtInputChar.Text, fontSizeInPoint, resolution);
                        break;
                    default:
                        throw new NotSupportedException();

                }
            }
        }


        static int s_POINTS_PER_INCH = 72; //default value, 
        static int s_PIXELS_PER_INCH = 96; //default value
        public static float ConvEmSizeInPointsToPixels(float emsizeInPoint)
        {
            return (int)(((float)emsizeInPoint / (float)s_POINTS_PER_INCH) * (float)s_PIXELS_PER_INCH);
        }

        //-------------------
        //https://www.microsoft.com/typography/otspec/TTCH01.htm
        //Converting FUnits to pixels
        //Values in the em square are converted to values in the pixel coordinate system by multiplying them by a scale. This scale is:
        //pointSize * resolution / ( 72 points per inch * units_per_em )
        //where pointSize is the size at which the glyph is to be displayed, and resolution is the resolution of the output device.
        //The 72 in the denominator reflects the number of points per inch.
        //For example, assume that a glyph feature is 550 FUnits in length on a 72 dpi screen at 18 point. 
        //There are 2048 units per em. The following calculation reveals that the feature is 4.83 pixels long.
        //550 * 18 * 72 / ( 72 * 2048 ) = 4.83
        //-------------------
        public static float ConvFUnitToPixels(ushort reqFUnit, float fontSizeInPoint, ushort unitPerEm)
        {
            //reqFUnit * scale             
            return reqFUnit * GetFUnitToPixelsScale(fontSizeInPoint, unitPerEm);
        }
        public static float GetFUnitToPixelsScale(float fontSizeInPoint, ushort unitPerEm)
        {
            //reqFUnit * scale             
            return ((fontSizeInPoint * s_PIXELS_PER_INCH) / (s_POINTS_PER_INCH * unitPerEm));
        }

        //from http://www.w3schools.com/tags/ref_pxtoemconversion.asp
        //set default
        // 16px = 1 em
        //-------------------
        //1. conv font design unit to em
        // em = designUnit / unit_per_Em       
        //2. conv font design unit to pixels


        // float scale = (float)(size * resolution) / (pointsPerInch * _typeface.UnitsPerEm);



        void RenderWithMiniAgg(Typeface typeface, char testChar, float sizeInPoint)
        {
            //2. glyph-to-vxs builder
            var builder = new GlyphPathBuilderVxs(typeface);
            builder.UseTrueTypeInterpreter = this.chkTrueTypeHint.Checked;
            builder.Build(testChar, sizeInPoint);
            VertexStore vxs = builder.GetVxs();

            //5. use PixelFarm's Agg to render to bitmap...
            //5.1 clear background
            p.Clear(PixelFarm.Drawing.Color.White);

            if (chkFillBackground.Checked)
            {
                //5.2 
                p.FillColor = PixelFarm.Drawing.Color.Black;
                //5.3
                p.Fill(vxs);
            }
            if (chkBorder.Checked)
            {
                //5.4 
                p.StrokeColor = PixelFarm.Drawing.Color.Green;
                //user can specific border width here...
                //p.StrokeWidth = 2;
                //5.5 
                p.Draw(vxs);
            }
            if (chkShowControlPoints.Checked)
            {
                //draw for debug ...
                //draw control point
                List<GlyphContour> contours = builder.GetContours();
                TessWithPolyTriAndDraw(contours, p);

                //int j = contours.Count;
                //for (int i = 0; i < j; ++i)
                //{
                //    GlyphContour cnt = contours[i];
                //    DrawGlyphContour(cnt, p);

                //    //for debug
                //    if (chkShowTess.Checked)
                //    {
                //        //TessContourAndDraw(cnt, p);
                //        if (i == 0)
                //        {
                //            TessWithPolyTriAndDraw(cnt, p);
                //        }
                //    }
                //}
            }

            //6. use this util to copy image from Agg actual image to System.Drawing.Bitmap
            PixelFarm.Agg.Imaging.BitmapHelper.CopyToGdiPlusBitmapSameSize(destImg, winBmp);
            //--------------- 
            //7. just render our bitmap
            g.Clear(Color.White);
            g.DrawImage(winBmp, new Point(30, 20));
        }
        void TessWithPolyTriAndDraw(List<GlyphContour> contours, AggCanvasPainter p)
        {


            List<Poly2Tri.PolygonPoint> points = new List<Poly2Tri.PolygonPoint>();
            int cntCount = contours.Count;

            Poly2Tri.Polygon polygon = CreatePolygon(contours[0]);//first contour            
            if (cntCount > 0)
            {
                //debug only
                for (int n = 1; n < cntCount; ++n)
                {
                    polygon.AddHole(CreatePolygon(contours[n]));
                }
            }

            Poly2Tri.P2T.Triangulate(polygon); //that poly is triangulated
            p.StrokeColor = PixelFarm.Drawing.Color.Magenta;
            p.FillColor = PixelFarm.Drawing.Color.Yellow;

            foreach (var tri in polygon.Triangles)
            {
                //draw each triangles
                p.Line(tri.P0.X, tri.P0.Y, tri.P1.X, tri.P1.Y);
                p.Line(tri.P1.X, tri.P1.Y, tri.P2.X, tri.P2.Y);
                p.Line(tri.P2.X, tri.P2.Y, tri.P1.X, tri.P1.Y);

                //find center of each triangle

                var p_centerx = tri.P0.X + tri.P1.X + tri.P2.X;
                var p_centery = tri.P0.Y + tri.P1.Y + tri.P2.Y;
                
                p.FillRectLBWH(p_centerx / 3, p_centery / 3, 1, 1);
            }
        }

        struct TmpPoint
        {
            public double x;
            public double y;
        }
        static Poly2Tri.Polygon CreatePolygon(GlyphContour cnt)
        {
            List<Poly2Tri.PolygonPoint> points = new List<Poly2Tri.PolygonPoint>();
            List<float> allPoints = cnt.allPoints;
            int lim = allPoints.Count - 1;

            //limitation: poly tri not accept duplicated points!
            double prevX = 0;
            double prevY = 0;

            Dictionary<TmpPoint, bool> tmpPoints = new Dictionary<TmpPoint, bool>();
            for (int i = 0; i < lim;)
            {
                var x = allPoints[i];
                var y = allPoints[i + 1];
                if (x != prevX && y != prevY)
                {
                    TmpPoint tmp_point = new TmpPoint();
                    tmp_point.x = x;
                    tmp_point.y = y;
                    if (!tmpPoints.ContainsKey(tmp_point))
                    {
                        tmpPoints.Add(tmp_point, true);
                        points.Add(new Poly2Tri.PolygonPoint(
                            x,
                            y));
                    }
                    prevX = x;
                    prevY = y;

                }
                i += 2;
            }

            Poly2Tri.Polygon polygon = new Poly2Tri.Polygon(points.ToArray());
            return polygon;
        }
        void TessContourAndDraw(GlyphContour cnt, AggCanvasPainter p)
        {
            cnt.Tess();
            var tessVertices = cnt.tessVertices;
            int vtxCount = tessVertices.Count;
            p.StrokeColor = PixelFarm.Drawing.Color.Magenta;
            for (int n = 1; n < vtxCount; ++n)
            {
                //var p0 = tessVertices[n - 2];
                var p0 = tessVertices[n - 1];
                var p1 = tessVertices[n];

                p.Line(p0.m_X, p0.m_Y, p1.m_X, p1.m_Y);
                //p.Line(p1.m_X, p1.m_Y, p2.m_X, p2.m_Y);
                //p.Line(p2.m_X, p2.m_Y, p0.m_X, p0.m_Y);

            }

        }
        void DrawGlyphContour(GlyphContour cnt, AggCanvasPainter p)
        {
            //for debug
            List<GlyphPart> parts = cnt.parts;
            int n = parts.Count;
            for (int i = 0; i < n; ++i)
            {
                GlyphPart part = parts[i];
                switch (part.Kind)
                {
                    default: throw new NotSupportedException();
                    case GlyphPartKind.Line:
                        {
                            GlyphLine line = (GlyphLine)part;
                            p.FillColor = PixelFarm.Drawing.Color.Red;
                            p.FillRectLBWH(line.x0, line.y0, 2, 2);
                            p.FillRectLBWH(line.x1, line.y1, 2, 2);
                        }
                        break;
                    case GlyphPartKind.Curve3:
                        {
                            GlyphCurve3 c = (GlyphCurve3)part;
                            p.FillColor = PixelFarm.Drawing.Color.Red;
                            p.FillRectLBWH(c.x0, c.y0, 2, 2);
                            p.FillColor = PixelFarm.Drawing.Color.Blue;
                            p.FillRectLBWH(c.p2x, c.p2y, 2, 2);
                            p.FillColor = PixelFarm.Drawing.Color.Red;
                            p.FillRectLBWH(c.x, c.y, 2, 2);
                        }
                        break;
                    case GlyphPartKind.Curve4:
                        {
                            GlyphCurve4 c = (GlyphCurve4)part;
                            p.FillColor = PixelFarm.Drawing.Color.Red;
                            p.FillRectLBWH(c.x0, c.y0, 2, 2);
                            p.FillColor = PixelFarm.Drawing.Color.Blue;
                            p.FillRectLBWH(c.p2x, c.p2y, 2, 2);
                            p.FillRectLBWH(c.p3x, c.p3y, 2, 2);
                            p.FillColor = PixelFarm.Drawing.Color.Red;
                            p.FillRectLBWH(c.x, c.y, 2, 2);
                        }
                        break;
                }
            }
        }

        void RenderWithPlugableGlyphRasterizer(Typeface typeface, char testChar, float sizeInPoint, int resolution)
        {
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            g.Clear(Color.White);
            ////credit:
            ////http://stackoverflow.com/questions/1485745/flip-coordinates-when-drawing-to-control
            g.ScaleTransform(1.0F, -1.0F);// Flip the Y-Axis 
            g.TranslateTransform(0.0F, -(float)300);// Translate the drawing area accordingly  

            //2. glyph to gdi path
            var gdiGlyphRasterizer = new NOpenType.CLI.GDIGlyphRasterizer();
            var builder = new GlyphPathBuilder(typeface, gdiGlyphRasterizer);
            builder.UseTrueTypeInterpreter = this.chkTrueTypeHint.Checked;
            builder.Build(testChar, sizeInPoint);


            if (chkFillBackground.Checked)
            {
                gdiGlyphRasterizer.Fill(g, Brushes.Black);
            }
            if (chkBorder.Checked)
            {
                gdiGlyphRasterizer.Draw(g, Pens.Green);
            }
            //transform back
            g.ScaleTransform(1.0F, -1.0F);// Flip the Y-Axis 
            g.TranslateTransform(0.0F, -(float)300);// Translate the drawing area accordingly            

        }
        void RenderWithTextPrinterAndMiniAgg(Typeface typeface, string str, float sizeInPoint, int resolution)
        {
            //1. 
            TextPrinter printer = new TextPrinter();
            printer.EnableKerning = this.chkKern.Checked;
            printer.EnableTrueTypeHint = this.chkTrueTypeHint.Checked;

            int len = str.Length;

            List<GlyphPlan> glyphPlanList = new List<GlyphPlan>(len);
            printer.Print(typeface, sizeInPoint, str, glyphPlanList);
            //--------------------------

            //5. use PixelFarm's Agg to render to bitmap...
            //5.1 clear background
            p.Clear(PixelFarm.Drawing.Color.White);
            //---------------------------
            //TODO: review here
            //fake subpixel rendering 
            //not correct
            //p.UseSubPixelRendering = true;
            //---------------------------
            if (chkFillBackground.Checked)
            {
                //5.2 
                p.FillColor = PixelFarm.Drawing.Color.Black;
                //5.3 
                int glyphListLen = glyphPlanList.Count;

                float ox = p.OriginX;
                float oy = p.OriginY;
                float cx = 0;
                float cy = 10;
                for (int i = 0; i < glyphListLen; ++i)
                {
                    GlyphPlan glyphPlan = glyphPlanList[i];
                    cx = glyphPlan.x;
                    p.SetOrigin(cx, cy);
                    p.Fill(glyphPlan.vxs);
                }
                p.SetOrigin(ox, oy);

            }
            if (chkBorder.Checked)
            {
                //5.4 
                p.StrokeColor = PixelFarm.Drawing.Color.Green;
                //user can specific border width here...
                //p.StrokeWidth = 2;
                //5.5 
                int glyphListLen = glyphPlanList.Count;
                float ox = p.OriginX;
                float oy = p.OriginY;
                float cx = 0;
                float cy = 10;
                for (int i = 0; i < glyphListLen; ++i)
                {
                    GlyphPlan glyphPlan = glyphPlanList[i];
                    cx = glyphPlan.x;
                    p.SetOrigin(cx, cy);
                    p.Draw(glyphPlan.vxs);
                }
                p.SetOrigin(ox, oy);
            }
            //6. use this util to copy image from Agg actual image to System.Drawing.Bitmap
            PixelFarm.Agg.Imaging.BitmapHelper.CopyToGdiPlusBitmapSameSize(destImg, winBmp);
            //--------------- 
            //7. just render our bitmap
            g.Clear(Color.White);
            g.DrawImage(winBmp, new Point(10, 0));
            //--------------------------

        }
        private void txtInputChar_TextChanged(object sender, EventArgs e)
        {
            button1_Click(this, EventArgs.Empty);
        }
        void cmbRenderChoices_SelectedIndexChanged(object sender, EventArgs e)
        {
            button1_Click(this, EventArgs.Empty);
        }

        private void lstFontSizes_SelectedIndexChanged(object sender, EventArgs e)
        {
            //new font size
            fontSizeInPoint = (int)lstFontSizes.SelectedItem;
            button1_Click(this, EventArgs.Empty);
        }

        private void chkKern_CheckedChanged(object sender, EventArgs e)
        {
            button1_Click(this, EventArgs.Empty);
        }

        private void chkTrueTypeHint_CheckedChanged(object sender, EventArgs e)
        {
            button1_Click(this, EventArgs.Empty);
        }

        private void chkShowTess_CheckedChanged(object sender, EventArgs e)
        {
            button1_Click(this, EventArgs.Empty);
        }
    }
}
