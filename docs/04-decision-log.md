# Architecture Decision Records (ADR)

This document records the key architectural and design decisions made during the development of the World Event Alert System PoC.

---

## ADR-001: Use In-Memory Channels over External Message Broker

* **Status:** `ACCEPTED`
* **Context:** The system requires an asynchronous event bus to decouple high-throughput event ingestion from rule evaluation and notification dispatching.
* **Decision:** We will use .NET `System.Threading.Channels` wrapped in an `IEventBus` interface instead of setting up external infrastructure like RabbitMQ or Apache Kafka.
* **Consequences:** 
  * **Pros:** Zero external infrastructure dependencies, near-zero latency, extremely lightweight, fully asynchronous non-blocking pipeline.
  * **Cons:** In-memory queue lacks durability (events in the channel are lost if the application process crashes or restarts).
  * **Mitigation:** Acceptable trade-off for a PoC. The `IEventBus` abstraction allows seamless swapping to RabbitMQ or Azure Service Bus in production without modifying core business logic.

---

## ADR-002: Modular Monolith over Microservices Architecture

* **Status:** `ACCEPTED`
* **Context:** The product brief is highly ambiguous, and the implementation timeframe is strictly constrained. We need clear boundaries between domains (Ingestion, Processing, Delivery) without introducing unnecessary operational complexity.
* **Decision:** Adopt a **Modular Monolith** architectural style in .NET. All bounded contexts reside within a single solution but maintain strict internal boundaries via interface abstractions and dependency injection.
* **Consequences:** 
  * **Pros:** Rapid setup and deployment, simple local debugging, zero network latency between internal modules.
  * **Cons:** Modules share CPU/Memory resources under high load.
  * **Mitigation:** Strict domain segregation allows individual modules (e.g., Notification Dispatcher) to be carved out into independent microservices in the future if scaling requirements demand it (YAGNI principle).

---

## ADR-003: Strategy Pattern for Extensible Notification Channels

* **Status:** `ACCEPTED`
* **Context:** The brief specifies support for Email and Slack while explicitly requiring: *"Make it flexible enough that we can add more channels later."*
* **Decision:** Implement the **Strategy Pattern** via an `INotificationProvider` interface. The `NotificationDispatcher` resolves all registered `IEnumerable<INotificationProvider>` implementations dynamically at runtime via .NET Dependency Injection.
* **Consequences:** 
  * **Pros:** Fully compliant with the Open/Closed Principle (OCP). Adding a new channel (e.g., SMS, Webhooks, Push Notifications) requires creating a new class implementing `INotificationProvider` with zero changes to the core Rule Engine.
  * **Cons:** Slight runtime overhead for strategy lookup (negligible in .NET DI container).

---

## ADR-004: Polly Resilience Pipelines for External Channel Executions

* **Status:** `ACCEPTED`
* **Context:** External notification channels (Slack Webhooks, SMTP servers) are inherently unreliable due to network instability, rate limits, and transient downtime.
* **Decision:** Wrap all `INotificationProvider.SendAsync()` invocations with **Polly Resilience Pipelines** enforcing Exponential Backoff Retry policies and strict execution Timeouts.
* **Consequences:** 
  * **Pros:** Prevents transient network failures from dropping notifications; insulates the core Background Worker from hanging on slow third-party API responses.
  * **Cons:** Increased complexity in error logging and handling max-retry failures.

---

## ADR-005: Thread-Safe In-Memory Persistence Strategy

* **Status:** `ACCEPTED`
* **Context:** Setting up physical database persistence (e.g., EF Core + PostgreSQL) within a short PoC timeframe consumes valuable time better spent on architecture, resilience, and process documentation.
* **Decision:** Maintain state (Alert Rules, Notification Logs) in-memory using `ConcurrentDictionary<Guid, T>` behind generic Repository interfaces (`IAlertRuleRepository`, `INotificationLogRepository`).
* **Consequences:** 
  * **Pros:** Maximum development velocity, zero database setup overhead, guaranteed thread safety between Web API REST requests and the async Background Worker.
  * **Cons:** All state is volatile and cleared upon application restart.
  * **Mitigation:** State interfaces decouple the application from the underlying storage. A production implementation can replace the in-memory implementations with Entity Framework Core without touching business logic.

---

## ADR-006: Angular Standalone Components & Signals for Admin UI

* **Status:** `ACCEPTED`
* **Context:** An Admin UI is required to configure rules and monitor event deliveries. We need a modern, scalable frontend architecture without heavy boilerplate.
* **Decision:** Build the UI using **Angular Standalone Components** and manage reactive UI state using **Angular Signals** (explicitly avoiding manual RxJS `subscribe()` calls where possible).
* **Consequences:** 
  * **Pros:** Cleaner component architecture, automatic change detection optimization, zero risk of subscription-based memory leaks.
  * **Cons:** Requires modern Angular (v17+) features.

