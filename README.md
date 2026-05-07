## Setup

**Prerequisites:** .NET 10 SDK · Node.js 20+ · SQL Server

```bash
git clone https://github.com/MarinoM0/fesb-helpdesk.git
cd fesb-helpdesk
cp .env.example .env   # fill in JWT_SECRET, GEMINI_API_KEY, SMTP_USERNAME, SMTP_PASSWORD

# Backend
cd backend/FesbHelpdesk.Api && dotnet ef database update && dotnet run

# Frontend (new terminal)
cd frontend/fesb-helpdesk-app && npm install && npm start
```

Backend runs on `http://localhost:5000`, frontend on `http://localhost:4200`.
First run seeds 4 default users and 10 categories.

### Default users

| Email | Password | Role |
|---|---|---|
| `admin@fesb.hr` | `Admin123!` | admin |
| `referada@fesb.hr` | `Referada123!` | student services |
| `nastavnik@fesb.hr` | `Nastavnik123!` | teacher |
| `student@fesb.hr` | `Student123!` | student |

---

## Technical overview

**Stack:** .NET 10 Web API · EF Core 10 · SQL Server · MailKit · JWT Bearer · BCrypt · Angular 21 (standalone, signals, zoneless) · Google Gemini 2.5 Flash

**Key features:**
- **JWT authentication** with role claim (`student`, `referada`, `nastavnik`, `admin`); BCrypt password hashing
- **Role-based access control** — server-side checks on every endpoint, frontend guards on navigation
- **AI classification** — Gemini deterministically picks one of the admin-managed categories; falls back to `Ostalo` when uncertain or on API failure
- **Email notifications** via SMTP — on inquiry submission, status change, and new replies
- **Inquiry workflow** — statuses (Novo / U obradi / Riješeno), reply thread, reassignment (student services → teacher; admin reassigns freely)
- **Admin interface** — category CRUD (default category protected), full inquiry overview, deletion
- **Security** — secrets loaded from env vars, FESB email domain validation on registration, role checks enforced server-side
- **Responsive UI** — mobile drawer, horizontally scrollable tables, adaptive typography
