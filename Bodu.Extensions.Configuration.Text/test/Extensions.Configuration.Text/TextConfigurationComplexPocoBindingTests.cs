// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TextConfigurationComplexPocoBindingTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Bodu.Extensions.Configuration.Text.Tests;

/// <summary>
/// Verifies POCO binding for a multi-site web application represented in a single
/// <c>.boduconfig</c> file. The same <see cref="SiteOptions" /> POCO type is reused across
/// three top-level sections (<c>public</c>, <c>admin</c>, <c>api</c>); a separate
/// <see cref="AppOptions" /> type occupies the <c>app</c> section. The suite validates two-
/// and three-level nesting, absent optional sub-sections, and DI resolution via both
/// <see cref="IOptions{TOptions}" /> and named <see cref="IOptionsMonitor{TOptions}" />.
/// </summary>
[TestClass]
public class TextConfigurationComplexPocoBindingTests
{
    // Single .boduconfig source covering all sections. Dotted keys are mapped to
    // colon-delimited configuration paths by the Bodu text configuration provider.
    // The api site intentionally omits ssl.certificatepath, ssl.keypath, ssl.thumbprint,
    // and ssl.renewal.* to exercise optional nested-object binding.
    private const string Source = """
app.name = Bodu Web Application
app.environment = Production
app.database.connectionstring = Server=db.example.com;Database=boduweb;User=app
app.database.commandtimeoutseconds = 30

public.url = https://www.example.com
public.port = 443
public.physicalpath = /var/www/public
public.ssl.enabled = true
public.ssl.certificatepath = /etc/ssl/certs/public.crt
public.ssl.keypath = /etc/ssl/private/public.key
public.ssl.thumbprint = A1B2C3D4E5F6
public.ssl.renewal.autorenew = true
public.ssl.renewal.daysbeforeexpiry = 30
public.healthcheck.path = /health
public.healthcheck.timeoutseconds = 5

admin.url = https://admin.example.com
admin.port = 8443
admin.physicalpath = /var/www/admin
admin.ssl.enabled = true
admin.ssl.certificatepath = /etc/ssl/certs/admin.crt
admin.ssl.keypath = /etc/ssl/private/admin.key
admin.ssl.thumbprint = B2C3D4E5F6A1
admin.ssl.renewal.autorenew = false
admin.ssl.renewal.daysbeforeexpiry = 14
admin.healthcheck.path = /admin/health
admin.healthcheck.timeoutseconds = 10

api.url = https://api.example.com
api.port = 8080
api.physicalpath = /var/www/api
api.ssl.enabled = false
api.healthcheck.path = /api/health
api.healthcheck.timeoutseconds = 3
""";

    private static IConfiguration BuildConfiguration()
    {
        using MemoryStream stream = new(Encoding.UTF8.GetBytes(Source));
        return new ConfigurationBuilder()
            .AddConfiguration(stream)
            .Build();
    }

    // ------------------------------------------------------------------
    // AppOptions — root scalar and one level of nesting
    // ------------------------------------------------------------------

    /// <summary>
    /// Verifies that the <c>app</c> section binds the root string properties
    /// <see cref="AppOptions.Name" /> and <see cref="AppOptions.Environment" /> correctly.
    /// </summary>
    [TestMethod]
    public void Bind_WhenAppSectionBound_ShouldResolveNameAndEnvironment()
    {
        IConfiguration configuration = BuildConfiguration();

        AppOptions options = configuration.GetSection("app").Get<AppOptions>() ?? new();

        Assert.AreEqual("Bodu Web Application", options.Name);
        Assert.AreEqual("Production", options.Environment);
    }

    /// <summary>
    /// Verifies that the <c>app.database</c> sub-section binds to the nested
    /// <see cref="AppOptions.Database" /> property, including the connection string and
    /// command timeout.
    /// </summary>
    [TestMethod]
    public void Bind_WhenAppSectionBound_ShouldResolveNestedDatabaseOptions()
    {
        IConfiguration configuration = BuildConfiguration();

        AppOptions options = configuration.GetSection("app").Get<AppOptions>() ?? new();

        Assert.IsNotNull(options.Database);
        Assert.AreEqual("Server=db.example.com;Database=boduweb;User=app", options.Database.ConnectionString);
        Assert.AreEqual(30, options.Database.CommandTimeoutSeconds);
    }

    // ------------------------------------------------------------------
    // SiteOptions — public site (url, port, path; ssl with renewal; healthcheck)
    // ------------------------------------------------------------------

