# Coach Training & Athlete Attendance Management System — Execution Plan

> Source of truth: `requirement.md`  
> Engineering standard: `skill.md`  
> Implement incrementally by module. Do not add features listed as Out of Scope unless the requirements are formally updated.

## 1. Project Setup

### 1.1 Repository and Solution Structure

- [x] Create backend project structure required by `skill.md`.
- [x] Create frontend project structure required by `skill.md`.
- [x] Configure backend dependency injection entry points.
- [x] Configure frontend application routing.
- [x] Configure shared frontend layout and navigation shell.
- [x] Configure PostgreSQL connection for the existing database environment.
- [x] Confirm no PostgreSQL Docker or database `docker-compose` setup is added.
- [x] Configure EF Core database context.
- [x] Configure backend environment settings for DEV, QAS, and PROD.
- [x] Configure frontend environment settings for DEV, QAS, and PROD.
- [x] Configure environment-variable handling for secrets.
- [x] Add backend health-check endpoint for deployment validation.

### 1.2 Frontend Foundation

- [x] Configure Tailwind CSS.
- [x] Configure Prompt as the application font.
- [x] Configure project theme tokens using Emerald as the primary color.
- [x] Define secondary, success, warning, danger, background, surface, text, and border theme values.
- [x] Create responsive application shell for mobile, tablet, and desktop.
- [x] Create shared page-header component.
- [x] Create shared confirmation dialog component.
- [x] Create shared status badge component for training statuses.
- [x] Create shared loading indicator component.
- [x] Create shared empty-state component.
- [x] Create shared error-state component.
- [x] Create shared pagination component or reusable pagination pattern.
- [x] Create shared search/filter toolbar following `skill.md` listing rules.
- [x] Configure a consistent free icon library supported by `skill.md`.
- [x] Confirm all initial UI labels, buttons, and messages are in Thai.

### 1.3 Backend Foundation

- [x] Configure consistent API success response handling.
- [x] Configure consistent API validation/error response handling.
- [x] Create global exception-handling middleware.
- [x] Configure structured application logging.
- [x] Add current-user context helper for user ID and roles.
- [x] Add shared pagination request/response DTOs.
- [x] Add shared date-range filter DTO where needed by dashboards and reports.
- [x] Enable nullable reference type checks.

---

## 2. Database Design

### 2.1 Identity and Access Data

- [x] Create User entity with explicit `UserId` primary key and common audit columns.
- [x] Create Role entity with explicit `RoleId` primary key and common audit columns.
- [x] Create UserRole entity for user-to-role assignments.
- [x] Add active/inactive state needed for user access management.
- [x] Add unique constraints required for user identity fields.

### 2.2 Coach Data

- [x] Create Coach entity with explicit `CoachId` primary key and common audit columns.
- [x] Add unique Coach Code constraint.
- [x] Add optional User-to-Coach relationship.
- [x] Add coach profile fields defined in `requirement.md`.
- [x] Preserve inactive coaches through soft delete/status rules rather than destructive deletion.

### 2.3 Athlete Data

- [x] Create Athlete entity with explicit `AthleteId` primary key and common audit columns.
- [x] Add unique Athlete Code constraint.
- [x] Add athlete profile fields defined in `requirement.md`.
- [x] Preserve inactive athletes through soft delete/status rules rather than destructive deletion.

### 2.4 Routine Schedule Data

- [x] Create RoutineSchedule entity with explicit `RoutineScheduleId` primary key and common audit columns.
- [x] Add coach relationship to RoutineSchedule.
- [x] Add recurrence, weekday, scheduled time, effective date, status, and remarks fields.
- [x] Confirm RoutineSchedule contains no Group field.
- [x] Confirm RoutineSchedule contains no Team field.
- [x] Confirm RoutineSchedule contains no Location field.
- [x] Add fields required to preserve recurring-schedule changes without modifying completed session history.

### 2.5 Training Session Data

- [x] Create TrainingSession entity with explicit `TrainingSessionId` primary key and common audit columns.
- [x] Add Training Type controlled value for Routine and Private.
- [x] Add Session Status controlled values defined in `requirement.md`.
- [x] Add scheduled start and end date/time fields.
- [x] Add actual start and end date/time fields.
- [x] Add originally assigned Coach relationship.
- [x] Add actual Coach relationship.
- [x] Add optional RoutineSchedule relationship.
- [x] Add optional Private Training location field.
- [x] Add cancellation reason fields.
- [x] Add reschedule relationship to the original TrainingSession.
- [x] Add fields required to identify conflict overrides and override reasons when used.
- [x] Add historical coach identity snapshot fields required by `NFR-002`.

### 2.6 Private Athlete Assignment Data

- [x] Create PrivateSessionAthlete entity linking Private Training sessions to assigned athletes.
- [x] Add unique constraint preventing duplicate athlete assignment within one Private Training session.
- [x] Add historical athlete identity snapshot fields required by `NFR-002`.

### 2.7 Attendance Data

- [x] Create Attendance entity linked to TrainingSession and Athlete.
- [x] Add attendance status controlled values required for Routine and Private Training.
- [x] Add arrival time field.
- [x] Add attendance remark field.
- [x] Add recorded-by and recorded-date audit data.
- [x] Add unique constraint preventing duplicate attendance for the same athlete and session.
- [x] Add historical athlete identity snapshot fields required by `NFR-002`.

### 2.8 Training Log Data

- [x] Create TrainingLog entity linked to TrainingSession.
- [x] Add training topic field.
- [x] Add training objective field.
- [x] Add exercise/drill field.
- [x] Add training focus field.
- [x] Add training intensity field.
- [x] Add coach notes field.
- [x] Add athlete notes field.
- [x] Add general remarks field.

### 2.9 Substitute Coach Data

- [x] Create CoachSubstitutionHistory entity linked to TrainingSession.
- [x] Store original coach, substitute coach, substitution reason, action user, and action date.
- [x] Preserve all substitution history for audit and reporting.

