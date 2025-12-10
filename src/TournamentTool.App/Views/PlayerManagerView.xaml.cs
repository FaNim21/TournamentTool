using System.Windows.Controls;
using System.Windows.Input;
using TournamentTool.ViewModels.Entities.Player;
using TournamentTool.ViewModels.Selectable;

namespace TournamentTool.App.Views;

public partial class PlayerManagerView : UserControl
{
    public PlayerManagerView()
    {
        InitializeComponent();
    }

    private void InPlayerItemMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is not ListBoxItem item) return;
        if (item.DataContext is not PlayerViewModel player) return;
        if (DataContext is not PlayerManagerViewModel vm) return;

        if (vm.EditPlayerCommand.CanExecute(player))
            vm.EditPlayerCommand.Execute(player);

        e.Handled = true;
    }
}
