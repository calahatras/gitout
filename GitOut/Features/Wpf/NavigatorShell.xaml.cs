using System;
using System.Windows;
using System.Windows.Input;

namespace GitOut.Features.Wpf
{
    public partial class NavigatorShell : Window
    {
        private StateNames resizeFromState;
        private Point lastPosition;
        private bool isResizing;

        public NavigatorShell(NavigatorShellViewModel dataContext)
        {
            InitializeComponent();
            DataContext = dataContext;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            GlassHelper.EnableBlur(this);
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            Point point = e.GetPosition(this);
            StateNames state = GetState(point);
            switch (state)
            {
                case StateNames.Normal:
                    try
                    {
                        DragMove();
                    }
                    catch { }
                    break;
                default:
                    {
                        Resize(state, point);
                    }
                    break;
            }
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonUp(e);
            isResizing = false;
            ReleaseMouseCapture();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            Point point = e.GetPosition(this);
            StateNames state = GetState(point);
            GoToState(state);

            switch (state)
            {
                case StateNames.ResizeTopLeft:
                case StateNames.ResizeBottomRight: Cursor = Cursors.SizeNWSE; break;
                case StateNames.ResizeBottomLeft: Cursor = Cursors.SizeNESW; break;
                case StateNames.ResizeLeft:
                case StateNames.ResizeRight: Cursor = Cursors.SizeWE; break;
                case StateNames.ResizeTop:
                case StateNames.ResizeBottom: Cursor = Cursors.SizeNS; break;
                case StateNames.Normal: Cursor = Cursors.Arrow; break;
            }
            if (isResizing)
            {
                if (!lastPosition.Equals(point))
                {
                    Vector delta = Point.Subtract(point, lastPosition);
                    switch (resizeFromState)
                    {
                        case StateNames.ResizeBottom:
                            if (Height + delta.Y >= MinHeight)
                            {
                                Height += delta.Y;
                            }
                            lastPosition = point;
                            break;
                        case StateNames.ResizeTop:
                            if (Height - delta.Y >= MinHeight)
                            {
                                Height -= delta.Y;
                                Top += delta.Y;
                            }
                            break;
                        case StateNames.ResizeLeft:
                            if (Width - delta.X >= MinWidth)
                            {
                                Width -= delta.X;
                                Left += delta.X;
                            }
                            break;
                        case StateNames.ResizeRight:
                            if (Width + delta.X >= MinWidth)
                            {
                                Width += delta.X;
                                lastPosition = point;
                            }
                            break;
                        case StateNames.ResizeBottomRight:
                            if (Width + delta.X >= MinWidth)
                            {
                                Width += delta.X;
                            }
                            if (Height + delta.Y >= MinHeight)
                            {
                                Height += delta.Y;
                            }
                            lastPosition = point;
                            break;
                        case StateNames.ResizeBottomLeft:
                            if (Width - delta.X >= MinWidth)
                            {
                                Width -= delta.X;
                                Left += delta.X;
                            }
                            if (Height + delta.Y >= MinHeight)
                            {
                                Height += delta.Y;
                            }
                            lastPosition = new Point(0, point.Y);
                            break;
                        case StateNames.ResizeTopLeft:
                            if (Height - delta.Y >= MinHeight)
                            {
                                Height -= delta.Y;
                                Top += delta.Y;
                            }
                            if (Width - delta.X >= MinWidth)
                            {
                                Width -= delta.X;
                                Left += delta.X;
                            }
                            break;
                    }
                }
            }
        }

        private void Resize(StateNames fromstate, Point origin)
        {
            resizeFromState = fromstate;
            lastPosition = origin;
            isResizing = true;
            CaptureMouse();
        }

        private StateNames GetState(Point mp)
        {
            const int epsilon = 7;
            if (mp.X < epsilon)
            {
                if (mp.Y < epsilon)
                {
                    return StateNames.ResizeTopLeft;
                }
                else if (mp.Y > Height - epsilon)
                {
                    return StateNames.ResizeBottomLeft;
                }
                else
                {
                    return StateNames.ResizeLeft;
                }
            }
            else if (mp.X > Width - epsilon)
            {
                if (mp.Y > Height - epsilon)
                {
                    return StateNames.ResizeBottomRight;
                }
                else
                {
                    return StateNames.ResizeRight;
                }
            }
            else if (mp.Y < epsilon)
            {
                return StateNames.ResizeTop;
            }
            else if (mp.Y > Height - epsilon)
            {
                return StateNames.ResizeBottom;
            }
            return StateNames.Normal;
        }

        private void GoToState(StateNames state) => VisualStateManager.GoToElementState(PART_Root, state.ToString(), true);

        public enum StateNames
        {
            None,
            Normal,
            ResizeTop,
            ResizeRight,
            ResizeBottomRight,
            ResizeBottom,
            ResizeBottomLeft,
            ResizeLeft,
            ResizeTopLeft,
        }
    }
}
