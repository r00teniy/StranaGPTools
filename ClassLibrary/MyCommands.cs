using Autodesk.AutoCAD.Runtime;

using ClassLibrary.GUI;

[assembly: CommandClass(typeof(ClassLibrary.MyCommands))]

namespace ClassLibrary;
internal class MyCommands
{
    [CommandMethod("CalculateParking")]
    public void CalculateParking()
    {
        var city = SettingsStorage.ReadData("Город");
        var window = new ParkingWindow();
        var model = new ParkingWindowModel(window, city);
        window.DataContext = model;
        window.Show();
    }
    [CommandMethod("KGPBlocks")]
    public void KGPBlocks()
    {
        var city = SettingsStorage.ReadData("Город");
        var window = new KGPBlocksWindow();
        var model = new KGPBlockWindowModel(window, city);
        window.DataContext = model;
        window.Show();
    }
}