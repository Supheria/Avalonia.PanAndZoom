using System;
using System.Diagnostics;
using Avalonia.Controls.Metadata;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Transformation;
using Avalonia.Reactive;
using static System.Math;

namespace Avalonia.Controls.PanAndZoom;

/// <summary>
/// Pan and zoom control for Avalonia.
/// </summary>

[PseudoClasses(":isPanning")]
public partial class ZoomBorder : Border
{
    [Conditional("DEBUG")]
    private static void Log(string message) { }

    private static double ClampValue(double value, double minimum, double maximum)
    {
        if (minimum > maximum)
            throw new ArgumentException(
                $"Parameter {nameof(minimum)} is greater than {nameof(maximum)}."
            );

        if (maximum < minimum)
            throw new ArgumentException(
                $"Parameter {nameof(maximum)} is lower than {nameof(minimum)}."
            );

        return Min(Max(value, minimum), maximum);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ZoomBorder"/> class.
    /// </summary>
    public ZoomBorder()
    {
        _isPanning = false;
        _matrix = Matrix.Identity;
        _captured = false;

        Focusable = true;
        Background = Brushes.Transparent;

        AttachedToVisualTree += PanAndZoom_AttachedToVisualTree;
        DetachedFromVisualTree += PanAndZoom_DetachedFromVisualTree;

        this.GetObservable(ChildProperty).Subscribe(new AnonymousObserver<Control?>(ChildChanged));

        GestureRecognizers.Add(new PinchGestureRecognizer());
    }

    private void PanAndZoom_AttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        Log($"[AttachedToVisualTree] {Name}");
        ChildChanged(Child);

        _updating = true;
        Invalidate(skipTransitions: false);
        _updating = false;
    }

