using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TournamentTool.Utils;

namespace TournamentTool.Components.Behaviors;

public class NumericTextBoxBehavior : BehaviorBase<TextBox>
{
    public static readonly DependencyProperty AllowNegativeProperty = 
        DependencyProperty.Register( nameof(AllowNegative), typeof(bool), typeof(NumericTextBoxBehavior), new PropertyMetadata(true));
    public static readonly DependencyProperty AllowDecimalProperty =
        DependencyProperty.Register(nameof(AllowDecimal), typeof(bool), typeof(NumericTextBoxBehavior), new PropertyMetadata(false)); 
    
    public bool AllowNegative
    {
        get => (bool)GetValue(AllowNegativeProperty);
        set => SetValue(AllowNegativeProperty, value);
    } 
    public bool AllowDecimal
    {
        get => (bool)GetValue(AllowDecimalProperty);
        set => SetValue(AllowDecimalProperty, value);
    } 
    
    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.PreviewTextInput += NumberValidationTextBox;
        DataObject.AddPastingHandler(AssociatedObject, TextBoxPasting);
    }
    protected override void OnCleanup()
    {
        base.OnCleanup();
        AssociatedObject.PreviewTextInput -= NumberValidationTextBox;
        DataObject.RemovePastingHandler(AssociatedObject, TextBoxPasting);
    }
    
    private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
    {
        if (sender is not TextBox textBox) return;
        string newText = textBox.Text.Insert(textBox.SelectionStart, e.Text);

        Regex regex = GetRegex();
        e.Handled = !regex.IsMatch(newText);
    }
    private void TextBoxPasting(object sender, DataObjectPastingEventArgs e)
    {
        if (sender is not TextBox textBox) return;
        if (e.DataObject.GetDataPresent(typeof(string)))
        {
            string pastedText = (string)e.DataObject.GetData(typeof(string))!;
            string newText = textBox.Text.Insert(textBox.SelectionStart, pastedText);

            Regex regex = GetRegex();
            if (!regex.IsMatch(newText))
            {
                e.CancelCommand();
            }
        }
        else
        {
            e.CancelCommand();
        }
    }
    
    private Regex GetRegex()
    {
        if (AllowDecimal && AllowNegative) return RegexPatterns.DecimalWithNegativePattern();
        if (AllowDecimal && !AllowNegative) return RegexPatterns.DecimalPattern();
        if (!AllowDecimal && AllowNegative) return RegexPatterns.NumbersPattern();
        return RegexPatterns.NumbersPatternDigitOnly();
    }
}