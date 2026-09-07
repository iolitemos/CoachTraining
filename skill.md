# Universal AI Project Skill

## Goal

Use this file as the universal development standard for all projects.

The AI must follow these rules when generating, modifying, refactoring, or reviewing code.

Main goals:
- Clean code
- Maintainable architecture
- Production-ready output
- Consistent UI/UX
- Consistent API standards
- Reduce hallucination
- Reduce unnecessary code generation

---

# Tech Stack

## Frontend
- Angular 21
- Tailwind CSS
- Font: Prompt
- Responsive design for mobile and desktop
- UI Language: Thai (all labels, buttons, messages in Thai)
- Primary color: Emerald (Tailwind emerald-500 #10B981)

## Backend
- .NET 10 Web API
- Entity Framework Core
- MailKit for email sending
- SignalR for real-time communication

## Database
- PostgreSQL

Rules:
- Do NOT create PostgreSQL Docker setup
- PostgreSQL already exists in Docker environment
- Do NOT generate docker-compose for database unless explicitly requested

---

# Core Philosophy

Prioritize:
- Simplicity
- Readability
- Maintainability
- Scalability
- Reusability
- Production readiness

Avoid:
- Overengineering
- Deep nesting
- Giant files
- Duplicated code
- Unnecessary abstractions
- Random dependencies

---

# Architecture Rules

Use simple clean architecture.

## Backend Structure

Preferred structure:
- Controllers
- Services
- Models
- DTOs
- Data
- Helpers
- Middleware
- EmailTemplate

Rules:
- Controllers must remain thin
- Business logic must be inside Services
- Controllers must not access DbContext directly
- Use DTOs for request and response
- Use dependency injection
- Use async/await
- Keep structure simple and maintainable

## Frontend Structure

Preferred structure:
- Pages
- Components
- Services
- Models
- Guards
- Interceptors
- Shared

Rules:
- Use Angular standalone components when appropriate
- API calls must go through services
- JWT token handling must use interceptor
- Keep components small and focused

---

# Database Naming Standards

Primary keys must use explicit entity names.

Preferred:
- UserId
- ProjectId
- OrderId
- DocumentId

Avoid:
- Id

Foreign keys must also use explicit names.

Example:
- UserId
- CreatedByUserId
- UpdatedByUserId

---

# Common Columns

All important tables should include:
- {EntityName}Id
- CreatedDate
- UpdatedDate
- CreatedByUserId
- UpdatedByUserId
- IsDeleted

---

# HTTP API Standards

Use proper HTTP methods:

- GET: retrieve data
- POST: create data
- PUT: full update
- PATCH: partial update
- DELETE: delete or soft delete

Use proper HTTP status codes:

## Success
- 200 OK
- 201 Created
- 204 No Content

## Client Errors
- 400 Bad Request
- 401 Unauthorized
- 403 Forbidden
- 404 Not Found
- 409 Conflict

## Server Errors
- 500 Internal Server Error

Rules:
- Auth failure must return 401
- Validation failure must return 400
- Success must return 2xx
- Error must return proper 4xx or 5xx status code
- Do not return 200 for failed operations

---

# API Response Standards

Use proper HTTP status codes for transport-level responses.

Additionally, use a consistent response body format for application-level responses.

HTTP status code and response body must both be meaningful and consistent.

## Success Response

```json
{
  "message": "Success",
  "data": {}
}
```

## Error Response

```json
{
  "message": "Error message",
  "errors": []
}
```

Rules:
- Do not use success: true/false
- HTTP status code is the primary success/failure indicator
- Response body should provide useful business/application information
- Error responses should include validation details when appropriate

---

# Validation Error Response Example

```json
{
  "message": "Validation failed",
  "errors": [
    {
      "field": "email",
      "message": "Email is required"
    }
  ]
}
```

---

# API Route Standards

All APIs must explicitly define routes.

Avoid:

```csharp
[HttpGet]
```

Preferred:

```csharp
[HttpGet("users")]
[HttpGet("{id}")]
[HttpPost("login")]
[HttpPut("{id}")]
[HttpPatch("{id}")]
[HttpDelete("{id}")]
```

---

# API Error Handling and Logging

All APIs and service methods must use try-catch when handling business operations.

Inside catch block:
- Log exception
- Log API route
- Log controller name
- Log service name
- Log function name
- Log user ID if available
- Log useful request context if safe

Rules:
- Never swallow exceptions silently
- Never expose raw exception details to users
- Return proper HTTP status code
- Log enough information to trace where the error came from

---

# Build Validation Rules

After every implementation or code modification:

## Frontend

```bash
ng build
```

## Backend

```bash
dotnet build
```

Rules:
- Fix all build errors before completing the task
- Never mark task as completed if build fails

---

# Soft Delete Standards

Never hard delete important business data.

Use:

```csharp
IsDeleted = true
```

---

# Email Standards

Use MailKit for sending email.

Email HTML templates must be stored in:

```txt
Backend/EmailTemplate
```

Rules:
- Use HTML template files
- Use {{param}} placeholders
- Replace placeholders before sending email
- Do not hardcode HTML inside services
- Do not generate email HTML inside controllers

---

# File Upload Standards

File storage provider: **AWS S3**

Preferred file upload flow:

1. Backend generates presigned upload URL
2. Frontend uploads directly to S3
3. Backend saves only file path/key in database
4. Frontend requests signed read URL when displaying/downloading files

---

# PDF and Print Standards

Use pdfmake as the primary library for PDF generation and printing.

Rules:
- Use pdfmake for printable documents
- Keep PDF layout clean and professional
- Use consistent font, spacing, header, footer, and table style

---

# Icon Standards

Use standard free third-party icon libraries.

Preferred:
- Lucide Icons
- Font Awesome Free
- Heroicons
- Material Icons

Rules:
- Avoid generating custom SVG icons unless necessary
- Use consistent icon style across the system
- Icons must follow system theme color

---

# Frontend UI/UX Standards

Preferred style:
- Clean
- Modern
- Minimal
- Professional
- SaaS style
- Soft shadow
- Rounded corners
- Good spacing
- Mobile-first

---

# Responsive Design Rules

Frontend must work well on:
- Mobile
- Tablet
- Desktop

Rules:
- Mobile-first design
- Responsive layout
- Flexible cards/tables/lists
- Avoid horizontal scrolling

---

# Data Listing UI Standards

This standard applies to:
- Tables
- List items
- Cards
- Timeline views
- Mobile list views

All data listing pages must support:
- Search
- Filter
- Pagination or infinite scroll
- Loading state
- Empty state
- Responsive layout

Search layout must contain:
- Search input
- Search button
- Filter button

Rules:
- Search input, search button, and filter button must stay in the same row
- Advanced filters must be hidden inside modal/drawer/popup
- Maximize data display area

---

# Form Standards

Forms must include:
- Validation
- Error messages
- Loading state
- Disabled submit button while processing
- Clear success/error feedback

Rules:
- Required input fields must display red asterisk (*)
- Use reactive forms when appropriate
- Optimize for mobile usage

---

# Theme and Color Standards

Every project must define:
- Primary color
- Secondary color
- Success color
- Warning color
- Danger color
- Background color
- Surface/Card color
- Text color
- Border color

Rules:
- Use consistent theme colors
- Do not use random colors
- LINE Flex Message must follow the same theme

---

# Real-time Communication Standards

Preferred real-time technology:
- SignalR

Use SignalR for:
- Notifications
- Chat
- Real-time comments
- Live dashboard updates
- Order status updates
- Task status updates
- Real-time progress tracking

Rules:
- Keep SignalR implementation simple
- Separate hub logic from business logic
- Avoid heavy processing inside hubs

---

# LINE Messaging Standards

If the system uses LINE Message API:

All messages must use:
- Flex Message

Rules:
- Do not send plain text messages unless explicitly required
- Flex Message must follow system theme color
- Use clean card-based layout
- Keep text short and readable

---

# Payment Integration Standards

Use BeamCheckout PromptPay when payment is required.

## Payment Flow

1. Backend creates Beam charge
2. Beam returns QR PromptPay
3. Frontend displays QR code
4. Customer scans and pays
5. Beam sends webhook
6. Backend verifies webhook signature
7. Backend updates payment status
8. Backend activates subscription or top-up

## Payment Success Status

Treat these statuses as paid:
- SUCCEEDED
- completed
- success
- paid

## Safety Rules

Never activate:
- subscription
- top-up
- message credit
- package upgrade

until payment is fully verified.

---

# Webhook Standards

All webhooks must:
- Verify signature
- Validate payload
- Save raw payload
- Save received timestamp
- Log webhook activity
- Prevent duplicate processing

---

# External API Standards

All external API integrations must:
- Use HttpClientFactory
- Validate response status
- Validate content type
- Handle timeout properly
- Log external API failures

---

# Environment Rules

Use separated environments:
- DEV
- QAS
- PROD

Rules:
- Use appsettings.{ENV}.json
- Use Angular environment files
- Use environment variables for secrets

---

# Deployment Automation Rules

Deployment should support automation.

Preferred deployment flow:
1. Pull latest source code
2. Install dependencies
3. Build frontend
4. Build backend
5. Run migrations
6. Publish backend
7. Deploy frontend
8. Restart services
9. Validate health check

Rules:
- Never deploy if build fails
- Separate DEV, QAS, and PROD deployment

---

# Security Standards

Rules:
- Validate all inputs
- Use HTTPS
- Never expose API keys
- Validate uploaded file types
- Protect against SQL injection
- Protect against XSS
- Validate user permissions in backend

---

# Authentication Standards

Use JWT authentication.

Rules:
- Use JWT Bearer Token
- Use role-based authorization
- Use interceptor to attach token
- Auth failure must return 401
- Permission failure must return 403

---

# Performance Standards

Rules:
- Lazy load when appropriate
- Optimize database queries
- Avoid unnecessary API calls
- Use pagination or virtual scroll for large data

---

# Async Standards

Rules:
- Use async/await consistently
- Avoid .Result
- Avoid .Wait()
- Avoid blocking async threads
- Use CancellationToken when appropriate

---

# Null Safety Standards

Rules:
- Validate null values properly
- Avoid possible null reference exceptions
- Use nullable reference types when possible

---

# Code Quality and Sonar Standards

Generated code should follow clean code principles and should be compatible with SonarQube/SonarCloud quality standards.

Rules:
- Avoid duplicated code
- Avoid unused variables
- Avoid unused methods
- Avoid deep nesting
- Avoid hardcoded secrets
- Avoid magic strings
- Avoid overly complex methods
- Avoid giant classes/components
- Use meaningful variable names
- Use proper null checking
- Dispose resources properly
- Handle async/await correctly
- Keep cyclomatic complexity low

Code should aim to pass:
- SonarQube
- SonarCloud
- Static analysis tools

---

# Naming Conventions

## Backend

- Class = PascalCase
- Interface = prefix I
- Method = PascalCase
- Variable = camelCase
- Constant = UPPER_CASE

## Frontend

- Component = kebab-case
- Variable = camelCase
- File = kebab-case

---

# AI Coding Rules

When AI generates code:
- Prioritize production-ready code
- Follow this skill file strictly
- Avoid unnecessary dependencies
- Generate modular code
- Avoid giant files
- Never hallucinate APIs or libraries
- Never mark task complete if build fails

---

# Language Rules

User may communicate in Thai.

Rules:
- Understand Thai prompts
- Generate code using English naming
- UI text can be Thai if requested

---

# Git Standards

## Branch Naming

```txt
feature/*
fix/*
hotfix/*
refactor/*
```

## Commit Style

```txt
feat:
fix:
refactor:
style:
docs:
test:
chore:
```

---

# TODO Workflow

When implementing features:

1. Create TODO checklist
2. Separate backend and frontend tasks
3. Mark completed tasks
4. Run build checks before marking complete

Example:

```md
- [x] Create API
- [x] Create service
- [ ] Create frontend page
- [ ] Run ng build
- [ ] Run dotnet build
```

---

# Final Rule

Always prioritize:
- Maintainability
- Simplicity
- User experience
- Production readiness
- Security

Do not overengineer simple problems.
