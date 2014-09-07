////////////////////////////////////////////////////////////////
//                Created By Richard Blythe 2008
//   There are no licenses or warranty attached to this object.
//   If you distribute the code as part of your application, please
//   be courteous enough to mention the assistance provided you.
////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.IO;

namespace Painting.Shapes
{
    public class GDIGenerationTool
    {
        StringBuilder sbBeginCode;
        StringBuilder sbShapeCode1;
        StringBuilder sbEndCode;
        int colorEnumCount = 0;
        int boundsEnumCount = 0;
        int blendEnumCount = 0;
        int textEnumCount = 0;

        public Rectangle AreaBounds = Rectangle.Empty;
        private bool blnWriteScalePoints;
        private bool blnHasGradientBrush;
        private bool blnEditColor;
        private bool blnEditBlend;
        private bool blnEditBounds;
        private bool blnEditText;

        private ShapeManager manager;


        public GDIGenerationTool(ShapeManager shapeManager)
        {
            manager = shapeManager; //copy the memory reference
        }

        public string GenerateCode()
        {
            sbShapeCode1 = null;
            sbBeginCode = null;
            sbEndCode = null;
            if (manager.ShapeCount == 0)
            {
                sbBeginCode.Append("No shapes are available to generate GDI+ code.");
            }
            else
            {
                //allocate 1500 characters of code per shape
                sbShapeCode1 = new StringBuilder(manager.ShapeCount * 1500);
                sbBeginCode = new StringBuilder(1800);
                sbEndCode = new StringBuilder(2400);

                blnHasGradientBrush = false;
                blnEditBlend = false;
                blnEditBounds = false;
                blnEditColor = false;
                blnEditText = false;

                colorEnumCount = 0;
                boundsEnumCount = 0;
                blendEnumCount = 0;
                textEnumCount = 0;

                blnWriteScalePoints = false;


                if (!manager.ClipRectIsOn)
                {
                    Rectangle rectArea = manager.GetTotalShapeArea();
                    AreaBounds = rectArea;
                }
                else
                {
                    AreaBounds = manager.ClipBounds;
                }


                //writes all the GDI+ code and sets some important helper
                //flags in code generation. Look inside method fot more info
                writeShapeCode();

                writeInit();
                write_Constructor();
                start_Method();
                   
                end_Method();
                write_helperMethods();
                write_End();
                write_EndingText();
            }


            return sbBeginCode.ToString() + sbShapeCode1.ToString() + sbEndCode.ToString();
        }

        public void GenerateScalePoints()
        {
            blnWriteScalePoints = true;
        }

