using System.Globalization;

using ClassLibrary.Interfaces;

namespace ClassLibrary.Models.BuildInParameters;
internal class BuildInLibraryModel : IBuildingParameters
{
    public double FloorArea { get; set; }
    public int NumberOfVisitors { get; set; }
    public int NumberOfSeats { get; set; }
    public BuildInLibraryModel(Dictionary<string, string> attributes, string[] attributeNames)
    {
        FloorArea = Convert.ToDouble(attributes[attributeNames[0]].Replace(',', '.'), CultureInfo.InvariantCulture);
        NumberOfVisitors = Convert.ToInt32(attributes[attributeNames[1]]);
        NumberOfSeats = Convert.ToInt32(attributes[attributeNames[2]]);
    }
}
