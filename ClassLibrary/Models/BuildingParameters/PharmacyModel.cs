using System.Globalization;

using ClassLibrary.Interfaces;

namespace ClassLibrary.Models.BuildingParameters;
internal class PharmacyModel : IBuildingParameters
{
    public double FloorArea { get; set; }
    public PharmacyModel(Dictionary<string, string> attributes, string[] attributeNames)
    {
        FloorArea = Convert.ToDouble(attributes[attributeNames[0]].Replace(',', '.'), CultureInfo.InvariantCulture);
    }
}
