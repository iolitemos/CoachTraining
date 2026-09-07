# AGENTS.md

## Purpose

This file defines how Codex must work on the **Coach Training & Athlete Attendance Management System**.

Codex must treat these project files as authoritative:

1. `skill.md` — engineering, architecture, coding, UI/UX, API, security, build, and deployment standards.
2. `requirement.md` — source of truth for business scope, roles, rules, validation, and expected functionality.
3. `todo.md` — execution plan and implementation checklist.
4. `CLAUDE.md` — optional handoff context when work was previously performed by Claude Code.

This repository may be developed by multiple AI coding agents. Codex must continue existing work safely without redoing completed implementation.

---

# 1. Mandatory Startup Protocol

Before generating, modifying, refactoring, or reviewing code:

1. Read `AGENTS.md`.
2. Read `skill.md`.
3. Read `requirement.md`.
4. Read `todo.md`.
5. Read `CLAUDE.md` when it exists.
6. Inspect the repository structure.
7. Inspect repository state with:
   - `git status`
   - `git diff`
   - `git log --oneline -10`
8. Inspect relevant existing code before creating new files.
9. Identify the exact requirement(s) and TODO task(s) related to the requested work.
10. Determine what has already been implemented by previous agents.
11. Continue from the current repository state.

Do not assume the repository is empty.

Do not redo completed work unless the existing implementation is incorrect, incomplete, or conflicts with `skill.md` or `requirement.md`.

---

# 2. Source of Truth and Precedence

## Business behavior

`requirement.md` is authoritative for:

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
- Reporting rules
- Scope boundaries
- Out-of-scope functionality

## Engineering behavior

`skill.md` is authoritative for:

- Technology stack
- Architecture
- API conventions
- Database conventions
- Authentication
- Security
- Error handling
- Logging
- UI/UX
- Responsive behavior
- Coding standards
- Build validation
- Deployment standards

## Execution state

`todo.md` is authoritative for:

- Remaining work
- Completed work
- Module sequencing
- Backend tasks
- Frontend tasks
- Database tasks
- Validation
- Testing
- Build validation
- Deployment preparation

## Agent instructions

`AGENTS.md` defines Codex execution behavior.

`CLAUDE.md` may provide handoff context but must not override:

1. `requirement.md` for business behavior.
2. `skill.md` for engineering standards.
3. `todo.md` for current execution state.

If files conflict:

- Business conflict → follow `requirement.md`.
- Engineering conflict → follow `skill.md`.
- Progress conflict → inspect code and Git history, then correct `todo.md` only if evidence shows its state is inaccurate.

Do not modify `skill.md` or `requirement.md` unless explicitly instructed by the user.

---

# 3. Multi-Agent Handoff Rules

This repository may switch between Claude Code and Codex.

When taking over existing work:

1. Inspect `git status`.
2. Inspect unstaged changes.
3. Inspect staged changes.
4. Inspect recent commits.
5. Read the relevant section of `todo.md`.
6. Inspect implementation files associated with completed and incomplete tasks.
7. Determine the last coherent completed implementation point.
8. Continue from the first valid incomplete task.

Never assume:

- An unchecked task means zero code exists.
- A checked task is correct without inspecting code when relevant.
- Uncommitted changes are disposable.
- Code written by another agent should be replaced.

Preserve existing valid work.

Do not overwrite unrelated uncommitted changes.

Do not reset, revert, checkout, clean, or delete existing work unless explicitly instructed or clearly required to repair the requested implementation.

Avoid destructive Git commands.

---

# 4. Continuation Strategy

When the user says to continue development without specifying a module:

1. Read `todo.md`.
2. Find the first incomplete task in the dependency-appropriate sequence.
3. Inspect whether partially completed code already exists.
4. Complete the smallest coherent vertical slice.
5. Validate it.
6. Update `todo.md`.
7. Continue to the next logically dependent task only when appropriate.

Prefer module completion over scattered implementation.

A coherent vertical slice usually includes:

- Database changes when required
- Backend model/entity
- DTOs
- Service
- Controller/API
- Authorization
- Validation
- Frontend model/service
- Page/component
- Loading/error/empty states
- Responsive behavior
- Tests
- Build validation

Do not attempt the entire system in one large change.

---

# 5. Scope Control

Implement only functionality defined in `requirement.md`.

The following are **Out of Scope** unless `requirement.md` is formally updated:

- Private Training Package Management
- Coach Compensation Management
- Athlete Performance Tracking
- Training Program / Exercise Library
- Athlete / Parent Portal
- Self-Service Private Training Booking
- Payment Management
- External Email notifications
- LINE notifications
- Messaging integrations
- Advanced Management Analytics
- Routine Training Group management
- Routine Training Team management
- Routine Training Location management
- Routine athlete roster assignment

