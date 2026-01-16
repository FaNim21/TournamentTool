using System.Windows;

namespace TournamentTool.App.Windows;

public partial class ConsoleWindow : Window
{
    public ConsoleWindow()
    {
        InitializeComponent();
    }

    private void ExitButtonClick(object sender, RoutedEventArgs e) => Close();
}