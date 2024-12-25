using Avalonia.Data;
using Avalonia.Media.Transformation;

namespace Avalonia.Controls.PanAndZoom;

/// <summary>
/// Pan and zoom control for Avalonia.
/// </summary>
public partial class ZoomBorder
{

    /// <summary>
    /// Zoom changed event.
    /// </summary>
    public event ZoomChangedEventHandler? ZoomChanged;
    
    volatile bool _updating;
    
    Control? Element { get; set; }
    Matrix Matrix { get; set; }
    double ZoomX => Matrix.M11;
    double ZoomY => Matrix.M22;
    double OffsetX => Matrix.M31;
    double OffsetY => Matrix.M32;
    TransformOperations.Builder TransformBuilder { get; set; }
    bool IsPanning { get; set; }
    Point Pan { get; set; }
    Point Previous { get; set; }
    bool Captured { get; set; }
    double Scale { get; set; } = 1.0;
    bool ZoomOut { get; set; }

    /// <summary>
    /// Gets or sets zoom speed ratio.
    /// </summary>
    public double ZoomSpeed
    {
        get => GetValue(ZoomSpeedProperty);
        set => SetValue(ZoomSpeedProperty, value);
    }

    /// <summary>
    /// Identifies the <seealso cref="ZoomSpeed"/> avalonia property.
    /// </summary>
    public static readonly StyledProperty<double> ZoomSpeedProperty = AvaloniaProperty.Register<
        ZoomBorder,
        double
    >(nameof(ZoomSpeed), 1.2, false, BindingMode.TwoWay);

    /// <summary>
    /// Gets or sets the power factor used to transform the mouse wheel delta value.
    /// </summary>
    public double PowerFactor
    {
        get => GetValue(PowerFactorProperty);
        set => SetValue(PowerFactorProperty, value);
    }

    /// <summary>
    /// Identifies the <seealso cref="PowerFactor"/> avalonia property.
    /// </summary>
    public static readonly StyledProperty<double> PowerFactorProperty = AvaloniaProperty.Register<
        ZoomBorder,
        double
    >(nameof(PowerFactor), 1, false, BindingMode.TwoWay);

    /// <summary>
    /// Gets or sets the threshold below which zoom operations will skip all transitions.
    /// </summary>
    public double TransitionThreshold
    {
        get => GetValue(TransitionThresholdProperty);
        set => SetValue(TransitionThresholdProperty, value);
    }

    /// <summary>
    /// Identifies the <seealso cref="TransitionThreshold"/> avalonia property.
    /// </summary>
    public static readonly StyledProperty<double> TransitionThresholdProperty =
        AvaloniaProperty.Register<ZoomBorder, double>(
            nameof(TransitionThreshold),
            0.5,
            false,
            BindingMode.TwoWay
        );

    /// <summary>
    /// Gets or sets flag indicating whether zoom ratio and pan offset constrains are applied.
    /// </summary>
    public bool EnableConstrains
    {
        get => GetValue(EnableConstrainsProperty);
        set => SetValue(EnableConstrainsProperty, value);
    }

    /// <summary>
    /// Identifies the <seealso cref="EnableConstrains"/> avalonia property.
    /// </summary>
    public static readonly StyledProperty<bool> EnableConstrainsProperty =
        AvaloniaProperty.Register<ZoomBorder, bool>(
            nameof(EnableConstrains),
            true,
            false,
            BindingMode.TwoWay
        );

    /// <summary>
    /// Gets or sets minimum zoom ratio for x axis.
    /// </summary>
    public double MinZoomX
    {
        get => GetValue(MinZoomXProperty);
        set => SetValue(MinZoomXProperty, value);
    }

    /// <summary>
    /// Identifies the <seealso cref="MinZoomX"/> avalonia property.
    /// </summary>
    public static readonly StyledProperty<double> MinZoomXProperty = AvaloniaProperty.Register<
        ZoomBorder,
        double
    >(nameof(MinZoomX), double.NegativeInfinity, false, BindingMode.TwoWay);

    /// <summary>
    /// Gets or sets maximum zoom ratio for x axis.
    /// </summary>
    public double MaxZoomX
    {
        get => GetValue(MaxZoomXProperty);
        set => SetValue(MaxZoomXProperty, value);
    }

    /// <summary>
    /// Identifies the <seealso cref="MaxZoomX"/> avalonia property.
    /// </summary>
    public static readonly StyledProperty<double> MaxZoomXProperty = AvaloniaProperty.Register<
        ZoomBorder,
        double
    >(nameof(MaxZoomX), double.PositiveInfinity, false, BindingMode.TwoWay);

    /// <summary>
    /// Gets or sets minimum zoom ratio for y axis.
    /// </summary>
    public double MinZoomY
    {
        get => GetValue(MinZoomYProperty);
        set => SetValue(MinZoomYProperty, value);
    }

    /// <summary>
    /// Identifies the <seealso cref="MinZoomY"/> avalonia property.
    /// </summary>
    public static readonly StyledProperty<double> MinZoomYProperty = AvaloniaProperty.Register<
        ZoomBorder,
        double
    >(nameof(MinZoomY), double.NegativeInfinity, false, BindingMode.TwoWay);

    /// <summary>
    /// Gets or sets maximum zoom ratio for y axis.
    /// </summary>
    public double MaxZoomY
    {
        get => GetValue(MaxZoomYProperty);
        set => SetValue(MaxZoomYProperty, value);
    }

