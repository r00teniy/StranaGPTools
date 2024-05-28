using System.Globalization;

using ClassLibrary.Interfaces;

namespace ClassLibrary.Models.BuildingParameters;
public class ResidentialModel : IBuildingParameters
{
    public double FloorArea { get; set; }
    public double FlatsArea { get; set; }
    public int NumberOfFlats { get; set; }
    public ResidentialModel(Dictionary<string, string> attributes, string[] attributeNames)
    {
        FloorArea = Convert.ToDouble(attributes[attributeNames[0]].Replace(',', '.'), CultureInfo.InvariantCulture);
        FlatsArea = Convert.ToDouble(attributes[attributeNames[1]].Replace(',', '.'), CultureInfo.InvariantCulture);
        NumberOfFlats = Convert.ToInt32(attributes[attributeNames[2]]);
    }
}
