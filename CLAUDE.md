# CLAUDE.md

## Purpose

This file defines how Claude Code must work on the **Coach Training & Athlete Attendance Management System**.

Claude Code must use the following project files as the authoritative sources:

1. `skill.md` — universal engineering, architecture, coding, UI/UX, API, security, build, and deployment standards.
2. `requirement.md` — source of truth for business scope, business behavior, roles, validation rules, and expected functionality.
3. `todo.md` — execution plan and implementation checklist.

Do not replace, reinterpret, or expand these files with assumptions.

---

# 1. Mandatory Startup Protocol

Before generating, modifying, refactoring, or reviewing code:

1. Read `skill.md`.
2. Read `requirement.md`.
3. Read `todo.md`.
4. Inspect the existing repository structure and relevant existing code.
5. Identify the exact requirement(s) and TODO task(s) related to the requested work.
6. Reuse existing project patterns when they comply with `skill.md`.
7. Implement only the requested or next approved scope.

Do not start implementation before understanding all three project files.

---

# 2. Source of Truth and Precedence

Use the following precedence when making decisions:

### Business behavior

`requirement.md` is authoritative.

Use it for:

- Business rules
- User roles
- Permissions
- Module behavior
- Training flows
- Session statuses
- Attendance behavior
- Schedule conflict rules
- Approval and locking rules
- Dashboard behavior
- Report calculations
- In-scope and out-of-scope decisions

### Engineering behavior

`skill.md` is authoritative.

Use it for:

- Technology stack
- Architecture
- API conventions
- Database conventions
- Authentication
- Security
- Error handling
- Logging
- UI/UX standards
- Responsive behavior
- Coding standards
- Build validation
- Deployment standards

### Execution order

`todo.md` is authoritative.

Use it for:

- Implementation sequence
- Module checklist
- Database tasks
- Backend tasks
- Frontend tasks
- Validation tasks
- Security tasks
- Testing tasks
- Build validation
- Deployment preparation

If `todo.md` appears to conflict with `requirement.md`, follow `requirement.md` and correct the execution plan only when appropriate.

If implementation code conflicts with `skill.md`, fix the implementation.

Do not modify `skill.md` or `requirement.md` unless explicitly instructed by the user.

---

# 3. Scope Control

Implement only functionality defined in `requirement.md`.

Do not add speculative functionality.

The following are explicitly **Out of Scope** for the initial implementation unless `requirement.md` is formally changed:

- Private Training Package Management
- Coach Compensation Management
- Athlete Performance Tracking
- Training Program / Exercise Library
- Athlete / Parent Portal
- Self-Service Private Training Booking
- Payment Management
- External Email / LINE / Messaging notifications
- Advanced Management Analytics
- Routine Training Group management
- Routine Training Team management
- Routine Training Location management
- Routine athlete roster assignment

Do not implement these features because they appear useful or because supporting standards exist in `skill.md`.

---

# 4. Critical Business Rules

These rules are especially important and must not be changed implicitly.

## 4.1 Training Types

The system has two initial training types:

- Routine Training
- Private Training

Every operational training occurrence must be represented as a Training Session.

---

## 4.2 Routine Training

Routine Training is recurring training at the gym's regular venue.

Routine Training must **not** contain:

- Group
- Team
- Location
- Fixed athlete roster

Routine attendance is recorded by selecting athletes who actually attended.

Unselected athletes must **not** automatically become Absent.

Routine attendance supports:

- Present
- Late

Do not create absence records for athletes who were not selected.

---

## 4.3 Private Training

Private Training:

- Has exactly one assigned coach.
- Has at least one assigned athlete.
- May have multiple athletes.
- May optionally contain a location.
- Requires attendance status for every assigned athlete before final submission.

Private attendance supports:

- Present
- Absent
- Late
- Leave / Excused

---

## 4.4 Coach Assignment

Always treat these as separate concepts:

- Assigned Coach
- Actual Coach

A substitute coach must not overwrite the original assigned coach.

Teaching-hour reporting must credit the coach who actually taught the completed session.

---

## 4.5 Historical Integrity

Never destroy or rewrite historical business meaning.

Changes to:

- Coach master data
- Athlete master data
- Routine schedules
- Coach assignments