    /// <summary>
    /// Verifies that the <c>public</c> section binds the root scalar properties of
    /// <see cref="SiteOptions" /> — URL, port, and physical path.
    /// </summary>
    [TestMethod]
    public void Bind_WhenPublicSiteBound_ShouldResolveRootSiteProperties()
    {
        IConfiguration configuration = BuildConfiguration();

        SiteOptions site = configuration.GetSection("public").Get<SiteOptions>() ?? new();

        Assert.AreEqual("https://www.example.com", site.Url);
        Assert.AreEqual(443, site.Port);
        Assert.AreEqual("/var/www/public", site.PhysicalPath);
    }

    /// <summary>
    /// Verifies that the <c>public.ssl</c> sub-section binds to the nested
    /// <see cref="SiteOptions.Ssl" /> property, including the enabled flag, certificate path,
    /// key path, and thumbprint.
    /// </summary>
    [TestMethod]
    public void Bind_WhenPublicSiteBound_ShouldResolveSslOptions()
    {
        IConfiguration configuration = BuildConfiguration();

        SiteOptions site = configuration.GetSection("public").Get<SiteOptions>() ?? new();

        Assert.IsNotNull(site.Ssl);
        Assert.IsTrue(site.Ssl.Enabled);
        Assert.AreEqual("/etc/ssl/certs/public.crt", site.Ssl.CertificatePath);
        Assert.AreEqual("/etc/ssl/private/public.key", site.Ssl.KeyPath);
        Assert.AreEqual("A1B2C3D4E5F6", site.Ssl.Thumbprint);
    }

    /// <summary>
    /// Verifies that the <c>public.ssl.renewal</c> sub-section — three levels deep — binds
    /// to <see cref="SslOptions.Renewal" />, including the auto-renew flag and the days-before-
    /// expiry threshold.
    /// </summary>
    [TestMethod]
    public void Bind_WhenPublicSiteBound_ShouldResolveSslRenewalOptions()
    {
        IConfiguration configuration = BuildConfiguration();

        SiteOptions site = configuration.GetSection("public").Get<SiteOptions>() ?? new();

        Assert.IsNotNull(site.Ssl);
        Assert.IsNotNull(site.Ssl.Renewal);
        Assert.IsTrue(site.Ssl.Renewal.AutoRenew);
        Assert.AreEqual(30, site.Ssl.Renewal.DaysBeforeExpiry);
    }

    /// <summary>
    /// Verifies that the <c>public.healthcheck</c> sub-section binds to the nested
    /// <see cref="SiteOptions.HealthCheck" /> property.
    /// </summary>
    [TestMethod]
    public void Bind_WhenPublicSiteBound_ShouldResolveHealthCheckOptions()
    {
        IConfiguration configuration = BuildConfiguration();

        SiteOptions site = configuration.GetSection("public").Get<SiteOptions>() ?? new();

        Assert.IsNotNull(site.HealthCheck);
        Assert.AreEqual("/health", site.HealthCheck.Path);
        Assert.AreEqual(5, site.HealthCheck.TimeoutSeconds);
    }

    // ------------------------------------------------------------------
    // SiteOptions — admin site (same POCO type, independent section values)
    // ------------------------------------------------------------------

    /// <summary>
    /// Verifies that the <c>admin</c> section binds to the same <see cref="SiteOptions" />
    /// POCO type as the public site, producing independent values from its own keys, including
    /// a distinct port, thumbprint, and renewal policy.
    /// </summary>
    [TestMethod]
    public void Bind_WhenAdminSiteBound_ShouldReusePocoTypeForDifferentSection()
    {
        IConfiguration configuration = BuildConfiguration();

        SiteOptions admin = configuration.GetSection("admin").Get<SiteOptions>() ?? new();

        Assert.AreEqual("https://admin.example.com", admin.Url);
        Assert.AreEqual(8443, admin.Port);
        Assert.AreEqual("/var/www/admin", admin.PhysicalPath);
        Assert.IsNotNull(admin.Ssl);
        Assert.IsTrue(admin.Ssl.Enabled);
        Assert.AreEqual("B2C3D4E5F6A1", admin.Ssl.Thumbprint);
        Assert.IsNotNull(admin.Ssl.Renewal);
        Assert.IsFalse(admin.Ssl.Renewal.AutoRenew);
        Assert.AreEqual(14, admin.Ssl.Renewal.DaysBeforeExpiry);
        Assert.IsNotNull(admin.HealthCheck);
        Assert.AreEqual("/admin/health", admin.HealthCheck.Path);
        Assert.AreEqual(10, admin.HealthCheck.TimeoutSeconds);
    }

