
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;

namespace ClassLibrary.Models;
internal class HatchStyleModel
{
    public string LayerName { get; set; } = "";
    public double PatternAngle { get; set; } = 0.0;
    public string PatternName { get; set; } = "SOLID";
    public double PatternScale { get; set; } = 1.0;
    public Color BackgroundColor { get; set; } = Color.FromColorIndex(ColorMethod.ByAci, 256);
    public Color PatternColor { get; set; } = Color.FromColorIndex(ColorMethod.ByAci, 256);
    public LineWeight LineWeight { get; set; } = LineWeight.ByLayer;
    public Transparency Transparency { get; set; } = new Transparency(TransparencyMethod.ByLayer);
}
