using Grpc.Core;

namespace AutoPolicy.TestApp.Grpc;

public sealed class ProbeGrpcService : Probe.ProbeBase
{
    public override Task<ProbeReply> Get(ProbeRequest request, ServerCallContext context)
    {
        _ = context;
        return Task.FromResult(new ProbeReply { Value = request.Value });
    }
}
