﻿using System.Windows;

namespace ClassLibrary.GUI;
/// <summary>
/// Interaction logic for KGPBlocksWindow.xaml
/// </summary>
public partial class KGPBlocksWindow : Window
{
    public KGPBlocksWindow()
    {
        InitializeComponent();
    }
    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
    private void InstructionButton_Click(object sender, RoutedEventArgs e)
    {
        this.Topmost = false;
        System.Diagnostics.Process.Start("explorer.exe", "https://docs.google.com/document/d/13m6hU-5oDlJDdOtGXGIq0GtESqn_trBZ");
    }
}