Do not add speculative functionality because it appears useful or because the engineering stack supports it.

---

# 6. Critical Business Rules

## 6.1 Training Types

Initial training types:

- Routine Training
- Private Training

Every operational training occurrence must be represented as a Training Session.

## 6.2 Routine Training

Routine Training is recurring training at the gym's regular venue.

Routine Training must not contain:

- Group
- Team
- Location
- Fixed athlete roster

Routine attendance is created by selecting athletes who actually attended.

Unselected athletes must **not** be treated as Absent.

Routine attendance supports:

- Present
- Late

Do not generate absent records for unselected athletes.

## 6.3 Private Training

Private Training:

- Has exactly one assigned coach.
- Has at least one assigned athlete.
- May contain multiple athletes.
- May optionally contain a location.
- Requires attendance status for each assigned athlete before final submission.

Private attendance supports:

- Present
- Absent
- Late
- Leave / Excused

## 6.4 Assigned Coach vs Actual Coach

Treat these as separate business concepts:

- Assigned Coach
- Actual Coach

Never overwrite the original assigned coach when a substitute is used.

Teaching-hour reporting must credit the Actual Coach who completed the session.

## 6.5 Historical Integrity

Historical business meaning must remain stable.

Changes to:

- Coach master
- Athlete master
- Routine schedule
- Coach assignment

must not rewrite finalized historical records.

Follow historical integrity and soft-delete rules from `skill.md`.

## 6.6 Cancellation

Cancelled sessions:

- Remain in history.
- Do not count as completed teaching time.
- Cannot be completed unless restored through an authorized business action.

Cancellation requires a reason.

## 6.7 Rescheduling

When rescheduling:

- Preserve the original session.
- Preserve traceability to the replacement.
- Validate schedule conflicts.
- Do not double-count reports.
- Exclude the rescheduled-original session from completed teaching hours.
- Exclude the rescheduled-original session from attendance totals.

## 6.8 Session Status and Locking

Support at minimum:

- Scheduled
- In Progress
- Completed
- Submitted
- Approved
- Locked
- Cancelled
- Rescheduled
- Coach Absent

Approved records become Locked.

Coach users cannot edit Locked records.

Authorized unlock requires a reason and must be auditable.

## 6.9 Conflict Validation

Conflict rules must be centralized and reused.

Validate:

- Coach overlap with another training session
- Athlete overlap between Private Training sessions
- Private Training overlap with the coach's Routine Training
- Substitute coach conflicts
- Rescheduled session conflicts

When override is authorized:

- Require an override reason.
- Record override history.

Never silently ignore conflicts.

---

# 7. TODO Execution Rules

`todo.md` is the execution checklist.

When implementing:

- Locate relevant unchecked tasks.
- Work on a small coherent group of tasks.
- Mark completed tasks as `- [x]`.
- Leave incomplete tasks as `- [ ]`.
- Do not delete incomplete tasks.
- Do not mark a task complete because only part of its responsibility is implemented.
- Do not mark a build task complete unless the build succeeds.
- Do not mark a test task complete unless the test actually passes.

If code already satisfies an unchecked task:

1. Verify the implementation.
2. Run applicable validation.
3. Mark the task complete only after verification.

If `requirement.md` clearly requires implementation missing from `todo.md`:

1. Add a granular TODO task to the correct section.
2. Implement it.
3. Validate it.

Do not add TODO tasks for out-of-scope functionality.

---

# 8. Backend Implementation Rules

Follow `skill.md`.

For each backend module:

1. Review existing entity/model.
2. Create or update entity/model when required.
3. Create request DTOs.
4. Create response DTOs.
5. Create or update Service.
6. Keep Controller thin.
7. Add explicit API routes.
8. Add business validation.
9. Add authorization.
10. Add error handling and logging.
11. Add tests for important rules.

Business logic belongs in Services.

Controllers must not access DbContext directly.

Do not return persistence entities directly when DTOs are required.

Reuse shared business services for status transitions, conflict validation, reporting calculations, and permission-sensitive business checks where appropriate.

---

# 9. Database Rules

Follow database standards from `skill.md`.

Before changing the database:

1. Inspect existing entities.
2. Inspect DbContext configuration.
3. Inspect existing migrations.
4. Avoid duplicate models or relationships.
5. Preserve historical data.

Use explicit entity-specific primary keys.

Required uniqueness behavior includes at least:

- Coach Code
- Athlete Code
- Athlete assignment within one Private Training session
- Athlete attendance within one Training Session