        private void writeInit()
        {
            sbBeginCode.Append(
                "//////////////////////////////////////////////////////////////////\r\n"+
                "//        Code generated by: GDI+ Generator\r\n"+
                "//         Developer: Richard Blythe (2008)\r\n"+
                "//      This code has no license or warranty\r\n"+
                "//////////////////////////////////////////////////////////////////\r\n\r\n"+
               "using System;\r\nusing System.Collections.Generic;\r\n"+
               "using System.Text;\r\nusing System.Drawing.Drawing2D;\r\n"+
               "using System.Drawing;\r\nnamespace MyCustomGraphics\r\n{\r\n"+
               "public class " + Path.GetFileNameWithoutExtension(manager.FileName) +
               " : IDisposable\r\n{\r\n");

            //----------------
            if (blnEditBlend || blnEditBounds || blnEditColor || blnEditText)
            {
                sbBeginCode.Append("#region Enums\r\n");

                if (blnEditColor)
                    write_ColorEnumeration();
                if (blnEditBounds)
                    write_BoundsEnumeration();
                if (blnEditBlend)
                    write_BlendEnumeration();
                if (blnEditText)
                    write_TextEnumeration();

                sbBeginCode.Append("//End Enums\r\n#endregion\r\n\r\n");
            }
            //-----------------

            if (!manager.ClipRectIsOn)
            {
                Rectangle rectArea = manager.GetTotalShapeArea();
                sbBeginCode.Append("Rectangle originalBounds = new Rectangle(" +
                     rectArea.Left + ", " + rectArea.Top + ", " +
                     rectArea.Width + ", " + rectArea.Height + ");\r\n");
                AreaBounds = rectArea;
            }
            else
            {
                sbBeginCode.Append("Rectangle originalBounds = new Rectangle(" +
                    manager.ClipBounds.X + ", " + manager.ClipBounds.Y + ", " +
                    manager.ClipBounds.Width + ", " + manager.ClipBounds.Height + ");");
                AreaBounds = manager.ClipBounds;
            }
            
            if (blnEditColor)
                sbBeginCode.Append(
                    "private Dictionary<eShapeColor, Color> dColors;\r\n");
            if (blnEditBounds)
                sbBeginCode.Append(
                    "private Dictionary<eShapeBounds, Rectangle> dBounds;\r\n");
            if (blnEditBlend)
                sbBeginCode.Append(
                     "private Dictionary<eShapeBlend, BlendArgs> dBlend;\r\n");
            if (blnEditText)
                sbBeginCode.Append(
                     "private Dictionary<eShapeText, string> dText;\r\n");

            sbBeginCode.Append(
                "private SizeF scale = SizeF.Empty;\r\n\r\n"+
                "private Size currentSize;\r\n"+
                "public Size CurrentSize\r\n{\r\n"+
                "get { return currentSize; }\r\n"+
                "set \r\n{\r\n currentSize = value;\r\nRefreshImage();\r\n}\r\n}\r\n"+
                
                "private bool antiAlias = true;\r\n"+
                "public bool AntiAlias\r\n{\r\n"+
                "get { return antiAlias; }\r\n"+
                "set { antiAlias = value; }\r\n}\r\n"+
                
                "private Bitmap bmpCanvas;\r\n"+
                "public Bitmap Image\r\n{\r\n"+
                "get \r\n{\r\n if (bmpCanvas == null) RefreshImage();\r\n"+
                "return bmpCanvas;\r\n }\r\n"+
                "}\r\n");
        }

        private void write_Constructor()
        {
            sbBeginCode.Append("public " + Path.GetFileNameWithoutExtension(manager.FileName) + "()\r\n{\r\n");

            if (blnEditColor)
                sbBeginCode.Append("fillColorDictionary(true);\r\n");
            if (blnEditBounds)
                sbBeginCode.Append("fillBoundsDictionary(true);\r\n");
            if (blnEditBlend)
                sbBeginCode.Append("fillBlendDictionary(true);\r\n");
            if (blnEditText)
                sbBeginCode.Append("fillTextDictionary(true);\r\n");

            sbBeginCode.Append("currentSize = originalBounds.Size;\r\n}\r\n");
        }

        public string FormatText(string text)
        {
            StringBuilder sb = new StringBuilder(text.Length+20);
            sb.Append("\"");

            int intCount = text.Length;
            for (int i = 0; i < intCount; i++)
            {
                if ((i+2 < intCount) && text.Substring(i, 2) == "\r\n")
                {
                    sb.Append("\\r\\n");
                    i += 2;
                }
                else
                    sb.Append(text[i]);
            }

            sb.Append("\"");
            return sb.ToString();
        }


        #region Enum Generation