### 2.10 Approval and Audit Data

- [x] Create TrainingApprovalHistory entity linked to TrainingSession.
- [x] Store submit, approve, reject, request-revision, and unlock actions.
- [x] Store action reason/comment where required.
- [x] Store action user and action date/time.
- [x] Create AuditLog entity for material business changes required by `FR-AUDIT-001`–`003`.
- [x] Store previous/new business values where needed for traceability.

### 2.11 Conflict Override Data

- [x] Create conflict-override history structure for authorized schedule overrides.
- [x] Store conflict type, affected session/schedule, override reason, action user, and action date.

### 2.12 Database Constraints and Indexes

- [x] Add foreign-key constraints for all required relationships.
- [x] Add indexes for coach/date session queries.
- [x] Add indexes for athlete/date attendance queries.
- [x] Add indexes for session status queries.
- [x] Add indexes for routine recurrence lookup.
- [x] Add indexes supporting dashboard date-range queries.
- [x] Add indexes supporting report date-range filters.
- [x] Apply soft-delete behavior to important business entities.
- [x] Configure default filtering so soft-deleted data is excluded from active lists but remains available to historical references.

### 2.13 Migrations

- [x] Create initial identity/access migration.
- [x] Create coach and athlete migration.
- [x] Create RoutineSchedule migration.
- [x] Create TrainingSession and PrivateSessionAthlete migration.
- [x] Create Attendance and TrainingLog migration.
- [x] Create substitution, approval, conflict-override, and audit migration.
- [x] Apply migrations to DEV database.
- [x] Verify all migration scripts against PostgreSQL.
- [x] Verify migration rollback/recovery approach before QAS deployment.

---

## 3. Authentication

### 3.1 Backend Authentication

- [x] Create login request and response DTOs.
- [x] Create authentication service.
- [x] Create JWT token service.
- [x] Create `AuthController` with explicit login route.
- [x] Add JWT Bearer authentication configuration.
- [x] Add role claims for Administrator, Coach, and Management / Viewer.
- [x] Add backend role-based authorization policies.
- [x] Return `401` for authentication failures.
- [x] Return `403` for permission failures.

### 3.2 User and Role Management Backend

- [x] Create user list/detail DTOs.
- [x] Create user create/update DTOs.
- [x] Create role assignment DTOs.
- [x] Create User service.
- [x] Create User controller with explicit routes.
- [x] Add API to list users with search/filter/pagination.
- [x] Add API to create user access.
- [x] Add API to update user access.
- [x] Add API to activate/deactivate user access.
- [x] Add API to assign/remove authorized roles.
- [x] Add API to link/unlink a Coach to a user account.
- [x] Prevent unauthorized role changes.

### 3.3 Frontend Authentication

- [x] Create login page.
- [x] Create authentication service.
- [x] Create authentication state model.
- [x] Create JWT interceptor.
- [x] Create authentication route guard.
- [x] Create role/permission route guard.
- [x] Create access-denied page.
- [x] Add logout action.
- [x] Hide navigation items the signed-in user is not authorized to access.
- [ ] Redirect Coach users to Coach Home after login.
- [ ] Redirect Administrator users to Administrator Dashboard after login.

### 3.4 User and Role Management Frontend

- [x] Create user management list page.
- [x] Add user search/filter/pagination.
- [x] Add loading, empty, and error states to user list.
- [x] Create user create/edit form.
- [x] Create role assignment UI.
- [x] Create optional Coach-account linking UI.
- [x] Add form validation and processing state.
- [x] Add activate/deactivate confirmation flow.
- [x] Make user management pages responsive.

---

## 4. Backend Modules

### 4.1 Coach Management Backend

- [x] Create Coach request/response DTOs (`FR-COACH-001`–`004`).
- [x] Create Coach service.
- [x] Create Coach controller with explicit routes.
- [x] Add API to list coaches with search/filter/pagination.
- [x] Add API to get coach detail.
- [x] Add API to create coach.
- [x] Add API to update coach.
- [x] Add API to activate/deactivate coach.
- [x] Validate Coach Code uniqueness.
- [x] Prevent inactive coaches from new active schedule assignment.
- [x] Preserve historical records when coach profile/status changes.

### 4.2 Athlete Management Backend

- [x] Create Athlete request/response DTOs (`FR-ATHLETE-001`–`004`).
- [x] Create Athlete service.
- [x] Create Athlete controller with explicit routes.
- [x] Add API to list athletes with search/filter/pagination.
- [x] Add API to get athlete detail.
- [x] Add API to create athlete.
- [x] Add API to update athlete.
- [x] Add API to activate/deactivate athlete.
- [x] Validate Athlete Code uniqueness.
- [x] Add API to search active athletes for Routine attendance selection.
- [x] Add API to search active athletes for Private Training assignment.
- [x] Preserve historical attendance when athlete profile/status changes.

### 4.3 Routine Training Backend

- [x] Create RoutineSchedule request/response DTOs (`FR-ROUTINE-001`–`009`).
- [x] Create RoutineSchedule service.
- [x] Create RoutineSchedule controller with explicit routes.
- [x] Add API to list routine schedules with search/filter/pagination.
- [x] Add API to get routine schedule detail.
- [x] Add API to create routine schedule.
- [x] Add API to update future routine schedule configuration.
- [x] Add API to activate/deactivate routine schedule.
- [x] Validate required coach, recurrence, start time, end time, and effective start date.
- [x] Validate optional effective end date against start date.
- [x] Validate Routine schedule coach conflicts before save.
- [x] Implement routine occurrence/session generation for applicable recurrence dates.
- [x] Ensure recurring-schedule edits do not modify completed historical sessions.
- [x] Ensure generated Routine sessions are identified as Routine throughout queries and reports.
- [x] Confirm Routine APIs do not expose Group, Team, or Location fields.

### 4.4 Private Training Backend

