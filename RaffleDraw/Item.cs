using Konaju.File.Csv;

namespace RaffleDraw
{
    internal class Item
    {
        [CsvColumn("Category")]
        public string Category;

        [CsvColumn("Item")]
        public string ItemName;

        [CsvColumn("Qty")]
        public double Quantity;

        [CsvColumn("SKU")]
        public string Sku;

        [CsvColumn("Modifiers Applied")]
        public string ModifiersApplied;

        [CsvColumn("Event Type")]
        public EventType EventType;

        [CsvColumn("Payment ID")]
        public string PaymentID;

        [CsvColumn("Customer Reference ID")]
        public string CustomerReferenceID;

        [CsvColumn("Details")]
        public string DetailsUrl;

        [CsvColumn("Transaction ID")]
        public string TransactionID;
    }
}
