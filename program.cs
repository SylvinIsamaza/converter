using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Windows.Forms;
using netDxf;
using netDxf.Collections;
using netDxf.Entities;

public class DxfToImageConverter
{
    private const int ImageWidth = 1920; 
    private const int ImageHeight = 1080; 

    public void drawEntities(System.Drawing.Bitmap bitmap,float offsetX,float offsetY,float scale,(float minX,float minY,float maxX,float maxY) bounds,DxfDocument dxf,Graphics g)
    {


        DrawingEntities entities = dxf.Entities;
        if (bitmap == null) throw new ArgumentNullException("Something went wrong while processing file");
        else
        {
           
                g.Clear(Color.White);
                g.SetClip(new Rectangle(0, 0, ImageWidth, ImageHeight));

                g.TranslateTransform(offsetX, offsetY);
                g.ScaleTransform(scale, -scale);
                g.TranslateTransform(-bounds.minX, -bounds.maxY);

            foreach (EntityObject entity in GetFinalEntities(dxf))
            {
                if (entity is Circle circle)
                {
                    var center = new PointF((float)circle.Center.X, (float)circle.Center.Y);
                    float radius = (float)circle.Radius;
                    g.DrawEllipse(Pens.Black,
                        center.X - radius,
                        center.Y - radius,
                        radius * 2,
                        radius * 2);
                }
                if (entity is netDxf.Entities.Line line)
                {
                    var startPoint = new PointF((float)line.StartPoint.X, (float)line.StartPoint.Y);
                    var endPoint = new PointF((float)line.EndPoint.X, (float)line.EndPoint.Y);
                    g.DrawLine(Pens.Black, startPoint, endPoint);
                }
                if (entity is netDxf.Entities.Arc arc)
                {
                    var center = new PointF((float)arc.Center.X, (float)arc.Center.Y);
                    float radius = (float)arc.Radius;
                    float startAngle = (float)arc.StartAngle;
                    float endAngle = (float)arc.EndAngle;

                    
                    if (endAngle < startAngle)
                        endAngle += 360;

                    g.DrawArc(Pens.Black,
                        center.X - radius,
                        center.Y - radius,
                        radius * 2,
                        radius * 2,
                        startAngle,
                        endAngle - startAngle);
                }
               
                if (entity is netDxf.Entities.Ellipse ellipse)
                {
                    var center = new PointF((float)ellipse.Center.X, (float)ellipse.Center.Y);
                    float radiusX = (float)ellipse.MajorAxis / 2;
                    float radiusY = (float)ellipse.MinorAxis / 2;

                    float endAngle = (float)ellipse.EndAngle;
                    float startAngle = (float)ellipse.StartAngle;
                    g.DrawEllipse(Pens.Black, center.X - radiusX, center.Y - radiusY, radiusX * 2, radiusY * 2);
                }
                if (entity is netDxf.Entities.Point point)
                {
                    var location = new PointF((float)point.Position.X, (float)point.Position.Y);
                    g.FillEllipse(Brushes.Black, location.X - 2, location.Y - 2, 4, 4);
                }
                if (entity is netDxf.Entities.Solid solid)
                {
                    System.Drawing.Point[] points = new System.Drawing.Point[]
                    {
                            new System.Drawing.Point((int)solid.FirstVertex.X, (int)solid.FirstVertex.Y),
                            new System.Drawing.Point((int)solid.SecondVertex.X, (int)solid.SecondVertex.Y),
                            new System.Drawing.Point((int)solid.ThirdVertex.X, (int)solid.ThirdVertex.Y),
        new System.Drawing.Point((int)solid.FourthVertex.X, (int)solid.FourthVertex.Y)
                };


                    g.FillPolygon(Brushes.White, points);
                    g.DrawPolygon(Pens.Black, points);
                }
                if(entity is netDxf.Entities.MText mtext) {
                    PointF position = new PointF((float)mtext.Position.X, (float)mtext.Position.Y);
                    
                   
                    Font font = new Font(mtext.Style.FontFamilyName,(float)mtext.Height);
                    Brush brush = Brushes.Black; 

                   
                    g.DrawString(mtext.Value, font, brush, position);
                }
                if (entity is netDxf.Entities.XLine xLine)
                {
                    var startPoint = new PointF((float)xLine.Origin.X, (float)xLine.Origin.Y);
                    var direction = new PointF((float)xLine.Direction.X, (float)xLine.Direction.Y);
                    var endPoint = new PointF(startPoint.X + direction.X * 1000, startPoint.Y + direction.Y * 1000);
                    g.DrawLine(Pens.Black, startPoint, endPoint);
                }

                if (entity is netDxf.Entities.Ray ray)
                {
                    var startPoint = new PointF((float)ray.Origin.X, (float)ray.Origin.Y);
                    var direction = new PointF((float)ray.Direction.X, (float)ray.Direction.Y);
                    var endPoint = new PointF(startPoint.X + direction.X * 1000, startPoint.Y + direction.Y * 1000);
                    g.DrawLine(Pens.Black, startPoint, endPoint);
                }
                if (entity is netDxf.Entities.MLine mLine)
                {
                    if (mLine.Vertexes.Count > 1)
                    {
                        for (int i = 0; i < mLine.Vertexes.Count - 1; i++)
                        {
                            var startPoint = new PointF((float)mLine.Vertexes[i].Position.X, (float)mLine.Vertexes[i].Position.Y);
                            var endPoint = new PointF((float)mLine.Vertexes[i + 1].Position.X, (float)mLine.Vertexes[i + 1].Position.Y);
                            g.DrawLine(Pens.Black, startPoint, endPoint);
                        }
                    }
                }
                if (entity is netDxf.Entities.Leader leader)
                {
                    if (leader.Vertexes.Count > 1)
                    {
                        for (int i = 0; i < leader.Vertexes.Count - 1; i++)
                        {
                            var startPoint = new PointF((float)leader.Vertexes[i].X, (float)leader.Vertexes[i].Y);
                            var endPoint = new PointF((float)leader.Vertexes[i + 1].X, (float)leader.Vertexes[i + 1].Y);
                            g.DrawLine(Pens.Black, startPoint, endPoint);
                        }
                    }
                }
                
            
        
    }



            


               
            
        }
    }

  
    public Bitmap ConvertDxfToBitmap(string dxfFilePath)
    {
        Bitmap bitmap = new Bitmap(ImageWidth, ImageHeight);
        try
        {
            DxfDocument dxf = DxfDocument.Load(dxfFilePath);

            if (!dxf.Entities.Lines.Any() && !dxf.Entities.Circles.Any() && !dxf.Entities.Arcs.Any() && !dxf.Entities.Polylines2D.Any() && !dxf.Entities.Wipeouts.Any()&& !dxf.Entities.Inserts.Any() && !dxf.Entities.Ellipses.Any()&&!dxf.Entities.Dimensions.Any()&&!dxf.Entities.Hatches.Any()&&!dxf.Entities.Images.Any()&&!dxf.Entities.Meshes.Any()&&!dxf.Entities.Leaders.Any()&&!dxf.Entities.MLines.Any()&&!dxf.Entities.MTexts.Any()&&!dxf.Entities.Polylines3D.Any()&&!dxf.Entities.PolyfaceMeshes.Any() && !dxf.Entities.PolygonMeshes.Any() && !dxf.Entities.Shapes.Any() && !dxf.Entities.Rays.Any() && !dxf.Entities.Solids.Any() &&!dxf.Entities.Splines.Any()&& !dxf.Entities.Texts.Any() && !dxf.Entities.Tolerances.Any()&& !dxf.Entities.Underlays.Any()&& !dxf.Entities.XLines.Any() &&!dxf.Entities.Faces3D.Any())
            {
                throw new InvalidOperationException("The DXF file contains no drawable entities.");
            }

            var bounds = CalculateBounds(dxf);
            float marginX = (bounds.maxX - bounds.minX) * 0.1f;
            float marginY = (bounds.maxY - bounds.minY) * 0.1f;
            bounds.minX -= marginX;
            bounds.maxX += marginX;
            bounds.minY -= marginY;
            bounds.maxY += marginY;

            var drawingWidth = bounds.maxX - bounds.minX;
            var drawingHeight = bounds.maxY - bounds.minY;

            float scaleX = ImageWidth / drawingWidth;
            float scaleY = ImageHeight / drawingHeight;
            float scale = Math.Min(scaleX, scaleY);

            float offsetX = (ImageWidth - (drawingWidth * scale)) / 2;
            float offsetY = (ImageHeight - (drawingHeight * scale)) / 2;
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                drawEntities(bitmap, offsetX, offsetY, scale, bounds, dxf, g);
            }




        }
        catch (Exception ex)
        {
           
            MessageBox.Show($"Error processing DXF file: {ex.Message}");
        }

