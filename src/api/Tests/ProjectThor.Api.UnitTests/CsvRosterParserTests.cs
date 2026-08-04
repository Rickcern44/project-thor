using ProjectThor.Api.Infrastructure.Import;

namespace ProjectThor.Api.UnitTests;

public class CsvRosterParserTests
{
    private const string SampleCsv =
        "Name,8-Jan,15-Jan,22-Jan,Attendance,Total Due ($8/night), Amount Paid ,Balance,,,,\n" +
        "John Cooke,x,x,,2, $ 14.00 , $ 21.00 , $ 7.00 ,,,,\n" +
        "Jerad,x,,x,2, $ 14.00 , $ 7.00 , $ (7.00),,,240,2/4\n";

    [Fact]
    public void Parses_name_and_attended_dates_from_x_marks()
    {
        var rows = CsvRosterParser.Parse(SampleCsv, seasonYear: 2026);

        var john = rows.Single(r => r.Name == "John Cooke");
        Assert.Equal([new DateOnly(2026, 1, 8), new DateOnly(2026, 1, 15)], john.AttendedDates);
    }

    [Fact]
    public void Parses_currency_with_dollar_sign_and_padding()
    {
        var rows = CsvRosterParser.Parse(SampleCsv, seasonYear: 2026);

        var john = rows.Single(r => r.Name == "John Cooke");
        Assert.Equal(14.00m, john.TotalDue);
        Assert.Equal(21.00m, john.AmountPaid);
    }

    [Fact]
    public void Ignores_unlabeled_trailing_columns()
    {
        var rows = CsvRosterParser.Parse(SampleCsv, seasonYear: 2026);

        var jerad = rows.Single(r => r.Name == "Jerad");
        Assert.Equal(14.00m, jerad.TotalDue);
        Assert.Equal(7.00m, jerad.AmountPaid);
    }
}
