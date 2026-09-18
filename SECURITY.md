# Security Policy

This repository is a portfolio implementation intended to demonstrate backend engineering patterns. It is not a production financial service and should not be deployed with real customer funds or credentials without additional security review and hardening.

## Reporting a security issue

Please report security concerns privately to the repository owner rather than opening a public issue containing exploit details or credentials.

## Configuration

Runtime secrets are supplied through environment variables. Do not commit `.env` files, private keys, API tokens, database passwords, JWT signing keys, or webhook signing secrets.

## Known hardening boundaries

The project documents its current security boundaries in `ARCHITECTURE.md`, including the scope of webhook destination validation and the need for additional production controls such as stronger egress isolation and database-backed integration testing.
