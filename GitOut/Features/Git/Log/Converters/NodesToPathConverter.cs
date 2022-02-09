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
                values.Length != 2
                || values[0] is not IEnumerable<GitTreeNode> nodes
                || values[1] is not double height
            )
            {
                return DependencyProperty.UnsetValue;
            }

            List<Path> paths = new();
            foreach (GitTreeNode? node in nodes)
            {
                if (!node.IsCommit)
                {
                    paths.Add(CreatePath(
                        color: node.Color,
                        useDashedLine: node.BottomLineType == LineType.Dashed,
                        geometry: CreateTopToBottomGeometry(node, height)
                    ));
                }
                else
                {
                    Path? commitPath = AddCommitGeometry(node, height);
                    if (commitPath is not null)
                    {
                        paths.Add(commitPath);
                    }

                    if (node.Top is Line upperLine)
                    {
                        paths.Add(CreatePath(
                            color: node.Color,
                            useDashedLine: node.TopLineType == LineType.Dashed,
                            geometry: CreateUpperGeometry(upperLine, height)
                        ));
                    }
                    if (node.Bottom is Line lowerLine)
                    {
                        paths.Add(CreatePath(
                            color: node.Color,
                            useDashedLine: node.BottomLineType == LineType.Dashed,
                            geometry: CreateLowerGeometry(lowerLine, height)
                        ));
                    }
                }
            }

            return paths.ToArray();
        }

        public object[]? ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => null;

        private static Path CreatePath(Color color, bool useDashedLine, PathGeometry geometry) => new Path()
        {
            Stroke = new SolidColorBrush(color),
            StrokeThickness = 2,
            SnapsToDevicePixels = true,
            Data = geometry,
            StrokeDashArray = useDashedLine ? new DoubleCollection(new[] { 3d, 1 }) : null
        };

        private static Path? AddCommitGeometry(GitTreeNode node, double height)
        {
            int index = GetCommitIndex(node);
            return index == -1
                ? null
                : CreatePath(
                    node.Color,
                    false,
                    node.BottomLineType == LineType.Solid
                        ? CreateCommitGeometry(height, index)
                        : CreateStashGeometry(index, height)
                );
        }

        private static int GetCommitIndex(GitTreeNode node) =>
            node.Bottom is Line bottomLine && bottomLine.Up == bottomLine.Down
                ? bottomLine.Up
                : node.Top is Line topLine && topLine.Up == topLine.Down
                    ? topLine.Up
                    : -1;

        private static PathGeometry CreateCommitGeometry(double height, int index)
        {
            var pathGeometry = new PathGeometry();
            pathGeometry.AddGeometry(new EllipseGeometry(new Point(index * XDistance + XOffset, height / 2), Size.Height / 2, Size.Width / 2));
            return pathGeometry;
        }

        private static PathGeometry CreateStashGeometry(int index, double height)
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
                true
            );
            return new PathGeometry(new[] { pathFigure });
        }

        private static PathGeometry CreateLowerGeometry(Line line, double height)
        {
            PathGeometry geometry = new();
            double bottomXCoordinate = XOffset + line.Down * XDistance;
            double offset = Size.Height / 2;

            if (line.Up == line.Down)
            {
                double middleXCoordinate = XOffset + line.Up * XDistance;
                double middleYCoordinate = height / 2 + offset;

                geometry.Figures.Add(new PathFigure(new Point(middleXCoordinate, middleYCoordinate), new[] { new LineSegment(new Point(bottomXCoordinate, height), true) }, false));
                return geometry;
            }

            BezierSegment bezierSegment = line.Up < line.Down
                ? new BezierSegment(
                    new Point(bottomXCoordinate, height * 3 / 4),
                    new Point(bottomXCoordinate, height / 2),
                    new Point(XOffset + line.Up * XDistance + offset + 1, height / 2),
                    true
                )
                : new BezierSegment(
                    new Point(bottomXCoordinate, height * 3 / 4),
                    new Point(bottomXCoordinate, height / 2),
                    new Point(XOffset + line.Up * XDistance - offset, height / 2),
                    true
                );
            geometry.Figures.Add(new PathFigure(new Point(bottomXCoordinate, height), new[] { bezierSegment }, false));
            return geometry;
        }

        private static PathGeometry CreateUpperGeometry(Line line, double height)
        {
            var geometry = new PathGeometry();
            double upperXCoordinate = XOffset + line.Up * XDistance;
            double offset = Size.Height / 2 + 1;

            if (line.Up == line.Down)
            {
                double middleXCoordinate = XOffset + line.Down * XDistance;
                double middleYCoordinate = height / 2 - offset + 1;
                geometry.Figures.Add(new PathFigure(new Point(upperXCoordinate, 0), new[] { new LineSegment(new Point(middleXCoordinate, middleYCoordinate), true) }, false));
                return geometry;
            }

            BezierSegment bezierSegment = line.Up < line.Down
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
            return geometry;
        }

        private static PathGeometry CreateTopToBottomGeometry(GitTreeNode node, double height)
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
