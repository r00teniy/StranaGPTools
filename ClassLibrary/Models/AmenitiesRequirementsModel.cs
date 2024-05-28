namespace ClassLibrary.Models;
internal class AmenitiesRequirementsModel
{
    public double ChildReqPeople { get; set; }
    public double ChildReqApartments { get; set; }
    public double ChildReqSqm { get; set; }
    public double SportReqPeople { get; set; }
    public double SportReqApartments { get; set; }
    public double SportReqSqm { get; set; }
    public double RestReqPeople { get; set; }
    public double RestReqApartments { get; set; }
    public double RestReqSqm { get; set; }
    public double UtilityReqPeople { get; set; }
    public double UtilityReqApartments { get; set; }
    public double UtilityReqSqm { get; set; }
    public double TrashReqPeople { get; set; }
    public double TrashReqApartments { get; set; }
    public double TrashReqSqm { get; set; }
    public double DogsReqPeople { get; set; }
    public double DogsReqApartments { get; set; }
    public double DogsReqSqm { get; set; }
    public double TotalReqPeople { get; set; }
    public double TotalReqApartments { get; set; }
    public double TotalReqSqm { get; set; }
    public double GreeneryReqPeople { get; set; }
    public double GreeneryReqApartments { get; set; }
    public double GreeneryReqSqm { get; set; }
    /*public AmenitiesForBuildingModel CalculateReqArea(string name, int numberOfPeople, int numberOfApartments, double ApartmentArea)
    {
        double[] values = [8];
        values[0] = numberOfPeople * ChildReqPeople + numberOfApartments * ChildReqApartments + ApartmentArea * ChildReqSqm;
        values[1] = numberOfPeople * SportReqPeople + numberOfApartments * SportReqApartments + ApartmentArea * SportReqSqm;
        values[2] = numberOfPeople * RestReqPeople + numberOfApartments * RestReqApartments + ApartmentArea * RestReqSqm;
        values[3] = numberOfPeople * UtilityReqPeople + numberOfApartments * UtilityReqApartments + ApartmentArea * UtilityReqSqm;
        values[4] = numberOfPeople * TrashReqPeople + numberOfApartments * TrashReqApartments + ApartmentArea * TrashReqSqm;
        values[5] = numberOfPeople * DogsReqPeople + numberOfApartments * DogsReqApartments + ApartmentArea * DogsReqSqm;
        values[6] = numberOfPeople * TotalReqPeople + numberOfApartments * TotalReqApartments + ApartmentArea * TotalReqSqm;
        values[7] = numberOfPeople * GreeneryReqPeople + numberOfApartments * GreeneryReqApartments + ApartmentArea * GreeneryReqSqm;
        return new AmenitiesForBuildingModel(name, values);
    }*/
}
