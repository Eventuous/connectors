using System.Text;
using Eventuous.Connector.EsdbGenericGrpc.Config;
using Eventuous.Gateway;
using Eventuous.Producers;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.Client;
using Grpc.Net.Client.Configuration;
using Microsoft.Extensions.Logging;

namespace Eventuous.Connector.EsdbGenericGrpc;

public class GrpcJsonProjector : BaseProducer<GrpcJsonProjectOptions> {
    readonly ILogger<GrpcJsonProjector> _log;
    readonly string                     _host;
    readonly ChannelCredentials         _creds;

    public GrpcJsonProjector(GrpcTargetConfig config, ILogger<GrpcJsonProjector> log) {
        _log   = log;
        _host  = config.GetHost();
        _creds = config.GetCredentials();
    }

    GrpcChannel GetChannel()
        => GrpcChannel.ForAddress(
            _host,
            new GrpcChannelOptions { Credentials = _creds, ServiceConfig = new ServiceConfig { MethodConfigs = { Defaults.DefaultMethodConfig } } }
        );

    protected override async Task ProduceMessages(
        StreamName                   stream,
        IEnumerable<ProducedMessage> messages,
        GrpcJsonProjectOptions?      options,
        CancellationToken            cancellationToken = default
    ) {
        using var channel = GetChannel();

        var client = new Projection.ProjectionClient(channel);

        var events  = messages.Select(AsProjectedEvent);
        var request = new ProjectRequest();
        request.Events.AddRange(events.Where(x => x.Stream[0] != '$'));

        _log.LogTrace("Projecting {Count} events to {Stream}", request.Events.Count, stream);
        var _ = await client.ProjectAsync(request, cancellationToken: cancellationToken);

        return;

        static ProjectedEvent AsProjectedEvent(ProducedMessage msg) {
            var json = Encoding.UTF8.GetString((msg.Message as byte[])!);

            var pe = new ProjectedEvent { EventId = msg.MessageId.ToString(), EventPayload = Struct.Parser.ParseJson(json), };

            if (msg.Metadata != null) { pe.Metadata.Add(msg.Metadata.ToHeaders()); }

            if (msg.AdditionalHeaders != null) {
                pe.Position    = (long)(ulong)msg.AdditionalHeaders[GatewayContextItems.OriginalGlobalPosition]!;
                pe.EventNumber = (int)(ulong)msg.AdditionalHeaders[GatewayContextItems.OriginalStreamPosition]!;
                pe.Stream      = ((StreamName)msg.AdditionalHeaders[GatewayContextItems.OriginalStream]!).ToString();
                pe.EventType   = (string)msg.AdditionalHeaders[GatewayContextItems.OriginalMessageType]!;
            }

            return pe;
        }
    }
}

public record GrpcJsonProjectOptions {
    public static GrpcJsonProjectOptions Default { get; } = new();
}