        private void write_ColorEnumeration()
        {
            sbBeginCode.Append("public enum eShapeColor\r\n{\r\n");
            int intResult = 0;
            int intCount = manager.ShapeCount;
            bool isFirstEnum = true;
            for (short i = 0; i < intCount; i++)
            {
                Shape shape = manager.GetShape(i);
                if (shape.GetShapeBounds(true).IntersectsWith(AreaBounds))
                {
                    if ((bool)shape.GetGDIValue(sGDIProperty.EditColor))
                    {
                        if (shape.painter.PaintFill)
                        {
                            if (shape.painter.ColorCount == 1)
                            {
                                colorEnumCount++;
                                if (isFirstEnum) isFirstEnum = false; else sbBeginCode.Append(", ");

                                sbBeginCode.Append(shape.Name + "FillColor");
                                Math.DivRem(colorEnumCount, 4, out intResult);
                                if (intResult == 0) sbBeginCode.Append("\r\n");
                            }
                            else
                            {
                                colorEnumCount++;
                                if (isFirstEnum) isFirstEnum = false; else sbBeginCode.Append(", ");

                                sbBeginCode.Append(shape.Name + "FillColor1");
                                Math.DivRem(colorEnumCount, 4, out intResult);
                                if (intResult == 0) sbBeginCode.Append("\r\n");

                                colorEnumCount++;
                                sbBeginCode.Append(", " + shape.Name + "FillColor2");
                                Math.DivRem(colorEnumCount, 4, out intResult);
                                if (intResult == 0) sbBeginCode.Append("\r\n");
                            }
                        }

                        if (shape.painter.PaintBorder)
                        {
                            colorEnumCount++;
                            if (isFirstEnum) isFirstEnum = false; else sbBeginCode.Append(", ");

                            sbBeginCode.Append(shape.Name + "BorderColor");

                            Math.DivRem(colorEnumCount, 4, out intResult);
                            if (intResult == 0) sbBeginCode.Append("\r\n");
                        }
                    }
                }
            }
            sbBeginCode.Append("}\r\n");

        }

        private void write_BoundsEnumeration()
        {
            sbBeginCode.Append("public enum eShapeBounds\r\n{\r\n");
            int intResult = 0;
            int intCount = manager.ShapeCount;
            bool isFirstEnum = true;
            for (short i = 0; i < intCount; i++)
            {
                Shape shape = manager.GetShape(i);
                if (shape.GetShapeBounds(true).IntersectsWith(AreaBounds))
                {
                    if ((bool)shape.GetGDIValue(sGDIProperty.EditBounds))
                    {
                        if (isFirstEnum) isFirstEnum = false; else sbBeginCode.Append(", ");

                        boundsEnumCount++;
                        sbBeginCode.Append(shape.Name);
                        Math.DivRem(boundsEnumCount, 4, out intResult);
                        if (intResult == 0) sbBeginCode.Append("\r\n");
                    }
                }
            }
            sbBeginCode.Append("}\r\n");

        }

        private void write_BlendEnumeration()
        {
            sbBeginCode.Append("public enum eShapeBlend\r\n{\r\n");
            int intResult = 0;
            int intCount = manager.ShapeCount;
            bool isFirstEnum = true;
            for (short i = 0; i < intCount; i++)
            {
                Shape shape = manager.GetShape(i);
                if (shape.GetShapeBounds(true).IntersectsWith(AreaBounds))
                {
                    if ((bool)shape.GetGDIValue(sGDIProperty.EditBounds))
                    {
                        if (isFirstEnum) isFirstEnum = false; else sbBeginCode.Append(", ");

                        blendEnumCount++;
                        sbBeginCode.Append(shape.Name);
                        Math.DivRem(blendEnumCount, 4, out intResult);
                        if (intResult == 0) sbBeginCode.Append("\r\n");
                    }
                }
            }
            sbBeginCode.Append("}\r\n\r\n");

        }

        private void write_TextEnumeration()
        {
            sbBeginCode.Append("public enum eShapeText\r\n{\r\n");
            int intResult = 0;
            int intCount = manager.ShapeCount;
            bool isFirstEnum = true;
            for (short i = 0; i < intCount; i++)
            {
                Shape shape = manager.GetShape(i);
                
                if (shape is ShapeText &&
                    shape.GetShapeBounds(true).IntersectsWith(AreaBounds))
                {
                    if ((bool)shape.GetGDIValue("Edit Text"))
                    {
                        if (isFirstEnum) isFirstEnum = false; else sbBeginCode.Append(", ");

                        blendEnumCount++;
                        sbBeginCode.Append(shape.Name);
                        Math.DivRem(blendEnumCount, 4, out intResult);
                        if (intResult == 0) sbBeginCode.Append("\r\n");
                    }
                }
            }
            sbBeginCode.Append("}\r\n\r\n");

        }

