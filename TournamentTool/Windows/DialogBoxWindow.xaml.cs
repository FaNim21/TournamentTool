using System.Media;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using TournamentTool.ViewModels.Modals;

namespace TournamentTool.Windows;

public sealed partial class DialogBoxWindow : Window
{
    public DialogBoxWindow()
    {
        InitializeComponent();

        Loaded += OnLoaded;
    }
    ~DialogBoxWindow()
    {
        Application.Current.Dispatcher.Invoke(delegate
        {
            Loaded -= OnLoaded;
        });
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        SetupTextWidth();
        PlayOpeningSound();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        if (PresentationSource.FromVisual(this) is HwndSource hwndSource)
            hwndSource.CompositionTarget.RenderMode = RenderMode.SoftwareOnly;
        base.OnSourceInitialized(e);
    }

    private void HeaderMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
            DragMove();
    }
    private void ExitButtonClick(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void SetupTextWidth()
    {
        if (Icon.Source != null) return;

        TextGrid.Margin = new Thickness(-60, 0, 5, 0);
        TextGrid.HorizontalAlignment = HorizontalAlignment.Center;
    }

    private void PlayOpeningSound()
    {
        if (DataContext is not DialogBoxViewModel dialogBox) return;

        switch (dialogBox.Icon)
        {
            case MessageBoxImage.Information:
                SystemSounds.Asterisk.Play();
                break;
            case MessageBoxImage.Error:
                SystemSounds.Hand.Play();
                break;
            case MessageBoxImage.Warning:
                SystemSounds.Exclamation.Play();
                break;
        }
    }

    private void WindowKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape) Close();
    }
}
