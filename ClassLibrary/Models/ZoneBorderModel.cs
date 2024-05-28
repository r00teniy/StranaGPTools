using Autodesk.AutoCAD.DatabaseServices;

namespace ClassLibrary.Models;
internal class ZoneBorderModel
{
    public string Name { get; private set; }
    public List<Polyline> Polylines { get; private set; }
    public MPolygon Region { get; private set; }
    public ZoneBorderModel(string BorderLayerPrefix, List<Polyline> polylines, MPolygon region)
    {
        Polylines = polylines;
        string layer = polylines[0].Layer;
        string layerWithoutXRef = layer.Contains("|") ? layer.Split('|')[1] : layer;
        Name = layerWithoutXRef.Replace(BorderLayerPrefix, "");
        Region = region;
    }
}