- [x] Create Private Training request/response DTOs (`FR-PRIVATE-001`–`009`).
- [x] Create Private Training service.
- [x] Create Private Training controller with explicit routes.
- [x] Add API to list Private Training sessions with search/filter/pagination.
- [x] Add API to get Private Training detail.
- [x] Add API to create Private Training session.
- [x] Add API to update future Private Training session.
- [x] Validate exactly one assigned coach.
- [x] Validate at least one assigned athlete.
- [x] Support multiple athlete assignments.
- [x] Validate coach conflict before create/update.
- [x] Validate athlete conflicts before create/update.
- [x] Support optional location without making it mandatory.
- [x] Reject duplicate athlete assignments in one session.

### 4.5 Training Session Backend

- [x] Create TrainingSession list/detail DTOs (`FR-SESSION-001`–`010`).
- [x] Create TrainingSession service.
- [x] Create TrainingSession controller with explicit routes.
- [x] Add API to retrieve session detail.
- [x] Add API to retrieve sessions by date range.
- [x] Add API to retrieve sessions by training type.
- [x] Add API to retrieve sessions by coach.
- [x] Add API to retrieve sessions by status.
- [x] Preserve scheduled time separately from actual teaching time.
- [x] Preserve assigned coach separately from actual coach.
- [x] Link rescheduled replacement sessions to original sessions. <!-- OriginalSessionId FK + DTO exposure; set by Rescheduling (4.13) -->
- [x] Exclude non-completed sessions from completed teaching-hour calculations. <!-- ISessionStatusService.CountsAsCompletedTeaching, for Reports (4.18) to consume -->
- [x] Keep cancelled and rescheduled-original sessions queryable in history.

### 4.6 Session Status Backend

- [x] Define controlled Session Status values from `requirement.md`.
- [x] Create centralized session-status transition rules (`FR-STATUS-001`–`005`).
- [x] Prevent invalid status transitions.
- [x] Prevent Coach edits to Locked sessions. <!-- IsEditableByCoach rule defined/tested; enforced by write actions once Coach Teaching (4.7) exists -->
- [x] Prevent Cancelled sessions from completion unless restored by an authorized user.
- [x] Exclude rescheduled-original sessions from completed-session totals.
- [x] Handle Coach Absent status without teaching-hour credit unless a substitute completes the session.

### 4.7 Coach Teaching Record Backend

- [x] Create teaching start/check-in request DTO.
- [x] Create teaching end/check-out request DTO.
- [x] Create Coach Teaching service (`FR-TEACH-001`–`006`).
- [x] Add API to record actual teaching start.
- [x] Add API to record actual teaching end.
- [x] Calculate actual teaching duration from recorded actual time.
- [x] Reject negative or invalid actual duration.
- [x] Associate actual teaching record with actual coach.
- [ ] Validate required session information before completion/submission. <!-- start-before-complete ordering enforced; attendance/log completeness waits on 4.8–4.10, final submit-gate on 4.15 -->
- [x] Preserve scheduled values when actual values are recorded.

### 4.8 Routine Attendance Backend

- [x] Create Routine Attendance request/response DTOs (`FR-RATT-001`–`006`).
- [x] Create Routine Attendance service.
- [x] Add API to list recorded Routine attendance for a session.
- [x] Add API to add an active athlete to Routine attendance.
- [x] Add API to update Routine attendance status/details while editable.
- [x] Add API to remove an attendance entry while session is editable.
- [x] Restrict Routine attendance statuses to Present and Late.
- [x] Validate optional arrival time for Late attendance.
- [x] Prevent duplicate athlete attendance within one Routine session.
- [x] Do not generate absent records for unselected athletes.
- [x] Do not require a pre-assigned Routine athlete roster.

### 4.9 Private Attendance Backend

- [x] Create Private Attendance request/response DTOs (`FR-PATT-001`–`005`).
- [x] Create Private Attendance service.
- [x] Add API to retrieve all assigned athletes and their attendance state.
- [x] Add API to record/update attendance for an assigned athlete.
- [x] Support Present, Absent, Late, and Leave / Excused statuses.
- [ ] Validate attendance status for every assigned athlete before final submission. <!-- IsComplete/RosterComplete flag exposed now; the actual submit-time block belongs to Approval (4.15) -->
- [x] Validate optional arrival time for Late attendance.
- [x] Allow remarks for Absent and Leave / Excused records.
- [x] Prevent duplicate attendance records for the same assigned athlete.
- [x] Prevent attendance creation for an athlete not assigned to the Private session.

### 4.10 Training Log Backend

- [x] Create TrainingLog request/response DTOs (`FR-LOG-001`–`004`).
- [x] Create TrainingLog service.
- [x] Add API to retrieve a session training log.
- [x] Add API to create/update a training log while session is editable.
- [x] Preserve training log in historical session detail.
- [x] Block training log changes after session becomes non-editable.

### 4.11 Substitute Coach Backend

- [ ] Create substitute-coach request/response DTOs (`FR-SUB-001`–`005`).
- [ ] Create Substitute Coach service.
- [ ] Add API to assign a substitute coach.
- [ ] Require substitution reason.
- [ ] Validate substitute coach is active.
- [ ] Validate substitute coach schedule conflicts.
- [ ] Preserve original assigned coach.
- [ ] Set/report actual coach without overwriting original assignment.
- [ ] Record substitution history.

### 4.12 Cancellation Backend

- [ ] Create cancellation request DTO (`FR-CR-001`–`003`).
- [ ] Add service operation to cancel future/uncompleted sessions.
- [ ] Add API to cancel a session.
- [ ] Require cancellation reason.
- [ ] Prevent cancellation of records that are no longer eligible for cancellation.
- [ ] Preserve cancelled sessions in historical queries.
- [ ] Exclude cancelled sessions from completed teaching-hour totals.
- [ ] Record cancellation audit history.

### 4.13 Rescheduling Backend

