namespace Pidar.Models.Ontology
{
    public class DatasetOntologyTerm
    {
        public int Id { get; set; }

        public int DatasetId { get; set; }
        public Dataset Dataset { get; set; } = null!;

        // Name of the Ontology property (e.g. "UberonOrganOrTissue")
        public string Category { get; set; } = null!;

        // Ontology code (NCIT:Cxxxx, DOID:xxxx, etc.)
        public string Code { get; set; } = null!;
    }
}
