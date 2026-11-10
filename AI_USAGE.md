# AI usage

This project was built with the help of **Claude (Anthropic)**, working from the planning document `planning-v1.md`.

- **Generated with AI assistance:** the ASP.NET Core backend, SQL schema, Docker and Compose files, xUnit tests, the React frontend and this documentation.
- **Human responsibility:** requirements, architecture decisions (pessimistic locking, hold duration rules) and final review remain the owner's.
- **Verification status:** the frontend was type-checked and production-built (`tsc` and `vite build`). The backend, Docker images and tests were written without a .NET SDK or Docker in the authoring environment, so they have **not been compiled or run by the author**. Run `docker compose up --build` and the test suites (see README) and review the results before relying on them or submitting.
