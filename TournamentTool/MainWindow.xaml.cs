using System.Windows;
using System.Windows.Input;
using System.Windows.Shapes;
using TournamentTool.Components.Controls;
using TournamentTool.Properties;
using TournamentTool.Utils;
using TournamentTool.ViewModels;

namespace TournamentTool;

public partial class MainWindow : Window
{
    private bool ResizeInProcess;

    public MainWindow()
    {
        InitializeComponent();

        labelVersion.Content = Consts.Version;

        if (Settings.Default.MainWindowLeft == -1 && Settings.Default.MainWindowTop == -1)
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
        else
        {
            WindowStartupLocation = WindowStartupLocation.Manual;
            Left = Settings.Default.MainWindowLeft;
            Top = Settings.Default.MainWindowTop;
        }
    }

    private void HeaderMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left) DragMove();
    }
    private void MinimizeButtonsClick(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void ChangeWindowMaximizeStateClick(object sender, RoutedEventArgs e)
    {
        if (WindowState == WindowState.Maximized)
        {
            WindowState = WindowState.Normal;
            MaximizeButton.ContentText = "🗖";
            ResizableRects.Visibility = Visibility.Visible;
        }
        else
        {
            WindowState = WindowState.Maximized;
            MaximizeButton.ContentText = "🗗";
            ResizableRects.Visibility = Visibility.Hidden;
        }
    }
    private void ExitButtonClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel mainViewModel) return;
        if (mainViewModel.SelectedViewModel != null && !mainViewModel.SelectedViewModel.OnDisable()) return;

        if (mainViewModel.SelectedViewModel != null && mainViewModel.SelectedViewModel is not PresetManagerViewModel)
        {
            var option = DialogBox.Show($"Are you sure you want to exit from here?", "WARNING", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (option == MessageBoxResult.Yes) Close();
            return;
        }

        Close();
    }

    private void OnClosed(object sender, EventArgs e)
    {
        Settings.Default.MainWindowLeft = Left;
        Settings.Default.MainWindowTop = Top;

        Settings.Default.Save();
        Application.Current.Shutdown();
    }

    #region ResizeWindows
    private void Resize_Init(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Rectangle senderRect) return;

        ResizeInProcess = true;
        senderRect.CaptureMouse();
    }
    private void Resize_End(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Rectangle senderRect) return;

        ResizeInProcess = false; ;
        senderRect.ReleaseMouseCapture();
    }
    private void Resizeing_Form(object sender, MouseEventArgs e)
    {
        if (!ResizeInProcess) return;
        if (sender is not Rectangle senderRect) return;

        Window mainWindow = senderRect.Tag as Window;
        double width = e.GetPosition(mainWindow).X;
        double height = e.GetPosition(mainWindow).Y;
        double temp = 0;
        senderRect.CaptureMouse();

        if (senderRect.Name.Contains("right", StringComparison.OrdinalIgnoreCase))
        {
            width += 5;
            if (width > 0)
                mainWindow.Width = width;
        }
        if (senderRect.Name.Contains("left", StringComparison.OrdinalIgnoreCase))
        {
            width -= 5;
            temp = mainWindow.Width - width;
            if ((temp > mainWindow.MinWidth) && (temp < mainWindow.MaxWidth))
            {
                mainWindow.Width = temp;
                mainWindow.Left += width;
            }
        }
        if (senderRect.Name.Contains("bottom", StringComparison.OrdinalIgnoreCase))
        {
            height += 5;
            if (height > 0)
                mainWindow.Height = height;
        }
        if (senderRect.Name.ToLower().Contains("top", StringComparison.OrdinalIgnoreCase))
        {
            height -= 5;
            temp = mainWindow.Height - height;
            if ((temp > mainWindow.MinHeight) && (temp < mainWindow.MaxHeight))
            {
                mainWindow.Height = temp;
                mainWindow.Top += height;
            }
        }
    }
    #endregion
}