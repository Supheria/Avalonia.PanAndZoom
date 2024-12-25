using System;
using Avalonia.Animation;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Transformation;
using Avalonia.Reactive;
using static System.Math;

namespace Avalonia.Controls.PanAndZoom;

/// <summary>
/// Pan and zoom control for Avalonia.
/// </summary>
public sealed partial class ZoomBorder : Border
{
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
        IsPanning = false;
        Matrix = Matrix.Identity;
        Captured = false;

        Focusable = true;
        Background = Brushes.Transparent;

        this.GetObservable(ChildProperty).Subscribe(new AnonymousObserver<Control?>(ChildChanged));

        GestureRecognizers.Add(new PinchGestureRecognizer());
    }

    /// <inheritdoc />
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        ChildChanged(Child);

        _updating = true;
        Invalidate();
        _updating = false;
    }

    /// <inheritdoc />
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        DetachElement();
    }

    private void ChildChanged(Control? element)
    {
        if (element != null && element != Element && Element != null)
            DetachElement();
        if (element != null && element != Element)
            AttachElement(element);
    }

    private void AttachElement(Control? element)
    {
        if (element == null)
            return;
        Element = element;
        AddHandler(DoubleTappedEvent, BorderOnDoubleTapped);
        AddHandler(Gestures.PinchEvent, GestureOnPinch);
        AddHandler(Gestures.PinchEndedEvent, GestureOnPinchEnded);
    }

    private void DetachElement()
    {
        if (Element == null)
            return;
        Element.RenderTransform = null;
        Element = null;
        RemoveHandler(DoubleTappedEvent, BorderOnDoubleTapped);
        RemoveHandler(Gestures.PinchEvent, GestureOnPinch);
        RemoveHandler(Gestures.PinchEndedEvent, GestureOnPinchEnded);
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        if (!EnableZoom)
            return;
        if (Element == null || Captured)
            return;
        var point = e.GetPosition(Element);
        ZoomDeltaTo(e.Delta.Y, point.X, point.Y);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (!EnablePan)
            return;
        if (Element == null || Captured != false || IsPanning != false)
            return;
        var point = e.GetPosition(Element);
        BeginPanTo(point.X, point.Y);
        Captured = true;
        IsPanning = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (!EnablePan)
            return;
        if (Element == null || Captured != true || IsPanning != true)
            return;
        var point = e.GetPosition(Element);
        ContinuePanTo(point.X, point.Y, true);
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (!EnablePan)
            return;
        if (Element == null || Captured != true || IsPanning != true)
            return;
        Captured = false;
        IsPanning = false;
    }

    private void BorderOnDoubleTapped(object? sender, TappedEventArgs e)
    {
        var point = e.GetPosition(Element);
        if (Element?.Bounds.Contains(point) is not true)
            return;
        if (ZoomOut)
        {
            ZoomDeltaTo(-ZoomInRatio, point.X, point.Y);
            ZoomOut = false;
        }
        else
        {
            ZoomDeltaTo(ZoomInRatio, point.X, point.Y);
            ZoomOut = true;
        }
    }

    private void GestureOnPinch(object? sender, PinchEventArgs e)
    {
        if (!EnableZoom)
            return;
        if (Element == null)
            return;
        Captured = false;
        IsPanning = false;
        var point = Element.PointToClient(this.PointToScreen(e.ScaleOrigin));
        var dScale = e.Scale - Scale;
        ZoomDeltaTo(dScale * GesturePinchSpeed, point.X, point.Y);
        Scale = e.Scale;
    }

    private void GestureOnPinchEnded(object? sender, PinchEndedEventArgs e)
    {
        Scale = 1;
    }

    private void RaiseZoomChanged()
    {
        var args = new ZoomChangedEventArgs(ZoomX, ZoomY, OffsetX, OffsetY);
        ZoomChanged?.Invoke(this, args);
    }

    private void Constrain()
    {
        var zoomX = ClampValue(Matrix.M11, MinZoomX, MaxZoomX);
        var zoomY = ClampValue(Matrix.M22, MinZoomY, MaxZoomY);
        var offsetX = ClampValue(Matrix.M31, MinOffsetX, MaxOffsetX);
        var offsetY = ClampValue(Matrix.M32, MinOffsetY, MaxOffsetY);
        Matrix = new(zoomX, 0.0, 0.0, zoomY, offsetX, offsetY);
    }

    /// <summary>
    /// Invalidate pan and zoom control.
    /// </summary>
    /// <param name="skipTransitions">The flag indicating whether transitions on the child element should be temporarily disabled.</param>
    private void Invalidate(bool skipTransitions = false)
    {
        if (Element == null)
            return;
        if (EnableConstrains)
            Constrain();
        InvalidateElement(skipTransitions);
        RaiseZoomChanged();
    }

    /// <summary>
    /// Invalidate child element.
    /// </summary>
    /// <param name="skipTransitions">The flag indicating whether transitions on the child element should be temporarily disabled.</param>
    private void InvalidateElement(bool skipTransitions)
    {
        if (Element == null)
            return;
        Transitions? backupTransitions = null;
        if (skipTransitions)
        {
            if (Element is Animatable anim)
            {
                backupTransitions = anim.Transitions;
                anim.Transitions = null;
            }
        }

        Element.RenderTransformOrigin = new RelativePoint(new Point(0, 0), RelativeUnit.Relative);
        TransformBuilder = new TransformOperations.Builder(1);
        TransformBuilder.AppendMatrix(Matrix);
        Element.RenderTransform = TransformBuilder.Build();

        if (skipTransitions && backupTransitions != null)
        {
            if (Element is Animatable anim)
                anim.Transitions = backupTransitions;
        }

        Element.InvalidateVisual();
    }

    /// <summary>
    /// Set pan and zoom matrix.
    /// </summary>
    /// <param name="matrix">The matrix to set as current.</param>
    /// <param name="skipTransitions">The flag indicating whether transitions on the child element should be temporarily disabled.</param>
    public void SetMatrix(Matrix matrix, bool skipTransitions = false)
    {
        if (_updating)
            return;
        _updating = true;

        Matrix = matrix;
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
    /// Zoom to provided zoom ratio and provided center point.
    /// </summary>
    /// <param name="ratio">The zoom ratio.</param>
    /// <param name="x">The center point x axis coordinate.</param>
    /// <param name="y">The center point y axis coordinate.</param>
    /// <param name="skipTransitions">The flag indicating whether transitions on the child element should be temporarily disabled.</param>
    public void ZoomTo(double ratio, double x, double y, bool skipTransitions = false)
    {
        if (_updating)
            return;

        if (
            (ZoomX >= MaxZoomX && ZoomY >= MaxZoomY && ratio > 1)
            || (ZoomX <= MinZoomX && ZoomY <= MinZoomY && ratio < 1)
        )
            return;

        _updating = true;

        Matrix = MatrixHelper.ScaleAtPrepend(Matrix, ratio, ratio, x, y);
        Invalidate(skipTransitions);

        _updating = false;
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
    /// Set pan origin.
    /// </summary>
    /// <param name="x">The origin point x axis coordinate.</param>
    /// <param name="y">The origin point y axis coordinate.</param>
    public void BeginPanTo(double x, double y)
    {
        Pan = new Point();
        Previous = new Point(x, y);
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
            return;
        _updating = true;

        var dx = x - Previous.X;
        var dy = y - Previous.Y;
        var delta = new Point(dx, dy);
        Previous = new Point(x, y);
        Pan = new Point(Pan.X + delta.X, Pan.Y + delta.Y);
        Matrix = MatrixHelper.TranslatePrepend(Matrix, Pan.X, Pan.Y);
        Invalidate(skipTransitions);

        _updating = false;
    }
}
