namespace SchoolAdmission.Application.Common;

public class AdmissionOptions
{
    /// <summary>Default hold duration for classes created without an explicit one (env HOLD_DURATION_SECONDS).</summary>
    public int DefaultHoldDurationSeconds { get; set; } = 600;

    /// <summary>Simulated admission fee recorded in the payments table.</summary>
    public decimal FeeAmount { get; set; } = 5000m;
}
