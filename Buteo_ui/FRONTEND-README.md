# Buteo Angular frontend preview

This frontend is a presentation-ready Angular mock-up for the Buteo Edge Collector & SOAR project.

## Run locally

Requirements: Node.js compatible with the Angular CLI version in `package.json` and npm.

```bash
npm install
npm start
```

Open the local URL printed by Angular CLI (normally http://localhost:4200).

## What's included

- Dashboard overview with sample KPI cards, event activity chart, incident severity, recent incidents, host health and detection-rule summary.
- Hosts inventory with search.
- Security event stream with search.
- Incident queue with severity filtering and locally editable status.
- Detection rules with locally toggleable enabled/disabled state.
- Responsive sidebar/navigation and mobile layout.

## Data and backend status

All records shown in this preview are fictional, in-memory demonstration data. The frontend does not currently call the C# API or write changes to SQL Server. This is deliberate so the UI can be developed and presented independently of backend readiness.

The uploaded API source currently exposes:

- `GET /api/hosts`
- `GET /api/events`
- `GET /api/events/{id}`
- `POST /api/events`

The backend's local HTTP launch profile uses `http://localhost:5277`. Before connecting Angular to it, configure CORS in ASP.NET Core and add Angular's `HttpClient` provider. Then replace the local sample-data source for hosts/events with HTTP requests. Incidents and detection rules still need corresponding backend endpoints before those screens can be persisted.

## Main files changed

- `src/app/app.ts` — sample models, demo records, navigation, filtering and local interactions.
- `src/app/app.html` — all five screens and shared application shell.
- `src/styles.css` — responsive visual system and component styling.
- `src/index.html` — Buteo page title and metadata.