must not alter finalized historical records.

Important business data must follow the soft-delete and historical-integrity standards defined in `skill.md`.

---

## 4.6 Cancellation

Cancelled sessions:

- Remain available in history.
- Do not count as completed teaching time.
- Cannot be completed unless restored through an authorized business action.

A cancellation reason is required.

---

## 4.7 Rescheduling

When rescheduling:

- Preserve the original session.
- Create or maintain a replacement session linked to the original.
- Validate the replacement schedule for conflicts.
- Do not double-count the original and replacement in reports.
- The rescheduled original must not contribute completed teaching hours or attendance totals.

---

## 4.8 Session Status and Locking

Use the session/status behavior defined in `requirement.md`.

At minimum, support:

- Scheduled
- In Progress
- Completed
- Submitted
- Approved
- Locked
- Cancelled
- Rescheduled
- Coach Absent

Do not allow invalid status transitions.

Approved records become Locked.

Coach users must not edit Locked records.

Authorized Administrator unlock actions require a reason and must remain auditable.

---

## 4.9 Conflict Validation

Schedule conflict rules must be centralized and reused.

Validate:

- Coach overlap with another session
- Athlete overlap between Private Training sessions
- Private Training overlap with the coach's Routine Training
- Substitute coach conflicts
- Rescheduled session conflicts

If an authorized override is supported:

- Require an override reason.
- Record override history.

Do not silently ignore conflicts.

---

# 5. Implementation Strategy

Work incrementally.

Preferred implementation approach:

1. Complete project foundation.
2. Complete required database models and migration for the target module.
3. Complete backend DTOs.
4. Complete backend service.
5. Complete backend controller/API.
6. Complete backend validation and authorization.
7. Build backend.
8. Complete frontend models/services.
9. Complete frontend page/components.
10. Complete frontend validation, loading, empty, and error states.
11. Complete responsive behavior.
12. Build frontend.
13. Add or update automated tests.
14. Validate relevant business scenario.
15. Update completed tasks in `todo.md`.

Do not implement the entire system in one large change.

Prefer one module or one coherent vertical slice at a time.

---

# 6. TODO Execution Rules

`todo.md` is the execution checklist.

When implementing:

- Locate the relevant unchecked task(s).
- Work on a small coherent group of related tasks.
- Do not mark a task complete before its implementation is actually finished.
- Mark completed tasks using `- [x]`.
- Leave incomplete tasks as `- [ ]`.
- Do not delete incomplete TODO items.
- Do not mark dependent frontend/backend tasks complete merely because one layer is finished.
- Do not mark build-validation tasks complete unless the corresponding build succeeds.
- Do not mark testing tasks complete unless the specified test actually passes.

When a task requires additional work not represented in `todo.md` but clearly required by `requirement.md`, add a granular TODO item in the correct section before implementing it.

Do not add TODO items for out-of-scope functionality.

---

# 7. Backend Rules

Follow the backend architecture and API standards from `skill.md`.

For every backend module:

1. Define or update the required model/entity.
2. Define request DTOs.
3. Define response DTOs.
4. Implement business logic in a Service.
5. Keep Controllers thin.
6. Add explicit API routes.
7. Add validation.
8. Add authorization.
9. Add appropriate error handling and logging.
10. Add tests for important business rules.

Controllers must not contain substantial business logic.

Controllers must not access the database context directly.

Do not return persistence entities directly when DTOs are required.

Use the HTTP status and response conventions defined in `skill.md`.

---

# 8. Database Rules

Follow database naming and common-column standards from `skill.md`.

Before changing the database:

- Review existing entities and migrations.
- Avoid duplicate tables or overlapping models.
- Preserve historical relationships.
- Use explicit entity-specific primary key names.
- Add foreign keys intentionally.
- Add uniqueness constraints required by business rules.
- Add indexes for common list, schedule, dashboard, and reporting queries.
- Use soft delete for important business data.

Required uniqueness rules include at least:

- Coach Code
- Athlete Code
- Athlete assignment within one Private Training session
- Athlete attendance within one Training Session

Database changes must include an EF Core migration.

Do not create PostgreSQL Docker infrastructure.

The PostgreSQL environment already exists as defined in `skill.md`.

