namespace SheetsSearchApp.Models
{
    public class AppointmentRow : ISheetRow
    {
        public string? Name { get; set; }
        public string? ContactNo { get; set; }
        public string? Treatment { get; set; }
        public string? Specialist { get; set; }
        public string? Channel { get; set; }
        public string? CheckIn { get; set; }
        public string? CheckOut { get; set; }
        public string? TotalTime { get; set; }
        public string? Room { get; set; }
        public decimal? Amount { get; set; }
        public string? Billing { get; set; }
        public string? Date { get; set; }

        public string ToSearchText()
        {
            return $"{Name} {ContactNo} {Treatment} {Specialist} {Channel} {Billing} {Date}";
        }
        public static AppointmentRow FromRow(IDictionary<string, object?> row) => new()
        {
            Name = row.GetString("NAME"),
            ContactNo = row.GetString("CONTACT NO."),
            Treatment = row.GetString("Treatment"),
            Specialist = row.GetNullableString("Treatment Specialist"),
            Channel = row.GetNullableString("Channel"),
            CheckIn = row.GetNullableString("Check IN"),
            CheckOut = row.GetNullableString("Check Out"),
            TotalTime = row.GetNullableString("Total Time"),
            Room = row.GetNullableString("Treatment Room"),
            Amount = row.GetDecimal("Net Amount"),
            Billing = row.GetNullableString("Billing Mode"),
            Date = row.GetNullableString("Date"),
        };
    }
}
