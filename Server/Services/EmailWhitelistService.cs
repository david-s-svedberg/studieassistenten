namespace StudieAssistenten.Server.Services;

public interface IEmailWhitelistService
{
    bool IsEmailWhitelisted(string email);
    List<string> GetWhitelistedEmails();
}

public class EmailWhitelistService : IEmailWhitelistService
{
    private readonly HashSet<string> _whitelistedEmails = new(StringComparer.OrdinalIgnoreCase)
    {
        "david.s.svedberg@gmail.com",
        "judith.svedberg@gmail.com"
    };

    public bool IsEmailWhitelisted(string email)
    {
        return _whitelistedEmails.Contains(email);
    }

    public List<string> GetWhitelistedEmails()
    {
        return _whitelistedEmails.ToList();
    }
}
