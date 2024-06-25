using System.Windows;
using System.Windows.Input;
using TournamentTool.Components;
using TournamentTool.Utils;
using TournamentTool.ViewModels;

namespace TournamentTool;

public partial class App : Application
{
    public MainViewModel MainViewModel { get; set; }

    public App() { }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        MainWindow window = new();
        MainViewModel = new MainViewModel();
        window.DataContext = MainViewModel;

        InputController.Instance.Initialize();
        HotkeySetup();

        window.Show();

        MainWindow = window;
    }

    protected override void OnExit(ExitEventArgs e)
    {
        base.OnExit(e);
    }

    private void HotkeySetup()
    {
        var renameTextBox = new Hotkey
        {
            Key = Key.F2,
            ModifierKeys = ModifierKeys.None,
            Description = "Bind to trigger renaming elements",
            Action = () =>
            {
                var textBlock = Helper.GetFocusedUIElement<EditableTextBlock>();
                if (textBlock is { IsEditable: true })
                    textBlock.IsInEditMode = true;
            }
        };

        var toggleHamburgerMenu = new Hotkey
        {
            Key = Key.F1,
            ModifierKeys = ModifierKeys.None,
            Description = "Toggle visibility for hamburger menu",
            Action = () =>
            {
                MainViewModel.IsHamburgerMenuOpen = !MainViewModel.IsHamburgerMenuOpen;
            }
        };

        InputController.Instance.AddHotkey(renameTextBox);
        InputController.Instance.AddHotkey(toggleHamburgerMenu);
    }
}
