using Konaju.File.Csv;

namespace RaffleDraw
{
    internal class Customer
    {
        [CsvColumn("Reference ID")]
        public string ReferenceID;

        [CsvColumn("First Name")]
        public string FirstName;

        [CsvColumn("Surname")]
        public string Surname;

        [CsvColumn("Email Address")]
        public string EmailAddress;

        [CsvColumn("Phone Number")]
        public string PhoneNumber;

        [CsvColumn("Square Customer ID")]
        public string SquareCustomerID;
    }
}
