using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using TournamentTool.Components.Controls;
using TournamentTool.Utils;
using TournamentTool.ViewModels;

namespace TournamentTool.Windows;

public partial class LeaderboardEntryEditWindow : Window
{
    public LeaderboardEntryEditWindow()
    {
        InitializeComponent();
        InputController.Instance.InitializeNewWindow(this);
    }
    
    private void ExitButtonClick(object sender, RoutedEventArgs e)
    {
        if (!((BaseViewModel)DataContext).OnDisable())
        {
            MessageBoxResult result = DialogBox.Show($"You have unsaved changes in entry.\nAre you sure you want to leave?", "Leaving entry",
                MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;
        }
        
        Close();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        ((IDisposable)DataContext).Dispose();
        base.OnClosing(e);
    }
    protected override void OnClosed(EventArgs e)
    {
        InputController.Instance.CleanupWindow(this);
        base.OnClosed(e);
    }
    
    private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
    {
        Regex regex = RegexPatterns.NumbersPattern();
        e.Handled = regex.IsMatch(e.Text);
    }
}