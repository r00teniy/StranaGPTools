using System.Globalization;

using ClassLibrary.Interfaces;

namespace ClassLibrary.Models.BuildingParameters;
internal class HospitalModel : IBuildingParameters
{
    public double FloorArea { get; set; }
    public int NumberOfPersonel { get; set; }
    public int NumberOfVisitors { get; set; }
    public int NumberOfBeds { get; set; }
    public HospitalModel(Dictionary<string, string> attributes, string[] attributeNames)
    {
        FloorArea = Convert.ToDouble(attributes[attributeNames[0]].Replace(',', '.'), CultureInfo.InvariantCulture);
        NumberOfPersonel = Convert.ToInt32(attributes[attributeNames[1]]);
        NumberOfVisitors = Convert.ToInt32(attributes[attributeNames[2]]);
        NumberOfBeds = Convert.ToInt32(attributes[attributeNames[3]]);
    }
}
