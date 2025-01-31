using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace func_nyforvarvslistan.Models
{
    public class ProductionHeader
    {
        [JsonProperty("CaseId")]
        public string CaseId { get; set; }

        [JsonProperty("ProdStatus")]
        public string ProdStatus { get; set; }

        [JsonProperty("OrderTypeId")]
        public string OrderTypeId { get; set; }

        [JsonProperty("ProdMediaType")]
        public string ProdMediaType { get; set; }

        [JsonProperty("TitleNo")]
        public string TitleNo { get; set; }

        // Lägg till andra relevanta fält om nödvändigt
    }

}