---

## ADR-007: Synthetic Event Generation via Bogus Library

* **Status:** `ACCEPTED`
* **Context:** No live event data source is provided in the brief, yet the Rule Engine and Notification Dispatcher require realistic domain events for validation and demonstration.
* **Decision:** Integrate the **Bogus** library to create a configurable `EventGenerator` service capable of emitting realistic fake events (Breaking News, Market Movements, Natural Disasters) on demand or via a timer loop.
* **Consequences:** 
  * **Pros:** Enables realistic end-to-end testing of complex rule matching and UI live feeds without hardcoded JSON strings.
  * **Cons:** Synthetic data generator code must be maintained alongside core logic (isolated under `/Simulations`).

---

## ADR-008: Thread-Safe Notification Logging & Graceful Worker Lifecycle Management

* **Status:** `ACCEPTED`
* **Context:** The Admin UI requires real-time delivery logs and success/failure statistics. Additionally, the async Background Worker must manage application lifecycle events cleanly without hanging threads or missing cancellation signals during application shutdown.
* **Decision:** Implement an `INotificationLogRepository` backed by a size-bounded, thread-safe `ConcurrentQueue<NotificationLog>` to store audit history in-memory. Enforce mandatory `CancellationToken` propagation across the entire worker pipeline, including channel subscribers and provider dispatchers.
* **Consequences:** 
  * **Pros:** Guarantees auditability and delivery status tracking for the Admin UI; ensures safe concurrent access between API queries and worker writes; enables graceful application teardown without orphaned background processes.
  * **Cons:** Log history is volatile and bounded (capped at max entry limit) to prevent memory leaks during long-running PoC sessions.
  * **Mitigation:** The repository interface abstraction (`INotificationLogRepository`) decouples storage from business logic, allowing effortless migration to a persistent database table (e.g., PostgreSQL via EF Core) in production.

---

## ADR-009: Concrete In-Memory Repository Implementation with Bounded Collections & Concurrency Audit

* **Status:** `ACCEPTED`
* **Context:** The system requires concrete in-memory repository implementations for `AlertRule` management and `NotificationLog` tracking that can safely handle concurrent reads and writes between Web API request threads and the asynchronous Background Worker loop.
* **Decision:** 
  1. Implemented `InMemoryAlertRuleRepository` using `ConcurrentDictionary<Guid, AlertRule>` for $O(1)$ thread-safe CRUD operations.
  2. Implemented `InMemoryNotificationLogRepository` using a bounded `ConcurrentQueue<NotificationLog>` capped at 500 entries. Enqueued logs trigger automatic, non-blocking `TryDequeue` trimming when exceeding the threshold to guarantee bounded memory usage.
  3. Conducted a dedicated code-level concurrency audit across both repository implementations to verify thread-handling behaviors, lock-free guarantees, and race-condition resistance.
* **Consequences:** 
  * **Pros:** Guaranteed lock-free thread safety under high concurrency; strict memory bounds preventing leaks during continuous event generation; verified compilation and clean domain integration (`dotnet build` successful).
  * **Cons:** Audit logs beyond the 500-entry threshold are discarded (acceptable trade-off for a PoC memory model).
  * **Concurrency Audit Findings:** Explicitly audited all concurrent access paths, lock mechanisms, and collection operations. **No thread-handling or synchronization defects were found**, confirming that `ConcurrentDictionary` and `ConcurrentQueue` appropriately isolate thread access without deadlocks or race conditions.

---

## ADR-010: Coarse-Grained Cooperative Cancellation in Background Worker Loops

* **Status:** `ACCEPTED`
* **Context:** Code review of the `EventProcessingWorker` loop identified that while the `CancellationToken` is propagated to asynchronous calls (`SubscribeAsync`, `DispatchAsync`), it is not explicitly checked via `cancellationToken.ThrowIfCancellationRequested()` inside synchronous internal loops (e.g., iterating through active `AlertRule` collections and target channels).
* **Decision:** Accept coarse-grained cooperative cancellation relying on async await boundaries for application shutdown, omitting intra-loop explicit token checks for the PoC.
* **Consequences:** 
  * **Pros:** Simpler, cleaner worker execution logic without boilerplate cancellation checks inside fast synchronous iterations.
  * **Cons:** Application shutdown (teardown) will only trigger at the next async await boundary, introducing minor shutdown latency if a single event matches a very large volume of alert rules.
  * **Mitigation:** Completely acceptable trade-off for the PoC demo scale (small rule set). For production environments with thousands of matched rules per event, `cancellationToken.ThrowIfCancellationRequested()` or `Parallel.ForEachAsync` with token boundings should be implemented to ensure instant process termination.

---


