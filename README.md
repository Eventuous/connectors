# Eventuous Connector

An event-sourced system that stores domain events in EventStoreDB could benefit a lot from replicating these events to a search engine, document, or relational database. It's normally done using [real-time subscriptions][1] in combination with [producers][2] or [projections][3]. 

Eventuous Connector provides a drop-in solution for some of those needs, based only on a configuration (for producers) or a relatively easy way to build projections using a gRPC sidecar (for projectors).

Currently implemented source is [EventStoreDB][4].

Available sinks are:

| Sink | Modes |
|:-----|:------|
| Elasticsearch | producer, projector |
| MongoDB | projector |
| MS SQL Server or Azure SQL | projector |

[Documentation][5] is WIP.

[1]: https://eventuous.dev/docs/subscriptions/subs-concept/
[2]: https://eventuous.dev/docs/producers/
[3]: https://eventuous.dev/docs/read-models/rm-concept/
[4]: https://eeventstore.com
[5]: https://eventuous.dev/connector/
