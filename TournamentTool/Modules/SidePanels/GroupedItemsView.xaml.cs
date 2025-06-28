using System.Windows;
using System.Windows.Controls;

namespace TournamentTool.Modules.SidePanels;

public partial class GroupedItemsView : UserControl
{
    public static readonly DependencyProperty GroupHeaderTemplateProperty =
        DependencyProperty.Register(
            nameof(GroupHeaderTemplate),
            typeof(DataTemplate),
            typeof(GroupedItemsView),
            new PropertyMetadata(null));
   
    public DataTemplate GroupHeaderTemplate
    {
        get => (DataTemplate)GetValue(GroupHeaderTemplateProperty);
        set => SetValue(GroupHeaderTemplateProperty, value);
    }
   
    public static readonly DependencyProperty SelectionModeProperty =
        DependencyProperty.Register(
            nameof(SelectionMode),
            typeof(SelectionMode),
            typeof(GroupedItemsView),
            new PropertyMetadata(SelectionMode.Multiple));
   
    public SelectionMode SelectionMode
    {
        get => (SelectionMode)GetValue(SelectionModeProperty);
        set => SetValue(SelectionModeProperty, value);
    }
   
    public static readonly DependencyProperty EmptyMessageProperty =
        DependencyProperty.Register(
            nameof(EmptyMessage),
            typeof(string),
            typeof(GroupedItemsView),
            new PropertyMetadata("No items"));
   
    public string EmptyMessage
    {
        get => (string)GetValue(EmptyMessageProperty);
        set => SetValue(EmptyMessageProperty, value);
    } 
    public GroupedItemsView()
    {
        InitializeComponent();
    }
}