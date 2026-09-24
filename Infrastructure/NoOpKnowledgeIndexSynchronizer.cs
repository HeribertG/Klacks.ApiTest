// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * Stand-in for the knowledge index synchronizer in the API test host. No API test reads the index, and
 * the real synchronizer re-embeds it (ONNX model load, ~2 GB) whenever the shared test database's skill
 * phrases changed since the last host start.
 */

using Klacks.Api.KnowledgeIndex.Application.Interfaces;

namespace Klacks.ApiTest.Infrastructure;

public sealed class NoOpKnowledgeIndexSynchronizer : IKnowledgeIndexSynchronizer
{
    public Task SyncAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
