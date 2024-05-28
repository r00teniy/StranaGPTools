﻿using System.Globalization;

using ClassLibrary.Interfaces;

namespace ClassLibrary.Models.BuildInParameters;
internal class BuildInOfficeModel : IBuildingParameters
{
    public double FloorArea { get; set; }
    public BuildInOfficeModel(Dictionary<string, string> attributes, string[] attributeNames)
    {
        FloorArea = Convert.ToDouble(attributes[attributeNames[0]].Replace(',', '.'), CultureInfo.InvariantCulture);
    }
}