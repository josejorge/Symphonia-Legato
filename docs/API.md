# API

This project's public API reference lives in **[`API_REFERENCE.md`](API_REFERENCE.md)**
(pre-existing under that name before the standard docs suite was audited into
place). This file exists so `API.md` — the standard name — resolves to
something rather than 404ing; it isn't a fork or a stale copy.

There is no network/HTTP API in this project — Symphonia Legato is a desktop
application. "API" here means the public C#/.NET surface of the Core
libraries (`SymphoniaLegato.Core`, `NotationEngine`, `LayoutEngine`, etc.) —
the types and methods another .NET project would call if it referenced these
libraries directly.
