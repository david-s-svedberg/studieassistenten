using Microsoft.Extensions.Configuration;

namespace StudieAssistenten.Server.Services;

public interface IEmailWhitelistService
{
    bool IsEmailWhitelisted(string email);
    List<string> GetWhitelistedEmails();
    bool IsWhitelistEnabled();
}

public class EmailWhitelistService : IEmailWhitelistService
{
    private readonly HashSet<string> _whitelistedEmails;
    private readonly bool _isEnabled;
    private readonly ILogger<EmailWhitelistService> _logger;

    public EmailWhitelistService(IConfiguration configuration, ILogger<EmailWhitelistService> logger)
    {
        _logger = logger;

        // Read whitelist configuration
        var emailList = configuration.GetSection("EmailWhitelist:AllowedEmails")
            .Get<List<string>>() ?? new List<string>();

        _whitelistedEmails = new HashSet<string>(emailList, StringComparer.OrdinalIgnoreCase);
        _isEnabled = configuration.GetValue<bool>("EmailWhitelist:EnableWhitelist", true);

        _logger.LogInformation(
            "Email whitelist initialized: {Count} emails, Enabled: {IsEnabled}",
            _whitelistedEmails.Count,
            _isEnabled);
    }

    public bool IsEmailWhitelisted(string email)
    {
        if (!_isEnabled)
        {
            // If whitelist is disabled, all emails are allowed
            return true;
        }

        return _whitelistedEmails.Contains(email);
    }

    public List<string> GetWhitelistedEmails()
    {
        return _whitelistedEmails.ToList();
    }

    public bool IsWhitelistEnabled()
    {
        return _isEnabled;
    }
}
