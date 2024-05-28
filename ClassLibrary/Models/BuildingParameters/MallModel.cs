﻿using System.Globalization;

using ClassLibrary.Interfaces;

namespace ClassLibrary.Models.BuildingParameters;
internal class MallModel : IBuildingParameters
{
    public double FloorArea { get; set; }
    public MallModel(Dictionary<string, string> attributes, string[] attributeNames)
    {
        FloorArea = Convert.ToDouble(attributes[attributeNames[0]].Replace(',', '.'), CultureInfo.InvariantCulture);
    }
}
