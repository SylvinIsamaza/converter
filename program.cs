using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Windows.Forms;
using IxMilia.Dxf.Entities;
using IxMilia.Dxf;
using IxMilia.Dxf.Blocks;
using System.Drawing.Drawing2D;
using netDxf.Entities;

public class InsertFeatures
{
    public DxfPoint Location { get; set; }
    public double XScaleFactor { get; set; }
    public double YScaleFactor { get; set; }
    public double ZScaleFactor { get; set; }
    public double Rotation { get; set; }
    public int ColumnCount { get; set; }
    public int RowCount { get; set; }
    public double ColumnSpacing { get; set; }
    public double RowSpacing { get; set; }
}

public struct CustomEntity
{
    public string Type { get; set; }
    public DxfEntity Entity { get; set; }
    public InsertFeatures? NestedInsertFeatures { get; set; }
    public InsertFeatures? TopInsertFeatures { get; set; }
    public string UniqueId { get; set; }

    public CustomEntity(string type, DxfEntity entity, string uniqueId,
                        InsertFeatures? nestedInsertFeatures = null,
                        InsertFeatures? topInsertFeatures = null)
    {
        Type = type;
        Entity = entity;
        NestedInsertFeatures = nestedInsertFeatures;
        TopInsertFeatures = topInsertFeatures;
        UniqueId = uniqueId;
    }

    public void HandleFeatures()
    {
        if (Type == "insert")
        {
            if (NestedInsertFeatures != null)
            {
                MessageBox.Show($"Nested Insert Features: Location = {NestedInsertFeatures.Location}, " +
                                $"XScaleFactor = {NestedInsertFeatures.XScaleFactor}, " +
                                $"YScaleFactor = {NestedInsertFeatures.YScaleFactor}, " +
                                $"Rotation = {NestedInsertFeatures.Rotation}");
            }

            if (TopInsertFeatures != null)
            {
                MessageBox.Show($"Top Insert Features: Location = {TopInsertFeatures.Location}, " +
                                $"XScaleFactor = {TopInsertFeatures.XScaleFactor}, " +
                                $"YScaleFactor = {TopInsertFeatures.YScaleFactor}, " +
                                $"Rotation = {TopInsertFeatures.Rotation}");
            }
        }
        else
        {
            MessageBox.Show("Type does not require insert features.");
        }
    }

    public DxfEntity GetEntity()
    {
        return Entity;
    }
}

public class DxfToImageConverter
{
    private const int ImageWidth = 1920;
    private const int ImageHeight = 1080;

