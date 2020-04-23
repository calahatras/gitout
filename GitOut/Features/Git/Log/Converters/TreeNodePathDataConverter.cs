using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace GitOut.Features.Git.Log.Converters
{
    public class TreeNodePathDataConverter : IMultiValueConverter
    {
        private const int XDistance = 15;
        private const int XOffset = 10;
        private static readonly Size Size = new Size(6, 6);

        public object Convert(object[] value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value.Length != 2
                || !(value[0] is GitTreeNode node)
                || !(value[1] is double height))
            {
                return DependencyProperty.UnsetValue;
            }

            if (!node.IsCommit)
            {
                return DrawBoth(node, height);
            }

            var geometry = new PathGeometry();
            if (node.Bottom is Line downLine && downLine.Up == downLine.Down)
            {
                geometry.AddGeometry(new EllipseGeometry(new Point(downLine.Up * XDistance + XOffset, height / 2), Size.Height / 2, Size.Width / 2));
            }
            else if (node.Top is Line upLine && upLine.Up == upLine.Down)
            {
                geometry.AddGeometry(new EllipseGeometry(new Point(upLine.Up * XDistance + XOffset, height / 2), Size.Height / 2, Size.Width / 2));
            }

            DrawTop(geometry, node, height);
            DrawBottom(geometry, node, height);
            return geometry;
        }

        public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();

        private void DrawBottom(PathGeometry geometry, GitTreeNode node, double height)
        {
            if (!(node.Bottom is Line line))
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
                double offset = node.IsCommit ? Size.Width / 2 : 0;
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

        private void DrawTop(PathGeometry geometry, GitTreeNode node, double height)
        {
            if (!(node.Top is Line line))
            {
                return;
            }

            double upperXCoordinate = XOffset + line.Up * XDistance;

            if (line.Up == line.Down)
            {
                double sameOffset = node.IsCommit ? Size.Height / 2 : 0;
                double middleXCoordinate = XOffset + line.Down * XDistance;
                double middleYCoordinate = height / 2 - sameOffset;
                geometry.Figures.Add(new PathFigure(new Point(upperXCoordinate, 0), new[] { new LineSegment(new Point(middleXCoordinate, middleYCoordinate), true) }, false));
                return;
            }
            BezierSegment bezierSegment;
            double offset = Size.Width / 2;

            if (line.Up < line.Down)
            {
                bezierSegment = new BezierSegment(
                    new Point(upperXCoordinate, height / 4),
                    new Point(upperXCoordinate, height / 2),
                    new Point(XOffset + line.Down * XDistance - offset, height / 2),
                    true
                );
            }
            else
            {
                bezierSegment = new BezierSegment(
                    new Point(upperXCoordinate, height / 4),
                    new Point(upperXCoordinate, height / 2),
                    new Point(XOffset + line.Down * XDistance + offset, height / 2),
                    true
                );
            }
            geometry.Figures.Add(new PathFigure(new Point(upperXCoordinate, 0), new[] { bezierSegment }, false));
        }

        private PathGeometry DrawBoth(GitTreeNode node, double height)
        {
            if (!(node.Top is Line topLayer))
            {
                throw new ArgumentException("no top layer when commit is missing", nameof(node));
            }

            if (!(node.Bottom is Line bottomLayer))
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