    // ------------------------------------------------------------------
    // SiteOptions — api site (ssl.enabled only; certificate/key/thumbprint/renewal absent)
    // ------------------------------------------------------------------

    /// <summary>
    /// Verifies that the <c>api</c> site, which omits <c>ssl.certificatepath</c>,
    /// <c>ssl.keypath</c>, <c>ssl.thumbprint</c>, and all <c>ssl.renewal.*</c> keys, binds
    /// those properties to <see langword="null" /> or their default values while still
    /// creating the <see cref="SslOptions" /> instance from the present
    /// <c>ssl.enabled</c> key.
    /// </summary>
    [TestMethod]
    public void Bind_WhenApiSiteHasNoSslDetails_ShouldBindPartialSslAndNullRenewal()
    {
        IConfiguration configuration = BuildConfiguration();

        SiteOptions api = configuration.GetSection("api").Get<SiteOptions>() ?? new();

        Assert.AreEqual("https://api.example.com", api.Url);
        Assert.AreEqual(8080, api.Port);
        Assert.IsNotNull(api.Ssl);
        Assert.IsFalse(api.Ssl.Enabled);
        Assert.IsNull(api.Ssl.CertificatePath);
        Assert.IsNull(api.Ssl.KeyPath);
        Assert.IsNull(api.Ssl.Thumbprint);
        Assert.IsNull(api.Ssl.Renewal);
        Assert.IsNotNull(api.HealthCheck);
        Assert.AreEqual("/api/health", api.HealthCheck.Path);
        Assert.AreEqual(3, api.HealthCheck.TimeoutSeconds);
    }

    // ------------------------------------------------------------------
    // DI — named IOptionsMonitor<SiteOptions> for all three sites
    // ------------------------------------------------------------------

    /// <summary>
    /// Verifies that all three sites can be registered as named <see cref="SiteOptions" />
    /// instances and resolved independently via <see cref="IOptionsMonitor{TOptions}" />
    /// from the DI container, confirming that each named registration carries its own values.
    /// </summary>
    [TestMethod]
    public void Bind_WhenAllSitesBoundAsNamedOptions_ShouldResolveEachSiteFromMonitor()
    {
        IConfiguration configuration = BuildConfiguration();
        ServiceCollection services = new();
        services.AddOptions();
        services.Configure<SiteOptions>("public", configuration.GetSection("public"));
        services.Configure<SiteOptions>("admin", configuration.GetSection("admin"));
        services.Configure<SiteOptions>("api", configuration.GetSection("api"));

        using ServiceProvider provider = services.BuildServiceProvider();
        IOptionsMonitor<SiteOptions> monitor = provider.GetRequiredService<IOptionsMonitor<SiteOptions>>();

        SiteOptions publicSite = monitor.Get("public");
        SiteOptions adminSite = monitor.Get("admin");
        SiteOptions apiSite = monitor.Get("api");

        Assert.AreEqual(443, publicSite.Port);
        Assert.IsNotNull(publicSite.Ssl?.Renewal);
        Assert.IsTrue(publicSite.Ssl!.Renewal!.AutoRenew);

        Assert.AreEqual(8443, adminSite.Port);
        Assert.IsNotNull(adminSite.Ssl?.Renewal);
        Assert.IsFalse(adminSite.Ssl!.Renewal!.AutoRenew);

        Assert.AreEqual(8080, apiSite.Port);
        Assert.IsNull(apiSite.Ssl?.Renewal);
    }

    /// <summary>
    /// Verifies that mixed POCO types from the same single <c>.boduconfig</c> source —
    /// <see cref="AppOptions" /> via
    /// <see cref="ConfigurationOptionsExtensions.AddConfigurationOptions{TOptions}(IServiceCollection, IConfiguration, string)" />
    /// and <see cref="SiteOptions" /> via named <see cref="IOptionsMonitor{TOptions}" /> — can
    /// all be resolved from one DI service provider.
    /// </summary>
    [TestMethod]
    public void Bind_WhenAppAndSitesBoundViaDi_ShouldResolveAllTypesFromServiceProvider()
    {
        IConfiguration configuration = BuildConfiguration();
        ServiceCollection services = new();
        services.AddOptions();
        services.AddConfigurationOptions<AppOptions>(configuration, "app");
        services.Configure<SiteOptions>("public", configuration.GetSection("public"));
        services.Configure<SiteOptions>("admin", configuration.GetSection("admin"));
        services.Configure<SiteOptions>("api", configuration.GetSection("api"));

        using ServiceProvider provider = services.BuildServiceProvider();

        AppOptions app = provider.GetRequiredService<IOptions<AppOptions>>().Value;
        IOptionsMonitor<SiteOptions> sites = provider.GetRequiredService<IOptionsMonitor<SiteOptions>>();

        Assert.AreEqual("Bodu Web Application", app.Name);
        Assert.AreEqual("Production", app.Environment);
        Assert.IsNotNull(app.Database);
        Assert.AreEqual(30, app.Database.CommandTimeoutSeconds);

        Assert.AreEqual(443, sites.Get("public").Port);
        Assert.AreEqual("/etc/ssl/certs/public.crt", sites.Get("public").Ssl?.CertificatePath);
        Assert.AreEqual(8443, sites.Get("admin").Port);
        Assert.AreEqual(8080, sites.Get("api").Port);
        Assert.IsNull(sites.Get("api").Ssl?.Renewal);
    }

