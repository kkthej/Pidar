/**
 * metadata-prefill.js
 * Handles drag-and-drop / file-browse of a filled PIDAR metadata template (.xlsx)
 * and auto-fills the Create Dataset form.
 *
 * Supports TWO xlsx formats automatically:
 *   FORMAT A — metadata_template.xlsx
 *     · Label in column B (index 1), value in column D (index 3)
 *     · Row 0 has "Metadata" in col B and "Dataset" in col D
 *   FORMAT B — Pidar_selected_*.xlsx (PIDAR export)
 *     · Label in column A (index 0), value in column B (index 1)
 *     · No named header row; first rows are "Dataset <id>" / section names
 *
 * Depends on: SheetJS (xlsx) — loaded separately via CDN or bundled.
 * The field map is passed in via a data-field-map attribute on the drop zone
 * element (JSON, emitted by Razor from CategoryProvider + aliases).
 *
 * HOW FIELD LOOKUP WORKS (three-tier, in order):
 *   1. SECTION_MAP — context-aware lookup: ambiguous labels resolved by which
 *      Excel section header the row falls under (e.g. "dose" under anesthesia
 *      maps to AnesthesiaDose, under contrast agent maps to ContrastAgentDose).
 *   2. EXPLICIT_MAP — hand-crafted label→name entries for known tricky cases.
 *      All values point to the EXACT C# model property name (verified against
 *      the EF Core migration snapshots). Pipe-separated fallbacks tried L→R.
 *   3. SERVER fieldMap from data-field-map (CategoryProvider.PrettyLabel keys).
 *   4. AUTO-DERIVE — convert label to PascalCase, try every known section prefix.
 *      Catches any label the maps above both miss.
 *
 * SECTION TRACKING:
 *   While iterating rows the parser tracks the current section heading
 *   (e.g. "f) anesthesia (for imaging)", "i) imaging", "a) cell lines",
 *   "section image data"). Ambiguous labels that appear in multiple sections
 *   (type / drugs / dose / commercial name / chemical name) are resolved
 *   via SECTION_MAP[currentSection][ambiguousLabel] before EXPLICIT_MAP is tried.
 */

