using System.Net;
using WebApplication3.Tests.Support;

namespace WebApplication3.Tests.Integration;

public sealed class AuthFlowTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AuthFlowTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task SuperAdminUrl_WhenAnonymous_RedirectsToLoginWithoutReturnUrl()
    {
        var client = _factory.CreateClient(new() { AllowAutoRedirect = false });

        var response = await client.GetAsync("/superadmin");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Account/Login", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task AjaxLogin_ForSuperAdmin_ReturnsSuperAdminDestinationAndSetsSessionCookies()
    {
        var client = _factory.CreateClient(new() { AllowAutoRedirect = false });
        var token = await GetLoginTokenAsync(client);

        var request = new HttpRequestMessage(HttpMethod.Post, "/Account/Login");
        request.Headers.Add("X-Requested-With", "XMLHttpRequest");
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Email"] = "nasermustafi@gmail.com",
            ["Password"] = "Admin123",
            ["RememberMe"] = "false",
            ["__RequestVerificationToken"] = token
        });

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();
        var cookies = response.Headers.TryGetValues("Set-Cookie", out var setCookies)
            ? string.Join("\n", setCookies)
            : string.Empty;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"success\":true", body);
        Assert.Contains("\"redirectUrl\":\"/SuperAdmin\"", body);
        Assert.Contains(".AspNetCore.Identity.Application", cookies);
        Assert.Contains("pos_session=", cookies);
    }

    [Fact]
    public async Task AjaxLogout_ReturnsOkAndClearsSessionMarker()
    {
        var client = _factory.CreateClient(new() { AllowAutoRedirect = false });
        var loginToken = await GetLoginTokenAsync(client);

        var loginRequest = new HttpRequestMessage(HttpMethod.Post, "/Account/Login");
        loginRequest.Headers.Add("X-Requested-With", "XMLHttpRequest");
        loginRequest.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Email"] = "nasermustafi@gmail.com",
            ["Password"] = "Admin123",
            ["RememberMe"] = "false",
            ["__RequestVerificationToken"] = loginToken
        });
        await client.SendAsync(loginRequest);

        var page = await client.GetStringAsync("/SuperAdmin");
        var logoutToken = HtmlTestHelpers.ExtractRequestVerificationToken(page);
        var logoutRequest = new HttpRequestMessage(HttpMethod.Post, "/Account/Logout");
        logoutRequest.Headers.Add("X-Requested-With", "XMLHttpRequest");
        logoutRequest.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = logoutToken
        });

        var logoutResponse = await client.SendAsync(logoutRequest);
        var body = await logoutResponse.Content.ReadAsStringAsync();
        var cookies = logoutResponse.Headers.TryGetValues("Set-Cookie", out var setCookies)
            ? string.Join("\n", setCookies)
            : string.Empty;

        Assert.Equal(HttpStatusCode.OK, logoutResponse.StatusCode);
        Assert.Contains("\"success\":true", body);
        Assert.Contains("\"redirectUrl\":\"/Account/Login\"", body);
        Assert.Contains("pos_session=", cookies);
        Assert.Contains("expires=", cookies, StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<string> GetLoginTokenAsync(HttpClient client)
    {
        var loginPage = await client.GetStringAsync("/Account/Login");
        return HtmlTestHelpers.ExtractRequestVerificationToken(loginPage);
    }
}