Add indexes for actual query needs, especially:

- Coach + training date
- Athlete + training date
- Session status
- Routine recurrence
- Dashboard date range
- Report date range

Database changes must include EF Core migrations.

Do not generate PostgreSQL Docker infrastructure or database `docker-compose` unless explicitly requested.

---

# 10. Frontend Implementation Rules

Follow frontend standards from `skill.md`.

Project UI language is Thai.

For each frontend module:

1. Inspect existing pages/components/services/models.
2. Reuse valid shared components.
3. Create/update frontend model.
4. Create/update API service.
5. Create/update page.
6. Split focused reusable components when useful.
7. Add form validation.
8. Add loading states.
9. Add error states.
10. Add empty states where applicable.
11. Add success feedback where applicable.
12. Add responsive behavior.
13. Add authorization-aware UI behavior.
14. Add tests where applicable.

Backend authorization remains mandatory even when frontend actions are hidden.

---

# 11. Project-Specific UI Rules

## 11.1 Coach Home

Prioritize:

- Today's sessions
- Upcoming sessions
- Training type
- Scheduled time
- Current status
- Required next action
- Pending / incomplete records
- Monthly teaching-hour summary

## 11.2 Coach Session

Present:

1. Session Summary
2. Start / Actual Teaching Information
3. Athlete Attendance
4. Training Log
5. Complete
6. Submit when applicable

Optimize for mobile.

## 11.3 Routine Attendance

- Search active athletes.
- Add only athletes who attended.
- Prevent duplicates.
- Do not display a mandatory roster.
- Do not display unselected athletes as Absent.

## 11.4 Private Attendance

- Display all assigned athletes.
- Require attendance status for all athletes before submission.
- Clearly show incomplete attendance records.

## 11.5 Routine Schedule

Never add:

- Group
- Team
- Location

## 11.6 Administrative Review

Display:

- Scheduled teaching information
- Actual teaching information
- Assigned Coach
- Actual Coach
- Athlete attendance
- Training Log
- Session status
- Submission/approval history
- Available workflow actions

---

# 12. Authentication and Authorization

Follow authentication standards in `skill.md`.

Initial roles:

- Administrator
- Coach
- Management / Viewer

Administrator can perform administrative operations defined in `requirement.md`.

Coach can access the coach's relevant operational records and own teaching information.

Management / Viewer is read-only for allowed dashboards, records, attendance information, history, and reports.

Permission checks must exist on the backend.

Do not rely only on hidden buttons, route guards, or frontend role checks.

---

# 13. Validation Rules

Implement business validation on the backend even when frontend validation also exists.

Important validation includes:

- Required fields
- Scheduled end time after scheduled start time
- Actual end time after actual start time
- Non-negative actual duration
- Routine effective date validity
- Active coach eligibility
- Active athlete eligibility
- Unique Coach Code
- Unique Athlete Code
- At least one athlete for Private Training
- No duplicate Private athlete assignment
- No duplicate attendance per athlete/session
- Allowed Routine attendance statuses
- Allowed Private attendance statuses
- Complete Private attendance before submission
- Cancellation eligibility
- Cancellation reason
- Rescheduling eligibility
- Substitute coach reason
- Conflict override reason
- Unlock reason
- Valid status transitions
- Locked-record edit restrictions
- Schedule conflict validation

Use HTTP and error-response standards from `skill.md`.

---

# 14. Reporting Rules

Dashboard and report calculations must use consistent business rules.

## Teaching Hours

- Separate Routine teaching hours.
- Separate Private teaching hours.
- Provide total teaching hours.
- Credit Actual Coach.
- Exclude Cancelled sessions.
- Exclude rescheduled-original sessions.
- Exclude Coach Absent sessions unless a valid substitute completes them.
- Avoid double-counting substitutions.
- Avoid double-counting reschedules.
- Use finalized records according to approval rules.

## Athlete Attendance

- Distinguish Routine from Private Training.
- Private may summarize Present, Absent, Late, Leave / Excused.
- Routine reports only explicitly recorded attendance.
- Never infer Routine absence from non-selection.
- Exclude rescheduled-original duplicate counts.

Dashboard totals and report totals must remain consistent.

---

# 15. Realtime Features

The initial `requirement.md` defines no realtime business requirement.

Therefore:

- Do not create SignalR hubs.
- Do not add realtime frontend subscriptions.
- Do not add live dashboard behavior.
- Do not add realtime notifications.

SignalR is available in `skill.md` but must not be implemented without a business requirement.

---

# 16. External Integrations

The initial scope does not require external integrations.

Do not add:

- Email integration
- LINE integration
- Payment integration
- External booking services
- Notification services
- Out-of-scope webhooks

unless `requirement.md` is formally updated.

---

# 17. Testing Expectations

Add tests incrementally.

Prioritize:

- Authentication
- Authorization
- Coach data-access scope
- Routine recurrence/session generation
- Schedule conflict detection
- Historical integrity
- Attendance rules
- Status transitions
- Substitute coach behavior
- Cancellation
- Rescheduling
- Approval
- Locking
- Teaching-hour calculations
- Attendance reports
- Dashboard filters
- Reporting consistency

Use `todo.md` as the detailed testing checklist.

---

# 18. Build Validation

After backend changes:

```bash
dotnet build
```

After frontend changes:

```bash
ng build
```

Run relevant automated tests.

Do not mark tasks complete when the required build fails.

If a build fails:

1. Inspect the failure.
2. Fix errors introduced by the current work.
3. Re-run the build.
4. Continue until successful or a genuine external blocker is identified.

Do not leave newly introduced build failures for a later agent.

---

# 19. Incremental Checkpoints

After completing a coherent module or vertical slice:

1. Review `git diff`.
2. Verify no unrelated files changed accidentally.
3. Run relevant tests.
4. Run required builds.
5. Update `todo.md`.
6. Summarize completed work.

If the user asked Codex to commit changes, use the commit naming standards in `skill.md`.

If the user did not request a Git commit, do not create one automatically.

---

# 20. Repository Change Discipline

Before creating new code:

- Search for existing equivalent code.
- Reuse existing valid patterns.
- Avoid duplicated services.
- Avoid duplicated business rules.
- Avoid giant files.
- Avoid speculative abstractions.
- Avoid unnecessary dependencies.
- Avoid unrelated refactoring.

Do not replace working code only because another pattern is preferred.

---

# 21. Handling Partial Work

If files contain incomplete changes from a previous agent:

1. Do not discard them.
2. Inspect their intent.
3. Compare them with `requirement.md`.
4. Compare them with the corresponding `todo.md` task.
5. Complete or repair them where appropriate.
6. Preserve unrelated valid changes.

If implementation is partially complete but not buildable, finish the coherent unit and restore build success before moving on.

---

# 22. Handling Ambiguity

Do not invent business behavior.

If missing information concerns only an implementation detail:

- Follow `skill.md`.
- Follow existing repository conventions.
- Choose the simplest maintainable approach.

If ambiguity materially affects business behavior, permissions, data meaning, historical integrity, reporting, or scope, do not silently create a new business rule.

When an existing requirement already resolves the issue, do not ask the user again.

---

# 23. Definition of Done

A task is complete only when all applicable items are satisfied:

- Requirement behavior is implemented.
- Database change is migrated when required.
- DTO/model changes are complete.
- Service logic is complete.
- API is complete when required.
- Authorization is enforced.
- Validation is enforced.
- Frontend UI is complete when required.
- Loading/error/empty states are implemented where applicable.
- Responsive behavior is implemented where applicable.
- Relevant automated tests pass.
- Backend build passes when backend changed.
- Frontend build passes when frontend changed.
- No out-of-scope functionality was introduced.
- Relevant `todo.md` task is marked `- [x]`.

Never claim completion when required validation fails.

---

# 24. Recommended Module Order

Unless the user explicitly requests another module, follow the dependency-aware order in `todo.md`.

Recommended progression:

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
21. Testing
22. Release Validation

Keep the repository buildable after every completed module.

---

# 25. Standard Continuation Behavior

When the user says `Continue implementation` or equivalent without specifying a task, Codex must:

1. Read `skill.md`.
2. Read `requirement.md`.
3. Read `todo.md`.
4. Inspect `git status`.
5. Inspect `git diff`.
6. Inspect recent commits.
7. Find the first dependency-valid incomplete task.
8. Inspect existing implementation related to that task.
9. Continue without redoing valid completed work.
10. Run relevant tests.
11. Run required build validation.
12. Fix errors.
13. Update `todo.md`.
14. Continue within the same coherent module when appropriate.

---

# 26. Final Working Rule

For every task:

1. Understand the requirement.
2. Understand current repository state.
3. Preserve valid existing work.
4. Implement the smallest complete scope.
5. Validate business rules.
6. Validate permissions.
7. Test important behavior.
8. Build affected applications.
9. Fix failures.
10. Update `todo.md`.
11. Report completed work and remaining work.

Prioritize:

- Correct business behavior
- Historical data integrity
- Simplicity
- Maintainability
- User experience
- Production readiness
- Security

Do not overengineer.
