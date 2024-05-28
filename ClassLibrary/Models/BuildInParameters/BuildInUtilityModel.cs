﻿using System.Globalization;

using ClassLibrary.Interfaces;

namespace ClassLibrary.Models.BuildInParameters;
internal class BuildInUtilityModel : IBuildingParameters
{
    public double FloorArea { get; set; }
    public BuildInUtilityModel(Dictionary<string, string> attributes, string[] attributeNames)
    {
        FloorArea = Convert.ToDouble(attributes[attributeNames[0]].Replace(',', '.'), CultureInfo.InvariantCulture);
    }
}
