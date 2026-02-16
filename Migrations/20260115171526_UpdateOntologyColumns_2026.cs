using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pidar.Migrations
{
    /// <inheritdoc />
    public partial class UpdateOntologyColumns_2026 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"ALTER TABLE public.ontology DROP COLUMN IF EXISTS ""ChebiContrastAgentCommercialName"";");


            // RENAMES (preserve data)
            migrationBuilder.RenameColumn(
                name: "NcitImaging",
                schema: "public",
                table: "ontology",
                newName: "NcitImagingModality");

            migrationBuilder.RenameColumn(
                name: "Doid",
                schema: "public",
                table: "ontology",
                newName: "DoidDiseaseModel");

            migrationBuilder.RenameColumn(
                name: "NcitAnatomy",
                schema: "public",
                table: "ontology",
                newName: "UberonOrganOrTissue");

            migrationBuilder.RenameColumn(
                name: "NcitStrain",
                schema: "public",
                table: "ontology",
                newName: "EfoStrain");

            migrationBuilder.RenameColumn(
                name: "NcitGene",
                schema: "public",
                table: "ontology",
                newName: "GoGene");

            migrationBuilder.RenameColumn(
                name: "ChebiPharmaco",
                schema: "public",
                table: "ontology",
                newName: "ChebiDrug");

            migrationBuilder.RenameColumn(
                name: "ChebiAnesthesia",
                schema: "public",
                table: "ontology",
                newName: "ChebiAnesthetic");

            migrationBuilder.RenameColumn(
                name: "ChebiContrastAgentChemicalName",
                schema: "public",
                table: "ontology",
                newName: "ChebiContrastAgent");

            migrationBuilder.RenameColumn(
                name: "Clo",
                schema: "public",
                table: "ontology",
                newName: "ClCellLineName");

            // ADD NEW COLUMNS
            migrationBuilder.AddColumn<string>(
                name: "UberonImagingCoverage",
                schema: "public",
                table: "ontology",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SwoStatisticalMethods",
                schema: "public",
                table: "ontology",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MpImmuneStatus",
                schema: "public",
                table: "ontology",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NcitGenotype",
                schema: "public",
                table: "ontology",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NcitGeneticManipulation",
                schema: "public",
                table: "ontology",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ObiTargetOrganTissue",
                schema: "public",
                table: "ontology",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ChebiAnalgesic",
                schema: "public",
                table: "ontology",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ObiEuthanasiaMethod",
                schema: "public",
                table: "ontology",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UberonTissueExcised",
                schema: "public",
                table: "ontology",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MpathHistologicalTissueDescription",
                schema: "public",
                table: "ontology",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ObiRouteAdministration",
                schema: "public",
                table: "ontology",
                type: "text",
                nullable: true);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // DROP new columns
            migrationBuilder.DropColumn(name: "UberonImagingCoverage", schema: "public", table: "ontology");
            migrationBuilder.DropColumn(name: "SwoStatisticalMethods", schema: "public", table: "ontology");
            migrationBuilder.DropColumn(name: "MpImmuneStatus", schema: "public", table: "ontology");
            migrationBuilder.DropColumn(name: "NcitGenotype", schema: "public", table: "ontology");
            migrationBuilder.DropColumn(name: "NcitGeneticManipulation", schema: "public", table: "ontology");
            migrationBuilder.DropColumn(name: "ObiTargetOrganTissue", schema: "public", table: "ontology");
            migrationBuilder.DropColumn(name: "ChebiAnalgesic", schema: "public", table: "ontology");
            migrationBuilder.DropColumn(name: "ObiEuthanasiaMethod", schema: "public", table: "ontology");
            migrationBuilder.DropColumn(name: "UberonTissueExcised", schema: "public", table: "ontology");
            migrationBuilder.DropColumn(name: "MpathHistologicalTissueDescription", schema: "public", table: "ontology");
            migrationBuilder.DropColumn(name: "ObiRouteAdministration", schema: "public", table: "ontology");

            // RENAMES back
            migrationBuilder.RenameColumn(name: "NcitImagingModality", schema: "public", table: "ontology", newName: "NcitImaging");
            migrationBuilder.RenameColumn(name: "DoidDiseaseModel", schema: "public", table: "ontology", newName: "Doid");
            migrationBuilder.RenameColumn(name: "UberonOrganOrTissue", schema: "public", table: "ontology", newName: "NcitAnatomy");
            migrationBuilder.RenameColumn(name: "EfoStrain", schema: "public", table: "ontology", newName: "NcitStrain");
            migrationBuilder.RenameColumn(name: "GoGene", schema: "public", table: "ontology", newName: "NcitGene");
            migrationBuilder.RenameColumn(name: "ChebiDrug", schema: "public", table: "ontology", newName: "ChebiPharmaco");
            migrationBuilder.RenameColumn(name: "ChebiAnesthetic", schema: "public", table: "ontology", newName: "ChebiAnesthesia");
            migrationBuilder.RenameColumn(name: "ChebiContrastAgent", schema: "public", table: "ontology", newName: "ChebiContrastAgentChemicalName");
            migrationBuilder.RenameColumn(name: "ClCellLineName", schema: "public", table: "ontology", newName: "Clo");

            // re-add dropped duplicate
            migrationBuilder.AddColumn<string>(
                name: "ChebiContrastAgentCommercialName",
                schema: "public",
                table: "ontology",
                type: "text",
                nullable: true);
        }
    }
}
