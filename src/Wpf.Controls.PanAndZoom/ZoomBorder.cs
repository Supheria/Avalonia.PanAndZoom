﻿// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using static System.Math;
using PanAndZoom;

namespace Wpf.Controls.PanAndZoom
{
    /// <summary>
    /// Pan and zoom control for WPF.
    /// </summary>
    public class ZoomBorder : Border
    {
        private static AutoFitMode[] _autoFitModes = (AutoFitMode[])Enum.GetValues(typeof(AutoFitMode));

        /// <summary>
        /// Gets available auto-fit modes.
        /// </summary>
        public static AutoFitMode[] AutoFitModes => _autoFitModes;

        private UIElement _element;
        private Point _pan;
        private Point _previous;
        private Matrix _matrix;
        private bool _isPanning;

        /// <summary>
        /// Gets or sets invalidate action for border child element.
        /// </summary>
        public Action<double, double, double, double> InvalidatedChild { get; set; }

        /// <summary>
        /// Gets or sets zoom speed ratio.
        /// </summary>
        public double ZoomSpeed
        {
            get { return (double)GetValue(ZoomSpeedProperty); }
            set { SetValue(ZoomSpeedProperty, value); }
        }

        /// <summary>
        /// Gets or sets auto-fit mode.
        /// </summary>
        public AutoFitMode AutoFitMode
        {
            get { return (AutoFitMode)GetValue(AutoFitModeProperty); }
            set { SetValue(AutoFitModeProperty, value); }
        }

        /// <summary>
        /// Identifies the <seealso cref="ZoomSpeed"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ZoomSpeedProperty =
            DependencyProperty.Register(
                nameof(ZoomSpeed),
                typeof(double),
                typeof(ZoomBorder),
                new FrameworkPropertyMetadata(1.2, FrameworkPropertyMetadataOptions.AffectsArrange));

        /// <summary>
        /// Identifies the <seealso cref="AutoFitMode"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty AutoFitModeProperty =
            DependencyProperty.Register(
                nameof(AutoFitMode),
                typeof(AutoFitMode),
                typeof(ZoomBorder),
                new FrameworkPropertyMetadata(AutoFitMode.Extent, FrameworkPropertyMetadataOptions.AffectsArrange));

        /// <summary>
        /// Initializes a new instance of the <see cref="ZoomBorder"/> class.
        /// </summary>
        public ZoomBorder()
            : base()
        {
            _isPanning = false;
            _matrix = MatrixHelper.Identity;

            ZoomSpeed = 1.2;
            AutoFitMode = AutoFitMode.None;

            Focusable = true;
            Background = Brushes.Transparent;

            Unloaded += PanAndZoom_Unloaded;
        }

        private void PanAndZoom_Unloaded(object sender, RoutedEventArgs e)
        {
            if (_element != null)
            {
                Unload();
            }
        }

        /// <summary>
        /// Gets or sets single child of a <see cref="ZoomBorder"/> control.
        /// </summary>
        public override UIElement Child
        {
            get { return base.Child; }
            set
            {
                if (value != null && value != _element && _element != null)
                {
                    Unload();
                }

                base.Child = value;

                if (value != null && value != _element)
                {
                    Initialize(value);
                }
            }
        }

        private void Initialize(UIElement element)
        {
            if (element != null)
            {
                _element = element;
                this.Focus();
                this.PreviewMouseWheel += Border_PreviewMouseWheel;
                this.PreviewMouseRightButtonDown += Border_PreviewMouseRightButtonDown;
                this.PreviewMouseRightButtonUp += Border_PreviewMouseRightButtonUp;
                this.PreviewMouseMove += Border_PreviewMouseMove;
            }
        }

        private void Unload()
        {
            if (_element != null)
            {
                this.PreviewMouseWheel -= Border_PreviewMouseWheel;
                this.PreviewMouseRightButtonDown -= Border_PreviewMouseRightButtonDown;
                this.PreviewMouseRightButtonUp -= Border_PreviewMouseRightButtonUp;
                this.PreviewMouseMove -= Border_PreviewMouseMove;
                _element.RenderTransform = null;
                _element = null;
            }
        }

        private void Border_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (_element != null && Mouse.Captured == null)
            {
                Point point = e.GetPosition(_element);
                ZoomDeltaTo(e.Delta, point);
                //e.Handled = true;
            }
        }