- [ ] Create reschedule request DTO (`FR-CR-004`–`007`).
- [ ] Add service operation to reschedule eligible sessions.
- [ ] Add API to reschedule a session.
- [ ] Validate new date/time.
- [ ] Validate coach conflict for replacement schedule.
- [ ] Validate assigned athlete conflicts for Private replacement schedule.
- [ ] Preserve original session as rescheduled history.
- [ ] Link replacement session to original session.
- [ ] Prevent original and replacement sessions from double-counting reports.
- [ ] Record rescheduling audit history.

### 4.14 Schedule Conflict Backend

- [x] Create reusable schedule conflict service (`FR-CONFLICT-001`–`005`). <!-- IScheduleConflictService; grows athlete/private-vs-routine checks with 4.4 -->
- [x] Detect overlapping sessions for the same coach.
- [x] Detect overlapping Private sessions for the same athlete.
- [x] Detect Private Training overlap with the coach's Routine Training. <!-- CheckCoachOverlapAsync queries all TrainingSessions regardless of type -->
- [x] Reuse conflict validation for routine create/update.
- [x] Reuse conflict validation for private create/update.
- [ ] Reuse conflict validation for substitution.
- [ ] Reuse conflict validation for rescheduling.
- [ ] Add authorized conflict-override operation where permitted.
- [ ] Require conflict-override reason.
- [ ] Record conflict-override history.

### 4.15 Approval and Locking Backend

- [ ] Create submission/approval action DTOs (`FR-APPROVAL-001`–`007`).
- [ ] Create Training Approval service.
- [ ] Add API for Coach to submit a Completed record.
- [ ] Add API for Administrator to approve a submitted record.
- [ ] Add API for Administrator to reject a submitted record.
- [ ] Add API for Administrator to request revision.
- [ ] Add API for authorized Administrator to unlock a Locked record.
- [ ] Require unlock reason.
- [ ] Change approved records to Locked state.
- [ ] Enforce edit restrictions for Locked records.
- [ ] Record every approval workflow action in history.

### 4.16 Coach Dashboard Backend

- [ ] Create Coach Dashboard response DTO (`FR-CDASH-001`–`003`).
- [ ] Create Coach Dashboard service.
- [ ] Add API for Coach today's sessions.
- [ ] Add API for Coach upcoming sessions.
- [ ] Add Coach completed/remaining session summary.
- [ ] Add Routine/Private session summary.
- [ ] Add pending/incomplete record summary.
- [ ] Add monthly teaching-hour summary.
- [ ] Restrict dashboard data to the signed-in Coach.

### 4.17 Administrator Dashboard Backend

- [ ] Create Administrator Dashboard response DTO (`FR-ADASH-001`–`004`).
- [ ] Create Administrator Dashboard service.
- [ ] Add session KPI summary by date/date range.
- [ ] Add completed/upcoming/cancelled counts.
- [ ] Add Routine/Private counts.
- [ ] Add Coaches Teaching Today summary.
- [ ] Add athlete attendance summary.
- [ ] Add coach teaching-hour summary.
- [ ] Add coach filter support.
- [ ] Add training-type filter support.
- [ ] Include identifiers needed to open related operational records.

### 4.18 Coach Teaching-Hour Report Backend

- [ ] Create Coach Teaching-Hour report filter DTO (`FR-RPT-COACH-001`–`009`).
- [ ] Create Coach Teaching-Hour report response DTO.
- [ ] Create Coach Teaching-Hour report service.
- [ ] Add report API with coach filter.
- [ ] Add report API date-range filtering.
- [ ] Add report API training-type filtering.
- [ ] Calculate Routine teaching hours.
- [ ] Calculate Private teaching hours.
- [ ] Calculate total teaching hours.
- [ ] Credit teaching time to actual coach after substitution.
- [ ] Exclude Cancelled sessions.
- [ ] Exclude rescheduled-original sessions.
- [ ] Exclude Coach Absent sessions unless substitute completion qualifies.
- [ ] Use finalized session records according to approval rules.

### 4.19 Athlete Attendance Report Backend

- [ ] Create Athlete Attendance report filter DTO (`FR-RPT-ATH-001`–`007`).
- [ ] Create Athlete Attendance report response DTO.
- [ ] Create Athlete Attendance report service.
- [ ] Add report API with athlete filter.
- [ ] Add report API date-range filtering.
- [ ] Distinguish Routine and Private attendance in report results.
- [ ] Calculate Private attendance status counts.
- [ ] Report only explicitly recorded Routine attendance.
- [ ] Do not infer Routine absence from non-selection.
- [ ] Exclude duplicate counts caused by rescheduled-original sessions.

### 4.20 History and Audit Backend

- [ ] Create audit/history response DTOs (`FR-AUDIT-001`–`003`).
- [ ] Create Audit service.
- [ ] Add API to retrieve session business history.
- [ ] Add API to retrieve approval history.
- [ ] Add API to retrieve substitution history.
- [ ] Add API to retrieve conflict-override history.
- [ ] Capture schedule creation/change events.
- [ ] Capture attendance change events.
- [ ] Capture completion/submission events.
- [ ] Capture cancellation/rescheduling events.
- [ ] Preserve history when coach or athlete becomes inactive.

---

## 5. Frontend Modules

### 5.1 Coach Management Frontend

- [x] Create Coach list page.
- [x] Add Coach search/filter/pagination.
- [x] Add Coach list loading state.
- [x] Add Coach list empty state.
- [x] Add Coach list error state.
- [x] Create Coach create form.
- [x] Create Coach edit form.
- [x] Add Coach Code uniqueness error feedback.
- [x] Add Coach activate/deactivate action.
- [x] Add Coach-account linking control when applicable. <!-- Read-only display in Coach form; the link itself stays owned by User & Role Management (todo.md 3.4) per CoachDetailDto -->
- [x] Make Coach list and forms responsive.

### 5.2 Athlete Management Frontend

- [x] Create Athlete list page.
- [x] Add Athlete search/filter/pagination.
- [x] Add Athlete list loading state.
- [x] Add Athlete list empty state.
- [x] Add Athlete list error state.
- [x] Create Athlete create form.
- [x] Create Athlete edit form.
- [x] Add Athlete Code uniqueness error feedback.
- [x] Add Athlete activate/deactivate action.
- [x] Make Athlete list and forms responsive.