        #endregion

        #region Dictionary Generation

        private void write_ColorDictionary()
        {
            sbEndCode.Append(
                "private void fillColorDictionary(bool isInit)\r\n{\r\n" +
                "if (isInit)\r\n"+
                "   dColors = new Dictionary<eShapeColor, Color>(" + colorEnumCount + ");\r\n"+
                "dColors.Clear();\r\n");
            for (short i = 0; i < manager.ShapeCount; i++)
            {
                Shape shape = manager.GetShape(i);
                if (shape.GetShapeBounds(true).IntersectsWith(AreaBounds))
                {
                    if ((bool)shape.GetGDIValue(sGDIProperty.EditColor))
                    {
                        if (shape.painter.PaintFill)
                        {
                            if (shape.painter.ColorCount == 1)
                            {
                                Color col1 = shape.painter.GetColor(0);
                                sbEndCode.Append("dColors.Add(eShapeColor." + shape.Name +
                                    "FillColor, Color.FromArgb(" + col1.A + ", " + col1.R + ", " +
                                                                     col1.G + ", " + col1.B + "));\r\n");
                            }
                            else
                            {
                                Color col1 = shape.painter.GetColor(0);
                                Color col2 = shape.painter.GetColor(1);

                                sbEndCode.Append("dColors.Add(eShapeColor." + shape.Name +
                                    "FillColor1, Color.FromArgb(" + col1.A + ", " + col1.R + ", " +
                                                                     col1.G + ", " + col1.B + "));\r\n" +

                                "dColors.Add(eShapeColor." + shape.Name +
                                    "FillColor2, Color.FromArgb(" + col2.A + ", " + col2.R + ", " +
                                                                     col2.G + ", " + col2.B + "));\r\n");
                                blnHasGradientBrush = true;
                            }
                        }

                        if (shape.painter.PaintBorder)
                        {
                            Color col1 = shape.painter.BorderColor;
                            sbEndCode.Append("dColors.Add(eShapeColor." + shape.Name +
                                "BorderColor, Color.FromArgb(" + col1.A + ", " + col1.R + ", " +
                                                                 col1.G + ", " + col1.B + "));\r\n");
                        }
                    }
                }
            }
            sbEndCode.Append("\r\n}\r\n\r\n");
        }

        private void write_BoundsDictionary()
        {
            sbEndCode.Append(
                "private void fillBoundsDictionary(bool isInit)\r\n{\r\n" +
                "if (isInit)\r\n" +
                "dBounds = new Dictionary<eShapeBounds, Rectangle>(" + boundsEnumCount + ");\r\n" +
                "dBounds.Clear();\r\n");

            for (short i = 0; i < manager.ShapeCount; i++)
            {
                Shape shape = manager.GetShape(i);
                if (shape.GetShapeBounds(true).IntersectsWith(AreaBounds))
                {
                    if ((bool)shape.GetGDIValue(sGDIProperty.EditBounds))
                    {
                        Rectangle r = shape.GetShapeBounds(true);
                        sbEndCode.Append("dBounds.Add(eShapeBounds." + shape.Name +
                            ", new Rectangle(" + (r.X - AreaBounds.X) + ", " +
                                               (r.Y - AreaBounds.Y) + ", " +
                                                r.Width + ", " + r.Height + "));\r\n");
                    }
                }
            }
            sbEndCode.Append("}\r\n\r\n");
        }

