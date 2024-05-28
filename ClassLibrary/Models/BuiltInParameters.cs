namespace ClassLibrary.Models;
internal class BuiltInParameters
{
    public string BlockName { get; set; } = "";
    public Dictionary<string, string> Parameters { get; set; } = [];
    public BuiltInParameters(string blockName, Dictionary<string, string> parameters)
    {
        BlockName = blockName;
        Parameters = parameters;
    }
}
