using SchoolAdmission.Application.Common;
using SchoolAdmission.Domain.Exceptions;
using Xunit;

namespace SchoolAdmission.Tests.Unit;

public class SeatMathTests
{
    [Theory]
    [InlineData(120, 0, 0, 120)]
    [InlineData(120, 119, 0, 1)]
    [InlineData(120, 100, 20, 0)]
    [InlineData(10, 8, 5, 0)] // never negative
    public void Available_seats_are_total_minus_held_minus_confirmed(int total, int held, int confirmed, int expected)
        => Assert.Equal(expected, SeatMath.Available(total, held, confirmed));

    [Fact]
    public void Expiry_is_now_plus_duration()
    {
        var now = new DateTime(2026, 11, 9, 10, 0, 0, DateTimeKind.Utc);
        Assert.Equal(now.AddMinutes(30), SeatMath.ComputeExpiry(now, 1800));
    }

    [Fact]
    public void Remaining_seconds_never_negative_and_floors()
    {
        var now = new DateTime(2026, 11, 9, 10, 0, 0, DateTimeKind.Utc);
        Assert.Equal(1800, SeatMath.RemainingSeconds(now.AddSeconds(1800), now));
        Assert.Equal(0, SeatMath.RemainingSeconds(now.AddSeconds(0.9), now));
        Assert.Equal(0, SeatMath.RemainingSeconds(now.AddSeconds(-5), now));
        Assert.Equal(0, SeatMath.RemainingSeconds(null, now));
    }

    [Fact]
    public void Deadline_reached_counts_as_overdue()
    {
        var now = DateTime.UtcNow;
        Assert.True(SeatMath.IsOverdue(now, now));
        Assert.False(SeatMath.IsOverdue(now.AddSeconds(1), now));
    }
}

public class ValidationTests
{
    [Theory]
    [InlineData("20152691234567890", true)]
    [InlineData("2015269123456789", false)]   // 16 digits
    [InlineData("201526912345678901", false)] // 18 digits
    [InlineData("2015269123456789a", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void Student_id_must_be_exactly_17_digits(string? value, bool valid)
        => Assert.Equal(valid, Validation.IsValidStudentId(value));

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(86_401)]
    [InlineData(null)]
    public void Invalid_hold_duration_is_rejected(int? seconds)
        => Assert.Throws<DomainException>(() => Validation.RequireHoldDuration(seconds));

    [Theory]
    [InlineData(1)]
    [InlineData(1800)]
    [InlineData(86_400)]
    public void Valid_hold_duration_is_accepted(int seconds)
        => Assert.Equal(seconds, Validation.RequireHoldDuration(seconds));

    [Theory]
    [InlineData(-1)]
    [InlineData(100_001)]
    [InlineData(null)]
    public void Invalid_capacity_is_rejected(int? seats)
        => Assert.Throws<DomainException>(() => Validation.RequireSeats(seats));

    [Fact]
    public void Shift_is_normalised_to_upper_case()
        => Assert.Equal("MORNING", Validation.NormalizeShift(" morning "));

    [Fact]
    public void Display_name_combines_name_and_shift_once()
    {
        Assert.Equal("Class 6 - Morning Shift", ClassMapper.DisplayName("Class 6", "MORNING"));
        Assert.Equal("Class 6 - Morning Shift", ClassMapper.DisplayName("Class 6 - Morning Shift", "MORNING"));
    }
}
