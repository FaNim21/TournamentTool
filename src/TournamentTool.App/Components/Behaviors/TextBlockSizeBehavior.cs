using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace TournamentTool.App.Components.Behaviors;

public class TextBlockSizeBehavior : BehaviorBase<TextBlock>
{
    private const float _BASE_MARGIN = 10;
    
    private DependencyPropertyDescriptor? _descriptor;
    
    public static readonly DependencyProperty ParentMaxWidthProperty = DependencyProperty.Register(nameof(ParentMaxWidth), typeof(double), typeof(TextBlockSizeBehavior));
    public static readonly DependencyProperty ParentMaxHeightProperty = DependencyProperty.Register(nameof(ParentMaxHeight), typeof(double), typeof(TextBlockSizeBehavior));

    public double ParentMaxWidth
    {
        get => (double)GetValue(ParentMaxWidthProperty);
        set => SetValue(ParentMaxWidthProperty, value);
    }
    public double ParentMaxHeight
    {
        get => (double)GetValue(ParentMaxHeightProperty);
        set => SetValue(ParentMaxHeightProperty, value);
    }
    
    
    protected override void OnAttached()
    {
        base.OnAttached();

        _descriptor = DependencyPropertyDescriptor.FromProperty(TextBlock.TextProperty, typeof(TextBlock));
        _descriptor.AddValueChanged(AssociatedObject, OnTextChanged);
    }
    protected override void OnCleanup()
    {
        base.OnCleanup();
        
        _descriptor?.RemoveValueChanged(AssociatedObject, OnTextChanged);
        _descriptor = null;
    }
    
    private void OnTextChanged(object? sender, EventArgs e)
    {
        if (AssociatedObject.Parent is not FrameworkElement parent) return;

        Size size = MeasureString(AssociatedObject.Text);
        parent.Width = size.Width;
        parent.Height = size.Height;
    }
    
    private Size MeasureString(string text)
    {
        FormattedText formattedText = new(text,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface(AssociatedObject.FontFamily, AssociatedObject.FontStyle, AssociatedObject.FontWeight, AssociatedObject.FontStretch),
            AssociatedObject.FontSize,
            AssociatedObject.Foreground,
            new NumberSubstitution(),
            VisualTreeHelper.GetDpi(AssociatedObject).PixelsPerDip)
        {
            MaxTextHeight = ParentMaxHeight,
            MaxTextWidth = ParentMaxWidth
        };

        double widthMargin = AssociatedObject.Margin.Left + AssociatedObject.Margin.Right + _BASE_MARGIN;
        double heightMargin = AssociatedObject.Margin.Top + AssociatedObject.Margin.Bottom;
        
        return new Size(formattedText.Width + widthMargin, formattedText.Height + heightMargin);
    }
}