### 5.3 Routine Training Frontend

- [x] Create Routine Schedule list page.
- [x] Add Routine Schedule search/filter/pagination.
- [x] Add loading, empty, and error states to Routine Schedule list.
- [x] Create Routine Schedule create form.
- [x] Create Routine Schedule edit form.
- [x] Add Coach selector using active coaches only.
- [x] Add recurrence/day-of-week inputs.
- [x] Add start/end time inputs.
- [x] Add effective start/end date inputs.
- [x] Add active/inactive control.
- [x] Add schedule-conflict feedback. <!-- 409 response (RoutineScheduleSaveResult.conflicts) rendered as a message list in the form -->
- [ ] Add authorized conflict-override UI only when backend permits override. <!-- Backend has no override operation yet (todo.md 4.14) — UI intentionally deferred until it exists -->
- [ ] Require override reason when override is used. <!-- Same dependency as above -->
- [x] Confirm Routine Schedule UI has no Group field.
- [x] Confirm Routine Schedule UI has no Team field.
- [x] Confirm Routine Schedule UI has no Location field.
- [x] Make Routine Schedule pages responsive.

### 5.4 Private Training Frontend

- [x] Create Private Training list page.
- [x] Add Private Training search/filter/pagination.
- [x] Add loading, empty, and error states to Private Training list.
- [x] Create Private Training create form.
- [x] Create Private Training edit form.
- [x] Add active Coach selector.
- [x] Add searchable multi-athlete selector.
- [x] Prevent duplicate athlete selection.
- [x] Add date/start/end time inputs.
- [x] Add optional location input.
- [x] Add remarks input.
- [x] Display coach conflict feedback. <!-- 409 response (ConflictDetail → ApiFieldError) rendered as a message list in the form -->
- [x] Display athlete conflict feedback. <!-- Same 409 message list; backend does not yet distinguish coach vs athlete conflicts beyond the field name -->
- [ ] Add authorized conflict-override UI only when backend permits override. <!-- Backend has no override operation yet (todo.md 4.14) — UI intentionally deferred until it exists -->
- [ ] Require override reason when override is used. <!-- Same dependency as above -->
- [x] Make Private Training pages responsive.

### 5.5 Coach Home Frontend

- [ ] Create Coach Home page (`FR-CDASH-001`–`003`).
- [ ] Create Today's Sessions section.
- [ ] Create Upcoming Sessions section.
- [ ] Display Routine/Private type for each session.
- [ ] Display scheduled time and current status.
- [ ] Display required next action for actionable sessions.
- [ ] Add completed/remaining session summary.
- [ ] Add monthly teaching-hour summary.
- [ ] Add pending/incomplete training-record summary.
- [ ] Add direct navigation from session card/list item to session detail.
- [ ] Add loading state.
- [ ] Add empty state when no sessions are scheduled.
- [ ] Add API error state and retry action.
- [ ] Optimize Coach Home for mobile-first use.

### 5.6 Coach Session Frontend

- [ ] Create shared Coach Session page for Routine and Private sessions.
- [ ] Create Session Summary section.
- [ ] Display scheduled start/end separately from actual start/end.
- [ ] Display assigned coach separately from actual coach.
- [ ] Add Start/Check-in action.
- [ ] Add End/Check-out action.
- [ ] Display calculated actual teaching duration.
- [ ] Disable invalid actions based on current session status.
- [ ] Add Attendance section appropriate to training type.
- [ ] Add Training Log section.
- [ ] Add Complete action.
- [ ] Add Submit action when applicable.
- [ ] Display validation errors blocking completion/submission.
- [ ] Display locked/read-only state for finalized records.
- [ ] Add loading and API error states.
- [ ] Make Coach Session flow responsive and optimized for mobile.

### 5.7 Routine Attendance Frontend

- [ ] Create Routine Attendance component.
- [ ] Add athlete search from active Athlete Master.
- [ ] Add selected athlete to attendance list.
- [ ] Prevent already-added athletes from duplicate selection.
- [ ] Add Present status option.
- [ ] Add Late status option.
- [ ] Add arrival-time input for Late status.
- [ ] Add attendance remark input.
- [ ] Add editable attendance removal while session is editable.
- [ ] Do not render a fixed roster.
- [ ] Do not label unselected athletes as Absent.
- [ ] Add saving/loading/error feedback.
- [ ] Optimize attendance controls for touch/mobile use.

### 5.8 Private Attendance Frontend

- [ ] Create Private Attendance component.
- [ ] Display all athletes assigned to the Private session.
- [ ] Add Present status option.
- [ ] Add Absent status option.
- [ ] Add Late status option.
- [ ] Add Leave / Excused status option.
- [ ] Add arrival-time input for Late status.
- [ ] Add remark input for Absent and Leave / Excused.
- [ ] Highlight assigned athletes missing attendance status.
- [ ] Prevent final submission until all assigned athletes have attendance status.
- [ ] Add saving/loading/error feedback.
- [ ] Optimize attendance controls for touch/mobile use.

### 5.9 Training Log Frontend

- [ ] Create Training Log form component.
- [ ] Add Training Topic input.
- [ ] Add Training Objective input.
- [ ] Add Exercise / Drill input.
- [ ] Add Training Focus input.
- [ ] Add Training Intensity input.
- [ ] Add Coach Notes input.
- [ ] Add Athlete Notes input.
- [ ] Add General Remarks input.
- [ ] Disable editing when session is non-editable.
- [ ] Add loading/saving/error feedback.
- [ ] Make Training Log form responsive.

### 5.10 Substitute Coach Frontend

- [ ] Add substitute-coach action to eligible session screens.
- [ ] Create substitute-coach dialog/form.
- [ ] Add active substitute Coach selector.
- [ ] Require substitution reason.
- [ ] Display schedule-conflict validation feedback.
- [ ] Display original assigned coach after substitution.
- [ ] Display actual/substitute coach after substitution.
- [ ] Display substitution history in session detail for authorized users.

