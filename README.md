# Subscription & Billing API

> A production-ready SaaS Subscription & Billing API engineered with ASP.NET Core 10 and Clean Architecture.

This project demonstrates scalable backend engineering, featuring a complete subscription state machine, secure Stripe webhook integration, background job processing with Hangfire, and precise usage quota enforcement. Designed with a minimalist, data-oriented approach.

## Tech Stack

| Layer / Domain | Technology |
|---|---|
| Framework | .NET 10 / ASP.NET Core Web API |
| Architecture | Clean Architecture |
| Database | PostgreSQL 17 / EF Core 10 |
| Background Jobs | Hangfire 1.8 |
| Payments | Stripe.net (Checkout & Webhooks) |
| Observability | Serilog & Scalar UI |

## Architecture Layout

The solution strictly enforces Clean Architecture dependency rules to isolate core business logic from infrastructure concerns.

* Domain: Core entities, state machines, and pure business rules.
* Application: Use case orchestration, DTOs, and FluentValidation (depends only on Domain).
* Infrastructure: PostgreSQL, Hangfire jobs, and Stripe SDK integrations.
* API: Controllers, Webhook routing, and quota enforcement middleware.

## Core Features

* Advanced Subscription State Machine: Handles Active -> GracePeriod -> Suspended -> Expired lifecycles directly within the Domain layer.
* Stripe Integration: Generates Checkout sessions and securely processes webhooks (checkout.session.completed, invoice.payment_failed) using signature verification.
* Asynchronous Processing: Utilizes Hangfire for critical background tasks like daily renewal checks, grace period expiries, and monthly usage resets.
* Metered Usage Enforcement: Implements a high-performance QuotaEnforcementFilter backed by composite database indexes to reject API requests when a user exceeds their tier limits.

## Local Setup (Docker)

1. Clone the repository.
2. Ensure Docker Desktop is running.
3. Update the appsettings.Development.json with your Stripe Test API Keys and Webhook Secret.
4. Run the infrastructure via the provided setup or standard `dotnet run` (migrations apply automatically on startup).

---

Ahmed Osama - Backend .NET Developer | Data-Oriented Developer
GitHub: [https://github.com/ahmedosamaexe](https://github.com/ahmedosamaexe)