        private void write_BlendDictionary()
        {
            sbEndCode.Append(
                "private void fillBlendDictionary(bool isInit)\r\n{\r\n" +
                "if (isInit)\r\n" +
                "dBlend = new Dictionary<eShapeBlend, BlendArgs>(" + blendEnumCount + ");\r\n" +
                "dBlend.Clear();\r\n");

            for (short i = 0; i < manager.ShapeCount; i++)
            {
                Shape shape = manager.GetShape(i);
                if (shape.GetShapeBounds(true).IntersectsWith(AreaBounds))
                {
                    if ((bool)shape.GetGDIValue(sGDIProperty.EditBlend))
                    {
                        sbEndCode.Append("dBlend.Add(eShapeBlend." + shape.Name +
                            ", new BlendArgs(" + shape.painter.Coverage + ", " +
                                   shape.painter.BlendSmoothness + "));\r\n");
                    }
                }
            }
            sbEndCode.Append("}\r\n\r\n");
        }

        private void write_TextDictionary()
        {
            sbEndCode.Append(
                "private void fillTextDictionary(bool isInit)\r\n{\r\n" +
                "if (isInit)\r\n" +
                "dText = new Dictionary<eShapeText, string>(" + textEnumCount + ");\r\n" +
                "dText.Clear();\r\n");

            for (short i = 0; i < manager.ShapeCount; i++)
            {
                Shape shape = manager.GetShape(i);
                if (shape is ShapeText &&
                    shape.GetShapeBounds(true).IntersectsWith(AreaBounds))
                {
                    if ((bool)shape.GetGDIValue("Edit Text"))
                    {
                        sbEndCode.Append("dText.Add(eShapeText." + shape.Name +
                            ", " + FormatText(((ShapeText)shape).Text) + ");\r\n");
                    }
                }
            }
            sbEndCode.Append("}\r\n\r\n");
        }

        #endregion

        private void start_Method()
        {
            sbBeginCode.Append("\r\n\r\npublic void RefreshImage()\r\n{\r\n" +
            "//This rectangle will be used to store the bounds of a shape\r\n" +
            "RectangleF shapeBoundsNew = RectangleF.Empty; \r\n" +
            "//This rectangle will be used to store the un-scaled bounds\r\n" +
            "RectangleF shapeBoundsOld = RectangleF.Empty; \r\n" +
            "scale.Width = ((float)currentSize.Width / (float)originalBounds.Width);\r\n"+
            "scale.Height = ((float)currentSize.Height / (float)originalBounds.Height);\r\n"+
            "bmpCanvas = new Bitmap(currentSize.Width, currentSize.Height);\r\n" +
            "Graphics g = Graphics.FromImage(bmpCanvas);\r\n" +
            "if (antiAlias) g.SmoothingMode = SmoothingMode.AntiAlias;"+
            "GraphicsPath path = null;\r\n\r\n");
        }

        private void writeShapeCode()
        {
            int count = manager.ShapeCount;
            if (manager.ClipRectIsOn) count--;
            for (short i = 0; i < count; i++)
            {
                if (manager.GetShape(i).GetShapeBounds(true).IntersectsWith(AreaBounds))
                {
                    sbShapeCode1.Append(manager.GetShape(i).EmitGDICode("g", this));

                    if (manager.GetShape(i).HasNodes)
                        blnWriteScalePoints = true;
                    if (manager.GetShape(i).painter.ColorCount > 1)
                        blnHasGradientBrush = true;
                    if ((bool)manager.GetShape(i).GetGDIValue(sGDIProperty.EditBlend))
                        blnEditBlend = true;
                    if ((bool)manager.GetShape(i).GetGDIValue(sGDIProperty.EditColor))
                        blnEditColor = true;
                    if ((bool)manager.GetShape(i).GetGDIValue(sGDIProperty.EditBounds))
                        blnEditBounds = true;
                    if (manager.GetShape(i) is ShapeText &&
                        (bool)manager.GetShape(i).GetGDIValue(ShapeText.EDIT_TEXT))
                        blnEditText = true;
                }
            }

        }


        private void end_Method()
        {
            //write the cleanup code
            sbEndCode.Append("//Cleanup\r\n" +
                       "g.Dispose();\r\nif (path != null)\r\npath.Dispose();\r\n}\r\n\r\n");
        }