    public void DrawEntities(Bitmap bitmap, float offsetX, float offsetY, float scale, (float minX, float minY, float maxX, float maxY) bounds, DxfFile dxf, Graphics g)
    {
        if (bitmap == null) throw new ArgumentNullException("Something went wrong while processing file");

        g.Clear(Color.White);
        g.SetClip(new Rectangle(0, 0, ImageWidth, ImageHeight));

        g.TranslateTransform(offsetX, offsetY);
        g.ScaleTransform(scale, -scale);
        g.TranslateTransform(-bounds.minX, -bounds.maxY);

        foreach (CustomEntity entity in GetFinalEntities(dxf))
        {
            if (entity.Type == "insert")
            {
                using (Matrix transformMatrix = new Matrix())
                {
                    if (entity.TopInsertFeatures != null)
                    {
                        transformMatrix.Translate((float)entity.TopInsertFeatures.Location.X, (float)entity.TopInsertFeatures.Location.Y);
                        transformMatrix.Rotate((float)entity.TopInsertFeatures.Rotation);
                        transformMatrix.Scale((float)entity.TopInsertFeatures.XScaleFactor, (float)entity.TopInsertFeatures.YScaleFactor);
                    }

                    if (entity.NestedInsertFeatures != null)
                    {
                        transformMatrix.Translate((float)entity.NestedInsertFeatures.Location.X, (float)entity.NestedInsertFeatures.Location.Y);
                        transformMatrix.Rotate((float)entity.NestedInsertFeatures.Rotation);
                        transformMatrix.Scale((float)entity.NestedInsertFeatures.XScaleFactor, (float)entity.NestedInsertFeatures.YScaleFactor);
                    }

                    var originalTransform = g.Transform;
                    g.MultiplyTransform(transformMatrix);
                    g.Transform = originalTransform;
                    //MessageBox.Show(entity.Entity.EntityType.ToString());
                    if (entity.Entity is DxfCircle circle)
                    {
                        var center = new PointF((float)circle.Center.X, (float)circle.Center.Y);
                        float radius = (float)circle.Radius;
                        g.DrawEllipse(Pens.Black,
                            center.X - radius,
                            center.Y - radius,
                            radius * 2,
                            radius * 2);
                    }
                    else if (entity.Entity is DxfLine line)
                    {
                        var startPoint = new PointF((float)line.P1.X, (float)line.P1.Y);
                        var endPoint = new PointF((float)line.P2.X, (float)line.P2.Y);
                        g.DrawLine(Pens.Black, startPoint, endPoint);
                    }
                    else if (entity.Entity is DxfArc arc)
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
                    else if (entity.Entity is DxfEllipse ellipse)
                    {
                        var center = new PointF((float)ellipse.Center.X, (float)ellipse.Center.Y);
                        float radiusX = (float)ellipse.MajorAxis.X * (float)ellipse.MajorAxis.X;
                        float radiusY = (float)ellipse.MajorAxis.Y;
                        g.DrawEllipse(Pens.Black, center.X - radiusX, center.Y - radiusY, radiusX * 2, radiusY * 2);
                    }
                    else if (entity.Entity is DxfMText mtext)
                    {
                        PointF position = new PointF((float)mtext.InsertionPoint.X, (float)mtext.InsertionPoint.Y);
                        Font font = new Font("Arial", 20);
                        Brush brush = Brushes.Black;
                        g.DrawString(mtext.Text, font, brush, position);
                    }
                    else if (entity.Entity is DxfXLine xLine)
                    {
                        var startPoint = new PointF((float)xLine.FirstPoint.X, (float)xLine.FirstPoint.Y);
                        var direction = new PointF((float)xLine.UnitDirectionVector.X, (float)xLine.UnitDirectionVector.Y);
                        var endPoint = new PointF(startPoint.X + direction.X * 1000, startPoint.Y + direction.Y * 1000);
                        g.DrawLine(Pens.Black, startPoint, endPoint);
                    }
                    else if (entity.Entity is DxfRay ray)
                    {
                        var startPoint = new PointF((float)ray.StartPoint.X, (float)ray.StartPoint.Y);
                        var direction = new PointF((float)ray.UnitDirectionVector.X, (float)ray.UnitDirectionVector.Y);
                        var endPoint = new PointF(startPoint.X + direction.X * 1000, startPoint.Y + direction.Y * 1000);
                        g.DrawLine(Pens.Black, startPoint, endPoint);
                    }
                    else if (entity.Entity is DxfLeader leader)
                    {
                        if (leader.Vertices.Count > 1)
                        {
                            for (int i = 0; i < leader.Vertices.Count - 1; i++)
                            {
                                var startPoint = new PointF((float)leader.Vertices[i].X, (float)leader.Vertices[i].Y);
                                var endPoint = new PointF((float)leader.Vertices[i + 1].X, (float)leader.Vertices[i + 1].Y);
                                g.DrawLine(Pens.Black, startPoint, endPoint);
                            }
                        }
                    }
                    else if (entity.Entity is DxfMLine mLine)
                    {
                        if (mLine.Vertices.Count > 1)
                        {
                            for (int i = 0; i < mLine.Vertices.Count - 1; i++)
                            {
                                var startPoint = new PointF((float)mLine.Vertices[i].X, (float)mLine.Vertices[i].Y);
                                var endPoint = new PointF((float)mLine.Vertices[i + 1].X, (float)mLine.Vertices[i + 1].Y);
                                g.DrawLine(Pens.Black, startPoint, endPoint);
                            }
                        }
                    }

                }
            }

            else
            {
                if (entity.Entity is DxfCircle circle)
                {
                    var center = new PointF((float)circle.Center.X, (float)circle.Center.Y);
                    float radius = (float)circle.Radius;
                    g.DrawEllipse(Pens.Black,
                        center.X - radius,
                        center.Y - radius,
                        radius * 2,
                        radius * 2);
                }
                else if (entity.Entity is DxfLine line)
                {
                    var startPoint = new PointF((float)line.P1.X, (float)line.P1.Y);
                    var endPoint = new PointF((float)line.P2.X, (float)line.P2.Y);
                    g.DrawLine(Pens.Black, startPoint, endPoint);
                }
                else if (entity.Entity is DxfArc arc)
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
                else if (entity.Entity is DxfEllipse ellipse)
                {
                    var center = new PointF((float)ellipse.Center.X, (float)ellipse.Center.Y);
                    float radiusX = (float)ellipse.MajorAxis.X * (float)ellipse.MajorAxis.X;
                    float radiusY = (float)ellipse.MajorAxis.Y;
                    g.DrawEllipse(Pens.Black, center.X - radiusX, center.Y - radiusY, radiusX * 2, radiusY * 2);
                }
                else if (entity.Entity is DxfMText mtext)
                {
                    PointF position = new PointF((float)mtext.InsertionPoint.X, (float)mtext.InsertionPoint.Y);
                    Font font = new Font("Arial", 20);
                    Brush brush = Brushes.Black;
                    g.DrawString(mtext.Text, font, brush, position);
                }
                else if (entity.Entity is DxfXLine xLine)
                {
                    var startPoint = new PointF((float)xLine.FirstPoint.X, (float)xLine.FirstPoint.Y);
                    var direction = new PointF((float)xLine.UnitDirectionVector.X, (float)xLine.UnitDirectionVector.Y);
                    var endPoint = new PointF(startPoint.X + direction.X * 1000, startPoint.Y + direction.Y * 1000);
                    g.DrawLine(Pens.Black, startPoint, endPoint);
                }
                else if (entity.Entity is DxfRay ray)
                {
                    var startPoint = new PointF((float)ray.StartPoint.X, (float)ray.StartPoint.Y);
                    var direction = new PointF((float)ray.UnitDirectionVector.X, (float)ray.UnitDirectionVector.Y);
                    var endPoint = new PointF(startPoint.X + direction.X * 1000, startPoint.Y + direction.Y * 1000);
                    g.DrawLine(Pens.Black, startPoint, endPoint);
                }
                else if (entity.Entity is DxfLeader leader)
                {
                    if (leader.Vertices.Count > 1)
                    {
                        for (int i = 0; i < leader.Vertices.Count - 1; i++)
                        {
                            var startPoint = new PointF((float)leader.Vertices[i].X, (float)leader.Vertices[i].Y);
                            var endPoint = new PointF((float)leader.Vertices[i + 1].X, (float)leader.Vertices[i + 1].Y);
                            g.DrawLine(Pens.Black, startPoint, endPoint);
                        }
                    }
                }
                else if (entity.Entity is DxfMLine mLine)
                {
                    if (mLine.Vertices.Count > 1)
                    {
                        for (int i = 0; i < mLine.Vertices.Count - 1; i++)
                        {
                            var startPoint = new PointF((float)mLine.Vertices[i].X, (float)mLine.Vertices[i].Y);
                            var endPoint = new PointF((float)mLine.Vertices[i + 1].X, (float)mLine.Vertices[i + 1].Y);
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
            DxfFile dxf = DxfFile.Load(dxfFilePath);

            if (!dxf.Entities.Any())
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

            float scale = Math.Min(ImageWidth / (bounds.maxX - bounds.minX), ImageHeight / (bounds.maxY - bounds.minY));
            float offsetX = (ImageWidth - scale * (bounds.maxX - bounds.minX)) / 2;
            float offsetY = (ImageHeight - scale * (bounds.maxY - bounds.minY)) / 2;

            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                DrawEntities(bitmap, offsetX, offsetY, scale, bounds, dxf, g);
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"An error occurred while converting the DXF file: {ex.Message}");
        }

        return bitmap;
    }

    private (float minX, float minY, float maxX, float maxY) CalculateBounds(DxfFile dxf)
    {
        float minX = float.MaxValue;
        float minY = float.MaxValue;
        float maxX = float.MinValue;
        float maxY = float.MinValue;

        foreach (var entity in dxf.Entities)
        {
            if (entity is DxfCircle circle)
            {
                float radius = (float)circle.Radius;
                minX = Math.Min(minX, (float)circle.Center.X - radius);
                minY = Math.Min(minY, (float)circle.Center.Y - radius);
                maxX = Math.Max(maxX, (float)circle.Center.X + radius);
                maxY = Math.Max(maxY, (float)circle.Center.Y + radius);
            }
            else if (entity is DxfLine line)
            {
                minX = Math.Min(minX, Math.Min((float)line.P1.X, (float)line.P2.X));
                minY = Math.Min(minY, Math.Min((float)line.P1.Y, (float)line.P2.Y));
                maxX = Math.Max(maxX, Math.Max((float)line.P1.X, (float)line.P2.X));
                maxY = Math.Max(maxY, Math.Max((float)line.P1.Y, (float)line.P2.Y));
            }
            else if (entity is DxfArc arc)
            {
                float radius = (float)arc.Radius;
                minX = Math.Min(minX, (float)arc.Center.X - radius);
                minY = Math.Min(minY, (float)arc.Center.Y - radius);
                maxX = Math.Max(maxX, (float)arc.Center.X + radius);
                maxY = Math.Max(maxY, (float)arc.Center.Y + radius);
            }
            else if (entity is DxfEllipse ellipse)
            {
                float radiusX = (float)ellipse.MajorAxis.X;
                float radiusY = (float)ellipse.MajorAxis.Y;
                minX = Math.Min(minX, (float)ellipse.Center.X - radiusX);
                minY = Math.Min(minY, (float)ellipse.Center.Y - radiusY);
                maxX = Math.Max(maxX, (float)ellipse.Center.X + radiusX);
                maxY = Math.Max(maxY, (float)ellipse.Center.Y + radiusY);
            }
            else if (entity is DxfXLine xLine)
            {
                minX = Math.Min(minX, (float)xLine.FirstPoint.X);
                minY = Math.Min(minY, (float)xLine.FirstPoint.Y);
                maxX = Math.Max(maxX, (float)xLine.FirstPoint.X);
                maxY = Math.Max(maxY, (float)xLine.FirstPoint.Y);
            }
            else if (entity is DxfRay ray)
            {
                minX = Math.Min(minX, (float)ray.StartPoint.X);
                minY = Math.Min(minY, (float)ray.StartPoint.Y);
                maxX = Math.Max(maxX, (float)ray.StartPoint.X);
                maxY = Math.Max(maxY, (float)ray.StartPoint.Y);
            }
            else if (entity is DxfLeader leader)
            {
                foreach (var vertex in leader.Vertices)
                {
                    minX = Math.Min(minX, (float)vertex.X);
                    minY = Math.Min(minY, (float)vertex.Y);
                    maxX = Math.Max(maxX, (float)vertex.X);
                    maxY = Math.Max(maxY, (float)vertex.Y);
                }
            }
            else if (entity is DxfMLine mLine)
            {
                foreach (var vertex in mLine.Vertices)
                {
                    minX = Math.Min(minX, (float)vertex.X);
                    minY = Math.Min(minY, (float)vertex.Y);
                    maxX = Math.Max(maxX, (float)vertex.X);
                    maxY = Math.Max(maxY, (float)vertex.Y);
                }
            }
        }

        return (minX, minY, maxX, maxY);
    }

    private IEnumerable<CustomEntity> GetFinalEntities(DxfFile dxf)
    {
        List<CustomEntity> entities = new List<CustomEntity>();

        foreach (var entity in dxf.Entities)
        {
            if (entity is DxfInsert insert)
            {
                var block = dxf.Blocks.FirstOrDefault(b => b.Name == insert.Name);
                if (block != null)
                {
                    var nestedEntities = block.Entities.Select(e => new CustomEntity(
                        "insert", e, Guid.NewGuid().ToString(),
                        new InsertFeatures
                        {
                            Location = insert.Location,
                            XScaleFactor = insert.XScaleFactor,
                            YScaleFactor = insert.YScaleFactor,
                            ZScaleFactor = insert.ZScaleFactor,
                            Rotation = insert.Rotation,
                            ColumnCount = insert.ColumnCount,
                            RowCount = insert.RowCount,
                            ColumnSpacing = insert.ColumnSpacing,
                            RowSpacing = insert.RowSpacing
                        })).ToList();

                    entities.AddRange(nestedEntities);
                }

            }
            else
            {
                entities.Add(new CustomEntity(entity.GetType().Name.ToLower(), entity, Guid.NewGuid().ToString()));
            }
        }

        return entities;
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
