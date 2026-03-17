namespace FesbHelpdesk.Api.Models;

public static class UserRole
{
    public const string Student = "student";
    public const string Referada = "referada";
    public const string Nastavnik = "nastavnik";
    public const string Admin = "admin";

    public static readonly string[] All = { Student, Referada, Nastavnik, Admin };
}
