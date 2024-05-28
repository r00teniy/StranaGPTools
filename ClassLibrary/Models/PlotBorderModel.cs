using Autodesk.AutoCAD.DatabaseServices;

namespace ClassLibrary.Models;
internal class PlotBorderModel
{
    public string PlotNumber { get; private set; }
    public List<Polyline> Polylines { get; private set; }
    public MPolygon Region { get; private set; }
    public PlotBorderModel(string BorderLayerPrefix, List<Polyline> polylines, MPolygon region)
    {
        Polylines = polylines;
        string layer = polylines[0].Layer;
        string layerWithoutXRef = layer.Contains("|") ? layer.Split('|')[1] : layer;
        PlotNumber = layerWithoutXRef.Replace(BorderLayerPrefix, "").Replace("_", ":");
        Region = region;
    }
}
