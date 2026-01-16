using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Pidar.Models
{
    public class DatasetInfo
    {
        [Key]
        
        public int DatasetId { get; set; }
        // inverse navigation required because your Fluent API uses .WithOne("Dataset")
        public Dataset Dataset { get; set; } = null!;

        public string? Institution { get; set; }
        public string? RorCodeOwner { get; set; }
        public string? Pi { get; set; }
        public string? PiOrchid { get; set; }
        public string? CoPi { get; set; }
        public string? CountryOfInstitution { get; set; }
        public string? ImagingFacility { get; set; }
        public string? EuroBioImagingNode { get; set; }
        public string? CountryOfImagingFacility { get; set; }
        public string? LinkToDataset { get; set; }
        public string? Funding { get; set; }
        public string? FundingAgency { get; set; }
        public string? GrantNumber { get; set; }
        public string? FunderId { get; set; }
        public string? DatasetAccess { get; set; }
        public string? License { get; set; }
        public string? LicenseFile { get; set; }
        public string? DuoDataUsePermission { get; set; }
        public string? DuoDataUseModifier { get; set; }
        public string? DuoInvestigation { get; set; }
        public string? ContactPerson { get; set; }
    }
}
