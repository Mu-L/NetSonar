using NetSonar.Avalonia.Controls;
using NetSonar.Avalonia.ViewModels;

namespace NetSonar.Avalonia.Views;

public partial class PingableServicesPage : UserControlBase
{
    private PingableServicesPageModel? _model;

    public PingableServicesPage()
    {
        InitializeComponent();
    }

    protected override void OnInitialized()
    {
        if (DataContext is PingableServicesPageModel result)
        {
            _model = result;
            _model.SetControls(ServicesGrid, PingRepliesGrid);
        }

        base.OnInitialized();
    }
}