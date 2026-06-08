# Privacy Policy — Imprelia Print Agent

**Last updated: June 8, 2026**
**Developer: Roberth Dudiver — [www.dudiver.net](https://www.dudiver.net)**

---

## Summary

Imprelia Print Agent is a local Windows application that runs a private HTTP server on your device (`localhost:9100`). It does **not** collect, transmit, or share any personal information.

---

## What the app does

- Runs a local HTTP server accessible only from the same computer (`127.0.0.1` / `localhost`).
- Receives print jobs from web applications running on the same machine and forwards them to locally-installed printers.
- Optionally stores configuration (port, printer name, API key) in `%APPDATA%\ImpreliaPrintAgent\config.json` on the local device only.

## What the app does NOT do

- Does **not** send data to any external server or cloud service.
- Does **not** collect names, addresses, emails, phone numbers, or any other personal identifiers.
- Does **not** track usage, install analytics, or report telemetry to the developer or any third party.
- Does **not** access the internet for any purpose other than listening for local connections.

## Data storage

The only data stored by the app is its configuration file (`config.json`) saved locally on your device. This file contains:
- The selected default printer name
- The server port (default: 9100)
- An optional API key (if you choose to enable authentication)

This data never leaves your device.

## Network access

The app declares the `internetClient` Windows capability to operate its local HTTP server. This capability is required for the server to accept connections from the local browser. No outbound connections to the internet are made.

## Contact

If you have questions about this privacy policy, contact:

**Roberth Dudiver**
Website: https://www.dudiver.net
