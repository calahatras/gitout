using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;

namespace GitOut.Features.Git.Log.Converters
{
    internal class NodesToPathConverter : IMultiValueConverter
    {
        private const int XDistance = 15;
        private const int XOffset = 10;
        private static readonly Size Size = new(6, 6);

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (
                values.Length != 2 ||
                values[0] is not IEnumerable<GitTreeNode> nodes ||
                values[1] is not double height
            )
            {
                return DependencyProperty.UnsetValue;
            }

            List<Path> paths = new();
            foreach (GitTreeNode? node in nodes)
            {
                if (!node.IsCommit)
                {
                    Path path = new()
                    {
                        Stroke = new SolidColorBrush(node.Color),
                        StrokeThickness = 2,
                        SnapsToDevicePixels = true
                    };
                    if (node.LineType == LineType.Dashed)
                    {
                        path.StrokeDashArray = new DoubleCollection(new[] { 3d, 1 });
                    }
                    path.Data = GeometryFromTopToBottom(node, height);
                    paths.Add(path);
                }
                else
                {

                    Path linePath = new()
                    {
                        Stroke = new SolidColorBrush(node.Color),
                        StrokeThickness = 2,
                        SnapsToDevicePixels = true
                    };
                    if (node.LineType == LineType.Dashed)
                    {
                        linePath.StrokeDashArray = new DoubleCollection(new[] { 3d, 1 });
                    }

                    Path? commitPath = AddCommitGeometry(node, height);
                    if (commitPath != null)
                        paths.Add(commitPath);

                    var pathGeometry = new PathGeometry();
                    linePath.Data = pathGeometry;
                    DrawBottom(pathGeometry, node, height);
                    DrawTop(pathGeometry, node, height);
                    paths.Add(linePath);
                }
            }

            return paths.ToArray();
        }

        private static Path? AddCommitGeometry(GitTreeNode node, double height)
        {
            int index = GetCommitIndex(node);
            if (index == -1) return null;

            Path path = new()
            {
                Stroke = new SolidColorBrush(node.Color),
                StrokeThickness = 2,
                SnapsToDevicePixels = true
            };
            var pathGeometry = new PathGeometry();
            // should be if (node.Variant == Variant.Commit | Variant.Stage or something instead
            if (node.LineType == LineType.Solid)
            {
                pathGeometry.AddGeometry(new EllipseGeometry(new Point(index * XDistance + XOffset, height / 2), Size.Height / 2, Size.Width / 2));
            }
            else
            {
                var pathFigure = new PathFigure(
                    new Point(index * XDistance + XOffset - (Size.Width * 2 / 3), height / 2 - (Size.Height * 2 / 3)),
                    new[]
                    {

                        new PolyLineSegment(new[]
                        {
                            new Point(index * XDistance + XOffset + (Size.Width * 2 / 3), height / 2 - (Size.Height *2 / 3)),
                            new Point(index * XDistance + XOffset, height / 2 + (Size.Height / 2)),
                        }, true),
                    },
                    true);

                pathGeometry.Figures.Add(pathFigure);

            }
            path.Data = pathGeometry;
            return path;
        }

        private static int GetCommitIndex(GitTreeNode node) =>
            node.Bottom is Line bottomLine && bottomLine.Up == bottomLine.Down
                ? bottomLine.Up
                : node.Top is Line topLine && topLine.Up == topLine.Down
                    ? topLine.Up
                    : -1;

        private static void DrawBottom(PathGeometry geometry, GitTreeNode node, double height)
        {
            if (node.Bottom is not Line line)
            {
                return;
            }

            double bottomXCoordinate = XOffset + line.Down * XDistance;

            if (line.Up == line.Down)
            {
                double offset = Size.Height / 2;
                double middleXCoordinate = XOffset + line.Up * XDistance;
                double middleYCoordinate = height / 2 + offset;

                geometry.Figures.Add(new PathFigure(new Point(middleXCoordinate, middleYCoordinate), new[] { new LineSegment(new Point(bottomXCoordinate, height), true) }, false));
            }
            else
            {
                var nodes = new List<BezierSegment>();
                double offset = node.LineType == LineType.Solid ? Size.Width / 2 : 0;
                if (line.Up < line.Down)
                {
                    nodes.Add(new BezierSegment(
                        new Point(bottomXCoordinate, height * 3 / 4),
                        new Point(bottomXCoordinate, height / 2),
                        new Point(XOffset + line.Up * XDistance + offset, height / 2),
                        true
                    ));
                }
                else
                {
                    nodes.Add(new BezierSegment(
                        new Point(bottomXCoordinate, height * 3 / 4),
                        new Point(bottomXCoordinate, height / 2),
                        new Point(XOffset + line.Up * XDistance - offset, height / 2),
                        true
                    ));
                }
                geometry.Figures.Add(new PathFigure(new Point(bottomXCoordinate, height), nodes, false));
            }
        }

        private static void DrawTop(PathGeometry geometry, GitTreeNode node, double height)
        {
            if (node.Top is not Line line)
            {
                return;
            }

            double upperXCoordinate = XOffset + line.Up * XDistance;

            if (line.Up == line.Down)
            {
                double sameOffset = node.LineType == LineType.Solid ? Size.Height / 2 : 0;
                double middleXCoordinate = XOffset + line.Down * XDistance;
                double middleYCoordinate = height / 2 - sameOffset;
                geometry.Figures.Add(new PathFigure(new Point(upperXCoordinate, 0), new[] { new LineSegment(new Point(middleXCoordinate, middleYCoordinate), true) }, false));
                return;
            }
            BezierSegment bezierSegment;
            double offset = Size.Width / 2;

            bezierSegment = line.Up < line.Down
                ? new BezierSegment(
                    new Point(upperXCoordinate, height / 4),
                    new Point(upperXCoordinate, height / 2),
                    new Point(XOffset + line.Down * XDistance - offset, height / 2),
                    true
                )
                : new BezierSegment(
                    new Point(upperXCoordinate, height / 4),
                    new Point(upperXCoordinate, height / 2),
                    new Point(XOffset + line.Down * XDistance + offset, height / 2),
                    true
                );
            geometry.Figures.Add(new PathFigure(new Point(upperXCoordinate, 0), new[] { bezierSegment }, false));
        }

        public object[]? ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => null;

        private static PathGeometry GeometryFromTopToBottom(GitTreeNode node, double height)
        {
            if (node.Top is not Line topLayer)
            {
                throw new ArgumentException("no top layer when commit is missing", nameof(node));
            }

            if (node.Bottom is not Line bottomLayer)
            {
                throw new ArgumentException("no bottom layer when commit is missing", nameof(node));
            }

            if (topLayer.Up == bottomLayer.Down)
            {
                return new PathGeometry
                {
                    Figures =
                    {
                        new PathFigure(new Point(XOffset + XDistance * topLayer.Up, 0), new[] { new LineSegment(new Point(XOffset + XDistance * bottomLayer.Down, height), true) }, false)
                    }
                };
            }

            var pathFigure = new PathFigure(
                new Point(XOffset + node.Top.GetValueOrDefault().Up * XDistance, 0),
                new[]
                {
                    new BezierSegment(
                        new Point(XOffset + topLayer.Up * XDistance, height * 0.9),
                        new Point(XOffset + bottomLayer.Down * XDistance, height * 0.3),
                        new Point(XOffset + bottomLayer.Down * XDistance, height),
                        true
                    )
                },
                false);

            return new PathGeometry { Figures = { pathFigure } };
        }

    }
}