### 5.11 Cancellation Frontend

- [ ] Add Cancel action to eligible session screens.
- [ ] Create cancellation confirmation dialog.
- [ ] Require cancellation reason.
- [ ] Display Cancelled status clearly after success.
- [ ] Remove invalid operational actions from Cancelled sessions.
- [ ] Add cancellation API loading/error feedback.

### 5.12 Rescheduling Frontend

- [ ] Add Reschedule action to eligible session screens.
- [ ] Create reschedule form/dialog.
- [ ] Add new date/start/end time inputs.
- [ ] Display coach conflict feedback.
- [ ] Display athlete conflict feedback for Private Training.
- [ ] Display authorized conflict-override controls when permitted.
- [ ] Require override reason when override is used.
- [ ] Display link/reference between original and replacement sessions.
- [ ] Add reschedule API loading/error feedback.

### 5.13 Administrative Review Frontend

- [ ] Create Submitted Training Records list page.
- [ ] Add review-list search/filter/pagination.
- [ ] Add loading, empty, and error states to review list.
- [ ] Create Administrative Review detail page.
- [ ] Display scheduled teaching information.
- [ ] Display actual teaching information.
- [ ] Display assigned coach and actual coach.
- [ ] Display athlete attendance.
- [ ] Display training log.
- [ ] Display session status.
- [ ] Display submission/approval history.
- [ ] Add Approve action.
- [ ] Add Reject action.
- [ ] Add Request Revision action.
- [ ] Add Unlock action for authorized Administrator.
- [ ] Require unlock reason.
- [ ] Refresh displayed status after workflow action.
- [ ] Make review pages responsive.

### 5.14 Administrator Dashboard Frontend

- [ ] Create Administrator Dashboard page (`FR-ADASH-001`–`004`).
- [ ] Create Training Sessions Today KPI card.
- [ ] Create Completed Sessions KPI card.
- [ ] Create Upcoming Sessions KPI card.
- [ ] Create Cancelled Sessions KPI card.
- [ ] Create Routine Sessions KPI card.
- [ ] Create Private Sessions KPI card.
- [ ] Create Coaches Teaching Today summary component.
- [ ] Create Athlete Attendance Summary component.
- [ ] Create Coach Teaching Hours summary component.
- [ ] Add date/date-range filter.
- [ ] Add Coach filter.
- [ ] Add Training Type filter.
- [ ] Add navigation from applicable dashboard items to related records.
- [ ] Add loading state for dashboard data.
- [ ] Add empty state where summary data is unavailable.
- [ ] Add dashboard API error state and retry action.
- [ ] Make dashboard responsive without horizontal overflow.

### 5.15 Coach Teaching-Hour Report Frontend

- [ ] Create Coach Teaching-Hour Report page.
- [ ] Add Coach filter.
- [ ] Add date-range filter.
- [ ] Add Training Type filter.
- [ ] Display Routine teaching hours.
- [ ] Display Private teaching hours.
- [ ] Display total teaching hours.
- [ ] Distinguish actual coach from originally assigned coach where relevant.
- [ ] Add report loading state.
- [ ] Add report empty state.
- [ ] Add report error state.
- [ ] Make report layout responsive.

### 5.16 Athlete Attendance Report Frontend

- [ ] Create Athlete Attendance Report page.
- [ ] Add Athlete filter.
- [ ] Add date-range filter.
- [ ] Distinguish Routine and Private attendance records.
- [ ] Display Private attendance status counts.
- [ ] Display Routine recorded-attendance history without inferred absence.
- [ ] Add report loading state.
- [ ] Add report empty state.
- [ ] Add report error state.
- [ ] Make report layout responsive.

### 5.17 History and Audit Frontend

- [ ] Create session history/timeline component.
- [ ] Display schedule changes.
- [ ] Display cancellation/rescheduling history.
- [ ] Display substitute-coach history.
- [ ] Display attendance change history when authorized.
- [ ] Display completion/submission/approval history.
- [ ] Display conflict-override history.
- [ ] Display responsible user and action date/time.
- [ ] Add loading and error states for history data.
- [ ] Make history/timeline responsive.

---

## 6. Realtime Features

- [ ] Confirm the initial `requirement.md` contains no realtime business requirement before adding realtime feature code.
- [ ] Keep SignalR hubs and realtime client subscriptions out of the initial implementation unless a realtime requirement is formally added.
- [ ] If realtime scope is later approved, use SignalR and keep hub logic separate from business services as required by `skill.md`.

---

## 7. Validation and Error Handling

### 7.1 Shared Backend Validation

- [ ] Validate all required DTO fields before business processing.
- [ ] Validate null values for optional relationships and optional fields.
- [ ] Validate scheduled end time is later than scheduled start time.
- [ ] Validate actual end time is later than actual start time.
- [x] Reject negative actual teaching duration.
- [ ] Validate effective Routine end date is not earlier than effective start date.
- [ ] Validate active Coach selection for new schedules and substitutions.
- [ ] Validate active Athlete selection for new Private assignments and Routine attendance.
- [ ] Validate unique Coach Code.
- [ ] Validate unique Athlete Code.
- [ ] Validate no duplicate Private athlete assignment.
- [ ] Validate no duplicate attendance entry per athlete/session.
- [ ] Validate Private Training has at least one athlete.
- [ ] Validate Private attendance completeness before final submission.
- [x] Validate Routine attendance uses only allowed statuses.
- [x] Validate Private attendance uses only allowed statuses.
- [ ] Validate cancellation eligibility and required reason.
- [ ] Validate rescheduling eligibility.
- [ ] Validate substitution reason.
- [ ] Validate conflict-override reason when override is used.
- [ ] Validate unlock reason.
- [x] Validate session status transitions centrally.
- [ ] Validate Locked record edit restrictions in service layer.

### 7.2 Schedule Conflict Validation