        private void write_helperMethods()
        {
            sbEndCode.Append("#region Helper Methods\r\n" +
                "//Click the minus button to the left of #region to "+
                "minimize the helper methods\r\n");
            
            writeScaleBoundsMethod();

            if (blnEditColor)
                write_ColorDictionary();
            if (blnEditBounds)
                write_BoundsDictionary();
            if (blnEditBlend)
                write_BlendDictionary();
            if (blnEditText)
                write_TextDictionary();

            if (blnWriteScalePoints) 
                writeScalePointsMethod();

            if (blnHasGradientBrush)
                writeBlendMethod();

            //----------------
            if (blnEditColor)
            {
                sbEndCode.Append("public void SetColor(eShapeColor eColor, Color color)\r\n" +
                  "{ dColors[eColor] = color;\r\n}\r\n\r\n" +

                  "public Color GetColor(eShapeColor eColor)\r\n{\r\n" +
                  "return dColors[eColor];\r\n}\r\n\r\n" +

                  "public void ResetColors()\r\n{\r\n fillColorDictionary(false);\r\n}\r\n\r\n");
            }

            if (blnEditBounds)
            {
                sbEndCode.Append("public void SetBounds(eShapeBounds eBounds, Rectangle rect)\r\n" +
                  "{ dBounds[eBounds] = rect;\r\n}\r\n\r\n" +
                  
                  "public Rectangle GetBounds(eShapeBounds eBounds, bool scaled)\r\n{\r\n" +
                  "if (scaled)\r\nreturn Rectangle.Round(GetScaledBounds(dBounds[eBounds]));\r\n\r\n"+
                  "return dBounds[eBounds];\r\n}\r\n\r\n"+

                  "public void ResetBounds()\r\n{\r\nfillBoundsDictionary(false);\r\n}\r\n\r\n");
            }

            if (blnEditBlend)
            {
                sbEndCode.Append("public void SetBlend(eShapeBlend eBlend, BlendArgs args)\r\n" +
                  "{ dBlend[eBlend] = args;\r\n}\r\n\r\n" +
                  
                  "public BlendArgs GetBlend(eShapeBlend eBlend)\r\n{\r\n" +
                  "return dBlend[eBlend];\r\n}\r\n\r\n"+

                  "public void ResetBlend()\r\n{\r\nfillBlendDictionary(false);\r\n}\r\n\r\n");
            }
            //----------------

            sbEndCode.Append(
                "public void Dispose()\r\n{\r\n"+
                "bmpCanvas.Dispose();\r\n}\r\n\r\n"+
                "#endregion //Helper Methods\r\n}\r\n\r\n");

        }

        private void write_End()
        {
            sbEndCode.Append("}\r\n");

            if (blnEditBlend)
                sbEndCode.Append(
                    "public class BlendArgs\r\n{\r\n"+
                    "public byte Coverage;\r\npublic byte BlendSmoothness;\r\n\r\n"+
                    "public BlendArgs(byte coverage, byte smoothness)\r\n{\r\n"+
                    "Coverage = coverage;\r\nBlendSmoothness = smoothness;\r\n}\r\n}\r\n\r\n"+

                    "}\r\n\r\n");
        }


        private void write_EndingText()
        {
            sbEndCode.Append("//*** End of code generated by GDI+ generator. ***\r\n\r\n");
        }

        public string GeneratePaintToolInit(Shape shape)
        {
           return GeneratePaintToolInit(shape, "shapeBoundsNew",true);
        }

