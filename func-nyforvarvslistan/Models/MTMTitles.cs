using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace func_nyforvarvslistan.Models
{
    public class MTMTitle
    {
        [JsonProperty("@odata.etag")]
        public string ODataEtag { get; set; }

        [JsonProperty("dataAreaId")]
        public string DataAreaId { get; set; }

        [JsonProperty("Title")]
        public string Title { get; set; }

        [JsonProperty("CoAuthor")]
        public string CoAuthor { get; set; }

        [JsonProperty("Publisher")]
        public string Publisher { get; set; }

        [JsonProperty("TokenAdd")]
        public int TokenAdd { get; set; }

        [JsonProperty("ISBNIdOrdered")]
        public string ISBNIdOrdered { get; set; }

        [JsonProperty("Category")]
        public string Category { get; set; }

        [JsonProperty("OriginalLanguage")]
        public string OriginalLanguage { get; set; }

        [JsonProperty("ThinOut")]
        public string ThinOut { get; set; }

        [JsonProperty("OriginalTitleId")]
        public string OriginalTitleId { get; set; }

        [JsonProperty("ItemGroup")]
        public string ItemGroup { get; set; }

        [JsonProperty("TypesAdd")]
        public int TypesAdd { get; set; }

        [JsonProperty("TypesTotal")]
        public int TypesTotal { get; set; }

        [JsonProperty("HeldBy")]
        public string HeldBy { get; set; }

        [JsonProperty("PublicationDate")]
        public string PublicationDate { get; set; }

        [JsonProperty("SubjectWord")]
        public string SubjectWord { get; set; }

        [JsonProperty("MTMStorageMethod")]
        public string MTMStorageMethod { get; set; }

        [JsonProperty("RequisitionSourceDescr")]
        public int RequisitionSourceDescr { get; set; }

        [JsonProperty("Author")]
        public string Author { get; set; }

        [JsonProperty("TokenOOV")]
        public int TokenOOV { get; set; }

        [JsonProperty("LibrisNumber")]
        public string LibrisNumber { get; set; }

        [JsonProperty("PublicationYear")]
        public int PublicationYear { get; set; }

        [JsonProperty("TitleId")]
        public string TitleId { get; set; }

        [JsonProperty("SeriesNumber")]
        public int SeriesNumber { get; set; }

        [JsonProperty("TokenTotal")]
        public int TokenTotal { get; set; }

        [JsonProperty("IsXMLMarkingOrdered")]
        public string IsXMLMarkingOrdered { get; set; }

        [JsonProperty("HasMusicNotes")]
        public string HasMusicNotes { get; set; }

        [JsonProperty("TokenCoverage")]
        public int TokenCoverage { get; set; }

        [JsonProperty("Translator")]
        public string Translator { get; set; }

        [JsonProperty("TypesOOV")]
        public int TypesOOV { get; set; }

        [JsonProperty("Source")]
        public string Source { get; set; }

        [JsonProperty("Language")]
        public string Language { get; set; }

        [JsonProperty("SourceFilePath")]
        public string SourceFilePath { get; set; }

        [JsonProperty("EncodingLevel")]
        public string EncodingLevel { get; set; }

        [JsonProperty("ClassificationCode")]
        public string ClassificationCode { get; set; }

        [JsonProperty("Series")]
        public string Series { get; set; }

        [JsonProperty("ThinOutCompleted")]
        public string ThinOutCompleted { get; set; }

        [JsonProperty("Edition")]
        public string Edition { get; set; }

        [JsonProperty("PictureDescription")]
        public string PictureDescription { get; set; }

        [JsonProperty("ItemId")]
        public string ItemId { get; set; }

        [JsonProperty("Notes")]
        public string Notes { get; set; }

        [JsonProperty("IsProdMaterialApproved")]
        public string IsProdMaterialApproved { get; set; }

        [JsonProperty("IsPreventLibrisEnrichment")]
        public string IsPreventLibrisEnrichment { get; set; }

        [JsonProperty("TypesCoverage")]
        public int TypesCoverage { get; set; }

        [JsonProperty("XMLMarking")]
        public string XMLMarking { get; set; }
    }
}
