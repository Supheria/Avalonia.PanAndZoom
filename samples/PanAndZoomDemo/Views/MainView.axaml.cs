using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Controls.PanAndZoom;
using Avalonia.Input;

namespace PanAndZoomDemo.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();

        ZoomBorder1 = this.Find<ZoomBorder>("ZoomBorder1");
        if (ZoomBorder1 != null)
        {
            ZoomBorder1.ZoomChanged += ZoomBorder_ZoomChanged;
        }

        ZoomBorder2 = this.Find<ZoomBorder>("ZoomBorder2");
        if (ZoomBorder2 != null)
        {
            ZoomBorder2.ZoomChanged += ZoomBorder_ZoomChanged;
        }

        DataContext = ZoomBorder1;
        ResetZoomButton.Click += (_, _) => ZoomBorder1.ResetMatrix();
    }

    private void ZoomBorder_ZoomChanged(object sender, ZoomChangedEventArgs e)
    {
        // Debug.WriteLine($"[ZoomChanged] {e.ZoomX} {e.ZoomY} {e.OffsetX} {e.OffsetY}");
    }

    private void TabControl_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is TabControl tabControl)
        {
            if (tabControl.SelectedItem is TabItem tabItem)
            {
                if (tabItem.Tag is string tag)
                {
                    if (tag == "1")
                    {
                        DataContext = ZoomBorder1;
                    }
                    else if (tag == "2")
                    {
                        DataContext = ZoomBorder2;
                    }
                }
            }
        }
    }
}