        public string GeneratePaintToolInit(Shape shape, string lgbBounds, bool initShapeBounds)
        {
            string str = "";
            if (initShapeBounds)
               str += SetShapeBounds(shape);

           if (shape.painter.PaintBorder)
           {
               if ((bool)shape.GetGDIValue(sGDIProperty.EditColor))
                str += "Pen " + shape.Name +
                            "Pen = new Pen(dColors[eShapeColor." + shape.Name + "BorderColor],"+
                            shape.painter.BorderThickness.ToString("F")+"f);\r\n";
               else
                str += "Pen " + shape.Name +
                            "Pen = new Pen(Color.FromArgb(" +
                            shape.painter.BorderColor.A +", " +
                            shape.painter.BorderColor.R + ", " +
                            shape.painter.BorderColor.G + ", " +
                            shape.painter.BorderColor.B + "), " +
                            shape.painter.BorderThickness.ToString("F") + "f);\r\n";
           }
            if (shape.painter.PaintFill)
            {
                if ((bool)shape.GetGDIValue(sGDIProperty.EditColor))
                {
                    if (shape.painter.ColorCount == 1)
                    {  //paint with a solid brush
                        str += "SolidBrush " + shape.Name +
                            "Brush = new SolidBrush(dColors[eShapeColor." + shape.Name + "FillColor]);\r\n";
                    }
                    else
                    { //Paint with a linear gradient brush

                        str += "LinearGradientBrush " + shape.Name +
                            "Brush = new LinearGradientBrush(" + lgbBounds + ",\r\n" +
                            "dColors[eShapeColor." + shape.Name + "FillColor1]," +
                            "dColors[eShapeColor." + shape.Name + "FillColor2],\r\n" +
                            shape.painter.LinearModeString + ");\r\n";
                    }
                }
                else
                {
                    if (shape.painter.ColorCount == 1)
                    {
                        Color color = shape.painter.GetColor(0);
                        //paint with a solid brush
                        str += "SolidBrush " + shape.Name +
                            "Brush = new SolidBrush(Color.FromArgb(" +
                                         color.A + ", "+
                                         color.R + ", "+
                                         color.G + ", "+
                                         color.B + "));\r\n";
                    }
                    else
                    {
                        Color color1 = shape.painter.GetColor(0);
                        Color color2 = shape.painter.GetColor(1);
                        //Paint with a linear gradient brush
                        str += "LinearGradientBrush " + shape.Name +
                            "Brush = new LinearGradientBrush("+lgbBounds+",\r\n" +
                                  "Color.FromArgb(" + color1.A + ", " +
                                       color1.R + ", " +
                                       color1.G + ", " +
                                       color1.B + "), " +
                                  "Color.FromArgb(" + color2.A + ", " +
                                       color2.R + ", " +
                                       color2.G + ", " +
                                       color2.B + ")," + shape.painter.LinearModeString + ");\r\n";
                    }
                }
                if (shape.painter.ColorCount > 1)
                {
                    if ((bool)shape.GetGDIValue(sGDIProperty.EditBlend))
                    {
                        str += shape.Name + "Brush.Blend = GetBlend(\r\n" +
                            "dBlend[eShapeBlend." + shape.Name + "].Coverage," +
                            "dBlend[eShapeBlend." + shape.Name + "].BlendSmoothness" + ");\r\n";
                    }
                    else
                    {
                        str += shape.Name + "Brush.Blend = GetBlend(" + shape.painter.Coverage +
                             "," + shape.painter.BlendSmoothness + ");\r\n";
                    }
                }
            }
            return str;
        }

        public string SetShapeBounds(Shape shape)
        {
            Rectangle rect = shape.GetShapeBounds(true);
            string str = 
             "//Sets the original shape bounds\r\n" +
             "shapeBoundsOld = new Rectangle(" +
                 (rect.X - AreaBounds.X) + ", " +
                 (rect.Y - AreaBounds.Y) + ", " +
                 (rect.Width) + ", " +
                 (rect.Height) + ");\r\n";

            if ((bool)shape.GetGDIValue(sGDIProperty.EditBounds))
            {
                str += "shapeBoundsNew = dBounds[eShapeBounds." + shape.Name + "];\r\n";
            }
            else
                str += "shapeBoundsNew = shapeBoundsOld;\r\n";
            //-------
            if ((bool)shape.GetGDIValue(sGDIProperty.AllowScaling))
            {
                str += "//Scale the demensions\r\n" +
                 "shapeBoundsNew = GetScaledBounds(shapeBoundsNew);\r\n";
            }

            return str;
        }