    private void PanAndZoom_DetachedFromVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        Log($"[DetachedFromVisualTree] {Name}");
        DetachElement();
    }

    private void BorderOnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (!EnableZoom)
        {
            return;
        }
        Wheel(e);
    }

    private void BorderOnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        Pressed(e);
    }

    private void BorderOnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        Released(e);
    }

    private void BorderOnPointerMoved(object? sender, PointerEventArgs e)
    {
        Moved(e);
    }
    

    private void ChildChanged(Control? element)
    {
        Log($"[ChildChanged] {element}");

        if (element != null && element != _element && _element != null)
        {
            DetachElement();
        }

        if (element != null && element != _element)
        {
            AttachElement(element);
        }
    }

    private void AttachElement(Control? element)
    {
        if (element == null)
        {
            return;
        }
        _element = element;
        PointerWheelChanged += BorderOnPointerWheelChanged;
        PointerPressed += BorderOnPointerPressed;
        PointerReleased += BorderOnPointerReleased;
        PointerMoved += BorderOnPointerMoved;
        AddHandler(DoubleTappedEvent, BorderOnDoubleTapped);
        AddHandler(Gestures.PinchEvent, GestureOnPinch);
        AddHandler(Gestures.PinchEndedEvent, GestureOnPinchEnded);
    }

    private void DetachElement()
    {
        if (_element == null)
        {
            return;
        }

        PointerWheelChanged -= BorderOnPointerWheelChanged;
        PointerPressed -= BorderOnPointerPressed;
        PointerReleased -= BorderOnPointerReleased;
        PointerMoved -= BorderOnPointerMoved;
        RemoveHandler(DoubleTappedEvent, BorderOnDoubleTapped);
        RemoveHandler(Gestures.PinchEvent, GestureOnPinch);
        RemoveHandler(Gestures.PinchEndedEvent, GestureOnPinchEnded);
        _element.RenderTransform = null;
        _element = null;
    }

    private void BorderOnDoubleTapped(object? sender, TappedEventArgs e)
    {
        var point = e.GetPosition(_element);
        if (_element?.Bounds.Contains(point) is not true)
            return;
        if (_zoomOut)
        {
            ZoomDeltaTo(-ZoomInRatio, point.X, point.Y);
            _zoomOut = false;
        }
        else
        {
            ZoomDeltaTo(ZoomInRatio, point.X, point.Y);
            _zoomOut = true;
        }
    }

    private void Wheel(PointerWheelEventArgs e)
    {
        if (_element == null || _captured)
        {
            return;
        }
        var point = e.GetPosition(_element);
        ZoomDeltaTo(e.Delta.Y, point.X, point.Y);
    }

    private void GestureOnPinch(object? sender, PinchEventArgs e)
    {
        if (!EnableZoom)
            return;
        if (_element == null)
            return;
        _isPanning = false;
        _captured = false;
        var point = _element.PointToClient(this.PointToScreen(e.ScaleOrigin));
        var dScale = e.Scale - _scale;
        ZoomDeltaTo(dScale * GesturePinchSpeed, point.X, point.Y);
        _scale = e.Scale;
    }

    private void GestureOnPinchEnded(object? sender, PinchEndedEventArgs e)
    {
        _scale = 1;
    }

    private void Pressed(PointerPressedEventArgs e)
    {
        if (!EnablePan)
            return;
        if (_element == null || _captured != false || _isPanning != false)
            return;
        var point = e.GetPosition(_element);
        BeginPanTo(point.X, point.Y);
        _captured = true;
        _isPanning = true;
        SetPseudoClass(":isPanning", _isPanning);
    }

    private void Released(PointerReleasedEventArgs e)
    {
        if (!EnablePan)
            return;
        if (_element == null || _captured != true || _isPanning != true)
            return;
        _captured = false;
        _isPanning = false;
        SetPseudoClass(":isPanning", _isPanning);
    }

    private void Moved(PointerEventArgs e)
    {
        if (!EnablePan)
            return;
        if (_element == null || _captured != true || _isPanning != true)
            return;
        var point = e.GetPosition(_element);
        ContinuePanTo(point.X, point.Y, true);
    }

    /// <summary>
    /// Raises <see cref="ZoomChanged"/> event.
    /// </summary>
    /// <param name="e">Zoom changed event arguments.</param>
    protected virtual void OnZoomChanged(ZoomChangedEventArgs e)
    {
        ZoomChanged?.Invoke(this, e);
    }

    private void RaiseZoomChanged()
    {
        var args = new ZoomChangedEventArgs(_zoomX, _zoomY, _offsetX, _offsetY);
        OnZoomChanged(args);
    }

    private void Constrain()
    {
        var zoomX = ClampValue(_matrix.M11, MinZoomX, MaxZoomX);
        var zoomY = ClampValue(_matrix.M22, MinZoomY, MaxZoomY);
        var offsetX = ClampValue(_matrix.M31, MinOffsetX, MaxOffsetX);
        var offsetY = ClampValue(_matrix.M32, MinOffsetY, MaxOffsetY);
        _matrix = new Matrix(zoomX, 0.0, 0.0, zoomY, offsetX, offsetY);
    }

    /// <summary>
    /// Invalidate pan and zoom control.
    /// </summary>
    /// <param name="skipTransitions">The flag indicating whether transitions on the child element should be temporarily disabled.</param>
    private void Invalidate(bool skipTransitions = false)
    {
        Log("[Invalidate] Begin");

        if (_element == null)
        {
            Log("[Invalidate] End");
            return;
        }

        if (EnableConstrains)
        {
            Constrain();
        }

        InvalidateProperties();
        InvalidateElement(skipTransitions);
        RaiseZoomChanged();

        Log("[Invalidate] End");
    }

    /// <summary>
    /// Invalidate properties.
    /// </summary>
    private void InvalidateProperties()
    {
        SetAndRaise(ZoomXProperty, ref _zoomX, _matrix.M11);
        SetAndRaise(ZoomYProperty, ref _zoomY, _matrix.M22);
        SetAndRaise(OffsetXProperty, ref _offsetX, _matrix.M31);
        SetAndRaise(OffsetYProperty, ref _offsetY, _matrix.M32);
    }

    /// <summary>
    /// Invalidate child element.
    /// </summary>
    /// <param name="skipTransitions">The flag indicating whether transitions on the child element should be temporarily disabled.</param>
    private void InvalidateElement(bool skipTransitions)
    {
        if (_element == null)
        {
            return;
        }

        Animation.Transitions? backupTransitions = null;

        if (skipTransitions)
        {
            Animation.Animatable? anim = _element as Animation.Animatable;

            if (anim != null)
            {
                backupTransitions = anim.Transitions;
                anim.Transitions = null;
            }
        }

        _element.RenderTransformOrigin = new RelativePoint(new Point(0, 0), RelativeUnit.Relative);
        _transformBuilder = new TransformOperations.Builder(1);
        _transformBuilder.AppendMatrix(_matrix);
        _element.RenderTransform = _transformBuilder.Build();

        if (skipTransitions && backupTransitions != null)
        {
            Animation.Animatable? anim = _element as Animation.Animatable;

            if (anim != null)
            {
                anim.Transitions = backupTransitions;
            }
        }

        _element.InvalidateVisual();
    }

    /// <summary>
    /// Set pan and zoom matrix.
    /// </summary>
    /// <param name="matrix">The matrix to set as current.</param>
    /// <param name="skipTransitions">The flag indicating whether transitions on the child element should be temporarily disabled.</param>
    public void SetMatrix(Matrix matrix, bool skipTransitions = false)
    {
        if (_updating)
        {
            return;
        }
        _updating = true;

        Log("[SetMatrix]");
        _matrix = matrix;
        Invalidate(skipTransitions);

        _updating = false;
    }

    /// <summary>
    /// Reset pan and zoom matrix.
    /// </summary>
    public void ResetMatrix()
    {
        SetMatrix(Matrix.Identity);
    }

    /// <summary>
    /// Zoom to provided zoom value and provided center point.
    /// </summary>
    /// <param name="zoom">The zoom value.</param>
    /// <param name="x">The center point x axis coordinate.</param>
    /// <param name="y">The center point y axis coordinate.</param>
    /// <param name="skipTransitions">The flag indicating whether transitions on the child element should be temporarily disabled.</param>
    public void Zoom(double zoomX, double zoomY, double x, double y, bool skipTransitions = false)
    {
        if (_updating)
        {
            return;
        }
        _updating = true;

        Log("[Zoom]");
        _matrix = MatrixHelper.ScaleAt(zoomX, zoomY, x, y);
        Invalidate(skipTransitions);

        _updating = false;
    }

    /// <summary>
    /// Zoom to provided zoom ratio and provided center point.
    /// </summary>
    /// <param name="ratio">The zoom ratio.</param>
    /// <param name="x">The center point x axis coordinate.</param>
    /// <param name="y">The center point y axis coordinate.</param>
    /// <param name="skipTransitions">The flag indicating whether transitions on the child element should be temporarily disabled.</param>
    public void ZoomTo(double ratio, double x, double y, bool skipTransitions = false)
    {
        if (_updating)
        {
            return;
        }

        if (
            (ZoomX >= MaxZoomX && ZoomY >= MaxZoomY && ratio > 1)
            || (ZoomX <= MinZoomX && ZoomY <= MinZoomY && ratio < 1)
        )
        {
            return;
        }

        _updating = true;

        Log("[ZoomTo]");
        _matrix = MatrixHelper.ScaleAtPrepend(_matrix, ratio, ratio, x, y);
        Invalidate(skipTransitions);

        _updating = false;
    }

    /// <summary>
    /// Zoom in one step positive delta ratio and panel center point.
    /// </summary>
    /// <param name="skipTransitions">The flag indicating whether transitions on the child element should be temporarily disabled.</param>
    public void ZoomIn(double x, double y, bool skipTransitions = false)
    {
        if (_element == null)
        {
            return;
        }
        ZoomTo(ZoomSpeed, x, y, skipTransitions);
    }

    /// <summary>
    /// Zoom out one step positive delta ratio and panel center point.
    /// </summary>
    /// <param name="skipTransitions">The flag indicating whether transitions on the child element should be temporarily disabled.</param>
    public void ZoomOut(bool skipTransitions = false)
    {
        if (_element == null)
        {
            return;
        }

        var x = _element.Bounds.Width / 2.0;
        var y = _element.Bounds.Height / 2.0;
        ZoomTo(1 / ZoomSpeed, x, y, skipTransitions);
    }

    /// <summary>
    /// Zoom to provided zoom delta ratio and provided center point.
    /// </summary>
    /// <param name="delta">The zoom delta ratio.</param>
    /// <param name="x">The center point x axis coordinate.</param>
    /// <param name="y">The center point y axis coordinate.</param>
    /// <param name="skipTransitions">The flag indicating whether transitions on the child element should be temporarily disabled.</param>
    public void ZoomDeltaTo(double delta, double x, double y, bool skipTransitions = false)
    {
        double realDelta = Sign(delta) * Pow(Abs(delta), PowerFactor);
        ZoomTo(
            Pow(ZoomSpeed, realDelta),
            x,
            y,
            skipTransitions || Abs(realDelta) <= TransitionThreshold
        );
    }

    /// <summary>
    /// Pan control to provided delta.
    /// </summary>
    /// <param name="dx">The target x axis delta.</param>
    /// <param name="dy">The target y axis delta.</param>
    /// <param name="skipTransitions">The flag indicating whether transitions on the child element should be temporarily disabled.</param>
    public void PanDelta(double dx, double dy, bool skipTransitions = false)
    {
        if (_updating)
        {
            return;
        }
        _updating = true;

        Log("[PanDelta]");
        _matrix = MatrixHelper.ScaleAndTranslate(
            _zoomX,
            _zoomY,
            _matrix.M31 + dx,
            _matrix.M32 + dy
        );
        Invalidate(skipTransitions);

        _updating = false;
    }

    /// <summary>
    /// Pan control to provided target point.
    /// </summary>
    /// <param name="x">The target point x axis coordinate.</param>
    /// <param name="y">The target point y axis coordinate.</param>
    /// <param name="skipTransitions">The flag indicating whether transitions on the child element should be temporarily disabled.</param>
    public void Pan(double x, double y, bool skipTransitions = false)
    {
        if (_updating)
        {
            return;
        }
        _updating = true;

        Log("[Pan]");
        _matrix = MatrixHelper.ScaleAndTranslate(_zoomX, _zoomY, x, y);
        Invalidate(skipTransitions);

        _updating = false;
    }

    /// <summary>
    /// Set pan origin.
    /// </summary>
    /// <param name="x">The origin point x axis coordinate.</param>
    /// <param name="y">The origin point y axis coordinate.</param>
    public void BeginPanTo(double x, double y)
    {
        _pan = new Point();
        _previous = new Point(x, y);
    }

    /// <summary>
    /// Continue pan to provided target point.
    /// </summary>
    /// <param name="x">The target point x axis coordinate.</param>
    /// <param name="y">The target point y axis coordinate.</param>
    /// <param name="skipTransitions">The flag indicating whether transitions on the child element should be temporarily disabled.</param>
    public void ContinuePanTo(double x, double y, bool skipTransitions = false)
    {
        if (_updating)
        {
            return;
        }
        _updating = true;

        Log("[ContinuePanTo]");
        var dx = x - _previous.X;
        var dy = y - _previous.Y;
        var delta = new Point(dx, dy);
        _previous = new Point(x, y);
        _pan = new Point(_pan.X + delta.X, _pan.Y + delta.Y);
        _matrix = MatrixHelper.TranslatePrepend(_matrix, _pan.X, _pan.Y);
        Invalidate(skipTransitions);

        _updating = false;
    }

    private void SetPseudoClass(string name, bool flag) => PseudoClasses.Set(name, flag);
}
