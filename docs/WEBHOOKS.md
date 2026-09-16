# Webhooks

**Not applicable.** Symphonia Legato is a local desktop application (plus an
Android companion app) with no server component, no HTTP endpoints, and
therefore no webhooks to configure, send, or receive.

The closest thing to an outbound network integration is the optional Claude
API call made from the AI Assistant panel (see `docs/INTEGRATIONS.md` and
`docs/AUTHENTICATION.md`) — that's a direct API request/response, not a
webhook.

This file exists to satisfy the project's standard documentation suite; it's
expected to stay a stub unless the architecture changes to include a server
or listener component.