        public string GeneratePaintToolCleanup(Shape shape)
        {
            string str = "//Cleanup paint tools\r\n";
            if (shape.painter.PaintFill)
                str += shape.Name + "Brush.Dispose();\r\n";
            if (shape.painter.PaintBorder)
                str += shape.Name + "Pen.Dispose();\r\n";

            return str + "//\r\n";
        }

        private void writeScaleBoundsMethod()
        {
            sbEndCode.Append("private RectangleF GetScaledBounds(RectangleF shapeBounds)\r\n{\r\n" +
            "//Scale the demensions\r\n"+
            "RectangleF scaledRect = RectangleF.Empty;\r\n"+
            "scaledRect.X = (int)(shapeBounds.X * scale.Width);\r\n"+
            "scaledRect.Y = (int)(shapeBounds.Y * scale.Height);\r\n"+
            "scaledRect.Width = (int)(shapeBounds.Width * scale.Width);\r\n"+
            "scaledRect.Height = (int)(shapeBounds.Height * scale.Height);\r\n"+
            "return scaledRect;\r\n}\r\n");
        }

        private void writeScalePointsMethod()
        {
            sbEndCode.Append("private void ScalePoints(ref PointF[] points, RectangleF oldBounds, RectangleF rectScaled)\r\n{\r\n" +
            "int intCount = points.Length;\r\n" +
            "for (int i = 0; i < intCount; i++)\r\n{\r\n" +
            "points[i].X = rectScaled.X + (rectScaled.Width * ((points[i].X - oldBounds.X) / oldBounds.Width));\r\n" +
            "points[i].Y = rectScaled.Y + (rectScaled.Height * ((points[i].Y - oldBounds.Y) / oldBounds.Height));\r\n}\r\n}\r\n");
        }

        private void writeBlendMethod()
        {
            sbEndCode.Append("//Gets the blend for the LinearGradient brush\r\n" +
                         "//Byte values from 0 to 100 are passed in,\r\n" +
                         "//representing the BlendSmoothing and gradient\r\n" +
                         "//CoverageArea.\r\n" +

                "protected Blend GetBlend(byte coverage, byte smoothness)\r\n" +
                "{\r\nBlend blend = new Blend(9);\r\nshort i = 0; //loop var\r\n" +
                "blend.Positions[0] = 0.0f;\r\nfor (i = 1; i < 9; i++)\r\n" +
                "blend.Positions[i] = (float)(blend.Positions[i-1] + 0.125f);\r\n\r\n" +
                "byte intHalf = Convert.ToByte(((float)(coverage+1)/100)*8);\r\n" +
                "float increment = (((float)(smoothness)/100) / (8 - intHalf));\r\n" +
                "blend.Factors[intHalf] = ((float)(100 - smoothness) / 100);\r\n\r\n" +
                "if (smoothness > 0 && intHalf != 0)\r\n{\r\nshort startingFactor;\r\n" +
                "if (smoothness == 100)\r\nstartingFactor = 0;\r\nelse\r\n" +
                "startingFactor = Convert.ToInt16(((1-smoothness/100.0f))*intHalf);\r\n" +
                "float smoothIncrement = (blend.Factors[intHalf] / (intHalf - startingFactor+1));\r\n\r\n" +
                "blend.Factors[startingFactor] = smoothIncrement;\r\n" +
                "for (i = (short)(startingFactor+1); i < intHalf; i++)\r\n" +
                "blend.Factors[i] = (float)(blend.Factors[i - 1] + smoothIncrement);\r\n" +
                "\r\nfor (i = (short)(intHalf + 1); i < 9; i++)\r\n" +
                "blend.Factors[i] = (float)(blend.Factors[i - 1] + increment);\r\n}\r\n"+
                "return blend;\r\n}\r\n");
        }
    }
}