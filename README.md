# Serilog + OTEL + Destructurama.Attributed

## What

Demo project that shows how to:

- Use `OpenTelemetry` with multiple instrumentation sources
- Write logs to an `OTEL collector` with Serilog
- Filter out PII with `Destructurama.Attributed`

## Requirements

- .NET 8
- Docker

## How to run

- `docker-compose up`
    - This starts a `.NET Aspire dashboard` which includes an `OTEL collector`
- `dotnet run`
    - This starts the application

## How to use

- Using `SwaggerUI` call both endpoints, and look at how the `OTEL` information arrives in the `Aspire dashboard`. 