namespace FesbHelpdesk.Api.Services;

public interface IGeminiCategoryService
{
    Task<string> ClassifyAsync(string title, string description, IEnumerable<string> categories, CancellationToken ct = default);
}
