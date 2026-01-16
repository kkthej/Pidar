using Pidar.Models;


namespace Pidar.Models.ViewModels
{
    public class DatasetCreateViewModel
    {
        public Dataset Dataset { get; set; } = new();
        public StudyDesign? StudyDesign { get; set; } = new();
        public Publication? Publication { get; set; } = new();
        public StudyComponent? StudyComponent { get; set; } = new();
        public DatasetInfo? DatasetInfo { get; set; } = new();
        public InVivo? InVivo { get; set; } = new();
        public Procedures? Procedures { get; set; } = new();
        public ImageAcquisition? ImageAcquisition { get; set; } = new();
        public ImageData? ImageData { get; set; } = new();
        public ImageCorrelation? ImageCorrelation { get; set; } = new();
        public Analyzed? Analyzed { get; set; } = new();
        public Ontology? Ontology { get; set; } = new();
    }
}
