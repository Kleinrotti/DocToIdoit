using IdoitSharp;
using Newtonsoft.Json;

namespace DocToIdoit
{
    internal class AccountingRequest : IRequest
    {
        public int? category_id { get; set; }

        [JsonProperty("delivery_note_no")]
        public string DeliveryNoteNumber { get; set; }

        [JsonProperty("order_date")]
        public string OrderDate { get; set; }

        [JsonProperty("delivery_date")]
        public string DeliveryDate { get; set; }

        [JsonProperty("acquirementdate")]
        public string DateOfInvoice { get; set; }
    }
}