0. (Gemini 3.1 Pro (extended)) default prompt
# Szerepköröd és Célod
A neved "Tech Lead Reviewer Agent". Egy tapasztalt, kritikus szemléletű Principal Software Architectként és Senior Tech Leadként működsz közre. 
A feladatod, hogy átnézd a kódrészleteket, architektúrális tervjavaslatokat és AI által generált kimeneteket, és megtaláld bennük a rejtett hibákat, skálázódási szűk keresztmetszeteket, elhibázott tervezési mintákat vagy túlbonyolításokat.

Nem a vak elfogadás a célod, hanem a szigorú mérnöki minőségbiztosítás (Quality Gate) és a kritikus gondolkodás felvonultatása.

# Fő fókuszterületek a felülvizsgálat során
1. **Szoftverarchitektúra & Bővíthetőség:**
   - Megfelel-e a kód a Clean Architecture, SOLID elveknek és a Strategy Pattern-nek?
   - Könnyen hozzáadható-e egy új értesítési csatorna (pl. SMS, Webhook) a meglévő logika módosítása nélkül?
2. **Kódminőség & Minták (.NET & Angular):**
   - **.NET:** Használ-e szinkron I/O-t? Hiányzik-e a CancellationToken, a Polly (retry policy), a Dependency Injection vagy a BackgroundService / Queue használata?
   - **Angular:** Van-e benne feliratkozás-szivárgás (memory leak), megfelelő-e a State Management (RxJS/Signals), el vannak-e különítve a Smart és Presentational komponensek?
3. **AI-Hibák és Naiv Megoldások Kiszűrése:**
   - Az adott megoldás szinkron-e ott, ahol aszinkronnak kéne lennie?
   - Kezeli-e a hálózati hibákat, timeoutokat, rate limitet?
   - Biztonságos-e (pl. nem hardkódolt secrets, API kulcsok)?


# Működési szabályzat
- Ne légy elnéző! A cél a hibák felderítése.
- Mindig válaszolj magyarul a kommunikáció során, KIVÉVE a 4. pontban lévő "Decision Log" blokkot, azt **angolul** fogalmazd meg, mert a beadandó repó angol nyelvű lesz.

1. bemásolom az általam tevezett architektúrát. Ha van észrevételed, javítsd ki és indokold meg!

-- itt bemásoltam a `01-plan.md` fájlban lévő saját részemet --

válasz:
# Architecture Specification: World Event Alert System (PoC)

## 1. Introduction
This document outlines the architecture for the "World Event Alert System", built in response to a generalized product brief. Given the extremely broad nature of the brief and the suggested 24-hour timeframe, this architecture explicitly focuses on **extensibility, clear boundaries, and resilience** over deep feature implementation. 

The system acts as a rule-matching engine: it ingests world events (JSON payloads), evaluates them against user-defined alert rules, and dispatches notifications via extensible channels (Email, Slack).

## 2. High-Level Architectural Style
We have selected a **Modular Monolith** architecture built on **.NET 10**.
* **Why not Microservices?** Deploying independent microservices for ingestion, processing, and notifications within a 24-hour constrained PoC introduces unnecessary infrastructural overhead (YAGNI). 
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
* **Responsibility:** A `.NET BackgroundService` continuously consumes the `IEventBus`. It matches incoming events against active user rules.
* **Concurrency:** Rule evaluation is processed asynchronously to maximize throughput.

### 3.4. Notification Subsystem (Strategy Pattern & Resilience)
* **Design:** Implements the **Strategy Pattern** (`INotificationProvider`) to dynamically select the correct delivery mechanism (Slack, Email). Adding future channels (SMS, Webhooks)[cite: 1] requires zero modification to the core engine (Open/Closed Principle).
* **Resilience:** External API calls are notorious for network instability and rate limiting. We mandate the use of **Polly (Resilience Pipelines)** wrapping all provider executions to enforce Retry policies, Timeouts, and Circuit Breakers.

### 3.5. Persistence Strategy (Thread-Safe In-Memory)
* **Decision:** Due to the time constraints[cite: 1] and the objective being a process-demonstration rather than a production-ready product[cite: 1], physical databases (SQL/EF Core) are bypassed.
* **Implementation:** State (Alert Rules, Event Logs) is maintained strictly in-memory using `ConcurrentDictionary` to guarantee thread safety between the Admin API requests and the Background Worker.
* **Abstraction:** All data access is hidden behind Repository interfaces (`IAlertRuleRepository`), fulfilling Dependency Inversion requirements.

### 3.6. Admin UI (Angular Standalone)
* **Architecture:** An Angular Single Page Application (SPA) utilizing modern Standalone Components.
* **State Management:** Utilizes Angular `Signals` for reactive state, explicitly avoiding legacy RxJS `subscribe()` memory leaks.
* **Features:** Provides a minimal dashboard to manage Alert Rules (CRUD) and view a live feed of ingested events and notification statuses[cite: 1].

## 4. Known Technical Debt & Future Roadmap
1. **Volatility:** In-memory persistence will result in data loss upon pod crash or application restart. Immediate roadmap item: implement `EFCoreAlertRuleRepository` with PostgreSQL.
2. **Queue Durability:** The in-memory channel lacks DLQ (Dead Letter Queue) capabilities. Roadmap item: transition to RabbitMQ.
3. **Security:** The Ingestion endpoint lacks authentication. Roadmap item: integrate OAuth2/JWT validation.