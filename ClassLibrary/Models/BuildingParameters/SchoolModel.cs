﻿using System.Globalization;

using ClassLibrary.Interfaces;

namespace ClassLibrary.Models.BuildingParameters;
internal class SchoolModel : IBuildingParameters
{
    public double FloorArea { get; set; }
    public int NumberOfPersonel { get; set; }
    public int NumberOfStudents { get; set; }
    public SchoolModel(Dictionary<string, string> attributes, string[] attributeNames)
    {
        FloorArea = Convert.ToDouble(attributes[attributeNames[0]].Replace(',', '.'), CultureInfo.InvariantCulture);
        NumberOfPersonel = Convert.ToInt32(attributes[attributeNames[1]]);
        NumberOfStudents = Convert.ToInt32(attributes[attributeNames[2]]);
    }
}