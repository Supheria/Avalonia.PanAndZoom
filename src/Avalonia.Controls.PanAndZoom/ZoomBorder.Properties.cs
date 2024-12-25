using System;
using Avalonia.Data;
using Avalonia.Media.Transformation;

namespace Avalonia.Controls.PanAndZoom;

/// <summary>
/// Pan and zoom control for Avalonia.
/// </summary>
public partial class ZoomBorder
{
    Control? _element;
    Point _pan;
    Point _previous;
    Matrix _matrix;
    TransformOperations.Builder _transformBuilder;
    bool _isPanning;
    volatile bool _updating;
    double _zoomX = 1.0;
    double _zoomY = 1.0;
    double _offsetX;
    double _offsetY;
    bool _captured;
    double _scale = 1;
    bool _zoomOut;

    /// <summary>
    /// Gets the render transform matrix.
    /// </summary>
    public Matrix Matrix => _matrix;

    /// <summary>
    /// Zoom changed event.
    /// </summary>
    public event ZoomChangedEventHandler? ZoomChanged;

    /// <summary>
    /// Gets available stretch modes.
    /// </summary>
    public static StretchMode[] StretchModes { get; } =
        (StretchMode[])Enum.GetValues(typeof(StretchMode));

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
    /// Gets or sets stretch mode.
    /// </summary>
    public StretchMode Stretch
    {
        get => GetValue(StretchProperty);
        set => SetValue(StretchProperty, value);
    }

    /// <summary>
    /// Identifies the <seealso cref="Stretch"/> avalonia property.
    /// </summary>
    public static readonly StyledProperty<StretchMode> StretchProperty = AvaloniaProperty.Register<
        ZoomBorder,
        StretchMode
    >(nameof(Stretch), StretchMode.Uniform, false, BindingMode.TwoWay);

    /// <summary>
    /// Gets the zoom ratio for x axis.
    /// </summary>
    public double ZoomX => _zoomX;

    /// <summary>
    /// Identifies the <seealso cref="ZoomX"/> avalonia property.
    /// </summary>
    public static readonly DirectProperty<ZoomBorder, double> ZoomXProperty =
        AvaloniaProperty.RegisterDirect<ZoomBorder, double>(nameof(ZoomX), o => o.ZoomX, null, 1.0);

    /// <summary>
    /// Gets the zoom ratio for y axis.
    /// </summary>
    public double ZoomY => _zoomY;

    /// <summary>
    /// Identifies the <seealso cref="ZoomY"/> avalonia property.
    /// </summary>
    public static readonly DirectProperty<ZoomBorder, double> ZoomYProperty =
        AvaloniaProperty.RegisterDirect<ZoomBorder, double>(nameof(ZoomY), o => o.ZoomY, null, 1.0);

    /// <summary>
    /// Gets the pan offset for x axis.
    /// </summary>
    public double OffsetX => _offsetX;

    /// <summary>
    /// Identifies the <seealso cref="OffsetX"/> avalonia property.
    /// </summary>
    public static readonly DirectProperty<ZoomBorder, double> OffsetXProperty =
        AvaloniaProperty.RegisterDirect<ZoomBorder, double>(nameof(OffsetX), o => o.OffsetX);

    /// <summary>
    /// Gets the pan offset for y axis.
    /// </summary>
    public double OffsetY => _offsetY;

    /// <summary>
    /// Identifies the <seealso cref="OffsetY"/> avalonia property.
    /// </summary>
    public static readonly DirectProperty<ZoomBorder, double> OffsetYProperty =
        AvaloniaProperty.RegisterDirect<ZoomBorder, double>(nameof(OffsetY), o => o.OffsetY);

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
    >(nameof(ZoomInRatio), 5.0, false, BindingMode.TwoWay);

    static ZoomBorder()
    {
        AffectsArrange<ZoomBorder>(
            ZoomSpeedProperty,
            StretchProperty,
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
