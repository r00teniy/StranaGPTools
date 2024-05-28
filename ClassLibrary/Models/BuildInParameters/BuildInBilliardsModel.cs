using System.Globalization;

using ClassLibrary.Interfaces;

namespace ClassLibrary.Models.BuildInParameters;
internal class BuildInBilliardsModel : IBuildingParameters
{
    public double FloorArea { get; set; }
    public int NumberOfPersonel { get; set; }
    public int NumberOfVisitors { get; set; }
    public BuildInBilliardsModel(Dictionary<string, string> attributes, string[] attributeNames)
    {
        FloorArea = Convert.ToDouble(attributes[attributeNames[0]].Replace(',', '.'), CultureInfo.InvariantCulture);
        NumberOfPersonel = Convert.ToInt32(attributes[attributeNames[1]]);
        NumberOfVisitors = Convert.ToInt32(attributes[attributeNames[2]]);
    }
}
