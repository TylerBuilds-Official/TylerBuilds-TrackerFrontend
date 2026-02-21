using Microsoft.Extensions.Configuration;

namespace JobTrackerFrontend.Services;

public class AppConfig
{
    private static readonly IConfiguration Configuration;

    static AppConfig()
    {
        Configuration = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();
    }

    // API
    public static string ApiBaseUrl => Configuration["Api:BaseUrl"] ?? "";

    // Azure AD
    public static string TenantId => Configuration["AzureAd:TenantId"] ?? "";
    public static string ClientId => Configuration["AzureAd:ClientId"] ?? "";
    public static string[] Scopes => Configuration.GetSection("AzureAd:Scopes").Get<string[]>() ?? Array.Empty<string>();
    public static string Authority => $"https://login.microsoftonline.com/{TenantId}";

    // Updates
    public static string UpdateFeedPath => Configuration["Updates:FeedPath"] ?? "";

    // Invoices
    public static string InvoiceTemplatePath => Configuration["Invoices:TemplatePath"] ?? "";
    public static string InvoiceOutputFolder => Configuration["Invoices:OutputFolder"] ?? "";
}
