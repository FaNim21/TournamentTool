﻿using System.ComponentModel;
using System.Windows;

namespace TournamentTool.Windows;

public partial class DebugWindow : Window
{
    public DebugWindow()
    {
        InitializeComponent();
    }

    private void MinimizeButtonsClick(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void ExitButtonClick(object sender, RoutedEventArgs e)
    {
        Close();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        ((IDisposable)DataContext).Dispose();
        base.OnClosing(e);
    }
}