- [ ] Add unit-tested overlap rule for coach vs coach schedule.
- [ ] Add unit-tested overlap rule for athlete vs athlete Private schedule.
- [ ] Add unit-tested overlap rule for Private Training vs coach Routine Training.
- [ ] Apply conflict checks consistently on create, update, substitution, and rescheduling.
- [ ] Return conflict responses using appropriate `409 Conflict` behavior where applicable.

### 7.3 API Error Handling

- [ ] Ensure Controllers remain thin and delegate business rules to Services.
- [ ] Add try/catch and required logging for business operations according to `skill.md`.
- [ ] Log API route, controller, service, function, user ID, and safe request context on failures.
- [ ] Prevent raw exception details from reaching users.
- [ ] Return `400` for validation failures.
- [ ] Return `401` for authentication failures.
- [ ] Return `403` for permission failures.
- [ ] Return `404` for missing resources.
- [ ] Return `409` for business conflicts where applicable.
- [ ] Return `500` for unexpected server failures.
- [ ] Use the standard application response body defined in `skill.md`.

### 7.4 Frontend Form Validation

- [ ] Add required-field indicators to all required fields.
- [ ] Add inline validation messages to all forms.
- [ ] Disable submit/action buttons while processing.
- [ ] Display backend validation messages in the relevant form context.
- [ ] Display clear success feedback after successful create/update/actions.
- [ ] Display clear error feedback after failed create/update/actions.
- [ ] Prevent duplicate form submission.

---

## 8. Security

### 8.1 Authorization

- [ ] Enforce Administrator-only access to administrative master-data changes.
- [ ] Enforce Administrator-only approval/review actions unless another role is explicitly authorized.
- [ ] Enforce authorized-user checks for cancellation, rescheduling, substitution, and conflict override.
- [x] Restrict Coach operational APIs to sessions relevant to the signed-in Coach.
- [ ] Restrict Coach history and teaching-hour APIs to the signed-in Coach unless elevated permission exists.
- [ ] Keep Management / Viewer operations read-only.
- [ ] Enforce permission checks in backend services/controllers; do not rely only on frontend guards.

### 8.2 Data and Transport Security

- [ ] Require HTTPS in deployed environments.
- [ ] Keep JWT signing keys and database credentials outside source control.
- [ ] Validate all incoming input on the backend.
- [ ] Verify EF/database access does not use unsafe raw SQL patterns.
- [ ] Verify rendered user-entered text is handled safely against XSS.
- [ ] Avoid exposing internal exception details, secrets, or sensitive configuration in API responses/logs.

### 8.3 Historical Data Protection

- [ ] Use soft delete for important business records.
- [ ] Prevent destructive deletion of coaches with historical teaching records.
- [ ] Prevent destructive deletion of athletes with historical attendance records.
- [ ] Prevent unauthorized modification of Locked training records.
- [ ] Preserve audit history for material finalized-record changes.

---

## 9. Testing

### 9.1 Backend Unit Tests

- [x] Test AuthService login (correct credentials, wrong password, inactive account, unknown username).
- [x] Test JwtTokenService issues correct identity/role claims.
- [x] Test User username/email uniqueness validation.
- [x] Test Coach Code uniqueness validation.
- [x] Test Athlete Code uniqueness validation.
- [x] Test Routine recurrence/session generation.
- [x] Test completed Routine history remains unchanged after recurring-schedule edits.
- [x] Test coach schedule conflict detection.
- [x] Test athlete Private schedule conflict detection.
- [x] Test Private-vs-Routine coach conflict detection.
- [ ] Test conflict override requires authorization and reason.
- [x] Test actual teaching duration calculation.
- [x] Test invalid/negative actual duration rejection.
- [x] Test Routine attendance duplicate prevention.
- [x] Test Routine attendance never infers absent athletes.
- [x] Test Private attendance duplicate prevention.
- [x] Test Private attendance completeness before submission. <!-- completeness computation tested; the submission trigger itself is Approval (4.15) -->
- [ ] Test substitute coach preserves original coach.
- [ ] Test substitute coach receives actual teaching-hour credit.
- [x] Test cancellation exclusion from teaching hours.
- [ ] Test rescheduled-original exclusion from teaching hours and attendance totals. <!-- teaching-hours half tested; attendance-totals half waits on Attendance (4.8/4.9) -->
- [x] Test Coach Absent teaching-hour rule.
- [x] Test session status transition rules.
- [x] Test Locked record edit restrictions.
- [ ] Test approval/reject/revision/unlock history creation.
- [ ] Test coach dashboard data is scoped to signed-in Coach.
- [ ] Test teaching-hour report filters and totals.
- [ ] Test athlete attendance report filters and totals.
- [ ] Test historical identity is preserved after coach/athlete master changes.

### 9.2 Backend API Tests

- [ ] Test authentication success and failure responses.
- [ ] Test `401` response for unauthenticated protected APIs.
- [ ] Test `403` response for unauthorized role actions.
- [ ] Test Coach CRUD APIs.
- [ ] Test Athlete CRUD APIs.
- [ ] Test Routine Schedule APIs.
- [ ] Test Private Training APIs.
- [ ] Test Training Session APIs.
- [ ] Test Routine Attendance APIs.
- [ ] Test Private Attendance APIs.
- [ ] Test Training Log APIs.
- [ ] Test substitution API.
- [ ] Test cancellation API.
- [ ] Test rescheduling API.
- [ ] Test approval workflow APIs.
- [ ] Test dashboard APIs.
- [ ] Test report APIs.
- [ ] Test audit/history APIs.

### 9.3 Frontend Tests

