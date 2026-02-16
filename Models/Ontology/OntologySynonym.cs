namespace Pidar.Models.Ontology
{
    public class OntologySynonym
    {
        public int Id { get; set; }

        public string Code { get; set; } = null!;
        public string Synonym { get; set; } = null!;
    }
}
