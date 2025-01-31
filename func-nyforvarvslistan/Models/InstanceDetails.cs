using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace func_nyforvarvslistan.Models
{
    public class InstanceDetail
    {
        [JsonProperty("@odata.etag")]
        public string ODataEtag { get; set; }

        [JsonProperty("dataAreaId")]
        public string DataAreaId { get; set; }

        [JsonProperty("CaseId")]
        public string CaseId { get; set; }

        [JsonProperty("NoOfVolumes")]
        public int NoOfVolumes { get; set; }

        [JsonProperty("SubTypeBook")]
        public int SubTypeBook { get; set; }

        [JsonProperty("ProposalCreatedDateTime")]
        public DateTime ProposalCreatedDateTime { get; set; }

        [JsonProperty("ProdMediaType")]
        public string ProdMediaType { get; set; }

        [JsonProperty("Format")]
        public string Format { get; set; }

        [JsonProperty("PictType")]
        public string PictType { get; set; }

        [JsonProperty("IsReadSlow")]
        public string IsReadSlow { get; set; }

        [JsonProperty("LastPagePS")]
        public int LastPagePS { get; set; }

        [JsonProperty("LineSpacing")]
        public string LineSpacing { get; set; }

        [JsonProperty("NoOfChar")]
        public int NoOfChar { get; set; }

        [JsonProperty("IsHybridProd")]
        public string IsHybridProd { get; set; }

        [JsonProperty("PlayTimeStr")]
        public string PlayTimeStr { get; set; }

        [JsonProperty("SubTypeId")]
        public int SubTypeId { get; set; }

        [JsonProperty("NoOfPagesPS")]
        public int NoOfPagesPS { get; set; }

        [JsonProperty("LibrisNumber")]
        public string LibrisNumber { get; set; }

        [JsonProperty("ETNo")]
        public string ETNo { get; set; }

        [JsonProperty("IsDoublePage")]
        public string IsDoublePage { get; set; }

        [JsonProperty("LengthFactorReal")]
        public int LengthFactorReal { get; set; }

        [JsonProperty("NoOfPagesIBD")]
        public int NoOfPagesIBD { get; set; }

        [JsonProperty("IntExt")]
        public string IntExt { get; set; }

        [JsonProperty("IsPictDescrProduced")]
        public string IsPictDescrProduced { get; set; }

        [JsonProperty("IsPublished")]
        public string IsPublished { get; set; }

        [JsonProperty("NoOfSheets")]
        public int NoOfSheets { get; set; }

        [JsonProperty("ProposalType")]
        public string ProposalType { get; set; }

        [JsonProperty("IsRelfPict")]
        public string IsRelfPict { get; set; }

        [JsonProperty("DaisyNo")]
        public string DaisyNo { get; set; }

        [JsonProperty("ReadComplexity")]
        public string ReadComplexity { get; set; }

        [JsonProperty("Reader")]
        public string Reader { get; set; }

        [JsonProperty("PSNo")]
        public string PSNo { get; set; }

        [JsonProperty("ISBNId")]
        public string ISBNId { get; set; }

        [JsonProperty("DesignerStr")]
        public string DesignerStr { get; set; }

        [JsonProperty("NoOfPict")]
        public int NoOfPict { get; set; }

        [JsonProperty("FileSize")]
        public string FileSize { get; set; }

        [JsonProperty("IsMarkedOriginal")]
        public string IsMarkedOriginal { get; set; }

        [JsonProperty("NoOfIllustrations")]
        public int NoOfIllustrations { get; set; }

        [JsonProperty("Comments")]
        public string Comments { get; set; }

        [JsonProperty("IsAuthorStatus")]
        public string IsAuthorStatus { get; set; }

        [JsonProperty("IsRawCopyOrdered")]
        public string IsRawCopyOrdered { get; set; }

        [JsonProperty("IsUpperCase")]
        public string IsUpperCase { get; set; }

        [JsonProperty("ItemId")]
        public string ItemId { get; set; }

        [JsonProperty("PlayTime")]
        public int PlayTime { get; set; }

        [JsonProperty("Length")]
        public int Length { get; set; }

        [JsonProperty("IsFrontCover")]
        public string IsFrontCover { get; set; }

        [JsonProperty("Printing")]
        public string Printing { get; set; }

        [JsonProperty("ProdMediaTypeDescr")]
        public string ProdMediaTypeDescr { get; set; }

        [JsonProperty("IsPictures")]
        public string IsPictures { get; set; }

        [JsonProperty("ReadSequence")]
        public string ReadSequence { get; set; }

        [JsonProperty("IsEditingReady")]
        public string IsEditingReady { get; set; }

        [JsonProperty("NoOfPagesXML")]
        public int NoOfPagesXML { get; set; }

        [JsonProperty("NoOfSheetsPSorPSS")]
        public int NoOfSheetsPSorPSS { get; set; }
    }

}
