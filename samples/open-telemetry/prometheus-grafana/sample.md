---
title: Monitoring NServiceBus endpoints with Prometheus and Grafana
summary: How to configure NServiceBus to export OpenTelemetry metrics to Prometheus and Grafana
component: Core
isLearningPath: true
reviewed: 2022-07-15
previewImage: grafana.png
related:
- nservicebus/operations/opentelemetry
---


[Prometheus](https://prometheus.io) is a monitoring solution for storing time series data like metrics. [Grafana](https://grafana.com) visualizes the data stored in Prometheus (and other sources). This sample demonstrates how to capture NServiceBus OpenTelemetry metrics, store them in Prometheus and visualize these metrics using a Grafana dashboard.

![Grafana NServiceBus fetched, processed and errored messages](grafana.png)

## Prerequisites

To run this sample, Prometheus and Grafana are required. This sample uses Docker and a `docker-compose.yml` file to run the stack.

## Code overview

The sample simulates message load with a random 10% failure rate using the `LoadSimulator` class:

snippet: prometheus-load-simulator

## Reporting metric values

NServiceBus uses the OpenTelemetry standard to report metrics. The metrics are disabled by default and must be enabled on the endpoint configuration.

snippet: enable-opentelemetry

Opt into a specific metric, either by name or by wildcard:

snippet: enable-opentelemetry-metrics

There are three metrics reported as a Counter, with the following keys:

* Number of fetched messages via `nservicebus.messaging.fetches`
* Number of failed messages via `nservicebus.messaging.failures`
* Number of successfully processed messages via `nservicebus.messaging.successes`

Each reported metric is tagged with the following additional information:

* the queue name of the endpoint
* the uniquely addressable address for the endpoint (if set)
* the .NET fully-qualified type information for the message being processed
* the exception type name (if applicable)

## Exporting metrics

The metrics are gathered using OpenTelemetry standards on the endpoint and must be reported and collected by an external service. A Prometheus exporter can expose this data via an HTTP endpoint and the Prometheus service, hosted as a docker service, can retrieve and store this information. The exporter is available via a NuGet package `OpenTelemetry.Exporter.Prometheus`. In this sample, the service that exposes the data to scrape is hosted on `http://localhost:9185/metrics`. To enable the Prometheus exporter, run the following code:

snippet: enable-prometheus-exporter

Note: the HTTP endpoint is also exposed through a local IP address so the Prometheus service running in Docker can reach it over the network.

The raw metrics retrieved through the scraping endpoint look as follows:

```text
# HELP nservicebus_messaging_successes Total number of messages processed successfully by the endpoint.
# TYPE nservicebus_messaging_successes counter
nservicebus_messaging_successes{nservicebus_discriminator="main",nservicebus_message_type="SomeCommand, Endpoint, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",nservicebus_queue="OpenTelemetryDemo"} 850 1657693075515

# HELP nservicebus_messaging_fetches Total number of messages fetched from the queue by the endpoint.
# TYPE nservicebus_messaging_fetches counter
nservicebus_messaging_fetches{nservicebus_discriminator="main",nservicebus_message_type="SomeCommand, Endpoint, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",nservicebus_queue="OpenTelemetryDemo"} 1060 1657693075515

# HELP nservicebus_messaging_failures Total number of messages processed unsuccessfully by the endpoint.
# TYPE nservicebus_messaging_failures counter
nservicebus_messaging_failures{nservicebus_discriminator="main",nservicebus_failure_type="System.Exception",nservicebus_message_type="SomeCommand, Endpoint, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",nservicebus_queue="OpenTelemetryDemo"} 210 1657693075515
```

The diagram below shows the overall component interactions:

```mermaid
graph TD
    A[NServiceBus Endpoint] -->|Report Metrics| B(Prometheus Exporter)
    B -->|Expose| C{Metric Endpoint}
    C -->|No Metrics| D[Status 200]
    C -->|Has Metrics| E[Return Metrics]
    F[Promethus Service] --> |Poll Metrics| E
    F --> |Store Metrics| F
    G[Grafana] --> |Query Data| F
```

## Docker stack

The Prometheus service must be configured to retrieve the metrics data from the endpoint. Grafana must also be configured to get the data from Promethus and visualize it as graphs.

To run the Docker stack, run `docker-compse up -d` in the directory where the `docker-compose.yml` file is located.

### Configuring Prometheus

Copy the following files into the mapped volumes of the Prometheus and Grafana.

* `prometheus_ds.yml` should be copied to `./grafana/provisioning/datasources` folder
* `prometheus.yml` should be copied to `./prometheus` folder

Open `prometheus.yml` and update the target IP address. This should be the address of the machine running the sample and the port that the Promethus exporter is configured to run on. The Docker containers should be able to reach this IP and port.

```yml
  - targets:
    - '192.168.0.10:9184'
```

### Show a graph

Start Prometheus by running the Docker stack. NServiceBus pushes events for *success, failure, and fetched*. These events must be converted to rates by a query. For example, the `nservicebus_messaging_successes` metric can be queried as:

```
avg(rate(nservicebus_messaging_successes[5m]))
```

![Prometheus graphs based on query](example-prometheus-graph.png)

## Grafana

Grafana must be installed and configured to display the data scraped and stored in Prometheus. For more information on how to install Grafana, refer to the [Grafana installation guide](https://docs.grafana.org/installation). In this sample, the Grafana service runs as part of the Docker stack mentioned above.

### Dashboard

To graph the metrics, the following steps must be performed:

* Add a new dashboard
* Add a graph
* Click its title to edit
* From the Data source dropdown, select Prometheus
* For the query, open the Metrics dropdown and select one of the metrics. Built-in functions (e.g. rate) can also be applied.

![Grafana dashboard with NServiceBus OpenTelemetry metrics](example-grafana-dashboard.png)

The sample includes an [export of the Grafana dashboard](grafana-endpoints-dashboard.json) which can be [imported](https://docs.grafana.org/reference/export_import/) as a reference.
