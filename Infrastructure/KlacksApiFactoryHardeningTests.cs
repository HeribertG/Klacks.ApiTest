// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * Guards the hardened API test host, analogous to Klacks.IntegrationTest/HardenedTestHostTests: the shared
 * KlacksApiFactory must not run a single hosted service of Klacks' own beyond the four that have no switch
 * and neither call out nor write (ONNX warm-up, which reads its disable flag and returns, the ONNX idle
 * unloader, the schedule time zone check and the messaging field encryption bootstrap). A background service
 * added later without a BackgroundServices flag, or a flag the factory's reflection does not reach, turns
 * this red instead of quietly talking to Slack, Telegram, IMAP, Nominatim or an LLM provider from the next
 * test run. Only Klacks' own types are checked; framework and library services are not switchable here.
 * It also pins that the switches beat appsettings.Development.json, which turns the Slack owner bridge on,
 * that the ONNX warm-up is disabled and that the knowledge index synchronizer is the no-op one. It reuses
 * the assembly's shared host, so it boots nothing of its own.
 */

using Klacks.Api.Application.Configuration;
using Klacks.Api.Infrastructure.Services.Schedules;
using Klacks.Api.KnowledgeIndex.Application.Constants;
using Klacks.Api.KnowledgeIndex.Application.Interfaces;
using Klacks.Api.KnowledgeIndex.Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Klacks.ApiTest.Infrastructure;

[TestFixture]
public class KlacksApiFactoryHardeningTests
{
    private const string KlacksNamespacePrefix = "Klacks.";
    private const string MessagingEncryptionInitializerName = "MessagingEncryptionInitializer";

    private static readonly HashSet<string> UnswitchedHostedServices = new(StringComparer.Ordinal)
    {
        nameof(OnnxWarmupService),
        nameof(OnnxSessionIdleUnloadService),
        nameof(ScheduleTimeZoneStartupCheckService),
        MessagingEncryptionInitializerName
    };

    private static IServiceProvider HostServices => Klacks.ApiTest.TestAssemblySetup.SharedFactory.Services;

    [Test]
    public void SharedHost_RunsNoSwitchableHostedService()
    {
        var hostedTypes = HostServices.GetServices<IHostedService>()
            .Select(service => service.GetType())
            .ToList();
        TestContext.Out.WriteLine("Hosted services: " + string.Join(", ", hostedTypes.Select(type => type.FullName)));

        var unexpected = hostedTypes
            .Where(type => (type.Namespace ?? string.Empty).StartsWith(KlacksNamespacePrefix, StringComparison.Ordinal))
            .Where(type => !UnswitchedHostedServices.Contains(type.Name))
            .Select(type => type.FullName)
            .ToList();

        unexpected.ShouldBeEmpty("the API test host must not start these hosted services");
    }

    [Test]
    public void SharedHost_SwitchesBeatTheDevelopmentSettings()
    {
        var configuration = HostServices.GetRequiredService<IConfiguration>();
        var stillOn = KlacksApiFactory.DisabledBackgroundServiceSwitches
            .Where(key => configuration.GetValue<bool>(key))
            .ToList();

        stillOn.ShouldBeEmpty();
        KlacksApiFactory.DisabledBackgroundServiceSwitches
            .ShouldContain(ConfigurationPath.Combine(
                BackgroundServiceOptions.SectionName, KlacksApiFactory.InboundMessagePollingSwitch));
        HostServices.GetRequiredService<IOptions<BackgroundServiceOptions>>().Value.SlackOwnerBridge
            .ShouldBeFalse("appsettings.Development.json turns the Slack owner bridge on; the test host must win");
        configuration.GetValue(KnowledgeIndexConstants.WarmupEnabledConfigKey, true).ShouldBeFalse();
    }

    [Test]
    public void SharedHost_UsesTheNoOpKnowledgeIndexSynchronizer()
    {
        using var scope = HostServices.CreateScope();

        scope.ServiceProvider.GetRequiredService<IKnowledgeIndexSynchronizer>()
            .ShouldBeOfType<NoOpKnowledgeIndexSynchronizer>();
    }
}
