using Autodesk.AutoCAD.Runtime;

using ClassLibrary.GUI;

[assembly: CommandClass(typeof(ClassLibrary.MyCommands))]

namespace ClassLibrary;
internal class MyCommands
{
    [CommandMethod("CalculateParking")]
    public void CalculateParking()
    {
        var window = new ParkingWindow();
        var model = new ParkingWindowModel(window);
        window.DataContext = model;
        window.Show();
    }
}
