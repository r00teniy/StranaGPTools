using System.Windows;

namespace ClassLibrary.GUI;
/// <summary>
/// Interaction logic for ParkingWindow.xaml
/// </summary>
public partial class ParkingWindow : Window
{
    public ParkingWindow()
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
        System.Diagnostics.Process.Start("explorer.exe", "https://docs.google.com/document/d/1OyBXYctV-OSKEtRH_gXMO4KQ77dhfxMg");
    }
}
