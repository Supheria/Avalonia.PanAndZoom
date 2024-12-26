using Avalonia.Data;
using Avalonia.Media.Transformation;

namespace Avalonia.Controls.PanAndZoom;

/// <summary>
/// Pan and zoom control for Avalonia.
/// </summary>
public partial class ZoomBorder
{
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

    public double ZoomSpeed
    {
        get => GetValue(ZoomSpeedProperty);
        set => SetValue(ZoomSpeedProperty, value);
    }
    public static readonly StyledProperty<double> ZoomSpeedProperty = AvaloniaProperty.Register<
        ZoomBorder,
        double
    >(nameof(ZoomSpeed), 1.2, false, BindingMode.TwoWay);

    public double PowerFactor
    {
        get => GetValue(PowerFactorProperty);
        set => SetValue(PowerFactorProperty, value);
    }
    public static readonly StyledProperty<double> PowerFactorProperty = AvaloniaProperty.Register<
        ZoomBorder,
        double
    >(nameof(PowerFactor), 1, false, BindingMode.TwoWay);

    public double MinZoomX
    {
        get => GetValue(MinZoomXProperty);
        set => SetValue(MinZoomXProperty, value);
    }
    public static readonly StyledProperty<double> MinZoomXProperty = AvaloniaProperty.Register<
        ZoomBorder,
        double
    >(nameof(MinZoomX), double.NegativeInfinity, false, BindingMode.TwoWay);

    public double MaxZoomX
    {
        get => GetValue(MaxZoomXProperty);
        set => SetValue(MaxZoomXProperty, value);
    }
    public static readonly StyledProperty<double> MaxZoomXProperty = AvaloniaProperty.Register<
        ZoomBorder,
        double
    >(nameof(MaxZoomX), double.PositiveInfinity, false, BindingMode.TwoWay);

    public double MinZoomY
    {
        get => GetValue(MinZoomYProperty);
        set => SetValue(MinZoomYProperty, value);
    }
    public static readonly StyledProperty<double> MinZoomYProperty = AvaloniaProperty.Register<
        ZoomBorder,
        double
    >(nameof(MinZoomY), double.NegativeInfinity, false, BindingMode.TwoWay);

    public double MaxZoomY
    {
        get => GetValue(MaxZoomYProperty);
        set => SetValue(MaxZoomYProperty, value);
    }
    public static readonly StyledProperty<double> MaxZoomYProperty = AvaloniaProperty.Register<
        ZoomBorder,
        double
    >(nameof(MaxZoomY), double.PositiveInfinity, false, BindingMode.TwoWay);

    public double MinOffsetX
    {
        get => GetValue(MinOffsetXProperty);
        set => SetValue(MinOffsetXProperty, value);
    }
    public static readonly StyledProperty<double> MinOffsetXProperty = AvaloniaProperty.Register<
        ZoomBorder,
        double
    >(nameof(MinOffsetX), double.NegativeInfinity, false, BindingMode.TwoWay);

    public double ZoomInRatio
    {
        get => GetValue(ZoomInRatioProperty);
        set => SetValue(ZoomInRatioProperty, value);
    }
    public static readonly StyledProperty<double> ZoomInRatioProperty = AvaloniaProperty.Register<
        ZoomBorder,
        double
    >(nameof(ZoomInRatio), 3.0, false, BindingMode.TwoWay);

    public double GesturePinchSpeed
    {
        get => GetValue(GesturePinchSpeedProperty);
        set => SetValue(GesturePinchSpeedProperty, value);
    }
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
            MinZoomXProperty,
            MaxZoomXProperty,
            MinZoomYProperty,
            MaxZoomYProperty,
            MinOffsetXProperty
        );
    }
}
