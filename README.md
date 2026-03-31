# PulseOps

PulseOps is a backend-first small business operations platform built with .NET, PostgreSQL, EF Core, Redis, and Docker.

The system models real-world business workflows and reliability patterns found in modern production systems, including order processing, inventory lifecycle management, billing, and event-driven integrations.

---

## Architecture Diagram

![PulseOps Architecture](./pulseops-architecture-diagram-v2.svg)

---

## Tech Stack

- .NET / ASP.NET Core Web API
- Entity Framework Core
- PostgreSQL
- Redis
- Docker Compose
- Swagger / OpenAPI
- xUnit

---

## Features

### Core Domain
- Product creation and management
- Inventory tracking and adjustments
- Customer management
- Order creation with validation
- Inventory reservation during order placement
- Order fulfillment with inventory reconciliation

### Billing
- Automatic invoice generation on order creation
- Invoice retrieval and status tracking
- Invoice payment updates

### Event System
- Domain event persistence for core business actions
- Event payload storage for observability
- Event inspection via API

### Webhooks
- Webhook endpoint registration per business
- Outbound event delivery to external systems
- Delivery tracking with status, attempts, and responses

### Reliability
- Retry scheduling with backoff
- Dead-letter handling after max attempts
- Manual retry and batch retry processing endpoints

### API Safety
- Idempotency key support for selected write endpoints
- Duplicate prevention for retried customer and order creation requests
- Reuse of original response for repeated requests

### Security
- HMAC SHA-256 webhook signing
- Per-endpoint signing secrets
- Signature header included on outbound webhook deliveries

---

## Project Structure

```text
src/
  PulseOps.Api
  PulseOps.Application
  PulseOps.Domain
  PulseOps.Infrastructure

tests/
  PulseOps.UnitTests