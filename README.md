# EventStore connector for Elasticsearch

An event-sourced system that stores domain events in EventStoreDB could benefit a lot from replicating these events to a search engine, such as Elasticsearch.
When all the events are available in Elasticsearch, the system can be queried for events, and when events properly convey information about business operations, you can discover quite a lot using tools like Kibana.

This connector allows you to replicate events to Elasticsearch without having any knowledge about your system insights like event contracts, or event types. The natural limitation of this approach is that events must be stored in EventStoreDB in JSON format.

## Configuration

The connector is configured using the `config.yaml` file. You can find an example of this file in the repository.

The config file consists of three sections:
* `connector` - the connector configuration
* `source` - configuration for the EventStoreDB source
* `target` - configuration for the Elasticsearch target

### Connector configuration

The connector section just needs one parameter - the connector id. This connector uses the connector id also as the checkpoint id.

```yaml
connector:
  subscriptionId: "esdb-elastic-connector"
```

If you run multiple instances of the connector, you should use different connector ids.

### Source configuration

The source configuration is used to connect to the EventStoreDB, as well as configure the subscription. At the moment, the connector will unconditionally subscribe to `$all` stream.

The following configuration parameters are supported:
* `connectionString` - EventStoreDB connection string using gRPC protocol. For example: `esdb://localhost:2113?tls=false`
* `concurrencyLimit` - the subscription concurrency limit. The default value is `1`.

```yaml
source:
    connectionString: "esdb://localhost:2113?tls=false"
    concurrencyLimit: 1
```

When the subscription concurrency limit is higher than `1`, the subscription will partition events between multiple Elasticsearch producer instances. As those producers will run in parallel, it will increase the overall throughput.

### Target configuration

The target configuration is used to connect to Elasticsearch, as well as create the necessary elements in Elasticsearch (index template, index rollover policy, and data stream).

The following configuration parameters are supported:
* `connectionString` - Elasticsearch connection string, should not be used when the `cloudId` is specified
* `cloudId` - Elasticsearch cloud id, should be used when the `connectionString` is not specified
* `apiKey` - Elasticsearch API key
* `dataStream` - the index configuration section
    * `indexName` - the index name (data stream name)
    * `template` - the template section
        * `templateName` - the template name
        * `numberOfShards` - the number of shards for the data stream, default is `1`
        * `numberOfReplicas` - the number of replicas for the data stream, default is `1`
    * `lifecycle` - the lifecycle section
        * `policyName` - the rollover policy name
        * `tiers` - the rollover policy tiers, see the structure of a `tier` section below

The `tier` section is used to configure the rollover policy tiers. The tier name must match the available tier in your Elasticsearch cluster.

* `tier` - the tier name (`hot`, `warm`, `cold`, etc)
* `minAge` - the minimum age of the data stream (for example `10d` for 10 days)
* `priority` - the priority of the tier (`0` is the lowest priority)
* `rollover` - the rollover policy section
    * `maxAge` - the maximum index age
    * `maxSize` - the maximum index size
    * `maxDocs` - the maximum index documents
* `forceMerge` - the force merge policy section
    * `maxNumSegments` - the maximum number of segments
* `readOnly` - if the tier will be read only
* `delete` - if the tier will be deleted

## Data in Elasticsearch

The connector will use Elastic data stream to store events. Documents in the data stream are immutable, which is a good choice for storing events.

Based on the configuration, the connector will create the following elements in Elasticsearch:
* Index template
* Data stream
* Index rollover policy

You can optimise the rollover policy to keep the index size optimal, as well as move older events to a cheaper storage tier.

Events are replicated to Elasticsearch in the following format:

* `messageId` - the unique identifier of the event 
* `messageType` - the type of the event, represented in a complex structure (see below)
* `streamPosition` - event position in the original stream
* `stream` - original stream name
* `globalPosition` - position of the event in the global stream (`$all`)
* `message` - the event payload
* `metadata` - flattened event metadata
* `@timestamp` - the timestamp of the event

The `messageType` field is a JSON object that contains the following fields:
* `version` - the version of the event type
* `type` - the name of the event type
* `originalType` - the original name of the event type
