# Event Hub Stress

Sending random events to Event Hub to trigger the intermediate hiccups / delays when sending messages.

## Configuration

### Default Configuration

#### Event Hub

- Number of Partitions: 4
- TU: 4
- Zone Redundancy: enabled

#### Stress utility

- Number of Messages / Interation / Partition: 5
- Send Interval: 200ms
- Message body size: 500 bytes

This will send 20 messages (5 per partition) on each iteration. ~20 messages / s, if we add the send delay. We are using a partition sender to explicitly send to the individual partitions. There is another implementation that is using batching (single items), which does also repro the problem and could be configured as an alternative.

### Infrastructure

To get the repro, run this utility against the infrastructure that is built using the terraform scripts in this repo (Infra/terraform). The region should be set to "eastus" as we seem to be able to trigger it there the easiest.

Use the terraform to setup the complete environment.

#### Parameters

|Name|Type|Description|Default|
|----|----|-----------|-------|
|region|string|Azure region, where the setup is installed.|eastus|
|imgVersion|string|Container Image Version.|latest|
|rgname|string|Azure resource group name.|EhThroughputTest|
|ehnsname|string|Azure Event Hub Namespace name.|EhThroughputTest|
|zoneredundant|string|true to enable the zone redundancy feature on the Event Hub Namespace otherwise false.|true|
|containers|list(string)|Container names to use to generate the load.|ehstress1,ehstress2|

## Monitoring

Once the solution is deployed, there is an Application Insights instance created in the resource group. Under Metrics there are custom metrics that can be used to analyze the performance of the send operation.

### Metrics

Metric Namespace: **ehstress**

|Name|Description|Dimensions|
|----|-----------|----------|
|Publish Performance|Number of milliseconds the send operation to Event Hub takes. Use the Machine Name custom dimension to split the between the 2 container instances and see the individual performance.|Machine Name, Partition Id, Perf Bucket|
|Number Of Messages|Number of messages sent to the event hub|Machine Name, Partition Id|
|Batch Size|Size of the batch sent to the event hub in bytes|Machine Name, Partition Id|
|Performance Buckets|Publish Performance devided into buckets: <500, 500 - 999, 1000 - 2999, 3000 - 4999|Machine Name, Partition Id, Perf Bucket|

The core metric setup you should use to see the repro is the **Publish Performance -> Max** split by **Machine Name**. That will demonstrate that the delays happen synchronized in the 2 container instances.

>If you don't get the option to split by the custom metric, enable **Enable alerting on custom metric dimensions**. Select **Usage and estimated costs** and then click on **Custom metrics (Preview)**.

## Repro Illustration
![Send delay illustration](./images/chart_send_delay_illustration.jpg)