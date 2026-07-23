# Saját tervem:

Target Architecture: .NET 10 Web APi + Worker Service & Angular (Standalone Components)

1. Problem Scoping (A brief értelmezése)
A brief rendkívül homályos, ezért a fejlesztés megkezdése előtt az alábbi dolgokat szabom meg:

- Események (Events): Az események (breaking news, market movements, natural disasters) JSON payloadokként érkeznek egy Ingestion API endpointra.

- Rendszer célja: Szabályok (Alert Rules) alapján illeszteni az beérkező eseményeket, és ha a szabály feltételei teljesülnek, értesítést küldeni a megadott csatornákon (Slack, Email).

- Bővíthetőség: A csatornáknak plug-and-play módon bővíthetőnek kell lenniük (Strategy Pattern) a későbbi csatornák (SMS, Webhook, Push) miatt.

- Admin UI: Egy Angular alapú felület a beérkező események (Event Feed), a szabályok (Alert Rules CRUD) és a kiküldési statisztikák/logok tekintéséhez.

2. System Architecture & High-Level Design
A rendszert Moduláris Monolitként tervezem meg .NET 10-ben, hogy elkerüljem a túlbonyolított microservice infrastruktúrát, de megőrizzem a tiszta határokat:

- Ingestion API: REST endpoint a beérkező események fogadására.

- In-Memory Event Queue: A beérkező eseményeket egy aszinkron sorba dobjuk, hogy az API azonnal (202 Accepted) válaszolhasson, ne blokkolja a hívót.

- Notification Background Worker: A háttérszál folyamatosan olvassa a sort, kiértékeli az aktív szabályokat (Rule Matching Engine), majd meghívja a megfelelő INotificationProvider implementációkat.

- Notification Providers (Strategy Pattern):
    - EmailNotificationProvider (SMTP / SendGrid mock)
    - SlackNotificationProvider (Slack Incoming Webhook)

- Admin REST API + Angular SPA: Szabályok kezelése, teszt események kiváltása, kézbesítési statisztikák megjelenítése.

3. Execution Sequence
Mivel a rendelkezésre álló időkeret szűkös, a fókuszt a robusztus architektúrára, a bővíthetőségre és a döntési folyamat dokumentálására helyezem, nem pedig a funkciók túlépítésére (Pragmatic Scope). Az AI agent segítségével a kódgenerálást felgyorsítom, így a saját időmet a felülvizsgálatra és a dokumentálásra fordítom.

Phase 1: Architecture & Scoping Setup
Deliverables: 
- Architecture ADR-ek, JSON Data Schema definíciók
- INotificationProvider interface.

Focus: Döntési határok meghúzása (in-memory szimuláció a szűkös idő miatt).

Phase 2: Core Backend Implementation
Deliverables:
- Event Ingestion Controller (202 Accepted).
- in-memory queue.
- Worker Service + Strategy Pattern (Email Mock Provider + Slack Webhook Provider).
- Bogus alapú Mock Event Generator (tesztadatok szimulálására).

AI Critique focus: Ne engedjük az AI-nak a szinkron hívásokat és a hardkódolt konfigurációkat.

Phase 3: Minimal Admin Dashboard (Angular Standalone)
Deliverables:
- Rule Management View: Szabályok listázása és létrehozása (egyszerű form).
- Live Event Feed / Logs View: Beérkező események és kézbesítési státuszok megjelenítése.

Focus: Lényegi UX/UI az adminisztrációhoz, egyszerű css


### AI (Custom Gem agent: Gemini 3.1 Pro (extended)) meglátásai a tervemre:
1. A scope szűkítése és a "moduláris monolit" választása stratégiailag kiváló, és pontosan azt a mérnöki gondolkodást mutatja, amit a feladat megkövetel. Azonban a tisztán In-Memory architektúra és a perzisztencia (adatbázis) definiálásának hiánya kritikus adatvesztési és skálázódási kockázatot jelent egy enterprise rendszerben, még PoC (Proof of Concept) szinten is.

Reakcióm: mivel ez egy proof of concept munka, így nem számít az adatvesztés, csak egy gyors demo erejéig kellenek az adatok, amik futási időben generálódnak

2. Resilience (Hibatűrés) hiánya: A terved említi a Slack és Email providereket, de nem említi a hálózati hibák kezelését. Mi történik, ha a Slack API rate-limitál (429 Too Many Requests), vagy az SMTP szerver timeoutol? Ha nincs Polly (Resilience Pipeline) beépítve a providerekbe, a Worker Service eldobja a riasztást, vagy rosszabb esetben megakad.

Reakcióm: a külső hívásokat valóban érdemes becsomagolni egy hibatűrő pipeline-ba




# AI által készített terv:

# Architecture Specification: World Event Alert System (PoC)

## 1. Introduction
This document outlines the architecture for the "World Event Alert System", built in response to a generalized product brief. Given the extremely broad nature of the brief and the suggested timeframe, this architecture explicitly focuses on **extensibility, clear boundaries, and resilience** over deep feature implementation. 

The system acts as a rule-matching engine: it ingests world events (JSON payloads), evaluates them against user-defined alert rules, and dispatches notifications via extensible channels (Email, Slack).

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