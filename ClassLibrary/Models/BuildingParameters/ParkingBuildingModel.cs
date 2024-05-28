using System.Globalization;

using ClassLibrary.Interfaces;

namespace ClassLibrary.Models.BuildingParameters;
public class ParkingBuildingModel : IBuildingParameters
{
    public double FloorArea { get; set; }
    public int NumberOfParkingSpaces { get; set; }
    public int NumberOfParkingSpacesForDisabled { get; set; }

    public ParkingBuildingModel(Dictionary<string, string> attributes, string[] attributeNames)
    {
        FloorArea = Convert.ToDouble(attributes[attributeNames[0]].Replace(',', '.'), CultureInfo.InvariantCulture);
        NumberOfParkingSpacesForDisabled = Convert.ToInt32(attributes[attributeNames[1]]);
        NumberOfParkingSpacesForDisabled = Convert.ToInt32(attributes[attributeNames[2]]);
    }
}
