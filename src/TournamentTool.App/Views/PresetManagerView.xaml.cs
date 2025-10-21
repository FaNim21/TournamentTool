using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using TournamentTool.Core.Utils;

namespace TournamentTool.App.Views;

public partial class PresetManagerView : UserControl
{
    public PresetManagerView()
    {
        InitializeComponent();
    }

    private void OpenDonateSite(object sender, RequestNavigateEventArgs e)
    {
        Helper.StartProcess(e.Uri.ToString());
    }
    private void OpenDonateSiteButton(object sender, RoutedEventArgs e)
    {
        Helper.StartProcess("https://ko-fi.com/fanim");
    }
}
