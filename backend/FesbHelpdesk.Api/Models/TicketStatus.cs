namespace FesbHelpdesk.Api.Models;

public static class TicketStatus
{
    public const string Novo = "Novo";
    public const string UObradi = "U obradi";
    public const string Rijeseno = "Riješeno";

    public static readonly string[] All = { Novo, UObradi, Rijeseno };
}
