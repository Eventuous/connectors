# Generic gRPC sink example

This example shows how to use the generic gRPC sink to use Connector for sending events to a gRPC server, which would project events to some database for querying.

## Sink implementation

The server uses Connector Protobuf contract to generate the service bootstrap code. Connector acts as a client in this case.

There are three operations that the sink server needs to implement:
- Load the checkpoint
- Project an event
- Store the checkpoint

Connector will manage the subscription with all the necessary work like checkpointing, reconnecting, etc.

This example doesn't use any database. It returns zero as the checkpoint, so Connector will always subscribe to the log head. It also doesn't store the checkpoint anywhere, but it logs the checkpoint value to the console.

The event projection endpoint also just logs the events it received to the console.

## Configuration

You need to run the Connector instance with the following configuration:

```yaml
connector:
  connectorId: "esdb-grpc-connector"
  connectorAssembly: "Eventuous.Connector.EsdbGenericGrpc"
  diagnostics:
    tracing:
      enabled: true
      exporters: [zipkin]
    metrics:
      enabled: true
      exporters: [prometheus]
    traceSamplerProbability: 0
source:
  connectionString: "esdb://localhost:2113?tls=false"
  concurrencyLimit: 2
target:
  uri: "http://localhost:9091"
  credentials: "insecure"
```

Here, the `target.uri` is the address of the sink workload. If you deploy it somewhere else, you need to change the address.

The `target.credentials` setting can be `insecure` or `ssl`. Right now, there's no support for header-based authentication.

The `source.connectionString` is the address of the EventStoreDB instance.

Everything in the `diagnostics` section is optional. If you don't want to use tracing or metrics, you can remove it.

When you set `target.concurrencyLimit` to a value greater than 1, Connector will be calling the sink server in parallel. 
This is useful when you want to increase the throughput. Events will still be delivered in order, but the order is only maintained per partition. The only supported partition key is the stream name, and it's used by default.