    /// <summary>
    /// Identifies the <seealso cref="MaxZoomY"/> avalonia property.
    /// </summary>
    public static readonly StyledProperty<double> MaxZoomYProperty = AvaloniaProperty.Register<
        ZoomBorder,
        double
    >(nameof(MaxZoomY), double.PositiveInfinity, false, BindingMode.TwoWay);

    /// <summary>
    /// Gets or sets minimum offset for x axis.
    /// </summary>
    public double MinOffsetX
    {
        get => GetValue(MinOffsetXProperty);
        set => SetValue(MinOffsetXProperty, value);
    }

    /// <summary>
    /// Identifies the <seealso cref="MinOffsetX"/> avalonia property.
    /// </summary>
    public static readonly StyledProperty<double> MinOffsetXProperty = AvaloniaProperty.Register<
        ZoomBorder,
        double
    >(nameof(MinOffsetX), double.NegativeInfinity, false, BindingMode.TwoWay);

    /// <summary>
    /// Gets or sets maximum offset for x axis.
    /// </summary>
    public double MaxOffsetX
    {
        get => GetValue(MaxOffsetXProperty);
        set => SetValue(MaxOffsetXProperty, value);
    }

    /// <summary>
    /// Identifies the <seealso cref="MaxOffsetX"/> avalonia property.
    /// </summary>
    public static readonly StyledProperty<double> MaxOffsetXProperty = AvaloniaProperty.Register<
        ZoomBorder,
        double
    >(nameof(MaxOffsetX), double.PositiveInfinity, false, BindingMode.TwoWay);

    /// <summary>
    /// Gets or sets minimum offset for y axis.
    /// </summary>
    public double MinOffsetY
    {
        get => GetValue(MinOffsetYProperty);
        set => SetValue(MinOffsetYProperty, value);
    }

    /// <summary>
    /// Identifies the <seealso cref="MinOffsetY"/> avalonia property.
    /// </summary>
    public static readonly StyledProperty<double> MinOffsetYProperty = AvaloniaProperty.Register<
        ZoomBorder,
        double
    >(nameof(MinOffsetY), double.NegativeInfinity, false, BindingMode.TwoWay);

    /// <summary>
    /// Gets or sets maximum offset for y axis.
    /// </summary>
    public double MaxOffsetY
    {
        get => GetValue(MaxOffsetYProperty);
        set => SetValue(MaxOffsetYProperty, value);
    }

    /// <summary>
    /// Identifies the <seealso cref="MaxOffsetY"/> avalonia property.
    /// </summary>
    public static readonly StyledProperty<double> MaxOffsetYProperty = AvaloniaProperty.Register<
        ZoomBorder,
        double
    >(nameof(MaxOffsetY), double.PositiveInfinity, false, BindingMode.TwoWay);

    /// <summary>
    /// Gets or sets flag indicating whether pan input events are processed.
    /// </summary>
    public bool EnablePan
    {
        get => GetValue(EnablePanProperty);
        set => SetValue(EnablePanProperty, value);
    }

    /// <summary>
    /// Identifies the <seealso cref="EnablePan"/> avalonia property.
    /// </summary>
    public static readonly StyledProperty<bool> EnablePanProperty = AvaloniaProperty.Register<
        ZoomBorder,
        bool
    >(nameof(EnablePan), true, false, BindingMode.TwoWay);

    /// <summary>
    /// Gets or sets flag indicating whether input zoom events are processed.
    /// </summary>
    public bool EnableZoom
    {
        get => GetValue(EnableZoomProperty);
        set => SetValue(EnableZoomProperty, value);
    }

    /// <summary>
    /// Identifies the <seealso cref="EnableZoom"/> avalonia property.
    /// </summary>
    public static readonly StyledProperty<bool> EnableZoomProperty = AvaloniaProperty.Register<
        ZoomBorder,
        bool
    >(nameof(EnableZoom), true, false, BindingMode.TwoWay);

    /// <summary>
    /// Gets or sets the ratio of zoom-in
    /// </summary>
    public double ZoomInRatio
    {
        get => GetValue(ZoomInRatioProperty);
        set => SetValue(ZoomInRatioProperty, value);
    }

    /// <summary>
    /// Identifies the <seealso cref="ZoomInRatio"/> avalonia property.
    /// </summary>
    public static readonly StyledProperty<double> ZoomInRatioProperty = AvaloniaProperty.Register<
        ZoomBorder,
        double
    >(nameof(ZoomInRatio), 3.0, false, BindingMode.TwoWay);

    /// <summary>
    /// Gets or sets the speed of gesture pinch
    /// </summary>
    public double GesturePinchSpeed
    {
        get => GetValue(GesturePinchSpeedProperty);
        set => SetValue(GesturePinchSpeedProperty, value);
    }

    /// <summary>
    /// Identifies the <seealso cref="GesturePinchSpeed"/> avalonia property.
    /// </summary>
    public static readonly StyledProperty<double> GesturePinchSpeedProperty =
        AvaloniaProperty.Register<ZoomBorder, double>(
            nameof(GesturePinchSpeed),
            5.0,
            false,
            BindingMode.TwoWay
        );

    static ZoomBorder()
    {
        AffectsArrange<ZoomBorder>(
            ZoomSpeedProperty,
            EnableConstrainsProperty,
            MinZoomXProperty,
            MaxZoomXProperty,
            MinZoomYProperty,
            MaxZoomYProperty,
            MinOffsetXProperty,
            MaxOffsetXProperty,
            MinOffsetYProperty,
            MaxOffsetYProperty
        );
    }
}
