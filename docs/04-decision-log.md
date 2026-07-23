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