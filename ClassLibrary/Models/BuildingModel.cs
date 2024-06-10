using System.Globalization;

using Autodesk.AutoCAD.Geometry;

using ClassLibrary.Models.Parking;

namespace ClassLibrary.Models;
internal class BuildingModel
{
    public string StageName { get; set; }
    public string Name { get; set; }
    public string PlotNumber { get; set; } = "";
    public string NumberOfFloors { get; set; }
    public double BuildingArea { get; set; }
    public double FloorArea { get; set; }
    public Point3d Location { get; set; }
    //public BuildingType Type { get; set; }
    public string BuildingBlockName { get; set; }
    public Dictionary<string, string> Parameters { get; set; }
    public List<BuiltInParameters> BuiltInParameters { get; set; } = [];
    public BuiltInParkingModel? BuiltInParking { get; set; }
    //public AmenitiesForBuildingModel Amenities { get; set; } = new();
    public ParkingModel ParkingReqirements { get; set; } = new();
    public List<ParkingBlockModel> ParkingBlocks { get; set; } = [];
    public List<InBuildingParkingBlockModel>? InBuildingParkingBlocks { get; set; } = null;
    public ParkingModel? ParkingProvidedOnPlot { get; set; }
    public ParkingModel? ParkingProvidedOutsidePlot { get; set; }
    public BuildingType BuildingType { get; set; }

    public BuildingModel(Dictionary<string, string> attributes, string[] attributeNames, Point3d pt, BuildingType type, string plotnumber, string blockName)
    {
        StageName = attributes[attributeNames[0]];
        Name = attributes[attributeNames[1]];
        NumberOfFloors = attributes[attributeNames[2]];
        BuildingArea = Convert.ToDouble(attributes[attributeNames[3]].Replace(',', '.'), CultureInfo.InvariantCulture);
        FloorArea = Convert.ToDouble(attributes[attributeNames[4]].Replace(',', '.'), CultureInfo.InvariantCulture);
        Location = pt;
        BuildingType = type;
        PlotNumber = plotnumber;
        Parameters = attributes;
        BuildingBlockName = blockName;
    }
    public string FillInProvidedParking(List<ParkingBlockModel> list, List<InBuildingParkingBlockModel> model)
    {
        try
        {
            ParkingBlocks = list.Where(x => x.ParkingIsForBuildingName == Name).ToList();
            InBuildingParkingBlocks = model;
            ParkingProvidedOnPlot = new ParkingModel(ParkingBlocks, InBuildingParkingBlocks, Name, PlotNumber, true);
            ParkingProvidedOutsidePlot = new ParkingModel(ParkingBlocks, InBuildingParkingBlocks, Name, PlotNumber, false);
        }
        catch (Exception e)
        {
            return "Ошибка при заполнении существующих парковок" + e.Message;
        }

        return "Ok";
    }
}
