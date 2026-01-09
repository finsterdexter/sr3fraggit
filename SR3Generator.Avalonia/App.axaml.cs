using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SR3Generator.Avalonia.Services;
using SR3Generator.Avalonia.ViewModels;
using SR3Generator.Avalonia.ViewModels.Tabs;
using SR3Generator.Avalonia.Views;
using SR3Generator.Database;
using SR3Generator.Database.Connection;
using System;
using System.IO;

namespace SR3Generator.Avalonia;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        Services = services.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = Services.GetRequiredService<MainWindowViewModel>()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // Logging
        services.AddLogging(builder =>
        {
            builder.AddDebug();
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        // Database configuration
        services.Configure<DatabaseOptions>(options =>
        {
            options.DatabasePath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "data", "data_6d7b26801.db");
        });

        // Database services - uses the public constructor
        services.AddSingleton<SkillDatabase>();
        services.AddSingleton<GearDatabase>();

        // Character builder service
        services.AddSingleton<ICharacterBuilderService, CharacterBuilderService>();

        // Tab ViewModels
        services.AddTransient<PrioritiesViewModel>();
        services.AddTransient<RaceViewModel>();
        services.AddTransient<MagicViewModel>();
        services.AddTransient<AttributesViewModel>();
        services.AddTransient<SkillsViewModel>();
        services.AddTransient<SpellsViewModel>();
        services.AddTransient<GearViewModel>();
        services.AddTransient<ContactsViewModel>();
        services.AddTransient<SummaryViewModel>();

        // Shell and Main ViewModels
        services.AddTransient<CharacterShellViewModel>();
        services.AddTransient<MainWindowViewModel>();
    }
}