(function () {
    "use strict";

    const ZONE_ID = "prefill-zone";
    const FILE_INPUT_ID = "prefill-file-input";
    const STATUS_ID = "prefill-status";
    const FILLED_COLOR = "#d4edda";
    const BORDER_DEFAULT = "#0dcaf0";
    const BORDER_HOVER = "#0d6efd";
    const BORDER_SUCCESS = "#198754";
    const BORDER_WARN = "#ffc107";

    // Section prefixes tried during auto-derive (tier 4), in priority order.
    // Must match the actual C# model class names used in name="Section.Property".
    const SECTION_PREFIXES = [
        "StudyDesign", "Publication", "StudyComponent", "DatasetInfo",
        "InVivo", "Procedures", "ImageAcquisition", "ImageData", "AnalysedData"
    ];

    // ------------------------------------------------------------------
    // SECTION_MAP — resolves labels that are AMBIGUOUS across sections.
    //
    // Structure:
    //   SECTION_MAP[normalisedSectionHeading][normalisedLabel] = "form name"
    //
    // The section heading is the normalised text of the most-recently-seen
    // Excel row whose col-B (Format A) or col-A (Format B) starts with a
    // subsection/section marker like "f)", "g)", "section …", "subsection …",
    // or is one of the known block headers listed in SECTION_TRIGGER_LABELS.
    //
    // Only labels that actually collide across sections need entries here.
    // Unambiguous labels stay in EXPLICIT_MAP.
    // ------------------------------------------------------------------
    const SECTION_MAP = {
        // ── f) Anesthesia ─────────────────────────────────────────────────
        "f) anesthesia (for imaging)": {
            "type": "Procedures.AnesthesiaType",
            "drugs": "Procedures.AnesthesiaDrugs",
            "dose": "Procedures.AnesthesiaDose",
        },
        "anesthesia (for imaging)": {
            "type": "Procedures.AnesthesiaType",
            "drugs": "Procedures.AnesthesiaDrugs",
            "dose": "Procedures.AnesthesiaDose",
        },

        // ── a) Pharmacological ────────────────────────────────────────────
        "a) pharmacological procedures (intervention and control)": {
            "dose": "Procedures.DrugDose",
        },
        "pharmacological procedures (intervention and control)": {
            "dose": "Procedures.DrugDose",
        },

        // ── e) Analgesic ──────────────────────────────────────────────────
        "e) analgesic plan to relieve pain, suffering and distress": {
            "dose": "Procedures.AnalgesicDose",
        },
        "e)  analgesic plan to relieve pain, suffering and distress": {
            "dose": "Procedures.AnalgesicDose",
        },
        "analgesic plan to relieve pain, suffering and distress": {
            "dose": "Procedures.AnalgesicDose",
        },

        // ── i) Imaging / contrast agent ───────────────────────────────────
        "i) imaging": {
            "commercial name": "Procedures.ContrastAgentCommercialDrug",
            "chemical name": "Procedures.ContrastAgentChemicalDrug",
            "dose": "Procedures.ContrastAgentDose",
        },
        "imaging": {
            "commercial name": "Procedures.ContrastAgentCommercialDrug",
            "chemical name": "Procedures.ContrastAgentChemicalDrug",
            "dose": "Procedures.ContrastAgentDose",
        },

        // ── section image data ────────────────────────────────────────────
        "section image data": {
            "type": "ImageData.ImageType",
        },
        "image data": {
            "type": "ImageData.ImageType",
        },

        // ── a) cell lines ─────────────────────────────────────────────────
        "a) cell lines": {
            "identification": "Procedures.CellLine",
        },
        "cell lines": {
            "identification": "Procedures.CellLine",
        },
    };

    // ------------------------------------------------------------------
    // SECTION_TRIGGER_LABELS — normalised labels that, when seen in the
    // label column, update the current section context.
    // These are the subsection/block headers from the Excel template.
    // ------------------------------------------------------------------
    const SECTION_TRIGGER_LABELS = new Set([
        "section study design",
        "subsection background",
        "subsection publication",
        "section study component",
        "subsection imaging technique",
        "subsection dataset information",
        "section ivep (in vivo experimental parameters)",
        "subsection study design",
        "subsection subject details",
        "section experimental procedures",
        "subsection procedures",
        "a) pharmacological procedures (intervention and control)",
        "pharmacological procedures (intervention and control)",
        "b) blood sampling",
        "c) surgical procedures (including sham surgery)",
        "surgical procedures (including sham surgery)",
        "d) pathogen infection (intervention and control)",
        "pathogen infection (intervention and control)",
        "e) analgesic plan to relieve pain, suffering and distress",
        "e)  analgesic plan to relieve pain, suffering and distress",
        "analgesic plan to relieve pain, suffering and distress",
        "f) anesthesia (for imaging)",
        "anesthesia (for imaging)",
        "g) euthanasia",
        "h) histology",
        "i) imaging",
        "subsection resources",
        "a) cell lines",
        "cell lines",
        "b) reagents",
        "c) equipment and software",
        "subsection additional information",
        "section image acquisition",
        "section image data",
        "image data",
        "section image data",
        "(result of image acquisition, or processing of image data)",
    ]);

    // ------------------------------------------------------------------
    // EXPLICIT_MAP
    // Keys: normalised label (lower-case, whitespace-collapsed, trimmed).
    // Values: exact name="..." attribute (verified against C# model snapshots),
    //         OR pipe-separated fallbacks tried left-to-right.
    //
    // NOTE: All property names here have been verified against the EF Core
    // migration snapshots and Models/*.cs files. Do NOT shorten property
    // names — the full name must match the name= attribute in Create.cshtml.
    // ------------------------------------------------------------------
    const EXPLICIT_MAP = {
        // ── Study Design ──────────────────────────────────────────────────
        "study design/ background": "StudyDesign.Background",
        "study design background": "StudyDesign.Background",

        // ── Publication ───────────────────────────────────────────────────
        "authors": "Publication.PaperAuthors",
        "journal": "Publication.PaperJournal",
        "year": "Publication.PaperYear",
        "doi": "Publication.PaperDoi",
        "paper doi": "Publication.PaperDoi",
        "paper linked": "Publication.PaperLinked",

        // ── Study Component ───────────────────────────────────────────────
        "multi modality images": "StudyComponent.MultiModalityImages",
        "multi-modality images": "StudyComponent.MultiModalityImages",
        "imaging sub modality": "StudyComponent.ImagingSubModality",
        "imaging sub-modality": "StudyComponent.ImagingSubModality",

        // ── Dataset Info ──────────────────────────────────────────────────
        "co pi": "DatasetInfo.CoPi",
        "co-pi": "DatasetInfo.CoPi",
        // EuroBioImagingNode is the FULL property name in the C# model
        "euro bio imaging node": "DatasetInfo.EuroBioImagingNode",
        "eubi node": "DatasetInfo.EuroBioImagingNode",
        "eubl node": "DatasetInfo.EuroBioImagingNode",
        // PiOrchid (with 'h') is the ACTUAL property name in the C# model
        "pi orcid": "DatasetInfo.PiOrcid",
        "pi orchid": "DatasetInfo.PiOrcid",
        "ror code owner": "DatasetInfo.RorCodeOwner",
        // Both label variants used in different template versions
        "link to dataset": "DatasetInfo.LinkToDataset",
        "dataset link": "DatasetInfo.LinkToDataset",
        "funding agency": "DatasetInfo.FundingAgency",
        "grant number": "DatasetInfo.GrantNumber",
        "funder id": "DatasetInfo.FunderId",
        "dataset access": "DatasetInfo.DatasetAccess",
        "duo data use permission": "DatasetInfo.DuoDataUsePermission",
        "duo data use modifier": "DatasetInfo.DuoDataUseModifier",
        "duo investigation": "DatasetInfo.DuoInvestigation",
        "contact person": "DatasetInfo.ContactPerson",

        // ── In Vivo ───────────────────────────────────────────────────────
        "sample size": "InVivo.OverallSampleSize",
        "overall sample size": "InVivo.OverallSampleSize",
        "organ/tissue": "InVivo.OrganOrTissue",
        "organ or tissue": "InVivo.OrganOrTissue",
        "number of groups": "InVivo.NumberOfGroups",
        "types of groups": "InVivo.TypesOfGroups",
        "animal condition": "InVivo.AnimalCondition",
        "sample size for each group": "InVivo.SampleSizeForEachGroup",
        "power calculation": "InVivo.PowerCalculation",
        "inclusion criteria": "InVivo.InclusionCriteria",
        "exclusion criteria": "InVivo.ExclusionCriteria",
        "procedures to keep treatments blind": "InVivo.ProceduresToKeepTreatmentsBlind",
        "procedures to keep experimenter blind": "InVivo.ProceduresToKeepExperimenterBlind",
        "outcome measures": "InVivo.OutcomeMeasures",
        "statistical methods": "InVivo.StatisticalMethods",
        "age at start experiment": "InVivo.AgeAtStartExperiment",
        "age at scanning experiment(s)": "InVivo.AgeAtScanningExperiment",
        "age at scanning experiments": "InVivo.AgeAtScanningExperiment",
        "weight at start experiment": "InVivo.WeightAtStartExperiment",
        "weight at end experiment": "InVivo.WeightAtEndExperiment",
        "immune status": "InVivo.ImmuneStatus",
        "genetic manipulation": "InVivo.GeneticManipulation",
        "source of animals": "InVivo.SourceOfAnimals",
        "registry number of animal authorization": "InVivo.RegistryNumberOfAnimalAuthorization",

        // ── Procedures — Pharmacological ──────────────────────────────────
        // Full property name: PharmacologicalProceduresInterventionAndControl
        "pharmacological procedures intervention and control"
            : "Procedures.PharmacologicalProceduresInterventionAndControl",
        "a) pharmacological procedures (intervention and control)"
            : "Procedures.PharmacologicalProceduresInterventionAndControl",
        // Full property name: PharmacologicalDrug  (NOT DrugName)
        "pharmacological drug": "Procedures.PharmacologicalDrug",
        "drug name": "Procedures.PharmacologicalDrug",
        // "dose" under pharmacological → DrugDose (handled via SECTION_MAP)
        // Full property name: DrugDose  (NOT just Dose)
        "drug dose": "Procedures.DrugDose",
        // Full property name: SiteOrRouteOfAdministration  (NOT SiteRouteOfAdministration)
        "site /route of administration": "Procedures.SiteOrRouteOfAdministration",
        "site/route of administration": "Procedures.SiteOrRouteOfAdministration",
        "site or route of administration": "Procedures.SiteOrRouteOfAdministration",
        "frequency of administration": "Procedures.FrequencyOfAdministration",
        "vehicle or carrier solution formulation": "Procedures.VehicleOrCarrierSolutionFormulation",
        // Full property name: DrugOrBatchSampleNumber  (NOT DrugBatchNumber)
        "drug batch/sample number": "Procedures.DrugOrBatchSampleNumber",
        "drug batch sample number": "Procedures.DrugOrBatchSampleNumber",

        // ── Procedures — Blood Sampling ───────────────────────────────────
        "b) blood sampling": "Procedures.BloodSampling",
        "blood sampling method": "Procedures.BloodSamplingMethod",
        "blood sample volume": "Procedures.BloodSampleVolume",
        "blood timing": "Procedures.BloodTiming",
        "blood collection time": "Procedures.BloodCollectionTiming",

        // ── Procedures — Surgical ─────────────────────────────────────────
        // Full property name: SurgicalProceduresIncludingShamSurgery  (NOT SurgicalProcedures)
        "surgical procedures including sham surgery"
            : "Procedures.SurgicalProceduresIncludingShamSurgery",
        "c) surgical procedures (including sham surgery)"
            : "Procedures.SurgicalProceduresIncludingShamSurgery",
        "description of the surgical procedure": "Procedures.DescriptionOfTheSurgicalProcedure",
        "reference to protocol": "Procedures.ReferenceToProtocol",
        "target organ/tissue": "Procedures.TargetOrganTissue",
        "target organ tissue": "Procedures.TargetOrganTissue",

        // ── Procedures — Pathogen Infection ───────────────────────────────
        // Full property name: PathogenInfectionInterventionAndControl  (NOT PathogenInfection)
        "pathogen infection intervention and control"
            : "Procedures.PathogenInfectionInterventionAndControl",
        "d) pathogen infection (intervention and control)"
            : "Procedures.PathogenInfectionInterventionAndControl",
        "infectious type": "Procedures.InfectiousType",
        "infectious agent": "Procedures.InfectiousAgent",
        "dose load": "Procedures.DoseLoad",
        "site and route of infection": "Procedures.SiteAndRouteOfInfection",
        "timing or frequency of infection": "Procedures.TimingOrFrequencyOfInfection",

        // ── Procedures — Analgesia ────────────────────────────────────────
        // Full property name: AnalgesicPlanToRelievePainSufferingAndDistress  (NOT AnalgesicPlan)
        "analgesic plan to relieve pain suffering and distress"
            : "Procedures.AnalgesicPlanToRelievePainSufferingAndDistress",
        "e) analgesic plan to relieve pain, suffering and distress"
            : "Procedures.AnalgesicPlanToRelievePainSufferingAndDistress",
        "e)  analgesic plan to relieve pain, suffering and distress"
            : "Procedures.AnalgesicPlanToRelievePainSufferingAndDistress",
        "analgesic name": "Procedures.AnalgesicName",
        // "dose" under analgesic → AnalgesicDose (handled via SECTION_MAP)
        "analgesic dose": "Procedures.AnalgesicDose",
        "route": "Procedures.Route",

        // ── Procedures — Anesthesia ───────────────────────────────────────
        "f) anesthesia (for imaging)": "Procedures.AnesthesiaForImaging",
        "anesthesia for imaging": "Procedures.AnesthesiaForImaging",
        // "type" / "drugs" / "dose" under anesthesia → handled via SECTION_MAP
        "anesthesia type": "Procedures.AnesthesiaType",
        "anesthesia drugs": "Procedures.AnesthesiaDrugs",
        "anesthesia dose": "Procedures.AnesthesiaDose",
        "monitoring regime": "Procedures.MonitoringRegime",

        // ── Procedures — Euthanasia & Histology ───────────────────────────
        "g) euthanasia": "Procedures.Euthanasia",
        "h) histology": "Procedures.Histology",
        "tissues collected post-euthanasia": "Procedures.TissuesCollectedPostEuthanasia",
        "tissues collected post euthanasia": "Procedures.TissuesCollectedPostEuthanasia",
        "timing of collection": "Procedures.TimingOfCollection",
        "tissue description": "Procedures.TissueDescription",
        "tissue perfused?": "Procedures.TissuePerfused",
        "tissue perfused": "Procedures.TissuePerfused",
        "perfusion method": "Procedures.PerfusionMethod",
        "histological procedure": "Procedures.HistologicalProcedure",
        "name of reagent(s)": "Procedures.NameOfReagentS",
        "name of reagents": "Procedures.NameOfReagentS",
        "length of fixation": "Procedures.LengthOfFixation",
        "specimen thickness": "Procedures.SpecimenThickness",

        // ── Procedures — Imaging / Contrast Agent ─────────────────────────
        "i) imaging": "Procedures.Imaging",
        "frequency of imaging": "Procedures.FrequencyOfImaging",
        "timing of imaging": "Procedures.TimingOfImaging",
        "overall scan length": "Procedures.OverallScanLength",
        "contrast agent or radio-isotope or challenge with gas/molecule"
            : "Procedures.ContrastAgentOrRadioIsotopeOrChallengeWithGasMolecule",
        "contrast agent or radio isotope or challenge with gas molecule"
            : "Procedures.ContrastAgentOrRadioIsotopeOrChallengeWithGasMolecule",
        // "commercial name" / "chemical name" / "dose" → SECTION_MAP under "i) imaging"
        "contrast agent commercial drug": "Procedures.ContrastAgentCommercialDrug",
        "contrast agent chemical drug": "Procedures.ContrastAgentChemicalDrug",
        "contrast agent dose": "Procedures.ContrastAgentDose",
        "injection volume": "Procedures.InjectionVolume",
        "injection time": "Procedures.InjectionTime",
        "vehicle": "Procedures.Vehicle",
        "route of administration": "Procedures.RouteOfAdministration",

        // ── Procedures — Cell Lines ───────────────────────────────────────
        "a) cell lines": "Procedures.CellLines",
        // "identification" under cell lines → CellLine  (the cell line name field)
        // Full property name: CellLine  (NOT CellLineIdentification)
        "identification": "Procedures.CellLine",
        "cell line": "Procedures.CellLine",
        "provenance": "Procedures.Provenance",
        "cell culture medium": "Procedures.CellCultureMedium",
        "modified cell line": "Procedures.ModifiedCellLine",
        "type of genetic modification": "Procedures.TypeOfGeneticModification",
        "gene modified": "Procedures.GeneModified",
        "virus-labelled or modified": "Procedures.VirusLabelledOrModified",
        "virus labelled or modified": "Procedures.VirusLabelledOrModified",
        "verification and authentication": "Procedures.VerificationAndAuthentication",
        "cell injection route": "Procedures.CellInjectionRoute",
        "cell injection procedure": "Procedures.CellInjectionProcedure",
        "number of cells": "Procedures.NumberOfCells",

        // ── Procedures — Reagents / Equipment ────────────────────────────
        "b) reagents": "Procedures.Reagents",
        "name of reagent": "Procedures.NameOfReagent",
        "catalogue number": "Procedures.CatalogueNumber",
        "c) equipment and software": "Procedures.EquipmentAndSoftware",
        "manufacturer": "Procedures.Manufacturer",
        "model/version number": "Procedures.ModelVersionNumber",
        "model version number": "Procedures.ModelVersionNumber",

        // ── Image Acquisition ─────────────────────────────────────────────
        "instrument vendor": "ImageAcquisition.InstrumentVendor",
        "instrument type": "ImageAcquisition.InstrumentType",
        "instrument specifics": "ImageAcquisition.InstrumentSpecifics",
        "image acquisition parameters": "ImageAcquisition.ImageAcquisitionParameters",
        "correction": "ImageAcquisition.Correction",
        "raw data": "ImageAcquisition.RawData",
        "qa/qc": "ImageAcquisition.QaQc",
        "qa qc": "ImageAcquisition.QaQc",

        // ── Image Data ────────────────────────────────────────────────────
        // "type" under image data → ImageType (handled via SECTION_MAP)
        "image type": "ImageData.ImageType",
        "image scale": "ImageData.ImageScale",
        "format & compression": "ImageData.FormatCompression",
        "format compression": "ImageData.FormatCompression",
        "overall number of images": "ImageData.OverallNumberOfImages",
        "field of view": "ImageData.FieldOfView",
        "dimension extents": "ImageData.DimensionExtents",
        "size description": "ImageData.SizeDescription",
        "pixel/voxel size description": "ImageData.PixelVoxelSizeDescription",
        "pixel voxel size description": "ImageData.PixelVoxelSizeDescription",
        "image processing methods": "ImageData.ImageProcessingMethods",
        "image reconstruction algorithm": "ImageData.ImageReconstructionAlgorithm",
        "image attenuation/correction": "ImageData.ImageAttenuationCorrection",
        "image attenuation correction": "ImageData.ImageAttenuationCorrection",
        "image smoothing or filtering algorithm": "ImageData.ImageSmoothingOrFilteringAlgorithm",
        "image registration algorithm": "ImageData.ImageRegistrationAlgorithm",
        "registration algorithms": "ImageData.RegistrationAlgorithms",
        "ai-enhanced": "ImageData.AiEnhanced",
        "ai enhanced": "ImageData.AiEnhanced",
        "quality control": "ImageData.QualityControl",
        "qc info": "ImageData.QcInfo",
        "corrections": "ImageData.Corrections",

        // ── Image Correlation ─────────────────────────────────────────────
        "spatial and temporal alignment": "ImageCorrelation.SpatialAndTemporalAlignment",
        "fiducials used": "ImageCorrelation.FiducialsUsed",
        "coregistered images": "ImageCorrelation.CoregisteredImages",
        "transformation matrix/ other info": "ImageCorrelation.TransformationMatrixOtherInfo",
        "transformation matrix other info": "ImageCorrelation.TransformationMatrixOtherInfo",
        "related images and relationship": "ImageCorrelation.RelatedImagesAndRelationship",

        // ── Analysed Data ─────────────────────────────────────────────────
        "analysis result type": "AnalysedData.AnalysisResultType",
        "data used for analysis": "AnalysedData.DataUsedForAnalysis|AnalysedData.DataUsed",
        "analysis method and details": "AnalysedData.AnalysisMethodAndDetails",
        "file format of result file (csv, json, txt, xlsx)": "AnalysedData.FileFormatOfResultFile",
    };

    // ------------------------------------------------------------------
    // Labels to silently skip — section/subsection headers, auto-set fields,
    // DUO ontology table rows, and other non-data rows.
    // ------------------------------------------------------------------
    const SKIP_LABELS = new Set([
        "dataset id", "dataset id - do not edit this line", "updated year",
        "module", "metadata", "comments/instructions", "dataset",
        "section study design", "subsection background", "subsection publication",
        "section study component", "subsection imaging technique",
        "subsection dataset information",
        "section ivep (in vivo experimental parameters)",
        "subsection study design", "subsection subject details",
        "section experimental procedures", "subsection procedures",
        "sample size", "exclusion and inclusion criteria",
        "randomisation", "blinding", "outcome measures", "statistical methods",
        "study design", "publication", "study component", "dataset info",
        "in vivo", "procedures", "image acquisition", "image data", "analyzed",
        "label", "data use permission", "general research use", "no restriction",
        "health/medical/biomedical research and clinical care",
        "disease-specific research and clinical care",
        "population origins or ancestry research", "research-specific restrictions",
        "no general methods research", "genetic studies only", "data use modifier",
        "not-for-profit use only", "publication required", "collaboration required",
        "ethics approval required", "geographical restriction",
        "publication moratorium", "time limit on use", "user-specific restriction",
        "project-specific restriction", "institution-specific restriction",
        "return to database/resource", "clinical care use",
        "population origins or ancestry research prohibited",
        "not for profit organisation use only", "non-commercial use only",
        "investigation", "age category research", "ancestry research",
        "biomedical research", "disease category research",
        "drug development research", "genetic research", "gender category research",
        "method development", "population research", "research control",
        // Sub-section dividers that appear as bold rows in Format A
        "subsection resources", "subsection additional information",
        "b) reagents", "c) equipment and software",
        "(contains image data and analysed data)",
        "(result of image acquisition, or processing of image data)",
    ]);

    // ------------------------------------------------------------------
    // norm: lower-case + collapse whitespace (trim included)
    // ------------------------------------------------------------------
    function norm(s) {
        return String(s).toLowerCase().replace(/\s+/g, " ").trim();
    }

    // ------------------------------------------------------------------
    // isSectionHeader: returns true when a label should update the current
    // section context.  Matches:
    //   · entries in SECTION_TRIGGER_LABELS
    //   · rows that start with "section " or "subsection "
    //   · rows that start with a letter + ") " pattern  (a), b), …)
    // ------------------------------------------------------------------
    function isSectionHeader(key) {
        if (SECTION_TRIGGER_LABELS.has(key)) return true;
        if (/^(section|subsection)\s/.test(key)) return true;
        if (/^[a-z]\)\s/.test(key)) return true;  // "a) …", "b) …", etc.
        return false;
    }

    // ------------------------------------------------------------------
    // toPascalCase: "Euro Bio Imaging Node" → "EuroBioImagingNode"
    // Strips punctuation that can't appear in a C# identifier.
    // ------------------------------------------------------------------
    function toPascalCase(label) {
        return label
            .replace(/[^a-zA-Z0-9 ]/g, " ")
            .split(/\s+/)
            .filter(Boolean)
            .map(function (w) { return w.charAt(0).toUpperCase() + w.slice(1).toLowerCase(); })
            .join("");
    }

    // ------------------------------------------------------------------
    // findElement: four-tier lookup returning the first matching DOM element.
    //
    //   Tier 1: SECTION_MAP  — context-aware (ambiguous labels)
    //   Tier 2: EXPLICIT_MAP — verified label→property aliases
    //   Tier 3: server fieldMap from data-field-map attribute
    //   Tier 4: auto-derive — PascalCase(label) tried under each section prefix
    // ------------------------------------------------------------------
    function findElement(key, rawLabel, currentSection, serverMap) {
        function queryName(name) {
            return document.querySelector('[name="' + name + '"]') ||
                document.getElementById(name) || null;
        }

        // Tier 1 — SECTION_MAP (context-aware, only for ambiguous labels)
        if (currentSection) {
            var secEntry = SECTION_MAP[currentSection];
            if (secEntry && secEntry[key]) {
                var el = queryName(secEntry[key]);
                if (el) return { el: el, tier: 1 };
            }
        }

        // Tier 2 — EXPLICIT_MAP (pipe-separated fallbacks tried L→R)
        if (EXPLICIT_MAP[key]) {
            var candidates = EXPLICIT_MAP[key].split("|");
            for (var i = 0; i < candidates.length; i++) {
                var el = queryName(candidates[i].trim());
                if (el) return { el: el, tier: 2 };
            }
        }

        // Tier 3 — server fieldMap
        if (serverMap[key]) {
            var el = queryName(serverMap[key]);
            if (el) return { el: el, tier: 3 };
        }

        // Tier 4 — auto-derive from label
        var pascal = toPascalCase(rawLabel);
        if (pascal) {
            var el = queryName(pascal);
            if (el) return { el: el, tier: 4 };
            for (var i = 0; i < SECTION_PREFIXES.length; i++) {
                el = queryName(SECTION_PREFIXES[i] + "." + pascal);
                if (el) return { el: el, tier: 4 };
            }
        }

        return null;
    }

    // ------------------------------------------------------------------
    // detectColumns(rows) → { labelCol, valueCol, format }
    // FORMAT A: row 0 has "Metadata" in some column and "Dataset" in another
    // FORMAT B: label col 0, value col 1
    // ------------------------------------------------------------------
    function detectColumns(rows) {
        var labelCol = -1, valueCol = -1;
        if (rows.length > 0) {
            rows[0].forEach(function (cell, i) {
                var c = String(cell).toLowerCase().trim();
                if (c === "metadata") labelCol = i;
                if (c === "dataset") valueCol = i;
            });
        }
        if (labelCol !== -1 && valueCol !== -1) {
            return { labelCol: labelCol, valueCol: valueCol, format: "A" };
        }
        return { labelCol: 0, valueCol: 1, format: "B" };
    }

    // ------------------------------------------------------------------
    // DOMContentLoaded — wire up events
    // ------------------------------------------------------------------
    document.addEventListener("DOMContentLoaded", function () {
        var zone = document.getElementById(ZONE_ID);
        var fileInput = document.getElementById(FILE_INPUT_ID);
        var status = document.getElementById(STATUS_ID);
        if (!zone || !fileInput || !status) return;

        var serverMap = {};
        try { serverMap = JSON.parse(zone.dataset.fieldMap || "{}"); }
        catch (e) { console.error("metadata-prefill: bad data-field-map", e); }

        zone.addEventListener("click", function () { fileInput.click(); });
        zone.addEventListener("dragover", function (e) {
            e.preventDefault();
            zone.style.borderColor = BORDER_HOVER;
        });
        zone.addEventListener("dragleave", function () {
            zone.style.borderColor = BORDER_DEFAULT;
        });
        zone.addEventListener("drop", function (e) {
            e.preventDefault();
            zone.style.borderColor = BORDER_DEFAULT;
            if (e.dataTransfer.files[0]) {
                readFile(e.dataTransfer.files[0], serverMap, zone, status);
            }
        });
        fileInput.addEventListener("change", function () {
            if (this.files[0]) readFile(this.files[0], serverMap, zone, status);
        });
    });

    // ------------------------------------------------------------------
    // readFile
    // ------------------------------------------------------------------
    function readFile(file, serverMap, zone, status) {
        if (!file.name.endsWith(".xlsx")) {
            showStatus(status, "error", "Please upload an .xlsx file.");
            return;
        }
        showStatus(status, "info", "Reading file\u2026");
        var reader = new FileReader();
        reader.onload = function (e) {
            try {
                fillForm(XLSX.read(e.target.result, { type: "array" }), serverMap, zone, status);
            } catch (err) {
                showStatus(status, "error", "Error reading file: " + err.message);
            }
        };
        reader.readAsArrayBuffer(file);
    }

    // ------------------------------------------------------------------
    // fillForm — main parsing + form-filling logic
    // ------------------------------------------------------------------
    function fillForm(wb, serverMap, zone, status) {
        var ws = wb.Sheets[wb.SheetNames[0]];
        var rows = XLSX.utils.sheet_to_json(ws, { header: 1, defval: "" });
        var _d = detectColumns(rows);
        var labelCol = _d.labelCol, valueCol = _d.valueCol, format = _d.format;

        var filled = 0;
        var notFound = [];   // passed lookup but no DOM element matched

        // ── Clear every field reachable by any map tier ───────────────────
        // EXPLICIT_MAP entries
        Object.values(EXPLICIT_MAP).forEach(function (spec) {
            spec.split("|").forEach(function (name) {
                var el = document.querySelector('[name="' + name.trim() + '"]') ||
                    document.getElementById(name.trim());
                if (el) {
                    el.value = "";
                    el.style.backgroundColor = "";
                    el.classList.remove("prefill-filled");
                }
            });
        });
        // SECTION_MAP entries
        Object.values(SECTION_MAP).forEach(function (secEntry) {
            Object.values(secEntry).forEach(function (name) {
                var el = document.querySelector('[name="' + name + '"]') ||
                    document.getElementById(name);
                if (el) {
                    el.value = "";
                    el.style.backgroundColor = "";
                    el.classList.remove("prefill-filled");
                }
            });
        });
        // Server map entries
        Object.values(serverMap).forEach(function (name) {
            var el = document.querySelector('[name="' + name + '"]') ||
                document.getElementById(name);
            if (el) {
                el.value = "";
                el.style.backgroundColor = "";
                el.classList.remove("prefill-filled");
            }
        });

        // ── Process rows, tracking section context ────────────────────────
        var currentSection = "";   // normalised heading of the current block

        rows.forEach(function (row, rowIdx) {
            if (rowIdx === 0 && format === "A") return;   // skip header row

            var rawLabel = String(row[labelCol] || "").trim();
            var val = String(row[valueCol] || "").trim();

            if (!rawLabel || rawLabel === "None") return;

            var key = norm(rawLabel);

            // Update section context whenever we hit a section/subsection header,
            // regardless of whether the row has a value — do this BEFORE the
            // value-empty check so that section headers always update context.
            if (isSectionHeader(key)) {
                currentSection = key;
                // Section headers are not data rows — skip filling
                if (SKIP_LABELS.has(key)) return;
                // Some section headers (e.g. "a) pharmacological...") ARE data
                // rows that should be filled (yes/no fields), so fall through.
            }

            if (!val || val === "None") return;   // nothing to fill
            if (SKIP_LABELS.has(key)) return;

            var hit = findElement(key, rawLabel, currentSection, serverMap);
            if (!hit) {
                if (rawLabel.length > 2) notFound.push(rawLabel);
                return;
            }

            hit.el.value = val;
            hit.el.style.backgroundColor = FILLED_COLOR;
            hit.el.style.transition = "background-color 2s";
            hit.el.classList.add("prefill-filled");
            filled++;
        });

        zone.style.borderColor = filled > 0 ? BORDER_SUCCESS : BORDER_WARN;
        renderFeedback(status, filled, format, notFound);
    }

    // ------------------------------------------------------------------
    // UI helpers
    // ------------------------------------------------------------------
    function showStatus(status, type, message) {
        var cls = { error: "text-danger", info: "text-muted" };
        status.innerHTML =
            '<span class="' + (cls[type] || "") + '">' + message + "</span>";
    }

    function makeDropdown(count, labelText, items, colorClass) {
        return (
            '<details class="mt-1 d-inline-block ms-2" style="vertical-align:middle;">' +
            '<summary class="' + colorClass +
            '" style="cursor:pointer; list-style:none;">' +
            '<span style="text-decoration:underline dotted;">' +
            count + " field(s) " + labelText +
            "</span> \u25be" +
            "</summary>" +
            '<ul class="small mb-0 text-start ' + colorClass + '" ' +
            'style="background:#fff;border:1px solid #dee2e6;border-radius:4px;' +
            'padding:6px 12px;margin-top:4px;list-style:disc;">' +
            items.map(function (l) { return "<li>" + l + "</li>"; }).join("") +
            "</ul></details>"
        );
    }

    function renderFeedback(status, filled, format, notFound) {
        var formatBadge =
            '<span class="badge bg-secondary ms-2" title="xlsx format detected">Format ' +
            format + "</span>";
        var html =
            '<span class="text-success fw-bold">\u2713 Filled ' +
            filled + " field(s).</span>" + formatBadge;
        if (notFound.length) {
            html += makeDropdown(
                notFound.length, "not found in form", notFound, "text-warning"
            );
        }
        status.innerHTML = html;
    }

})();