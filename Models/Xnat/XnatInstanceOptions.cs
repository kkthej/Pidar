namespace Pidar.Models.Xnat;

public sealed class XnatInstanceOptions
{
    public string Key { get; set; } = "";
    public string Name { get; set; } = "";
    public string BaseUrl { get; set; } = "";
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? Username { get; set; }  
    public string? Password { get; set; }  
}