- [ ] Test login and role-based redirects.
- [ ] Test route guards for Administrator, Coach, and Management / Viewer.
- [ ] Test Coach form validation.
- [ ] Test Athlete form validation.
- [ ] Test Routine Schedule form excludes Group, Team, and Location.
- [ ] Test Routine Schedule conflict feedback.
- [ ] Test Private Training multi-athlete selection.
- [ ] Test Private Training conflict feedback.
- [ ] Test Coach Session action availability by status.
- [ ] Test Routine Attendance athlete add/remove behavior.
- [ ] Test Routine Attendance does not show inferred absence.
- [ ] Test Private Attendance requires status for every assigned athlete.
- [ ] Test Locked session renders read-only.
- [ ] Test Administrative Review actions.
- [ ] Test dashboard filters.
- [ ] Test report filters.
- [ ] Test loading, empty, and error states on major listing/dashboard/report pages.

### 9.4 Responsive and UX Testing

- [ ] Test Coach Home on mobile viewport.
- [ ] Test Coach Session flow on mobile viewport.
- [ ] Test Routine Attendance on mobile viewport.
- [ ] Test Private Attendance on mobile viewport.
- [ ] Test administrative forms on tablet viewport.
- [ ] Test dashboards on desktop, tablet, and mobile.
- [ ] Test reports on desktop, tablet, and mobile.
- [ ] Verify data tables/lists do not require avoidable horizontal scrolling.
- [ ] Verify search input, search button, and filter button remain in one row where required by `skill.md`.

### 9.5 End-to-End Business Scenarios

- [ ] Test Administrator creates Routine schedule and Coach completes/submits a generated Routine session.
- [ ] Test Coach records Routine attendance by selecting only athletes who attended.
- [ ] Test Administrator creates Private session with multiple athletes and Coach records all attendance statuses.
- [ ] Test substitute Coach flow through completion and reporting.
- [ ] Test cancellation flow and reporting exclusion.
- [ ] Test rescheduling flow and duplicate-count prevention.
- [ ] Test approval-to-Locked flow.
- [ ] Test Request Revision and resubmission flow.
- [ ] Test Unlock and correction traceability.
- [ ] Test Management / Viewer can read dashboards/reports without edit actions.

---

## 10. Build Validation

### 10.1 Incremental Validation

- [x] Run `dotnet build` after Project Setup backend changes.
- [x] Run `ng build` after Project Setup frontend changes.
- [x] Run `dotnet build` after Authentication backend implementation.
- [x] Run `ng build` after Authentication frontend implementation.
- [x] Run `dotnet build` after Coach module backend implementation.
- [x] Run `ng build` after Coach module frontend implementation.
- [x] Run `dotnet build` after Athlete module backend implementation.
- [x] Run `ng build` after Athlete module frontend implementation.
- [x] Run `dotnet build` after Routine Training backend implementation.
- [x] Run `ng build` after Routine Training frontend implementation.
- [x] Run `dotnet build` after Private Training backend implementation.
- [x] Run `ng build` after Private Training frontend implementation.
- [x] Run `dotnet build` after Training Session/Teaching backend implementation.
- [ ] Run `ng build` after Coach Session frontend implementation.
- [x] Run `dotnet build` after Attendance/Training Log backend implementation.
- [ ] Run `ng build` after Attendance/Training Log frontend implementation.
- [ ] Run `dotnet build` after substitution/cancellation/rescheduling/conflict implementation.
- [ ] Run `ng build` after substitution/cancellation/rescheduling UI implementation.
- [ ] Run `dotnet build` after approval/audit implementation.
- [ ] Run `ng build` after administrative review/history UI implementation.
- [ ] Run `dotnet build` after dashboard/report backend implementation.
- [ ] Run `ng build` after dashboard/report frontend implementation.

### 10.2 Final Validation

- [ ] Run all backend automated tests.
- [ ] Run all frontend automated tests.
- [ ] Run final `dotnet build` with zero build errors.
- [ ] Run final `ng build` with zero build errors.
- [ ] Run configured SonarQube/SonarCloud or static-analysis checks when available.
- [ ] Fix critical/high quality issues before release candidate completion.
- [ ] Confirm no task is marked complete when its required build validation fails.

---

## 11. Deployment Preparation

### 11.1 Environment Preparation

- [ ] Verify DEV configuration uses DEV database and environment settings.
- [ ] Verify QAS configuration uses QAS database and environment settings.
- [ ] Verify PROD configuration uses PROD database and environment settings.
- [ ] Verify secrets are supplied through environment configuration rather than committed files.
- [ ] Verify HTTPS configuration for deployed environments.
- [ ] Verify frontend API base URLs per environment.
- [ ] Verify JWT configuration per environment.

### 11.2 Database Deployment

- [ ] Review pending EF Core migrations before deployment.
- [ ] Apply migrations to DEV and validate application startup.
- [ ] Apply migrations to QAS and validate application startup.
- [ ] Prepare production migration execution steps.
- [ ] Verify production migration does not require destructive deletion of historical business data.
- [ ] Confirm no PostgreSQL Docker provisioning is included in deployment artifacts.

### 11.3 Automated Deployment Flow

- [ ] Prepare automated dependency-install step.
- [ ] Prepare automated frontend build step.
- [ ] Prepare automated backend build step.
- [ ] Prepare automated migration step with environment safeguards.
- [ ] Prepare automated backend publish/deploy step.
- [ ] Prepare automated frontend deploy step.
- [ ] Prepare service/application restart step where applicable.
- [ ] Prepare post-deployment health-check validation.
- [ ] Block deployment when frontend or backend build fails.
- [ ] Keep DEV, QAS, and PROD deployment paths separated.

### 11.4 Release Readiness

- [ ] Verify Administrator can complete core master-data and scheduling operations in QAS.
- [ ] Verify Coach can complete Routine Training flow in QAS.
- [ ] Verify Coach can complete Private Training flow in QAS.
- [ ] Verify substitute/cancel/reschedule flows in QAS.
- [ ] Verify approval and record-locking flow in QAS.
- [ ] Verify dashboards and reports use correct finalized data in QAS.
- [ ] Verify Management / Viewer access is read-only in QAS.
- [ ] Verify historical/audit records remain accessible in QAS.
- [ ] Verify mobile, tablet, and desktop layouts in QAS.
- [ ] Confirm Out of Scope features have not been included in the release.
