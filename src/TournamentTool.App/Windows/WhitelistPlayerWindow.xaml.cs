using System.Windows;
using TournamentTool.ViewModels;

namespace TournamentTool.App.Windows;
public partial class WhitelistPlayerWindow : Window
{
    public WhitelistPlayerWindow()
    {
        InitializeComponent();
    }

    private void ExitButtonClick(object sender, RoutedEventArgs e) => Close();
}
