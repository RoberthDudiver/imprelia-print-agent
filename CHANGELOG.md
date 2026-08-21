# Changelog

All notable changes to Imprelia Print Agent will be documented here.

## 1.2.0

### Fixed

- **Port change now applies live.** Saving a new port (desktop settings or `PUT /api/settings`) re-binds the HTTP listener immediately — no app restart required. The dashboard/settings now show the live listening port. On failure it rolls back to the previous port so the agent never ends up down.
- **Resilient startup binding.** Each address (`127.0.0.1`, `localhost`, custom host) is now bound on its own listener. If one fails (typically `localhost` "access denied" without a URL ACL on some PCs), the agent still runs on `127.0.0.1` instead of failing to start entirely. An actionable log message explains how to enable `localhost` with `netsh http add urlacl`.

### Changed

- **Offline API guide.** `/docs` is now a fully self-contained page (HTML/CSS/JS, no CDN) rendered from `/openapi.json`. It works without internet, unlike the previous Scalar-from-CDN page.

## 1.0.0 - Initial Public Release

### Added

- Windows tray application.
- Local HTTP API.
- Legacy endpoints: `/ping`, `/printers`, `/print`.
- New API endpoints under `/api`.
- Printer discovery from Windows installed printers.
- Universal print request contract.
- Print by purpose/routes.
- Test print endpoint.
- Recent job history.
- Configurable port, CORS origins, routes, and optional API key.
- Scalar API documentation at `/docs`.
- OpenAPI document at `/openapi.json`.
- WinForms settings panel.
- Non-commercial source license.
- Bilingual README and documentation.
