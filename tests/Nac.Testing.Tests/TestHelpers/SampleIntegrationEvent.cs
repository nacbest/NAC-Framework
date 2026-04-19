using Nac.Core.Abstractions.Events;

namespace Nac.Testing.Tests.TestHelpers;

public sealed record SampleIntegrationEvent(Guid EventId, DateTime OccurredOn, string Data) : IIntegrationEvent;
