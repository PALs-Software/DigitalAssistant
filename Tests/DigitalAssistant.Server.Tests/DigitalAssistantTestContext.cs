using BlazorBase.Abstractions.CRUD.Interfaces;
using BlazorBase.CRUD.Services;
using DigitalAssistant.Server.Modules.Commands.Services;
using DigitalAssistant.Server.Tests.Mockups;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Threading;
using BlazorBase.CRUD;
using DigitalAssistant.Server.Modules.Localization;

namespace DigitalAssistant.Server.Tests;

/// <summary>
/// Test context wrapper for bUnit.
/// Read more about using <see cref="DigitalAssistantTestContext"/> <seealso href="https://bunit.dev/docs/getting-started/writing-tests.html#remove-boilerplate-code-from-tests">here</seealso>.
/// </summary>
public abstract class DigitalAssistantTestContext : TestContextWrapper
{
    [TestInitialize]
    public virtual void Setup()
    {
        Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("en");
        CultureInfo.DefaultThreadCurrentCulture = Thread.CurrentThread.CurrentCulture;
        CultureInfo.DefaultThreadCurrentUICulture = Thread.CurrentThread.CurrentCulture;

        TestContext = new Bunit.TestContext();

        TestContext.Services.AddDbContext<DbContextMockup>(options => options.UseInMemoryDatabase(databaseName: "DigitalAssistantTestDbMockup"));
        TestContext.Services.AddBlazorBaseCRUD<DbContextMockup>(options =>
        {
            options.UseAsyncDbContextMethodsPerDefaultInBaseDbContext = false;
        });

        TestContext.Services
            .AddLocalization()
            .AddSingleton<CommandTemplateParser>()

            .AddTransient<CommandParameterParser>()
            .AddTransient(typeof(JsonStringLocalizer<>))
        ;
    }

    [TestCleanup]
    public void TearDown() => TestContext?.Dispose();
}