    // ------------------------------------------------------------------
    // POCO definitions
    // ------------------------------------------------------------------

    /// <summary>
    /// Represents application-level configuration, including the runtime environment and
    /// database connection details.
    /// </summary>
    private sealed class AppOptions
    {
        /// <summary>
        /// Gets or sets the application display name.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the runtime environment (for example, <c>Production</c> or
        /// <c>Development</c>).
        /// </summary>
        public string? Environment { get; set; }

        /// <summary>
        /// Gets or sets the database connection options.
        /// </summary>
        public DatabaseOptions? Database { get; set; }
    }

    /// <summary>
    /// Represents database connectivity options.
    /// </summary>
    private sealed class DatabaseOptions
    {
        /// <summary>
        /// Gets or sets the ADO.NET connection string.
        /// </summary>
        public string? ConnectionString { get; set; }

        /// <summary>
        /// Gets or sets the command timeout in seconds.
        /// </summary>
        public int CommandTimeoutSeconds { get; set; }
    }

    /// <summary>
    /// Represents configuration for a single web site or endpoint — URL, port, physical path,
    /// TLS settings, and a health-check probe. Reused across multiple sections of the
    /// <c>.boduconfig</c> file for independent site instances.
    /// </summary>
    private sealed class SiteOptions
    {
        /// <summary>
        /// Gets or sets the public base URL of the site.
        /// </summary>
        public string? Url { get; set; }

        /// <summary>
        /// Gets or sets the TCP port the site listens on.
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// Gets or sets the physical root directory served by the site.
        /// </summary>
        public string? PhysicalPath { get; set; }

        /// <summary>
        /// Gets or sets the TLS/SSL configuration for the site.
        /// </summary>
        public SslOptions? Ssl { get; set; }

        /// <summary>
        /// Gets or sets the health-check probe configuration for the site.
        /// </summary>
        public HealthCheckOptions? HealthCheck { get; set; }
    }

    /// <summary>
    /// Represents TLS/SSL certificate and key configuration for a site.
    /// </summary>
    private sealed class SslOptions
    {
        /// <summary>
        /// Gets or sets a value indicating whether TLS is enabled for the site.
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// Gets or sets the file system path to the PEM-encoded certificate.
        /// </summary>
        public string? CertificatePath { get; set; }

        /// <summary>
        /// Gets or sets the file system path to the PEM-encoded private key.
        /// </summary>
        public string? KeyPath { get; set; }

        /// <summary>
        /// Gets or sets the certificate thumbprint (SHA-1 hex) used for identity verification.
        /// </summary>
        public string? Thumbprint { get; set; }

        /// <summary>
        /// Gets or sets the automatic renewal policy for the certificate.
        /// <see langword="null" /> when no renewal configuration is present.
        /// </summary>
        public RenewalOptions? Renewal { get; set; }
    }

    /// <summary>
    /// Represents the automatic certificate renewal policy nested within SSL options.
    /// </summary>
    private sealed class RenewalOptions
    {
        /// <summary>
        /// Gets or sets a value indicating whether the certificate should be renewed automatically.
        /// </summary>
        public bool AutoRenew { get; set; }

        /// <summary>
        /// Gets or sets the number of days before expiry at which renewal is triggered.
        /// </summary>
        public int DaysBeforeExpiry { get; set; }
    }

    /// <summary>
    /// Represents the HTTP health-check probe configuration for a site.
    /// </summary>
    private sealed class HealthCheckOptions
    {
        /// <summary>
        /// Gets or sets the URL path of the health-check endpoint (for example, <c>/health</c>).
        /// </summary>
        public string? Path { get; set; }

        /// <summary>
        /// Gets or sets the timeout in seconds before the health check is considered failed.
        /// </summary>
        public int TimeoutSeconds { get; set; }
    }
}
