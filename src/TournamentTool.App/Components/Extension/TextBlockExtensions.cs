using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace TournamentTool.App.Components.Extension;

public class TextBlockExtensions
{
    public static readonly DependencyProperty BindableInlinesProperty = DependencyProperty.RegisterAttached("BindableInlines", typeof(IEnumerable<Inline>), 
        typeof(TextBlockExtensions), new PropertyMetadata(null, OnBindableInlinesChanged));
    
    public static IEnumerable<Inline> GetBindableInlines(DependencyObject obj)
    {
        return (IEnumerable<Inline>) obj.GetValue(BindableInlinesProperty);
    }

    public static void SetBindableInlines(DependencyObject obj, IEnumerable<Inline> value)
    {
        obj.SetValue(BindableInlinesProperty, value);
    }


    private static void OnBindableInlinesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TextBlock Target || e.NewValue == null) return;
        
        Target.Inlines.Clear();
        Target.Inlines.AddRange((System.Collections.IEnumerable)e.NewValue);
    }
}