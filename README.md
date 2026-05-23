# Secure Voting System (Educational Project)

This repository contains a demo Secure Voting System full-stack app:
- Frontend: React + Vite
- Backend: ASP.NET Core (.NET 8) Web API
- Database: MySQL (Pomelo EF Core provider)
- Docker Compose, Jenkins, Prometheus, Grafana

## Quick setup (development)

1. Start MySQL (or use Docker compose):

```bash
# from repository root
cd docker
docker compose up -d mysql
```

2. Configure connection string in `backend/SecureVotingSystem/appsettings.json` (replace `YOUR_PASSWORD`).

3. Run EF migrations (requires dotnet-ef):

```bash
cd backend/SecureVotingSystem
dotnet tool install --global dotnet-ef --version 8.0.0
dotnet ef migrations add InitialCreate
dotnet ef database update
```

4. Run backend:

```bash
cd backend/SecureVotingSystem
dotnet run
# API available at http://localhost:5000
```

5. Run frontend:

```bash
cd frontend
npm install
npm run dev
# app at http://localhost:5173
```

6. Login as sample admin:
- Email: `admin@local`
- Password: `Admin123!`

## Notes
- OTP is fake: the 6-digit code is printed to the ASP.NET console when you login.
- This project is for educational/demo purposes only.
- Use Docker Compose (`docker/docker-compose.yml`) to run all services including Jenkins, Prometheus and Grafana.

## Seed data
Seed data creates an `admin@local` account and a sample election with two candidates.

## Security
- Passwords hashed with BCrypt
- JWT auth configured
- Duplicate vote prevention enforced at DB and service level

## Further work
- Add SignalR real-time updates
- Add PDF export for results
- Improve frontend UX and charts

