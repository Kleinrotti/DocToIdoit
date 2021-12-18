using IdoitSharp.CMDB.Category;
using Newtonsoft.Json;

namespace DocToIdoit
{
    internal class AccountingResponse : ISingleValueResponse
    {
        public string id { get; set; }

        public string objID { get; set; }

        [JsonProperty("delivery_note_no")]
        public string DeliveryNoteNumber { get; set; }

        [JsonProperty("order_date")]
        public string OrderDate { get; set; }

        [JsonProperty("delivery_date")]
        public string DeliveryDate { get; set; }

        [JsonProperty("acquirementdate")]
        public string DateOfInvoice { get; set; }

        public string category_id { get; } = "C__CATG__ACCOUNTING";
    }
}