        return bitmap;
    }
    public static List<EntityObject> GetFinalEntities(DxfDocument dxf)
    {
        var finalEntities = new List<EntityObject>();
        

        foreach (var entity in dxf.Entities.All)
        {
            if (entity is Insert insert)
            {
                GetEntitiesFromInsert(insert, finalEntities);
            }
            else
            {
                finalEntities.Add(entity);
            }
        }

        return finalEntities;
    }

    private static void GetEntitiesFromInsert(Insert insert, List<EntityObject> finalEntities)
    {
        foreach (var entity in insert.Block.Entities)
        {
            if (entity is Insert nestedInsert)
            {
                GetEntitiesFromInsert(nestedInsert, finalEntities);
            }
            else
            {
                finalEntities.Add(entity);
            }
        }
    }

    private (float minX, float minY, float maxX, float maxY) CalculateBounds(DxfDocument dxf)
    {
        var allPoints = new List<Vector3>();
       
        foreach (EntityObject entity in GetFinalEntities(dxf))
        {
           
         if(entity is Circle circle)
            {
                
                allPoints.Add(circle.Center);
            }
         if(entity is netDxf.Entities.Line line)
            {
                allPoints.Add(line.StartPoint);
                allPoints.Add(line.EndPoint);
            }
         if(entity is netDxf.Entities.Arc arc)
            {
                allPoints.Add(arc.Center);
            }
        }
        

        if (!allPoints.Any())
        {
            throw new InvalidOperationException("No drawable entities found in the DXF file.");
        }
       foreach(EntityObject entity in GetFinalEntities(dxf))
        {
           
            
        }
        float minX = allPoints.Min(pt => (float)pt.X);
        float minY = allPoints.Min(pt => (float)pt.Y);
        float maxX = allPoints.Max(pt => (float)pt.X);
        float maxY = allPoints.Max(pt => (float)pt.Y);

        return (minX, minY, maxX, maxY);
    }


    public System.Drawing.Image ConvertDxfToJpeg(string dxfFilePath)
    {
        Bitmap bitmap = ConvertDxfToBitmap(dxfFilePath);
        return bitmap;
    }

    public void SaveAsJpeg(Bitmap bitmap, string outputPath, int quality = 100)
    {
        ImageCodecInfo jpgEncoder = GetEncoder(ImageFormat.Jpeg);
        Encoder myEncoder = Encoder.Quality;
        EncoderParameters myEncoderParameters = new EncoderParameters(1);
        EncoderParameter myEncoderParameter = new EncoderParameter(myEncoder, quality);
        myEncoderParameters.Param[0] = myEncoderParameter;
        bitmap.Save(outputPath, jpgEncoder, myEncoderParameters);
    }

    private ImageCodecInfo GetEncoder(ImageFormat format)
    {
        ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();
        foreach (ImageCodecInfo codec in codecs)
        {
            if (codec.FormatID == format.Guid)
            {
                return codec;
            }
        }
        return null;
    }

    public Bitmap SelectAndConvertDxf()
    {
        Bitmap bitmap = null;
        using (OpenFileDialog openFileDialog = new OpenFileDialog())
        {
            openFileDialog.Filter = "DXF files (*.dxf)|*.dxf";
            openFileDialog.Title = "Select a DXF file";

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                string dxfFilePath = openFileDialog.FileName;
                bitmap = ConvertDxfToBitmap(dxfFilePath);
            }
        }
        return bitmap;
    }
}

