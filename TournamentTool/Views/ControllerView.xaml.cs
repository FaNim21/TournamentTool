using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Windows.Input;
using TournamentTool.Utils;

namespace TournamentTool.Views;

public partial class ControllerView : UserControl
{
    public ControllerView()
    {
        InitializeComponent();
    }

    /*private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        //Trace.WriteLine("Wcisniety guzik");
        //TODO: 0 PROBLEMY Z CZYSZCZENIEM NA WCISNIECIU ESCAPE MOZE LEPIEJ ZROBIC INPUT CONTROLLER
        //NIC NIE CHCE DZIALAC MOZE GLOBAL HOTKEY STAD?  https://blog.magnusmontin.net/2015/03/31/implementing-global-hot-keys-in-wpf/
        if (e.Key == Key.Escape)
        {
            WhiteList.SelectedItem = null;
            WhiteList.UnselectAll();
            PaceMan.SelectedItem = null;
            PaceMan.UnselectAll();
            Keyboard.ClearFocus();
            if (DataContext is not ControllerViewModel viewModel) return;
            viewModel.UnSelectItems(true);
        }
    }*/

    private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
    {
        Regex regex = RegexPatterns.NumbersPattern();
        e.Handled = regex.IsMatch(e.Text);
    }

}