---

# 9. Frontend Rules

Follow frontend and UI/UX standards from `skill.md`.

Project UI language is Thai.

For each data-listing page, include the listing behaviors required by `skill.md`.

For every major page or async operation, implement:

- Loading state
- Error state
- Empty state where applicable
- Success feedback where applicable
- Disabled processing state for actions/forms

Forms must provide clear validation feedback.

Do not expose actions the current user cannot perform.

Backend authorization remains mandatory even when frontend actions are hidden.

---

# 10. Project-Specific UI Priorities

## Coach Home

Prioritize:

- Today's sessions
- Upcoming sessions
- Training type
- Scheduled time
- Current status
- Required next action
- Pending/incomplete records
- Monthly teaching-hour summary

The Coach must be able to open an actionable session directly.

## Coach Session

Keep the primary workflow easy to follow:

1. Session Summary
2. Start / Actual Teaching Information
3. Athlete Attendance
4. Training Log
5. Complete
6. Submit when applicable

Optimize this workflow for mobile use.

## Routine Attendance UI

- Search active athletes.
- Add only athletes who attended.
- Prevent duplicate athlete selection.
- Do not show a mandatory roster.
- Do not imply that unselected athletes are absent.

## Private Attendance UI

- Display all assigned athletes.
- Require attendance status for every assigned athlete before submission.
- Clearly identify missing attendance data.

## Routine Schedule UI

Do not add:

- Group
- Team
- Location

## Administrative Review

Display enough information to review the record without navigating unnecessarily:

- Scheduled teaching data
- Actual teaching data
- Assigned Coach
- Actual Coach
- Attendance
- Training Log
- Status
- Approval history
- Available workflow actions

---

# 11. Authentication and Authorization

Use the authentication standards defined in `skill.md`.

Initial business roles:

- Administrator
- Coach
- Management / Viewer

Minimum authorization behavior:

### Administrator

Can manage master data, schedules, session administration, review/approval, corrections, reporting, and audit history as defined in `requirement.md`.

### Coach

Can access the coach's relevant operational records and own teaching information.

A Coach must not gain unrestricted access to another coach's operational records.

### Management / Viewer

Read-only for dashboards, teaching records, attendance information, and reports.

Do not rely only on frontend route guards.

Enforce permissions on the backend.

---

# 12. Validation Rules

Business validation must be implemented in backend services even when equivalent validation exists in the frontend.

Important validation includes:

- Required business fields
- Start/end date-time consistency
- Non-negative actual teaching duration
- Active coach eligibility
- Active athlete eligibility
- Unique Coach Code
- Unique Athlete Code
- Private Training requires at least one athlete
- No duplicate Private athlete assignment
- No duplicate Attendance per athlete/session
- Routine attendance uses only allowed Routine statuses
- Private attendance uses only allowed Private statuses
- Private attendance completeness before submission
- Cancellation reason
- Rescheduling eligibility
- Substitute coach reason
- Conflict override reason
- Unlock reason
- Valid session status transitions
- Locked-record edit restrictions
- Schedule conflict rules

Use the error-response standards from `skill.md`.

---

# 13. Reporting Rules

All dashboard and report calculations must use the same business rules.

## Teaching Hours

- Separate Routine and Private teaching hours.
- Support total teaching hours.
- Credit the Actual Coach.
- Exclude Cancelled sessions.
- Exclude rescheduled-original sessions.
- Exclude Coach Absent sessions unless a substitute validly completes the session.
- Do not double-count substituted or rescheduled sessions.
- Use finalized records according to the approval rules.

## Athlete Attendance

- Distinguish Routine and Private Training.
- Private attendance may summarize Present, Absent, Late, and Leave / Excused.
- Routine attendance reports only explicitly recorded attendance.
- Never infer Routine absence from non-selection.
- Do not double-count rescheduled-original sessions.

Dashboard totals and report totals must remain consistent.

---

# 14. Realtime Features

There is no initial realtime business requirement in `requirement.md`.

Therefore:

- Do not create SignalR hubs for the initial scope.
- Do not add realtime subscriptions to frontend code.
- Do not introduce realtime behavior merely because SignalR is part of the engineering standard.

If realtime functionality is formally added to `requirement.md`, then follow the SignalR standards in `skill.md`.

