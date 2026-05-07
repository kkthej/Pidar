namespace Pidar.Models.Xnat;

public sealed class XnatProjectsApiResponse
{
    public ResultSetDto ResultSet { get; set; } = new();

    public sealed class ResultSetDto
    {
        public List<ProjectRow> Result { get; set; } = new();
        public string? totalRecords { get; set; }
    }

    public sealed class ProjectRow
    {
        public string ID { get; set; } = "";
        public string? name { get; set; }
        public string? description { get; set; }
        public string? URI { get; set; }
        public string? secondary_ID { get; set; }
        public string? pi_firstname { get; set; }
        public string? pi_lastname { get; set; }
    }
}