using SchoolAdmission.Application.Common;

namespace SchoolAdmission.Api.Controllers;

public static class StudentIdentity
{
    public const string HeaderName = "X-Student-Id";

    /// <summary>
    /// Reads the simulated student identity header. This is an assignment-level identity contract,
    /// not production authentication.
    /// </summary>
    public static string Require(HttpRequest request)
        => Validation.RequireStudentId(request.Headers[HeaderName].FirstOrDefault());
}
