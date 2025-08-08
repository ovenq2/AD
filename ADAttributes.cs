namespace ADUserManagement.Models.Enums
{
    public static class ADAttributes
    {
        public static Dictionary<string, string> UserAttributes = new Dictionary<string, string>
        {
            { "telephoneNumber", "Telefon" },
            { "mobile", "Mobil Telefon" },
            { "department", "Birim" },
            { "title", "Unvan" },
            { "physicalDeliveryOfficeName", "Ofis" },
            { "streetAddress", "Adres" },
            { "postalCode", "Posta Kodu" },
            { "l", "Şehir" }, // location/city
            { "st", "İl/Eyalet" }, // state
            { "description", "Açıklama" }
        };
    }
}