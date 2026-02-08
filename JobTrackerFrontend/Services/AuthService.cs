using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;

namespace JobTrackerFrontend.Services;
using System.IO;

public class AuthService
{
    private readonly IPublicClientApplication _msalClient;
    private AuthenticationResult? _cachedAuth;
    private bool _cacheRegistered;

    private static readonly string CacheDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "TylerBuilds", "JobTracker"
    );
    private const string CacheFileName = "msal_cache.bin";

    public AuthService()
    {
        _msalClient = PublicClientApplicationBuilder
            .Create(AppConfig.ClientId)
            .WithAuthority(AppConfig.Authority)
            .WithRedirectUri("http://localhost")
            .Build();
    }

    public string? UserDisplayName => _cachedAuth?.Account?.Username;
    public bool IsAuthenticated => _cachedAuth != null;

    public async Task EnablePersistentCacheAsync()
    {
        if (_cacheRegistered) return;

        var storageProperties = new StorageCreationPropertiesBuilder(CacheFileName, CacheDir).Build();
        var cacheHelper = await MsalCacheHelper.CreateAsync(storageProperties);
        cacheHelper.RegisterCache(_msalClient.UserTokenCache);
        _cacheRegistered = true;
    }

    public void DisablePersistentCache()
    {
        // Clear any persisted cache file
        var cachePath = Path.Combine(CacheDir, CacheFileName);
        if (File.Exists(cachePath))
            File.Delete(cachePath);
    }

    public async Task<bool> TrySilentLoginAsync()
    {
        var accounts = await _msalClient.GetAccountsAsync();
        var account = accounts.FirstOrDefault();
        if (account == null) return false;

        try
        {
            _cachedAuth = await _msalClient
                .AcquireTokenSilent(AppConfig.Scopes, account)
                .ExecuteAsync();
            return true;
        }
        catch (MsalUiRequiredException)
        {
            return false;
        }
    }

    public async Task<string> GetAccessTokenAsync()
    {
        var accounts = await _msalClient.GetAccountsAsync();
        var account = accounts.FirstOrDefault();

        if (account != null)
        {
            try
            {
                _cachedAuth = await _msalClient
                    .AcquireTokenSilent(AppConfig.Scopes, account)
                    .ExecuteAsync();
                return _cachedAuth.AccessToken;
            }
            catch (MsalUiRequiredException)
            {
                // Fall through to interactive
            }
        }

        return await LoginAsync();
    }

    public async Task<string> LoginAsync()
    {
        _cachedAuth = await _msalClient
            .AcquireTokenInteractive(AppConfig.Scopes)
            .WithUseEmbeddedWebView(false)
            .ExecuteAsync();

        return _cachedAuth.AccessToken;
    }

    public async Task LogoutAsync()
    {
        var accounts = await _msalClient.GetAccountsAsync();
        foreach (var account in accounts)
        {
            await _msalClient.RemoveAsync(account);
        }

        DisablePersistentCache();
        _cachedAuth = null;
    }
}
