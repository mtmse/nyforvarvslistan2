using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace func_nyforvarvslistan.Models
{

    public class ProductionRoute
    {
        [JsonProperty("@odata.etag")]
        public string ODataEtag { get; set; }

        [JsonProperty("dataAreaId")]
        public string DataAreaId { get; set; }

        [JsonProperty("ActivityNumber")]
        public string ActivityNumber { get; set; }

        [JsonProperty("OperNo")]
        public int OperNo { get; set; }

        [JsonProperty("Ended")]
        public DateTime Ended { get; set; }

        [JsonProperty("ConfirmedDlv")]
        public DateTime ConfirmedDelivery { get; set; }

        [JsonProperty("NoOfPictCopies")]
        public int NumberOfPictCopies { get; set; }

        [JsonProperty("IsClaimedByProponent")]
        public string IsClaimedByProponent { get; set; }

        [JsonProperty("CaseId")]
        public string CaseId { get; set; }

        [JsonProperty("ProdFileType")]
        public string ProductionFileType { get; set; }

        [JsonProperty("Placement")]
        public string Placement { get; set; }

        [JsonProperty("DeliveryDate")]
        public DateTime DeliveryDate { get; set; }

        [JsonProperty("IsRunJob")]
        public string IsRunJob { get; set; }

        [JsonProperty("Job")]
        public string Job { get; set; }

        [JsonProperty("Started")]
        public DateTime Started { get; set; }

        [JsonProperty("PurchOperNo")]
        public int PurchasingOperationNumber { get; set; }

        [JsonProperty("JobStatus")]
        public string JobStatus { get; set; } // Kan också definieras som enum

        [JsonProperty("ConsFileType")]
        public string ConsignmentFileType { get; set; }

        [JsonProperty("OperationStatus")]
        public string OperationStatus { get; set; }

        [JsonProperty("Approved")]
        public string Approved { get; set; }

        [JsonProperty("AccTime")]
        public double AccumulatedTime { get; set; }

        [JsonProperty("ProcessTime")]
        public double ProcessTime { get; set; }

        [JsonProperty("ClaimStatus")]
        public string ClaimStatus { get; set; }

        [JsonProperty("VendorId")]
        public string VendorId { get; set; }
    }
}