        private void Border_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_element != null && Mouse.Captured == null && _isPanning == false)
            {
                Point point = e.GetPosition(_element);
                StartPan(point);
                _element.CaptureMouse();
                //e.Handled = true;
                _isPanning = true;
            }
        }

        private void Border_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_element != null && _element.IsMouseCaptured == true && _isPanning == true)
            {
                _element.ReleaseMouseCapture();
                //e.Handled = true;
                _isPanning = false;
            }
        }

        private void Border_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (_element != null && _element.IsMouseCaptured == true && _isPanning == true)
            {
                Point point = e.GetPosition(_element);
                PanTo(point);
                //e.Handled = true;
            }
        }

        /// <summary>
        /// Arranges the control's child.
        /// </summary>
        /// <param name="finalSize">The size allocated to the control.</param>
        /// <returns>The space taken.</returns>
        protected override Size ArrangeOverride(Size finalSize)
        {
            if (_element != null && _element.IsMeasureValid)
            {
                AutoFit(this.DesiredSize, _element.DesiredSize);
            }

            return base.ArrangeOverride(finalSize);
        }

        /// <summary>
        /// Invalidate child element.
        /// </summary>
        public void Invalidate()
        {
            if (_element != null)
            {
                this.InvalidatedChild?.Invoke(_matrix.M11, _matrix.M12, _matrix.OffsetX, _matrix.OffsetY);
                _element.RenderTransformOrigin = new Point(0, 0);
                _element.RenderTransform = new MatrixTransform(_matrix);
                _element.InvalidateVisual();
            }
        }

        /// <summary>
        /// Zoom to provided zoom ratio and provided center point.
        /// </summary>
        /// <param name="zoom">The zoom ratio.</param>
        /// <param name="point">The center point.</param>
        public void ZoomTo(double zoom, Point point)
        {
            _matrix = MatrixHelper.ScaleAtPrepend(_matrix, zoom, zoom, point.X, point.Y);

            Invalidate();
        }

        /// <summary>
        /// Zoom to provided zoom delta ratio and provided center point.
        /// </summary>
        /// <param name="delta">The zoom delta ratio.</param>
        /// <param name="point">The center point.</param>
        public void ZoomDeltaTo(int delta, Point point)
        {
            ZoomTo(delta > 0 ? ZoomSpeed : 1 / ZoomSpeed, point);
        }

        /// <summary>
        /// Set pan origin.
        /// </summary>
        /// <param name="point">The pan origin position.</param>
        public void StartPan(Point point)
        {
            _pan = new Point();
            _previous = new Point(point.X, point.Y);
        }

        /// <summary>
        /// Pan control to provided position.
        /// </summary>
        /// <param name="point">The pan destination position.</param>
        public void PanTo(Point point)
        {
            Point delta = new Point(point.X - _previous.X, point.Y - _previous.Y);
            _previous = new Point(point.X, point.Y);

            _pan = new Point(_pan.X + delta.X, _pan.Y + delta.Y);
            _matrix = MatrixHelper.TranslatePrepend(_matrix, _pan.X, _pan.Y);

            Invalidate();
        }

        /// <summary>
        /// Zoom and pan to panel extents while maintaining aspect ratio.
        /// </summary>
        /// <param name="panelSize">The panel size.</param>
        /// <param name="elementSize">The element size.</param>
        public void Extent(Size panelSize, Size elementSize)
        {
            if (_element != null)
            {
                double pw = panelSize.Width;
                double ph = panelSize.Height;
                double ew = elementSize.Width;
                double eh = elementSize.Height;
                double zx = pw / ew;
                double zy = ph / eh;
                double zoom = Min(zx, zy);
                double cx = ew / 2.0;
                double cy = eh / 2.0;

                _matrix = MatrixHelper.ScaleAt(zoom, zoom, cx, cy);

                Invalidate();
            }
        }

        /// <summary>
        /// Zoom and pan to fill panel.
        /// </summary>
        /// <param name="panelSize">The panel size.</param>
        /// <param name="elementSize">The element size.</param>
        public void Fill(Size panelSize, Size elementSize)
        {
            if (_element != null)
            {
                double pw = panelSize.Width;
                double ph = panelSize.Height;
                double ew = elementSize.Width;
                double eh = elementSize.Height;
                double zx = pw / ew;
                double zy = ph / eh;

                _matrix = MatrixHelper.ScaleAt(zx, zy, ew / 2.0, eh / 2.0);

                Invalidate();
            }
        }

        /// <summary>
        /// Zoom and pan child elemnt inside panel using auto-fit mode.
        /// </summary>
        /// <param name="panelSize">The panel size.</param>
        /// <param name="elementSize">The element size.</param>
        public void AutoFit(Size panelSize, Size elementSize)
        {
            if (_element != null)
            {
                switch (AutoFitMode)
                {
                    case AutoFitMode.Extent:
                        Extent(panelSize, elementSize);
                        break;
                    case AutoFitMode.Fill:
                        Fill(panelSize, elementSize);
                        break;
                }

                Invalidate();
            }
        }

        /// <summary>
        /// Set next auto-fit mode.
        /// </summary>
        public void ToggleAutoFitMode()
        {
            switch (AutoFitMode)
            {
                case AutoFitMode.None:
                    AutoFitMode = AutoFitMode.Extent;
                    break;
                case AutoFitMode.Extent:
                    AutoFitMode = AutoFitMode.Fill;
                    break;
                case AutoFitMode.Fill:
                    AutoFitMode = AutoFitMode.None;
                    break;
            }
        }

        /// <summary>
        /// Reset pan and zoom matrix.
        /// </summary>
        public void Reset()
        {
            _matrix = MatrixHelper.Identity;

            Invalidate();
        }

        /// <summary>
        /// Zoom and pan to panel extents while maintaining aspect ratio.
        /// </summary>
        public void Extent()
        {
            Extent(this.DesiredSize, _element.RenderSize);
        }

        /// <summary>
        /// Zoom and pan to fill panel.
        /// </summary>
        public void Fill()
        {
            Fill(this.DesiredSize, _element.RenderSize);
        }

        /// <summary>
        /// Zoom and pan child elemnt inside panel using auto-fit mode.
        /// </summary>
        public void AutoFit()
        {
            if (_element != null)
            {
                AutoFit(this.DesiredSize, _element.RenderSize);
            }
        }
    }
}
