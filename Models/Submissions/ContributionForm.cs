namespace Pidar.Models.Submissions
{
    // =====================================================================================
    // The guided contribution form ("Make your data FAIR"), described once here and used by:
    //   - Views/Submissions/Edit.cshtml       to draw the wizard
    //   - wwwroot/js/contribution-wizard.js   (through data-* attributes) to show/hide fields
    //   - SubmissionService                   to validate on Submit and to build the Dataset on approval
    //
    // It follows metadata_template.xlsx (ver2) and guidelines.docx: same sections, same wording,
    // "black" template rows are Required, "green" rows are optional. Instructions from the
    // template's Comments column are the help text under each field.
    //
    // Field.Path is the "Section.Property" name of the Dataset model (as in the Create form and the
    // Excel prefill). Ontology.* codes are NOT asked from data providers: curators add them after
    // approval. The Disease Category DOID is filled automatically from the chosen category.
    //
    // Required only applies while a field is visible: e.g. "Drug name" is required only when
    // "Pharmacological procedures" is Yes. Hidden answers are kept in the draft (switching back
    // brings them back) but are ignored by validation and never copied into the dataset.
    // =====================================================================================

    public enum FieldKind
    {
        Text,
        TextArea,
        Number,
        Year,
        Email,
        Url,
        Orcid,
        /// <summary>Yes / No buttons, stored "yes" / "no".</summary>
        YesNo,
        /// <summary>One of Options as buttons; AllowOther adds a free-text "Other" box.</summary>
        Choice,
        /// <summary>Several of Options as tick boxes, stored comma-separated; AllowOther adds free text.</summary>
        Multi,
        /// <summary>Drop-down list of Options.</summary>
        Select,
        /// <summary>The 12-group Imaging Modality pick-list (Helpers/ImagingModalityGroups).</summary>
        ImagingModality,
        /// <summary>The Disease Category drop-down (Helpers/DiseaseCategories).</summary>
        DiseaseCategory,
    }

    public sealed record FieldOption(string Value, string Label, string? Description = null);

    /// <summary>Visible when the answer to <see cref="Field"/> is one of <see cref="Values"/> (case-insensitive).</summary>
    public sealed record Condition(string Field, IReadOnlyList<string> Values)
    {
        public bool Matches(string? answer)
        {
            var a = (answer ?? "").Trim();
            return Values.Any(v => string.Equals(v, a, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>For data-show-if: "Field|value1|value2".</summary>
        public string ToAttribute() => Field + "|" + string.Join("|", Values);
    }

    public sealed class FormField
    {
        public string Path { get; init; } = "";
        public string Label { get; init; } = "";
        public string? Help { get; init; }
        public FieldKind Kind { get; init; } = FieldKind.Text;
        public bool Required { get; set; }
        public Condition? ShowIf { get; set; }
        public IReadOnlyList<FieldOption> Options { get; init; } = Array.Empty<FieldOption>();
        public bool AllowOther { get; init; }
        public string? Placeholder { get; set; }
        /// <summary>Suggestions shown while typing (HTML datalist); any text is still accepted.</summary>
        public IReadOnlyList<string> Suggestions { get; set; } = Array.Empty<string>();

        /// <summary>DOM-safe id, e.g. "f-Publication-PaperTitle".</summary>
        public string DomId => "f-" + Path.Replace('.', '-');

        public FormField Req() { Required = true; return this; }
        public FormField When(string field, params string[] values) { ShowIf = new Condition(field, values); return this; }
        public FormField Hint(string placeholder) { Placeholder = placeholder; return this; }
        public FormField Suggest(params string[] items) { Suggestions = items; return this; }
    }

    public sealed class FormGroup
    {
        public string Title { get; init; } = "";
        public string? Intro { get; init; }
        public Condition? ShowIf { get; init; }
        public IReadOnlyList<FormField> Fields { get; init; } = Array.Empty<FormField>();
    }

    public sealed class FormStep
    {
        public string Key { get; init; } = "";
        public string Title { get; init; } = "";
        public string Icon { get; init; } = "bi-circle";
        public string? Intro { get; init; }
        public IReadOnlyList<FormGroup> Groups { get; init; } = Array.Empty<FormGroup>();

        public IEnumerable<FormField> Fields => Groups.SelectMany(g => g.Fields);
    }

    public static class ContributionForm
    {
        /// <summary>Payload key for the provider's free-text note to the PIDAR team (not copied to the dataset).</summary>
        public const string CommentKey = "_comment";

        /// <summary>Longest answer accepted for one field (characters).</summary>
        public const int MaxValueLength = 8000;

        // ---------------------------------------------------------------- option lists

        private static readonly FieldOption[] YesNoOptions = { new("yes", "Yes"), new("no", "No") };

        private static FieldOption[] Opts(params string[] values) =>
            values.Select(v => new FieldOption(v, char.ToUpper(v[0]) + v[1..])).ToArray();

        public static readonly string[] Countries =
        {
            "Austria", "Belgium", "Bulgaria", "Croatia", "Cyprus", "Czech Republic", "Denmark", "Estonia",
            "Finland", "France", "Germany", "Greece", "Hungary", "Iceland", "Ireland", "Israel", "Italy",
            "Latvia", "Lithuania", "Luxembourg", "Malta", "Netherlands", "Norway", "Poland", "Portugal",
            "Romania", "Slovakia", "Slovenia", "Spain", "Sweden", "Switzerland", "Turkey", "United Kingdom",
            "Australia", "Brazil", "Canada", "China", "India", "Japan", "South Korea", "United States"
        };

        // Stored values follow the existing PIDAR data ("CC BY", "CC BY NC", …) so the licence logos keep working
        private static readonly FieldOption[] Licenses =
        {
            new("CC BY", "CC BY 4.0 – Attribution (recommended for public datasets)"),
            new("CC BY SA", "CC BY-SA 4.0 – Attribution, ShareAlike"),
            new("CC BY NC", "CC BY-NC 4.0 – Attribution, NonCommercial"),
            new("CC BY NC SA", "CC BY-NC-SA 4.0 – Attribution, NonCommercial, ShareAlike"),
            new("CC BY ND", "CC BY-ND 4.0 – Attribution, NoDerivatives"),
            new("CC BY NC ND", "CC BY-NC-ND 4.0 – Attribution, NonCommercial, NoDerivatives"),
            new("CC0", "CC0 1.0 – Public domain dedication"),
        };

        // Data Use Ontology terms from the template's "DUO ontology" sheet, grouped as there
        private static readonly FieldOption[] DuoPermission =
        {
            new("DUO:0000042", "General research use", "Use is allowed for health/medical/biomedical purposes and other biological research, including the study of population origins or ancestry."),
            new("DUO:0000004", "No restriction", "There is no restriction on use."),
            new("DUO:0000006", "Health/medical/biomedical research and clinical care", "Use is allowed for health/medical/biomedical purposes; does not include the study of population origins or ancestry."),
            new("DUO:0000007", "Disease-specific research and clinical care", "Use is allowed provided it is related to the specified disease."),
            new("DUO:0000011", "Population origins or ancestry research", "Use is limited to the study of population origins or ancestry."),
        };

        private static readonly FieldOption[] DuoModifier =
        {
            new("DUO:0000012", "Research-specific restrictions", "Use is limited to studies of a certain research type."),
            new("DUO:0000015", "No general methods research", "Methods development research (e.g. software or algorithms) only within the bounds of other use limitations."),
            new("DUO:0000016", "Genetic studies only", "Use is limited to genetic studies only."),
            new("DUO:0000018", "Not-for-profit use only", "Use is limited to not-for-profit organizations and not-for-profit use."),
            new("DUO:0000019", "Publication required", "Requestor agrees to make results of studies using the data available to the larger scientific community."),
            new("DUO:0000020", "Collaboration required", "Requestor must agree to collaboration with the primary study investigator(s)."),
            new("DUO:0000021", "Ethics approval required", "Requestor must provide documentation of local IRB/ERB approval."),
            new("DUO:0000022", "Geographical restriction", "Use is limited to within a specific geographic region."),
            new("DUO:0000024", "Publication moratorium", "Requestor agrees not to publish results of studies until a specific date."),
            new("DUO:0000025", "Time limit on use", "Use is approved for a specific number of months."),
            new("DUO:0000026", "User-specific restriction", "Use is limited to use by approved users."),
            new("DUO:0000027", "Project-specific restriction", "Use is limited to use within an approved project."),
            new("DUO:0000028", "Institution-specific restriction", "Use is limited to use within an approved institution."),
            new("DUO:0000029", "Return to database/resource", "Requestor must return derived/enriched data to the database/resource."),
            new("DUO:0000043", "Clinical care use", "Use is allowed for clinical use and care."),
            new("DUO:0000044", "Population origins or ancestry research prohibited", "Use for purposes of population, origin, or ancestry research is prohibited."),
            new("DUO:0000045", "Not-for-profit organisation use only", "Use is limited to not-for-profit organizations."),
            new("DUO:0000046", "Non-commercial use only", "Use is limited to non-commercial use."),
        };

        private static readonly FieldOption[] DuoInvestigation =
        {
            new("DUO:0000037", "Biomedical research", "Health, medical, or biomedical research."),
            new("DUO:0000040", "Disease category research", "Biomedical research concerning specific disease(s)."),
            new("DUO:0000039", "Drug development research", "Biomedical research concerning drug development."),
            new("DUO:0000038", "Genetic research", "Biomedical research concerning genetics."),
            new("DUO:0000031", "Method development", "Development of methods, algorithms, software or analytical tools."),
            new("DUO:0000036", "Research control", "Use of data as reference or control material."),
            new("DUO:0000034", "Age category research", "Investigation concerning specific age categories."),
            new("DUO:0000035", "Gender category research", "Investigation concerning specific gender categories."),
            new("DUO:0000032", "Population research", "Investigation concerning a specific population group."),
            new("DUO:0000033", "Ancestry research", "Investigation concerning ancestry or population origins."),
        };

        // ---------------------------------------------------------------- field builders

        private static FormField Text(string path, string label, string? help = null) => new() { Path = path, Label = label, Help = help, Kind = FieldKind.Text };
        private static FormField Area(string path, string label, string? help = null) => new() { Path = path, Label = label, Help = help, Kind = FieldKind.TextArea };
        private static FormField Num(string path, string label, string? help = null) => new() { Path = path, Label = label, Help = help, Kind = FieldKind.Number };
        private static FormField YesNo(string path, string label, string? help = null) => new() { Path = path, Label = label, Help = help, Kind = FieldKind.YesNo, Options = YesNoOptions };
        private static FormField Choice(string path, string label, string? help, FieldOption[] options, bool other = false) => new() { Path = path, Label = label, Help = help, Kind = FieldKind.Choice, Options = options, AllowOther = other };
        private static FormField Multi(string path, string label, string? help, FieldOption[] options, bool other = false) => new() { Path = path, Label = label, Help = help, Kind = FieldKind.Multi, Options = options, AllowOther = other };

        private static Condition If(string field, params string[] values) => new(field, values);

        // ---------------------------------------------------------------- the form

        public static readonly IReadOnlyList<FormStep> Steps = Build();

        private static IReadOnlyList<FormStep> Build() => new List<FormStep>
        {
            // 1 ───────────────────────────────────────────────────────── STUDY DESIGN
            new()
            {
                Key = "study", Title = "Study design", Icon = "bi-journal-text",
                Intro = "What the study is about, and whether it is linked to a publication.",
                Groups = new[]
                {
                    new FormGroup
                    {
                        Title = "Background",
                        Fields = new[]
                        {
                            Text("StudyDesign.StudyDescription", "Study name / short description", "e.g. the study name, as you would like it listed in PIDAR.").Req(),
                            Area("StudyDesign.StudyDesignBackground", "Study design / background", "Type of the overall study, which may include other imaging and/or non-imaging data (free text, a few sentences).").Req(),
                            Choice("StudyDesign.StudyType", "Study type", null, Opts("preclinical", "clinical")).Req(),
                            Multi("StudyDesign.StudySubtype", "Study subtype", "Tick all that apply.", Opts("in vivo", "in vitro", "ex vivo", "postmortem")).Req(),
                        }
                    },
                    new FormGroup
                    {
                        Title = "Publication",
                        Fields = new[]
                        {
                            Choice("Publication.PaperLinked", "Is a paper linked to this dataset?", "Choose \"in progress\" if a manuscript is being prepared or under review.",
                                new FieldOption[] { new("yes", "Yes"), new("in progress", "In progress"), new("no", "No") }).Req(),
                            Text("Publication.PaperTitle", "Paper title").Req().When("Publication.PaperLinked", "yes", "in progress"),
                            Area("Publication.PaperAuthors", "Authors", "List of the authors, e.g. Surname, Name, Surname2, Name2, …").Req().When("Publication.PaperLinked", "yes"),
                            Text("Publication.Affiliation", "Affiliation", "Institute or University.").Req().When("Publication.PaperLinked", "yes"),
                            Text("Publication.PaperJournal", "Journal", "Full journal name, e.g. Magnetic Resonance in Medicine.").Req().When("Publication.PaperLinked", "yes"),
                            new FormField { Path = "Publication.PaperYear", Label = "Year of publication", Kind = FieldKind.Year, Required = true, ShowIf = If("Publication.PaperLinked", "yes") },
                            Text("Publication.PaperDoi", "DOI", "Digital object identifier, e.g. 10.1002/mrm.12345.").Req().When("Publication.PaperLinked", "yes").Hint("10.xxxx/…"),
                            YesNo("Publication.OpenAccess", "Open access?").Req().When("Publication.PaperLinked", "yes"),
                        }
                    }
                }
            },

            // 2 ───────────────────────────────────────────────────────── STUDY COMPONENT
            new()
            {
                Key = "component", Title = "Imaging technique", Icon = "bi-camera",
                Intro = "How the images were acquired.",
                Groups = new[]
                {
                    new FormGroup
                    {
                        Title = "Imaging technique",
                        Fields = new[]
                        {
                            YesNo("StudyComponent.MultiModalityImages", "Multi-modality images?", "Does the dataset contain images from more than one modality?").Req(),
                            new FormField { Path = "StudyComponent.ImagingModality", Label = "Imaging modality", Kind = FieldKind.ImagingModality, Required = true,
                                Help = "Technique used to acquire the image data. Use PET/CT, SPECT/CT or PET/MRI for hybrid scanners." },
                            Text("StudyComponent.ImagingSubModality", "Imaging sub-modality",
                                "e.g. T1w, T2w, T1CE, DCE, DWI, DTI, ASL, CEST, 19F, 13C-HP, 31P for MRI; DECT, PCCT, SR-CT, phase-contrast CT for CT.").Req(),
                            YesNo("StudyComponent.Radiation", "Ionising radiation used?"),
                            Text("StudyComponent.ImagingCoverage", "Imaging coverage", "Whole body or a specific organ (e.g. brain, liver, lungs).").Req()
                                .Suggest("Whole body", "Brain", "Head", "Heart", "Thorax", "Lungs", "Abdomen", "Liver", "Kidneys", "Tumour", "Hind limb"),
                            Text("StudyComponent.ImagingTarget", "Imaging target", "Target of acquisition, e.g. receptor expression, enzyme activity (glycolysis), blood flow, anatomy.").Req(),
                        }
                    }
                }
            },

            // 3 ───────────────────────────────────────────────────────── DATASET INFORMATION
            new()
            {
                Key = "dataset", Title = "Dataset & access", Icon = "bi-building",
                Intro = "Who holds the dataset, how it was funded, and how others may use it.",
                Groups = new[]
                {
                    new FormGroup
                    {
                        Title = "Institution and people",
                        Fields = new[]
                        {
                            Text("DatasetInfo.Institution", "Institution", "University of … / Center for … / Hospital …").Req(),
                            new FormField { Path = "DatasetInfo.RorCodeOwner", Label = "ROR identifier of the institution", Kind = FieldKind.Url,
                                Help = "Find it on https://ror.org (search your institution and copy its link).", Placeholder = "https://ror.org/…" },
                            Text("DatasetInfo.Pi", "Principal Investigator (PI)", "Name Surname.").Req(),
                            new FormField { Path = "DatasetInfo.PiOrcid", Label = "PI ORCID", Kind = FieldKind.Orcid,
                                Help = "ORCID iD of the PI (https://orcid.org).", Placeholder = "0000-0000-0000-0000" },
                            Text("DatasetInfo.CoPi", "Co-PI", "Name Surname."),
                            Text("DatasetInfo.CountryOfInstitution", "Country of the institution", "Country of the institution holding the dataset.").Req().Suggest(Countries),
                            Text("DatasetInfo.ImagingFacility", "Imaging facility", "Name of the imaging facility where the images were acquired.").Req(),
                            Text("DatasetInfo.EuroBioImagingNode", "Euro-BioImaging node", "If the images were acquired in a Euro-BioImaging node, its name."),
                            Text("DatasetInfo.CountryOfImagingFacility", "Country of the imaging facility", "Country where the images were acquired.").Req().Suggest(Countries),
                            new FormField { Path = "DatasetInfo.LinkToDataset", Label = "Link to the image dataset", Kind = FieldKind.Url,
                                Help = "e.g. the XNAT project or repository where the images are stored, if available.", Placeholder = "https://…" },
                            new FormField { Path = "DatasetInfo.ContactPerson", Label = "Contact person email", Kind = FieldKind.Email, Required = true,
                                Help = "Who we (and future users) can ask for information about the dataset. Filled with your email; change it if needed." },
                        }
                    },
                    new FormGroup
                    {
                        Title = "Funding",
                        Fields = new[]
                        {
                            YesNo("DatasetInfo.Funding", "Was the dataset acquired thanks to a grant or fund?").Req(),
                            Text("DatasetInfo.FundingAgency", "Funding agency", "If more than one, separate with commas: XX, YY.").When("DatasetInfo.Funding", "yes"),
                            Text("DatasetInfo.GrantNumber", "Grant number", "If more than one, separate with commas in the same order as the agencies.").When("DatasetInfo.Funding", "yes"),
                            Text("DatasetInfo.FunderId", "Funder ID", "Code of the funding agency in the Open Funder Registry (https://www.crossref.org/services/funder-registry/).").When("DatasetInfo.Funding", "yes"),
                        }
                    },
                    new FormGroup
                    {
                        Title = "Access and licence",
                        Intro = "To keep PIDAR FAIR, we recommend a licence that is as open as possible (e.g. CC BY).",
                        Fields = new[]
                        {
                            Choice("DatasetInfo.DatasetAccess", "Dataset access", null, Opts("public", "limited")).Req(),
                            new FormField { Path = "DatasetInfo.License", Label = "Licence", Kind = FieldKind.Select, Options = Licenses, Required = true,
                                ShowIf = If("DatasetInfo.DatasetAccess", "public"),
                                Help = "Usually CC BY is the right choice for a public dataset. See https://creativecommons.org/licenses/." },
                            Area("DatasetInfo.LicenseFile", "Access conditions / licence document",
                                "For limited access: describe the conditions or give a link to the licence/agreement document. You can also send us the PDF when we review your submission.")
                                .Req().When("DatasetInfo.DatasetAccess", "limited"),
                            Multi("DatasetInfo.DuoDataUsePermission", "Data use permission (DUO)", "How may the data be used? Choose one or more.", DuoPermission).Req(),
                            Multi("DatasetInfo.DuoDataUseModifier", "Data use conditions (DUO)", "Additional conditions for use, if any.", DuoModifier),
                            Multi("DatasetInfo.DuoInvestigation", "Type of investigation (DUO)", "What kind of investigation is the dataset associated with? Choose one or more.", DuoInvestigation).Req(),
                        }
                    }
                }
            },

            // 4 ───────────────────────────────────────────────────────── IN VIVO
            new()
            {
                Key = "invivo", Title = "Animals & study design", Icon = "bi-clipboard2-pulse",
                Intro = "In vivo experimental parameters (IVEP): groups, disease model and animals.",
                Groups = new[]
                {
                    new FormGroup
                    {
                        Title = "Groups and disease model",
                        Fields = new[]
                        {
                            Num("InVivo.NumberOfGroups", "Number of groups", "How many groups or cohorts.").Req(),
                            Text("InVivo.TypesOfGroups", "Types of groups", "Treated groups, positive or negative control groups, knock-down, etc., separated by commas.").Req(),
                            Num("InVivo.OverallSampleSize", "Overall sample size", "Overall number of animals, including all groups.").Req(),
                            Text("InVivo.SampleSizeForEachGroup", "Sample size of each group",
                                "e.g. 10, 10 for control and treated group (genetically modified vs wild-type, active treatment vs placebo, surgery vs sham, …).").Req(),
                            Choice("InVivo.AnimalCondition", "Animal condition", null, Opts("healthy animals", "disease model")).Req(),
                            new FormField { Path = "InVivo.DiseaseCategory", Label = "Main disease category", Kind = FieldKind.DiseaseCategory, Required = true,
                                Help = "Choose \"Healthy\" for healthy animals with no induced or spontaneous disease, including healthy controls." },
                            Text("InVivo.DiseaseModel", "Specific disease model",
                                "e.g. breast cancer, acute kidney disease, bacterial gastritis, inflammatory bowel disease, lactic acidosis, Wolfram syndrome.")
                                .Req().When("InVivo.AnimalCondition", "disease model"),
                            Text("InVivo.OrganOrTissue", "Organ or tissue", "e.g. liver, lung, heart, mammary gland.").Req(),
                            Area("InVivo.PowerCalculation", "Power calculation", "The power calculation used, if any."),
                            Area("InVivo.InclusionCriteria", "Inclusion criteria", "Short description of the inclusion criteria used."),
                            Area("InVivo.ExclusionCriteria", "Exclusion criteria", "Short description of the exclusion criteria used."),
                            Text("InVivo.Randomization", "Randomisation", "No — or Yes, with the method used (e.g. Yes, block randomisation by body weight).").Req(),
                            YesNo("InVivo.Blinding", "Blinding?").Req(),
                            Area("InVivo.ProceduresToKeepTreatmentsBlind", "Procedures to keep treatments blind",
                                "e.g. the experimenter had no access to animal identities until the end of the study.").When("InVivo.Blinding", "yes"),
                            Area("InVivo.ProceduresToKeepExperimenterBlind", "Procedures to keep the experimenter blind").When("InVivo.Blinding", "yes"),
                            Area("InVivo.OutcomeMeasures", "Outcome measures", "e.g. volume of damaged tissue, number of dead cells, specific molecular marker or behavioural changes."),
                            Area("InVivo.StatisticalMethods", "Statistical methods", "Which statistical analyses were performed, with references for the methods used."),
                        }
                    },
                    new FormGroup
                    {
                        Title = "Subject details",
                        Fields = new[]
                        {
                            Text("InVivo.Species", "Species").Req().Suggest("Mouse", "Rat", "Zebrafish", "Xenopus", "Drosophila", "C. elegans", "Rabbit", "Pig", "Sheep", "Non-human primate"),
                            Text("InVivo.Strain", "Strain", "e.g. NOD-scid, Balb/c, NSG, C57BL/6.").Req(),
                            Choice("InVivo.ImmuneStatus", "Immune status", null, Opts("immunocompetent", "immunodeficient")).Req(),
                            Multi("InVivo.Sex", "Sex", null, Opts("male", "female")).Req(),
                            Text("InVivo.Age", "Age", "e.g. 6 weeks old, or the developmental stage such as \"embryo\" or \"juvenile\".").Req(),
                            Text("InVivo.AgeAtStartExperiment", "Age at start of experiment", "In days for preclinical studies."),
                            Text("InVivo.AgeAtScanningExperimentS", "Age at scanning session(s)", "Age at each scanning session, e.g. 14, 20 days."),
                            Text("InVivo.Weight", "Weight", "In grams (preclinical).").Req(),
                            Text("InVivo.WeightAtStartExperiment", "Weight at start of experiment", "In grams."),
                            Text("InVivo.WeightAtEndExperiment", "Weight at end of experiment", "In grams."),
                            Choice("InVivo.Genotype", "Genotype", null, Opts("wild type", "homozygous", "heterozygous"), other: true).Req(),
                            Text("InVivo.GeneticManipulation", "Genetic manipulation",
                                "knockout / knock-in transgenic, constitutive / inducible, Cre line, doxycycline / tamoxifen. Write \"none\" if not applicable.").Req(),
                            Text("InVivo.Gene", "Gene", "Name of the gene. Write \"none\" if not applicable.").Req(),
                            Text("InVivo.SourceOfAnimals", "Source of animals", "Commercial vendor / collaborator / in-house breeding."),
                            Text("InVivo.RegistryNumberOfAnimalAuthorization", "Animal authorisation number", "Registry number of the authorisation for the animal experiments."),
                        }
                    }
                }
            },

            // 5 ───────────────────────────────────────────────────────── EXPERIMENTAL PROCEDURES
            new()
            {
                Key = "procedures", Title = "Experimental procedures", Icon = "bi-eyedropper",
                Intro = "Answer Yes or No for each procedure; details are asked only for the ones you used.",
                Groups = new[]
                {
                    Gate("a) Pharmacological procedures (intervention and control)", "Procedures.PharmacologicalProceduresInterventionAndControl", new[]
                    {
                        Text("Procedures.PharmacologicalDrug", "Drug name", "Commercial name of the drug.").Req(),
                        Text("Procedures.Company", "Company", "Company producing/delivering the drug."),
                        Text("Procedures.Formulation", "Formulation"),
                        Text("Procedures.DrugDose", "Dose", "mg/kg").Req(),
                        Text("Procedures.Volume", "Volume", "ml"),
                        Text("Procedures.Concentration", "Concentration", "Concentration of the drug in the vehicle (mM)."),
                        Text("Procedures.SiteOrRouteOfAdministration", "Site / route of administration", "e.g. oral, intravenous, intraperitoneal.").Req()
                            .Suggest("oral", "intravenous", "intraperitoneal", "subcutaneous", "intramuscular"),
                        Text("Procedures.FrequencyOfAdministration", "Frequency of administration", "Times per week or per month.").Req(),
                        Text("Procedures.VehicleOrCarrierSolutionFormulation", "Vehicle or carrier solution", "Vehicle used to dissolve the drug.").Req(),
                        Text("Procedures.DrugOrBatchSampleNumber", "Drug batch / sample number"),
                    }),
                    Gate("b) Blood sampling", "Procedures.BloodSampling", new[]
                    {
                        Text("Procedures.BloodSamplingMethod", "Blood sampling method"),
                        Text("Procedures.BloodSampleVolume", "Blood sample volume"),
                        Text("Procedures.BloodTiming", "How many times was blood sampled?"),
                        Text("Procedures.BloodCollectionTiming", "Time of day of collection", "When the blood was collected during the day (circadian rhythm)."),
                    }),
                    Gate("c) Surgical procedures (including sham surgery)", "Procedures.SurgicalProceduresIncludingShamSurgery", new[]
                    {
                        Area("Procedures.DescriptionOfTheSurgicalProcedure", "Description of the surgical procedure"),
                        Text("Procedures.ReferenceToProtocol", "Reference to protocol", "PMID or DOI."),
                        Text("Procedures.TargetOrganTissue", "Target organ / tissue").Req(),
                    }),
                    Gate("d) Pathogen infection (intervention and control)", "Procedures.PathogenInfectionInterventionAndControl", new[]
                    {
                        Choice("Procedures.InfectiousType", "Infectious type", "Toxic (possibly hazardous agents), GMO (genetically modified material) or BM (biological agents).", Opts("Toxic", "GMO", "BM"), other: true),
                        Text("Procedures.InfectiousAgent", "Infectious agent").Req(),
                        Text("Procedures.DoseLoad", "Dose load").Req(),
                        Text("Procedures.SiteAndRouteOfInfection", "Site and route of infection").Req(),
                        Text("Procedures.TimingOrFrequencyOfInfection", "Timing or frequency of infection").Req(),
                    }),
                    Gate("e) Analgesic plan to relieve pain, suffering and distress", "Procedures.AnalgesicPlanToRelievePainSufferingAndDistress", new[]
                    {
                        Text("Procedures.AnalgesicName", "Analgesic name", "Commercial name of the analgesic drug.").Req(),
                        Text("Procedures.Route", "Route", "e.g. oral, intravenous, intraperitoneal, intramuscular."),
                        Text("Procedures.AnalgesicDose", "Dose", "mg/kg"),
                    }),
                    Gate("f) Anesthesia (for imaging)", "Procedures.AnesthesiaForImaging", new[]
                    {
                        Choice("Procedures.AnesthesiaType", "Type", null, Opts("gas", "intramuscular", "intraperitoneal", "intravenous"), other: true).Req(),
                        Text("Procedures.Duration", "Duration", "In minutes."),
                        Text("Procedures.AnesthesiaDrugs", "Drugs", "Commercial name of the anesthetic(s), e.g. isoflurane.").Req(),
                        Text("Procedures.AnesthesiaDose", "Dose", "ml/min for gas, or mg/kg for intramuscular and intraperitoneal.").Req(),
                        Text("Procedures.MonitoringRegime", "Monitoring regime", "e.g. respiratory rate, cardiac rate."),
                    }),
                    Gate("g) Euthanasia", "Procedures.Euthanasia", new[]
                    {
                        Text("Procedures.Method", "Method", "How the animals were sacrificed.").Req(),
                    }),
                    Gate("h) Histology", "Procedures.Histology", new[]
                    {
                        Text("Procedures.TissuesCollectedPostEuthanasia", "Tissues collected post-euthanasia", "Tissue(s) excised.").Req(),
                        Text("Procedures.TimingOfCollection", "Timing of collection").Req(),
                        Text("Procedures.TissueDescription", "Tissue description", "Type of tissue findings, e.g. hyperplasia, haemorrhage, fibrosis, inflammation.").Req(),
                        YesNo("Procedures.TissuePerfused", "Tissue perfused?").Req(),
                        Text("Procedures.PerfusionMethod", "Perfusion method", "e.g. ice-cold PBS, saline, PFA.").When("Procedures.TissuePerfused", "yes"),
                        Choice("Procedures.HistologicalProcedure", "Histological procedure", null, Opts("frozen", "fixed (formalin)"), other: true),
                        Text("Procedures.NameOfReagentS", "Name of reagent(s)", "List of markers, e.g. Ki67 or Ki67/CD31."),
                        Text("Procedures.CatalogueNumber", "Catalogue number"),
                        Text("Procedures.LengthOfFixation", "Length of fixation"),
                        Text("Procedures.SpecimenThickness", "Specimen thickness", "Section thickness in micrometres."),
                    }),
                    Gate("i) Imaging", "Procedures.Imaging", new[]
                    {
                        Text("Procedures.FrequencyOfImaging", "Frequency of imaging", "e.g. 2 per week, every three days.").Req(),
                        Text("Procedures.TimingOfImaging", "Timing of imaging", "e.g. for cancer studies: time from cancer cell inoculation.").Req(),
                        Text("Procedures.OverallScanLength", "Overall scan length", "Overall duration of the session, in minutes."),
                        YesNo("Procedures.ContrastAgentOrRadioIsotopeOrChallengeWithGasMolecule", "Contrast agent, radio-isotope or challenge with gas/molecule?").Req(),
                        Text("Procedures.ContrastAgentCommercialDrug", "Commercial name", "e.g. Isovue.").Req().When("Procedures.ContrastAgentOrRadioIsotopeOrChallengeWithGasMolecule", "yes"),
                        Text("Procedures.ContrastAgentChemicalDrug", "Chemical name", "e.g. iopamidol.").Req().When("Procedures.ContrastAgentOrRadioIsotopeOrChallengeWithGasMolecule", "yes"),
                        Text("Procedures.ContrastAgentDose", "Dose", "g/kg, and the activity of the radiotracer (MBq) for PET and SPECT.").Req().When("Procedures.ContrastAgentOrRadioIsotopeOrChallengeWithGasMolecule", "yes"),
                        Text("Procedures.InjectionVolume", "Injection volume", "In ml.").Req().When("Procedures.ContrastAgentOrRadioIsotopeOrChallengeWithGasMolecule", "yes"),
                        Text("Procedures.InjectionTime", "Injection time", "Time taken for the injection, e.g. 12 seconds.").When("Procedures.ContrastAgentOrRadioIsotopeOrChallengeWithGasMolecule", "yes"),
                        Text("Procedures.Vehicle", "Vehicle", "Vehicle in which the contrast agent was dissolved.").When("Procedures.ContrastAgentOrRadioIsotopeOrChallengeWithGasMolecule", "yes"),
                        Text("Procedures.RouteOfAdministration", "Route of administration", null).Req().When("Procedures.ContrastAgentOrRadioIsotopeOrChallengeWithGasMolecule", "yes")
                            .Suggest("intravenous", "intramuscular", "oral", "intraperitoneal", "retrobulbar", "inhalation"),
                    }),
                }
            },

            // 6 ───────────────────────────────────────────────────────── RESOURCES + ADDITIONAL INFO
            new()
            {
                Key = "resources", Title = "Resources & other info", Icon = "bi-box-seam",
                Intro = "Cell lines, reagents and equipment used, and other experimental information.",
                Groups = new[]
                {
                    Gate("a) Cell lines", "Procedures.CellLines", new[]
                    {
                        Text("Procedures.CellLine", "Cell line name").Req(),
                        Text("Procedures.Provenance", "Provenance", "Source of the cell line.").Req(),
                        Text("Procedures.CellCultureMedium", "Cell culture medium", "e.g. DMEM, RPMI, DMEM F12.").Req(),
                        YesNo("Procedures.ModifiedCellLine", "Modified cell line?"),
                        Text("Procedures.TypeOfGeneticModification", "Type of genetic modification", "KO / KD / silenced / constitutive / inducible.").When("Procedures.ModifiedCellLine", "yes"),
                        Text("Procedures.GeneModified", "Gene modified").When("Procedures.ModifiedCellLine", "yes"),
                        Text("Procedures.VirusLabelledOrModified", "Virus-labelled or modified", "e.g. AAV or lentivirus.").When("Procedures.ModifiedCellLine", "yes"),
                        Text("Procedures.VerificationAndAuthentication", "Verification and authentication"),
                        Text("Procedures.CellInjectionRoute", "Cell injection route", "e.g. for cancer cells: subcutaneous, orthotopic, intracardiac."),
                        Text("Procedures.CellInjectionProcedure", "Cell injection procedure", "e.g. cells only, or with Matrigel."),
                        Text("Procedures.NumberOfCells", "Number of injected cells"),
                    }, required: true),
                    Gate("b) Reagents", "Procedures.Reagents", new[]
                    {
                        Text("Procedures.NameOfReagent", "Name of reagent(s)", "Chemical name(s)."),
                        Text("Procedures.CatalogueNumbers", "Catalogue number(s)"),
                    }, required: false),
                    Gate("c) Equipment and software", "Procedures.EquipmentAndSoftware", new[]
                    {
                        Text("Procedures.Manufacturer", "Manufacturer", "Vendor / manufacturer, e.g. of stereotaxic instrumentation."),
                        Text("Procedures.ModelVersionNumber", "Model / version number"),
                    }, required: false),
                    new FormGroup
                    {
                        Title = "Additional information",
                        Fields = new[]
                        {
                            Text("Procedures.FrequencyOfExperimentalProcedures", "Frequency of experimental procedures"),
                            Text("Procedures.TimingOfExperimentalProcedures", "Timing of experimental procedures"),
                            Text("Procedures.FrequencyOfExperimentalMeasurements", "Frequency of experimental measurements"),
                            Text("Procedures.TimingOfExperimentalMeasurements", "Timing of experimental measurements"),
                            Choice("Procedures.HousingRoom", "Housing room", null, new FieldOption[] { new("SPF", "SPF (specific pathogen free)"), new("germ free", "Germ free"), new("not SPF", "Not SPF") }, other: true),
                            YesNo("Procedures.DietaryIntervention", "Dietary intervention?"),
                            YesNo("Procedures.RespirationRate", "Respiration rate available?"),
                            YesNo("Procedures.BodyTempuratureEtc", "Body temperature (etc.) available?"),
                            YesNo("Procedures.FoodIntakeMeasured", "Food intake measured?"),
                        }
                    }
                }
            },

            // 7 ───────────────────────────────────────────────────────── IMAGE ACQUISITION
            new()
            {
                Key = "acquisition", Title = "Image acquisition", Icon = "bi-cpu",
                Intro = "The scanner and the acquisition settings.",
                Groups = new[]
                {
                    new FormGroup
                    {
                        Title = "Instrument and parameters",
                        Fields = new[]
                        {
                            Text("ImageAcquisition.InstrumentVendor", "Instrument vendor").Req()
                                .Suggest("Bruker", "MILabs", "Mediso", "Siemens", "GE HealthCare", "Philips", "FUJIFILM VisualSonics", "PerkinElmer / Revvity", "iThera Medical", "MR Solutions", "Aspect Imaging"),
                            Text("ImageAcquisition.InstrumentType", "Instrument type", "Details about the instrument used, e.g. Pharmascan 70/16.").Req(),
                            Area("ImageAcquisition.InstrumentSpecifics", "Instrument specifics", "e.g. field strength for MRI, FOV size for PET, collimator details for SPECT.").Req(),
                            Area("ImageAcquisition.ImageAcquisitionParameters", "Image acquisition parameters",
                                "e.g. energy window for PET and SPECT; tube voltage and current (kVp, µA), number of projections, exposure time (ms) and filter (e.g. 1 mm aluminium) for CT; excitation and emission filters for optical imaging.").Req(),
                            Area("ImageAcquisition.Correction", "Corrections",
                                "e.g. for PET: decay, random, dead time, attenuation correction; for CT: beam hardening, ring artefact correction.").Req(),
                            YesNo("ImageAcquisition.RawData", "Raw data available?", "Are raw data available for custom reconstruction?").Req(),
                            YesNo("ImageAcquisition.QaQc", "QA/QC information available?", "Is information on quality assurance / quality check available?"),
                        }
                    }
                }
            },

            // 8 ───────────────────────────────────────────────────────── IMAGE DATA + CORRELATION + ANALYSED
            new()
            {
                Key = "images", Title = "Image data & analysis", Icon = "bi-images",
                Intro = "The images in the dataset, how they were processed, and any analysed data.",
                Groups = new[]
                {
                    new FormGroup
                    {
                        Title = "Image data",
                        Fields = new[]
                        {
                            Multi("ImageData.ImageType", "Image type", null, Opts("primary image", "processed image", "segmentation"), other: true).Req(),
                            Text("ImageData.ImageScale", "Image scale", "e.g. HU for CT, kBq/cc or SUV for PET.").Req(),
                            Multi("ImageData.FormatCompression", "File format & compression", null, Opts("DICOM", "NIfTI", "raw", "Analyze", "TIFF", "Bruker ParaVision"), other: true).Req(),
                            Multi("ImageData.Dimensions", "Dimensions", null, new FieldOption[] { new("2D", "2D"), new("3D", "3D"), new("4D (3D + time)", "4D (3D + time)") }).Req(),
                            Num("ImageData.OverallNumberOfImages", "Overall number of images", "Total number of images in the dataset (number of volumes, not single slices).").Req(),
                            Text("ImageData.FieldOfView", "Field of view").Req(),
                            Text("ImageData.DimensionExtents", "Dimension extents", "Volume in pixels: x, y, z.").Req().Hint("256 x 256 x 32"),
                            Text("ImageData.SizeDescription", "Size description", "Physical size of the image volume in x, y, z & units.").Req(),
                            Text("ImageData.PixelVoxelSizeDescription", "Pixel / voxel size", "Physical size of pixels in x, y, z & units.").Req(),
                            Area("ImageData.ImageProcessingMethods", "Image processing methods", "Reconstruction, smoothing, registration, AI-enhancement or other processing applied to this dataset.").Req(),
                            Area("ImageData.ImageReconstructionAlgorithm", "Image reconstruction algorithm",
                                "PET: FBP / OSEM / MAP with their parameters (kernel; system matrix, subsets and iterations; beta). CT: FBP or iterative.").Req(),
                            Text("ImageData.ImageAttenuationCorrection", "Attenuation / scatter correction", "For PET: whether attenuation or scatter correction was applied to the image data."),
                            Text("ImageData.ImageSmoothingOrFilteringAlgorithm", "Image smoothing or filtering algorithm"),
                            YesNo("ImageData.ImageRegistrationAlgorithm", "Image registration applied?").Req(),
                            Text("ImageData.RegistrationAlgorithms", "Registration algorithm(s)").When("ImageData.ImageRegistrationAlgorithm", "yes"),
                            YesNo("ImageData.AiEnhanced", "AI-enhanced?").Req(),
                            Text("ImageData.AiEnhancedAlgorithm", "AI-enhanced algorithm(s)").When("ImageData.AiEnhanced", "yes"),
                            YesNo("ImageData.QualityControl", "Quality control performed?").Req(),
                            Area("ImageData.QcInfo", "QC information", "QC score for image quality if applicable (e.g. contrast agent injection success, image noise).").When("ImageData.QualityControl", "yes"),
                            Area("ImageData.Corrections", "Corrections in image formation", "In the order applied, e.g. for PET: decay, randoms, scatter, attenuation."),
                        }
                    },
                    new FormGroup
                    {
                        Title = "Image correlation",
                        Intro = "Only for multi-modality datasets.",
                        ShowIf = If("StudyComponent.MultiModalityImages", "yes"),
                        Fields = new[]
                        {
                            Text("ImageCorrelation.SpatialAndTemporalAlignment", "Spatial and temporal alignment", "Method used to correlate images from different modalities (manual overlay, alignment algorithm, …)."),
                            Text("ImageCorrelation.FiducialsUsed", "Fiducials used", "Features used for co-localisation."),
                            YesNo("ImageCorrelation.CoregisteredImages", "Co-registered images available?"),
                            Text("ImageCorrelation.TransformationMatrixOtherInfo", "Transformation matrix / other info"),
                            Text("ImageCorrelation.RelatedImagesAndRelationship", "Related images and relationship"),
                        }
                    },
                    new FormGroup
                    {
                        Title = "Analysed data",
                        Fields = new[]
                        {
                            Text("Analyzed.AnalysisResultType", "Analysis result type", "e.g. numerical analyses, categorical features/phenotypes."),
                            Text("Analyzed.DataUsedForAnalysis", "Data used for analysis", "Feature set used, e.g. volume measurements, SUV, ADC.").Req(),
                            Area("Analyzed.AnalysisMethodAndDetails", "Analysis method and details", "Method and software used to analyse the data."),
                            Multi("Analyzed.FileFormatOfResultFileCsvJsonTxtXlsx", "File format of result files", null, Opts("csv", "json", "txt", "xlsx"), other: true),
                        }
                    }
                }
            },
        };

        /// <summary>A "Did you use …? Yes/No" question whose details appear only on Yes.</summary>
        private static FormGroup Gate(string title, string gatePath, FormField[] details, bool required = true)
        {
            foreach (var f in details)
            {
                // details depend on the gate; a field with its own condition (e.g. Perfusion method) keeps it
                f.ShowIf ??= new Condition(gatePath, new[] { "yes" });
            }
            var gate = new FormField { Path = gatePath, Label = "Used in this study?", Kind = FieldKind.YesNo, Options = YesNoOptions, Required = required };
            return new FormGroup { Title = title, Fields = new[] { gate }.Concat(details).ToArray() };
        }

        // ---------------------------------------------------------------- lookups

        public static readonly IReadOnlyDictionary<string, FormField> FieldsByPath =
            Steps.SelectMany(s => s.Fields).ToDictionary(f => f.Path, StringComparer.Ordinal);

        private static readonly Dictionary<string, FormGroup> GroupOfField =
            Steps.SelectMany(s => s.Groups).SelectMany(g => g.Fields.Select(f => (f.Path, g)))
                 .ToDictionary(x => x.Path, x => x.g, StringComparer.Ordinal);

        /// <summary>
        /// A field is shown when its group's condition and its own condition hold, and the fields those
        /// conditions look at are themselves shown (so details of a hidden question are hidden too).
        /// Must match isVisible() in wwwroot/js/contribution-wizard.js.
        /// </summary>
        public static bool IsVisible(FormField field, IReadOnlyDictionary<string, string> answers) =>
            IsVisible(field, answers, depth: 0);

        private static bool IsVisible(FormField field, IReadOnlyDictionary<string, string> answers, int depth)
        {
            if (depth > 10) return true; // guard against a condition loop in the definitions
            var group = GroupOfField[field.Path];
            return Holds(group.ShowIf, answers, depth) && Holds(field.ShowIf, answers, depth);
        }

        private static bool Holds(Condition? c, IReadOnlyDictionary<string, string> answers, int depth)
        {
            if (c == null) return true;
            if (FieldsByPath.TryGetValue(c.Field, out var controller) && !IsVisible(controller, answers, depth + 1))
                return false;
            answers.TryGetValue(c.Field, out var value);
            return c.Matches(value);
        }
    }
}