public class MainForm : Form
{
    private Button convertButton;
    private StatusStrip statusStrip;
    private ToolStripStatusLabel statusLabel;

    public MainForm()
    {
        this.Text = "DXF to Image Converter";
        this.Size = new Size(300, 200);

        convertButton = new Button
        {
            Text = "Select and Convert DXF",
            Dock = DockStyle.Top
        };
        convertButton.Click += ConvertButton_Click;

        statusStrip = new StatusStrip();
        statusLabel = new ToolStripStatusLabel("Ready");
        statusStrip.Items.Add(statusLabel);

        this.Controls.Add(convertButton);
        this.Controls.Add(statusStrip);
    }

    private void ConvertButton_Click(object sender, EventArgs e)
    {
        DxfToImageConverter converter = new DxfToImageConverter();
        statusLabel.Text = "Selecting DXF file...";
        System.Windows.Forms.Application.DoEvents(); 

        Bitmap bmp = converter.SelectAndConvertDxf();
        if (bmp != null)
        {
            statusLabel.Text = "Converting DXF to image...";
            System.Windows.Forms.Application.DoEvents(); 

            string outputPath = "output.jpg";
            converter.SaveAsJpeg(bmp, outputPath);
            bmp.Save("output.bmp", ImageFormat.Bmp);

            statusLabel.Text = "Conversion complete.";
        }
        else
        {
            statusLabel.Text = "Failed to convert DXF.";
        }
    }
}

class Program
{
    [STAThread]
    static void Main()
    {
        System.Windows.Forms.Application.EnableVisualStyles();
        System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);
        System.Windows.Forms.Application.Run(new MainForm());
    }
}