---

# 15. Integrations

The initial scope does not require external integrations.

Do not add:

- Email sending
- LINE messaging
- Payment gateway integration
- External booking integration
- External notification services

unless the project requirements are formally updated.

---

# 16. Testing Expectations

Add tests alongside implementation rather than postponing all tests until the end.

Prioritize automated tests for business-critical behavior, including:

- Authentication and authorization
- Schedule conflict detection
- Routine recurrence/session generation
- Historical integrity
- Attendance rules
- Status transitions
- Substitute coach behavior
- Cancellation
- Rescheduling
- Approval and locking
- Teaching-hour calculations
- Attendance reporting
- Role-scoped data access

Use `todo.md` as the detailed testing checklist.

---

# 17. Build Validation

After backend code changes:

```bash
dotnet build
```

After frontend code changes:

```bash
ng build
```

Run relevant tests when available.

A task is not complete when its required build fails.

Before marking a module complete:

- Fix all build errors.
- Fix newly introduced compile/type errors.
- Run the module's relevant automated tests.
- Confirm the implementation satisfies the linked requirements.
- Update `todo.md`.

Before final release validation:

- Run backend tests.
- Run frontend tests.
- Run final backend build.
- Run final frontend build.
- Run configured static analysis when available.

---

# 18. Repository Change Discipline

Before creating a new file or abstraction:

- Search for an existing equivalent.
- Reuse existing components/services/models where appropriate.
- Avoid duplicated business logic.
- Avoid unnecessary dependencies.
- Avoid speculative abstractions.
- Keep files focused and maintainable.

Do not refactor unrelated areas unless required to safely implement the requested change.

Do not rewrite working modules merely for stylistic preference.

---

# 19. Handling Ambiguity

Do not invent new business behavior.

If an implementation detail is not specified but does not alter business behavior:

- Follow `skill.md`.
- Follow existing repository conventions.
- Choose the simplest maintainable option.

If a missing decision would materially change:

- Business rules
- User permissions
- Data meaning
- Reporting calculations
- Historical behavior
- Scope

do not silently guess.

Identify the ambiguity before implementing that behavior.

---

# 20. Definition of Done

A task is complete only when all applicable items below are satisfied:

- Requirement behavior is implemented.
- Database change is migrated when required.
- DTO/model changes are complete.
- Service logic is complete.
- API is complete when required.
- Authorization is enforced.
- Validation is enforced.
- Frontend UI is complete when required.
- Loading/error/empty states are implemented where applicable.
- Responsive behavior is verified where applicable.
- Relevant automated tests pass.
- `dotnet build` passes for backend changes.
- `ng build` passes for frontend changes.
- No out-of-scope feature was introduced.
- Relevant `todo.md` checkbox is updated to `- [x]`.

Never claim completion when build validation fails.

---

# 21. Recommended Module Order

Unless the user explicitly requests another order, use the dependency-aware execution order represented in `todo.md`.

A practical progression is:

1. Project Setup
2. Database Foundation
3. Authentication / User Roles
4. Coach Management
5. Athlete Management
6. Routine Training
7. Private Training
8. Training Session / Teaching Record
9. Routine Attendance
10. Private Attendance
11. Training Log
12. Substitute Coach
13. Cancellation
14. Rescheduling
15. Conflict Handling
16. Approval / Locking
17. Coach Dashboard
18. Administrator Dashboard
19. Reports
20. History / Audit
21. Testing and Release Validation

Complete each module incrementally and keep the application buildable throughout development.

---

# 22. Final Instruction

For every Claude Code task:

1. Read the relevant parts of `skill.md`.
2. Read the relevant parts of `requirement.md`.
3. Locate the corresponding task in `todo.md`.
4. Inspect existing code before creating new code.
5. Implement the smallest complete scope that satisfies the requirement.
6. Validate business rules and permissions.
7. Run the required build/tests.
8. Fix failures before stopping.
9. Update `todo.md` only for tasks genuinely completed.
10. Report what changed, what was validated, and what remains.

Prioritize:

- Correct business behavior
- Historical data integrity
- Simplicity
- Maintainability
- User experience
- Production readiness
- Security

Do not overengineer the system.
