# Architecture Specification: World Event Alert System (PoC)

## 1. Introduction
This document outlines the architecture for the "World Event Alert System", built in response to a generalized product brief. The goal is to notify users when important events occur (e.g., breaking news, market movements, natural disasters). Given the extremely broad nature of the brief and the suggested 24-hour timeframe, this architecture explicitly focuses on **extensibility, clear boundaries, and resilience** over deep feature implementation. 

The system acts as a rule-matching engine: it ingests world events (JSON payloads), evaluates them against user-defined alert rules, and dispatches notifications via extensible channels (currently Email and Slack, with planned support for others).

## 2. High-Level Architectural Style
We have selected a **Modular Monolith** architecture built on **.NET 10**.
* **Why not Microservices?** Deploying independent microservices for ingestion, processing, and notifications within a constrained PoC introduces unnecessary infrastructural overhead (YAGNI). 
* **Why Modular?** By strictly segregating bounded contexts within the monolith (Ingestion, Rule Engine, Notification Dispatcher), we maintain the ability to carve out microservices later if scaling demands it.

## 3. Core Components

### 3.1. Ingestion API (Web API)
* **Responsibility:** Receives incoming world events via a REST API endpoint.
* **Behavior:** To avoid blocking the caller during high-throughput event bursts (e.g., a major natural disaster), the API returns a `202 Accepted` immediately after publishing the payload to an internal queue.

### 3.2. Asynchronous Event Bus (System.Threading.Channels)
* **Responsibility:** Decouples event reception from rule processing.
* **Design:** We use `System.Threading.Channels` as an in-memory Publisher/Subscriber model.
* **Extensibility:** The channel is abstracted behind an `IEventBus` interface, allowing a seamless swap to an external message broker (like RabbitMQ or Azure Service Bus) in a production environment.

### 3.3. Background Worker & Rule Engine
* **Responsibility:** A `.NET BackgroundService` continuously consumes the `IEventBus`. It matches incoming events against active user rules and orchestrates notification dispatch.
* **Concurrency & Lifecycle:** Rule evaluation is processed asynchronously. The worker strictly propagates `CancellationToken`s to ensure graceful shutdown during application teardown, preventing zombie threads or incomplete HTTP requests.

### 3.4. Notification Subsystem (Strategy Pattern & Resilience)
* **Design:** Implements the **Strategy Pattern** (`INotificationProvider`) to dynamically select the correct delivery mechanism (Slack, Email). Adding future channels requires zero modification to the core engine (Open/Closed Principle).
* **Resilience:** External API calls are notorious for network instability and rate limiting. We mandate the use of **Polly (Resilience Pipelines)** wrapping all provider executions to enforce Retry policies, Timeouts, and Circuit Breakers.

### 3.5. Persistence Strategy (Thread-Safe In-Memory)
* **Decision:** Due to the time constraints and the objective being a process-demonstration rather than a production-ready product (where code is evidence, not the point), physical databases (SQL/EF Core) are bypassed.
* **Implementation:** State is maintained strictly in-memory using thread-safe collections:
  * `ConcurrentDictionary` for **Alert Rules** (guaranteeing thread safety between the Admin API and the Worker).
  * `ConcurrentQueue` for **Notification Logs** (capped size to prevent memory leaks, used to serve delivery stats).
* **Abstraction:** All data access is hidden behind Repository interfaces (`IAlertRuleRepository`, `INotificationLogRepository`), fulfilling Dependency Inversion requirements.

### 3.6. Admin UI (Angular Standalone)
* **Architecture:** An Angular Single Page Application (SPA) utilizing modern Standalone Components to fulfill the admin view requirement.
* **State Management:** Utilizes Angular `Signals` for reactive state, explicitly avoiding legacy RxJS `subscribe()` memory leaks.
* **Features:** Provides a minimal dashboard to manage Alert Rules (CRUD) and view a live feed of ingested events and notification statuses.

## 4. Known Technical Debt & Future Roadmap
1. **Volatility:** In-memory persistence will result in data loss upon pod crash or application restart. Immediate roadmap item: implement EF Core Repositories with PostgreSQL.
2. **Queue Durability:** The in-memory channel lacks DLQ (Dead Letter Queue) capabilities. Roadmap item: transition to RabbitMQ.
3. **Security:** The Ingestion endpoint lacks authentication. Roadmap item: integrate OAuth2/JWT validation.