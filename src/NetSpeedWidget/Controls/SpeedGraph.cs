using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using Pen = System.Windows.Media.Pen;
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;
using Point = System.Windows.Point;

namespace NetSpeedWidget.Controls;

public class SpeedGraph : FrameworkElement
{
    private readonly LinkedList<double> _downloadHistory = new();
    private readonly LinkedList<double> _uploadHistory = new();
    private const int MaxHistoryPoints = 40;

    private readonly Pen _downloadPen;
    private readonly Pen _uploadPen;
    private readonly Brush _downloadAreaBrush;
    private readonly Brush _uploadAreaBrush;
    private readonly Pen _gridPen;

    public SpeedGraph()
    {
        // Smooth anti-aliased pens
        _downloadPen = new Pen(new SolidColorBrush(Color.FromRgb(0x38, 0xbd, 0xf8)), 1.5); // Sky blue
        _downloadPen.Freeze();

        _uploadPen = new Pen(new SolidColorBrush(Color.FromRgb(0xa8, 0x55, 0xf7)), 1.5); // Purple / violet
        _uploadPen.Freeze();

        var downArea = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(0, 1)
        };
        downArea.GradientStops.Add(new GradientStop(Color.FromArgb(0x44, 0x38, 0xbd, 0xf8), 0.0));
        downArea.GradientStops.Add(new GradientStop(Color.FromArgb(0x05, 0x38, 0xbd, 0xf8), 1.0));
        downArea.Freeze();
        _downloadAreaBrush = downArea;

        var upArea = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(0, 1)
        };
        upArea.GradientStops.Add(new GradientStop(Color.FromArgb(0x33, 0xa8, 0x55, 0xf7), 0.0));
        upArea.GradientStops.Add(new GradientStop(Color.FromArgb(0x02, 0xa8, 0x55, 0xf7), 1.0));
        upArea.Freeze();
        _uploadAreaBrush = upArea;

        _gridPen = new Pen(new SolidColorBrush(Color.FromArgb(0x22, 0xff, 0xff, 0xff)), 1.0);
        _gridPen.DashStyle = DashStyles.Dot;
        _gridPen.Freeze();

        // Seed with zeros
        for (int i = 0; i < MaxHistoryPoints; i++)
        {
            _downloadHistory.AddLast(0);
            _uploadHistory.AddLast(0);
        }
    }

    public void AddPoints(double downloadBytesPerSec, double uploadBytesPerSec)
    {
        _downloadHistory.AddLast(downloadBytesPerSec);
        if (_downloadHistory.Count > MaxHistoryPoints)
            _downloadHistory.RemoveFirst();

        _uploadHistory.AddLast(uploadBytesPerSec);
        if (_uploadHistory.Count > MaxHistoryPoints)
            _uploadHistory.RemoveFirst();

        InvalidateVisual();
    }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);

        double width = ActualWidth;
        double height = ActualHeight;

        if (width <= 10 || height <= 10) return;

        // Draw subtle background grid lines
        dc.DrawLine(_gridPen, new Point(0, height * 0.5), new Point(width, height * 0.5));

        // Find max speed to scale the graph dynamically
        double maxSpeed = 100 * 1024; // minimum 100 KB/s scale
        foreach (var v in _downloadHistory) if (v > maxSpeed) maxSpeed = v;
        foreach (var v in _uploadHistory) if (v > maxSpeed) maxSpeed = v;

        // Render Download curve & area
        RenderSeries(dc, _downloadHistory, maxSpeed, width, height, _downloadPen, _downloadAreaBrush);

        // Render Upload curve & area
        RenderSeries(dc, _uploadHistory, maxSpeed, width, height, _uploadPen, _uploadAreaBrush);
    }

    private void RenderSeries(DrawingContext dc, LinkedList<double> points, double max, double width, double height, Pen pen, Brush areaBrush)
    {
        if (points.Count < 2) return;

        double stepX = width / (MaxHistoryPoints - 1);
        var lineGeom = new StreamGeometry();
        var areaGeom = new StreamGeometry();

        using (var ctx = lineGeom.Open())
        {
            int index = 0;
            Point firstPt = new Point(0, height);

            foreach (var val in points)
            {
                double norm = Math.Clamp(val / max, 0, 1);
                double x = index * stepX;
                double y = height - (norm * (height - 4)) - 2;

                if (index == 0)
                {
                    ctx.BeginFigure(new Point(x, y), false, false);
                    firstPt = new Point(x, y);
                }
                else
                {
                    ctx.LineTo(new Point(x, y), true, true);
                }
                index++;
            }
        }

        using (var ctx = areaGeom.Open())
        {
            int index = 0;
            ctx.BeginFigure(new Point(0, height), true, true);

            foreach (var val in points)
            {
                double norm = Math.Clamp(val / max, 0, 1);
                double x = index * stepX;
                double y = height - (norm * (height - 4)) - 2;

                ctx.LineTo(new Point(x, y), true, true);
                index++;
            }

            ctx.LineTo(new Point(width, height), true, true);
        }

        lineGeom.Freeze();
        areaGeom.Freeze();

        dc.DrawGeometry(areaBrush, null, areaGeom);
        dc.DrawGeometry(null, pen, lineGeom);
    }
}
