// Health check endpoints are mapped via MapHealthChecks in Program.cs
// GET /health       → liveness (always healthy if API is up)
// GET /health/ready → readiness with MongoDB connectivity check
