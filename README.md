# PulseOps

PulseOps is a backend-first small business operations platform built with .NET, PostgreSQL, EF Core, Redis, and Docker.

The system models real-world business workflows including:

* product management
* inventory tracking and adjustments
* customer management
* order creation and validation
* inventory reservation and fulfillment

This project is designed to demonstrate production-style backend architecture, domain modeling, and business logic.

---

## Tech Stack

* .NET / ASP.NET Core Web API
* Entity Framework Core
* PostgreSQL
* Redis
* Docker Compose
* Swagger / OpenAPI
* xUnit

---

## Features

* Health check endpoint
* Product creation and lookup
* Inventory tracking and adjustment
* Customer creation
* Order creation with validation
* Inventory reservation during order placement
* Order completion with inventory fulfillment

---

## Project Structure

```
src/
  PulseOps.Api
  PulseOps.Application
  PulseOps.Domain
  PulseOps.Infrastructure

tests/
  PulseOps.UnitTests
```

---

## Running Locally

### 1. Prerequisites

Make sure you have installed:

* .NET SDK (v8+ or v10)
* Docker Desktop (running)

---

### 2. Start infrastructure (PostgreSQL + Redis)

From the project root:

```bash
docker compose up -d
```

Verify containers are running:

```bash
docker ps
```

---

### 3. Apply database migrations

Run:

```bash
dotnet ef database update \
  --project src/PulseOps.Infrastructure \
  --startup-project src/PulseOps.Api
```

This will create the database schema in PostgreSQL.

---

### 4. Run the API

```bash
dotnet run --project src/PulseOps.Api
```

---

### 5. Open Swagger UI

Once the app is running, open:

```
https://localhost:xxxx/swagger
```

(Port will be shown in the terminal)

---

### 6. Seed data

On startup, the app automatically creates a demo business:

```
Business ID: 11111111-1111-1111-1111-111111111111
```

Use this ID when creating products, customers, and orders.

---

## Example API Flow

### Create a Product

```
POST /api/products
```

```json
{
  "businessId": "11111111-1111-1111-1111-111111111111",
  "name": "Matcha Candle",
  "sku": "MATCHA-CANDLE-001",
  "price": 24.99,
  "initialQuantity": 50,
  "reorderThreshold": 10
}
```

---

### Create a Customer

```
POST /api/customers
```

```json
{
  "businessId": "11111111-1111-1111-1111-111111111111",
  "fullName": "Nicole Creech",
  "email": "nicole@example.com",
  "phoneNumber": "555-0101"
}
```

---

### Create an Order

```
POST /api/orders
```

```json
{
  "businessId": "11111111-1111-1111-1111-111111111111",
  "customerId": "PASTE-CUSTOMER-ID",
  "items": [
    {
      "productId": "PASTE-PRODUCT-ID",
      "quantity": 3
    }
  ]
}
```

---

### Complete an Order (Fulfillment)

```
PATCH /api/orders/{orderId}/complete
```

This will:

* reduce available inventory
* clear reserved inventory
* mark the order as completed

---

## Roadmap

* invoice generation
* domain events
* webhook delivery system
* retry + dead-letter handling
* idempotency keys
* Redis caching
* Hangfire background jobs
* audit logging

---

## Status

Actively in development as a portfolio project focused on backend system design and production-style workflows.
