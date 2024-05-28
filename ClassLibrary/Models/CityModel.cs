namespace ClassLibrary.Models;
public class CityModel
{
    public string Name { get; set; } = "";
    public string LongResidentialParkingFormula { get; set; } = "";
    public string ShortResidentialParkingFormula { get; set; } = "";
    public string[] NonResidentialParkingFormulas { get; set; } = [];
    public string[] BuiltInParkingFormulas { get; set; } = [];
    public string DisabledResidentialLongFormula { get; set; } = "";
    public string DisabledResidentialShortFormula { get; set; } = "";
    public string DisabledCommercialShortFormula { get; set; } = "";
    public string DisabledBigResidentialLongFormula { get; set; } = "";
    public string DisabledBigResidentialShortFormula { get; set; } = "";
    public string DisabledBigCommercialShortFormula { get; set; } = "";
    public string ElecticResidentialLongFormula { get; set; } = "";
    public string ElecticResidentialShortFormula { get; set; } = "";
    public string ElecticCommercialShortFormula { get; set; } = "";
    //public SiteRequrementsModel? SiteRequrements { get; set; }{ get; set; } = ""
    public double SquareMetersPerPerson { get; set; } = 0;

}
