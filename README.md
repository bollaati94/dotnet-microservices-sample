# Gate Event Simulation System - Detailed Documentation

This project is a minimal, scalable microservices demonstration implementing a real-time data pipeline: **Event Generation $\rightarrow$ Message Queue $\rightarrow$ Document Store $\rightarrow$ Visualization**.

## 🏗 Architecture Overview

The system consists of three primary components:
1. **Publisher Service**: A .NET 10 Web API that simulates gate entry/exit events.
2. **Consumer Service**: A .NET 10 Background Worker that processes events and stores them.
3. **Infrastructure**: RabbitMQ (Broker), Elasticsearch (Search Engine), and Kibana (Visualization).

### Data Flow
`Publisher` -> `RabbitMQ (gate_events_queue)` -> `Consumer` -> `Elasticsearch` -> `Kibana`

---

## 🛠 Component Details

### 1. Common Library (`/Common`)
Contains shared data contracts to ensure type safety across services.
- **`GateEvent`**: A record containing `EventId`, `GateId`, `PersonId`, `PersonType` (Employee/Guest), `EntryType` (Arrive/Exit), and `Timestamp`.

### 2. Publisher Service (`/PublisherService`)
A scalable producer that generates synthetic traffic.
- **Simulation Engine**: Uses `GateSimulationJob` to create random gate events.
- **Hangfire Integration**: 
    - Uses **Hangfire.MemoryStorage** for job orchestration.
    - **Recurring Job**: Configured via `RecurringJob.AddOrUpdate` to run every minute (or as specified in `appsettings.json`).
    - **Dashboard**: Accessible at `/hangfire` to monitor, trigger, or stop simulations.
- **RabbitMQ Producer**: Implements a `Persistent` message strategy, ensuring events survive broker restarts.

### 3. Consumer Service (`/ConsumerService`)
A high-throughput worker designed for horizontal scaling.
- **Async Processing**: Utilizes `AsyncEventingBasicConsumer` from RabbitMQ Client 7.x for non-blocking I/O.
- **Fair Dispatch**: Configured with `BasicQos(0, 10, false)`, meaning it only pulls 10 messages at a time. This allows you to scale the service (e.g., `docker-compose up --scale consumer=3`) to distribute load evenly across instances.
- **Elasticsearch Integration**: Uses the `NEST` client to index documents into the `gate-events` index.
- **Reliability (Ack/Nack)**: 
    - **Manual Ack**: Messages are only acknowledged (`BasicAckAsync`) *after* successful indexing in Elasticsearch.
    - **Poison Pill Protection**: If a message fails to be indexed, it is `Nack`ed with `requeue: false` to prevent infinite retry loops that would crash the system.

---

## 🚀 Deployment & Usage

### Running the Stack
```bash
cd sample
docker-compose up --build -d
```

### Access Points
| Service | URL | Purpose |
| :--- | :--- | :--- |
| **Publisher Dashboard** | `http://localhost:5001/hangfire` | Monitor and trigger simulations |
| **Consumer Status** | `http://localhost:5002/` | Health check for worker |
| **Kibana** | `http://localhost:5601` | Visualize and query gate events |
| **RabbitMQ Mgmt** | `http://localhost:15672` | Monitor queue depths and rates (guest/guest) |
| **Elasticsearch** | `http://localhost:9200` | Raw API access to the search engine |

### Visualizing Data in Kibana
1. Open **Kibana** $\rightarrow$ **Stack Management** $\rightarrow$ **Index Patterns**.
2. Create an index pattern for `gate-events*`.
3. Go to **Discover** to see the real-time stream of employees and guests arriving/exiting the gates.

---

## 📈 Scalability & Design Decisions

- **Async-First**: Both services use the latest `.NET 10` and `RabbitMQ.Client 7.x` async APIs to maximize throughput.
- **Statelessness**: Both microservices are entirely stateless, allowing them to be scaled horizontally in a Kubernetes or Docker Swarm environment without coordination.
- **Loose Coupling**: The Publisher does not know about Elasticsearch; the Consumer does not know about Hangfire. They communicate solely via the `gate_events_queue` contract.
- **Resource Efficiency**: Used `MemoryStorage` for Hangfire in the sample to remove the need for a SQL Server/Redis dependency, keeping the footprint minimal.
