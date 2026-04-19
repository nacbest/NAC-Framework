using Nac.Cqrs;

namespace Nac.Testing.Tests.TestHelpers;

public sealed record SampleRequest(string Input) : IBaseRequest<string>;
