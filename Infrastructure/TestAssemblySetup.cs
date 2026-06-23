// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * Assembly-level setup that creates one shared KlacksApiFactory for the entire test run.
 * One factory = one ASP.NET Core in-process server = one connection pool — avoids
 * "too many connections" errors when many test fixtures run in sequence.
 * Must live in the root namespace (no sub-namespace) so NUnit applies it to all tests.
 */

using Klacks.ApiTest.Infrastructure;
using NUnit.Framework;

namespace Klacks.ApiTest;

[SetUpFixture]
public class TestAssemblySetup
{
    public static KlacksApiFactory SharedFactory { get; private set; } = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        SharedFactory = new KlacksApiFactory();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        SharedFactory?.Dispose();
    }
}
