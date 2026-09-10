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
- [x] Show the signed-in username in the mobile header and full account/role details in the mobile navigation drawer.
- [x] Remove the redundant generic Home link from desktop and mobile navigation while retaining role-based root redirects.
- [x] Remove the empty generic Menu heading from the desktop sidebar.
- [x] Configure PostgreSQL connection for the existing database environment.
- [x] Confirm no PostgreSQL Docker or database `docker-compose` setup is added.
- [x] Configure EF Core database context.
- [x] Configure backend environment settings for DEV, QAS, and PROD.
- [x] Configure frontend environment settings for DEV, QAS, and PROD.
- [x] Configure environment-variable handling for secrets.
- [x] Add backend health-check endpoint for deployment validation.

### 1.2 Frontend Foundation

- [x] Persist list, dashboard, and report filters per user with URL query parameters taking priority over sessionStorage.
- [x] Standardize displayed dates and date-picker inputs across the frontend as `dd MMM yyyy` using Thai abbreviated month names and Gregorian years.
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
- [x] Add a persistent calendar color to each Coach.
- [x] Replace Coach Type/Coaching Specialization with bank account reference fields (BankName, BankAccountNumber, BankAccountName) per updated `requirement.md` 4.2, and apply the corresponding EF Core migration to DEV.

### 2.3 Athlete Data

- [x] Create Athlete entity with explicit `AthleteId` primary key and common audit columns.
- [x] Add unique Athlete Code constraint.
- [x] Add athlete profile fields defined in `requirement.md`.
- [x] Add Athlete Type classification for Affiliated Athlete and General Athlete with an EF Core migration.
- [x] Preserve inactive athletes through soft delete/status rules rather than destructive deletion.

### 2.4 Routine Schedule Data

- [x] Create RoutineSchedule entity with explicit `RoutineScheduleId` primary key and common audit columns.
- [x] Add coach relationship to RoutineSchedule.
- [x] Store scheduled time, effective start date, status, and remarks for RoutineSchedule.
- [x] Treat EffectiveStartDate as the single selected Routine Training date without recurrence.
- [x] Remove RoutineSchedule Name, DayOfWeek, EffectiveEndDate, and RecurrencePattern columns with an EF Core migration.
- [x] Create and apply the Coach calendar-color migration to DEV.
- [x] Remove legacy repeated Routine sessions that are still Scheduled and do not match the selected date.
- [x] Confirm RoutineSchedule contains no Group field.
- [x] Confirm RoutineSchedule contains no Team field.
- [x] Confirm RoutineSchedule contains no Location field.
- [x] Preserve completed session history when a schedule configuration changes.

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
- [x] Add indexes for Routine Schedule date lookup.
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
- [x] Apply `RemoveRoutineScheduleManagedColumns` migration to DEV database after explicit destructive-change approval.

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
- [x] Redirect Coach users to Coach Home after login.
- [x] Redirect Administrator users to Administrator Dashboard after login.
- [x] Redirect Management / Viewer users to Administrator Dashboard in read-only mode after login.
- [x] Show a no-role fallback page only when an authenticated account has no supported role.

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
- [x] Add a touch-friendly User card list for mobile while preserving the desktop table.

### 3.5 Password Change and Reset Backend

- [x] Create password change, reset request, and reset confirmation DTOs (`FR-USER-005`–`010`).
- [x] Add secure single-use password-reset token persistence with issued, expiry, used, and invalidated state.
- [x] Add an EF Core migration for password-reset token persistence and required indexes.
- [x] Apply the password-reset token migration to the DEV database.
- [x] Create a password service and keep password business logic out of the controller.
- [x] Add an authenticated API for an account owner to change only the owner's own password.
- [x] Require and verify the current password before changing a password.
- [x] Add a public forgot-password API that always returns a neutral response.
- [x] Send the password-reset link only to the email address registered to the active account.
- [x] Store only a secure hash of the reset token and never persist or log the raw token.
- [x] Set password-reset token expiry to 10 minutes and make each token single use.
- [x] Invalidate older reset tokens when a newer token is issued for the same account.
- [x] Enforce a minimum password length of 8 characters for user creation, password change, and password reset.
- [x] Add a MailKit email sender and HTML reset-password template under `Backend/EmailTemplate`.
- [x] Keep the frontend reset URL and email settings in environment-specific configuration outside source control where sensitive.

### 3.6 Password Change and Reset Frontend

- [x] Create an authenticated change-password page for the signed-in account owner.
- [x] Create a forgot-password page that accepts the account email address.
- [x] Create a reset-password page that accepts the reset token and new password.
- [x] Enforce and clearly display the minimum 8-character password rule.
- [x] Add password confirmation validation, processing state, success feedback, and safe error states.
- [x] Handle invalid, expired, and already-used reset links without exposing account information.
- [x] Make password change and reset screens responsive and use Thai UI text.

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
- [x] Accept and validate Coach calendar color in create/update APIs.

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
- [x] Accept, validate, and return Athlete Type in Athlete management APIs.

### 4.3 Routine Training Backend

- [x] Create RoutineSchedule request/response DTOs (`FR-ROUTINE-001`–`009`).
- [x] Create RoutineSchedule service.
- [x] Create RoutineSchedule controller with explicit routes.
- [x] Add API to list routine schedules with search/filter/pagination.
- [x] Add API to get routine schedule detail.
- [x] Add API to create routine schedule.
- [x] Add API to update future routine schedule configuration.
- [x] Add Administrator API to delete an incorrect untouched Routine Schedule and its Scheduled session while preserving progressed history.
- [x] Add API to activate/deactivate routine schedule.
- [x] Validate required coach, start time, end time, and effective start date.
- [x] Derive Routine Schedule display name from its selected training date without recurrence.
- [x] Validate Routine schedule coach conflicts before save.
- [x] Generate exactly one Routine Training Session for the selected schedule date.
- [x] Ensure schedule edits do not modify completed historical sessions.
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
- [x] Validate required session information before completion/submission. <!-- start-before-complete ordering enforced by CoachTeachingService; Private attendance completeness enforced by TrainingApprovalService.SubmitAsync (4.15) -->
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
- [x] Validate attendance status for every assigned athlete before final submission. <!-- Enforced in TrainingApprovalService.SubmitAsync (4.15) -->
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

- [x] Create substitute-coach request/response DTOs (`FR-SUB-001`–`005`).
- [x] Create Substitute Coach service.
- [x] Add API to assign a substitute coach.
- [x] Require substitution reason.
- [x] Validate substitute coach is active.
- [x] Validate substitute coach schedule conflicts.
- [x] Preserve original assigned coach.
- [x] Set/report actual coach without overwriting original assignment.
- [x] Record substitution history.

### 4.12 Cancellation Backend

- [x] Create cancellation request DTO (`FR-CR-001`–`003`).
- [x] Add service operation to cancel future/uncompleted sessions.
- [x] Add API to cancel a session.
- [x] Require cancellation reason.
- [x] Prevent cancellation of records that are no longer eligible for cancellation.
- [x] Preserve cancelled sessions in historical queries.
- [x] Exclude cancelled sessions from completed teaching-hour totals.
- [x] Record cancellation audit history.

### 4.13 Rescheduling Backend

- [x] Create reschedule request DTO (`FR-CR-004`–`007`).
- [x] Add service operation to reschedule eligible sessions.
- [x] Add API to reschedule a session.
- [x] Validate new date/time. <!-- End-after-start via IValidatableObject, consistent with Routine/Private DTOs -->
- [x] Validate coach conflict for replacement schedule.
- [x] Validate assigned athlete conflicts for Private replacement schedule.
- [x] Preserve original session as rescheduled history. <!-- Original row kept, Status -> Rescheduled -->
- [x] Link replacement session to original session. <!-- Replacement.OriginalSessionId -->
- [x] Prevent original and replacement sessions from double-counting reports. <!-- Rescheduled excluded from ISessionStatusService.CountsAsCompletedTeaching; Athlete Attendance Report (4.19) excludes Rescheduled sessions -->
- [x] Record rescheduling audit history. <!-- AuditLog entry ("Reschedule") on the original TrainingSession -->

### 4.14 Schedule Conflict Backend

- [x] Create reusable schedule conflict service (`FR-CONFLICT-001`–`005`). <!-- IScheduleConflictService; grows athlete/private-vs-routine checks with 4.4 -->
- [x] Detect overlapping sessions for the same coach.
- [x] Detect overlapping Private sessions for the same athlete.
- [x] Detect Private Training overlap with the coach's Routine Training. <!-- CheckCoachOverlapAsync queries all TrainingSessions regardless of type -->
- [x] Reuse conflict validation for routine create/update.
- [x] Reuse conflict validation for private create/update.
- [x] Reuse conflict validation for substitution.
- [x] Reuse conflict validation for rescheduling. <!-- ReschedulingService reuses IScheduleConflictService -->
- [x] Add authorized conflict-override operation where permitted. <!-- OverrideConflict/OverrideReason on Routine/Private create-update and Substitute Coach requests; Administrator-only per existing controller policies -->
- [x] Require conflict-override reason.
- [x] Record conflict-override history. <!-- ConflictOverrideHistory rows written by RoutineScheduleService/PrivateSessionService/SubstituteCoachService -->

### 4.15 Approval and Locking Backend

- [x] Create submission/approval action DTOs (`FR-APPROVAL-001`–`007`).
- [x] Create Training Approval service.
- [x] Add API for Coach to submit a Completed record.
- [x] Add API for Administrator to approve a submitted record.
- [x] Add API for Administrator to reject a submitted record.
- [x] Add API for Administrator to request revision.
- [x] Add API for authorized Administrator to unlock a Locked record.
- [x] Require unlock reason.
- [x] Change approved records to Locked state. <!-- ApproveAsync transitions Submitted -> Approved -> Locked as one action; requirement.md defines no separate Lock action -->
- [x] Enforce edit restrictions for Locked records. <!-- Already enforced via ISessionStatusService.IsEditableByCoach (Locked excluded) used by Attendance/TrainingLog/CoachTeaching services -->
- [x] Record every approval workflow action in history. <!-- TrainingApprovalHistory row per Submit/Approve/Reject/RequestRevision/Unlock -->

### 4.16 Coach Dashboard Backend

- [x] Create Coach Dashboard response DTO (`FR-CDASH-001`–`003`).
- [x] Create Coach Dashboard service.
- [x] Add API for Coach today's sessions.
- [x] Add API for Coach upcoming sessions.
- [x] Add Coach completed/remaining session summary. <!-- Scoped to the current calendar month -->
- [x] Add Routine/Private session summary. <!-- Scoped to the current calendar month -->
- [x] Add pending/incomplete record summary. <!-- Overdue Scheduled/InProgress + not-yet-submitted Completed sessions -->
- [x] Replace the monthly teaching-hour summary with a distinct monthly teaching-day summary as requested by the user.
- [x] Restrict dashboard data to the signed-in Coach. <!-- GET /api/dashboard/coach always scopes to _currentUser.CoachId -->

### 4.17 Administrator Dashboard Backend

- [x] Create Administrator Dashboard response DTO (`FR-ADASH-001`–`004`).
- [x] Create Administrator Dashboard service.
- [x] Add session KPI summary by date/date range. <!-- Unset range defaults to today -->
- [x] Add completed/upcoming/cancelled counts.
- [x] Add Routine/Private counts.
- [x] Add Coaches Teaching Today summary.
- [x] Add athlete attendance summary.
- [x] Add coach teaching-hour summary.
- [x] Add coach filter support.
- [x] Add training-type filter support.
- [x] Include identifiers needed to open related operational records. <!-- CoachTeachingTodayDto.TrainingSessionIds -->

### 4.18 Coach Teaching-Hour Report Backend

- [x] Create Coach Teaching-Hour report filter DTO (`FR-RPT-COACH-001`–`009`).
- [x] Create Coach Teaching-Hour report response DTO.
- [x] Create Coach Teaching-Hour report service.
- [x] Add report API with coach filter.
- [x] Add report API date-range filtering.
- [x] Add report API training-type filtering.
- [x] Calculate Routine teaching hours.
- [x] Calculate Private teaching hours.
- [x] Calculate total teaching hours.
- [x] Credit teaching time to actual coach after substitution.
- [x] Exclude Cancelled sessions.
- [x] Exclude rescheduled-original sessions.
- [x] Exclude Coach Absent sessions unless substitute completion qualifies. <!-- All via ISessionStatusService.CountsAsCompletedTeaching, the single shared rule -->
- [x] Use finalized session records according to approval rules. <!-- Same shared rule: Completed/Submitted/Approved/Locked count, per todo.md 4.5/4.6 design -->
- [x] Adjust the coach teaching report to summarize distinct teaching days per coach instead of teaching-hour duration, including same-day de-duplication requested by the user.

### 4.19 Athlete Attendance Report Backend

- [x] Create Athlete Attendance report filter DTO (`FR-RPT-ATH-001`–`007`).
- [x] Create Athlete Attendance report response DTO.
- [x] Create Athlete Attendance report service.
- [x] Add report API with athlete filter.
- [x] Add report API date-range filtering.
- [x] Distinguish Routine and Private attendance in report results.
- [x] Calculate Private attendance status counts.
- [x] Report only explicitly recorded Routine attendance.
- [x] Do not infer Routine absence from non-selection.
- [x] Exclude duplicate counts caused by rescheduled-original sessions. <!-- Rescheduled-status sessions excluded from the query entirely -->

### 4.20 History and Audit Backend

- [x] Create audit/history response DTOs (`FR-AUDIT-001`–`003`).
- [x] Create Audit service.
- [x] Add API to retrieve session business history. <!-- GET /api/training-sessions/{id}/history -->
- [x] Add API to retrieve approval history. <!-- GET .../history/approvals -->
- [x] Add API to retrieve substitution history. <!-- GET .../history/substitutions -->
- [x] Add API to retrieve conflict-override history. <!-- GET .../history/conflict-overrides -->
- [x] Capture schedule creation/change events. <!-- RoutineSchedule/PrivateSession/TrainingSession CreatedDate+CreatedByUserId, UpdatedDate+UpdatedByUserId (AuditableEntity) -->
- [x] Capture attendance change events. <!-- Attendance.RecordedByUserId/RecordedDate already identify actor+time per FR-AUDIT-001; no redundant AuditLog row needed -->
- [x] Capture completion/submission events. <!-- Completion via TrainingSession's own Updated*/Actual* columns; Submission explicitly via TrainingApprovalHistory (ActionType.Submit) -->
- [x] Capture cancellation/rescheduling events. <!-- Dedicated AuditLog ("Cancel"/"Reschedule") entries -->
- [x] Preserve history when coach or athlete becomes inactive. <!-- Deactivation only sets IsActive=false (query filter is IsDeleted-only, see CoachConfiguration), so history navigations/snapshots remain resolvable -->

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
- [x] Add Coach color picker and color preview to Coach management.
- [x] Replace Coach Type/Specialization inputs with bank account detail inputs (bank name, account number, account name) in the Coach form; drop the Coach Type column from the Coach list.
- [x] Make the bank-name field a dropdown of Thai bank names with PromptPay listed first.
- [x] Make Coach list and forms responsive.
- [x] Add a touch-friendly Coach card list for mobile while preserving the desktop table.

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
- [x] Add a touch-friendly Athlete card list for mobile while preserving the desktop table.
- [x] Add required Athlete Type selection to Athlete forms and display it in Athlete lists.
- [x] Separate the Athlete list into Affiliated Athlete and General Athlete tabs with server-side filtering and correct pagination.

### 5.3 Routine Training Frontend

- [x] Create Routine Schedule list page.
- [x] Limit the Routine Schedule list to training date, time, Coach nickname, and edit/delete icon actions, ordered by training date.
- [x] Create Routine Schedule monthly calendar view.
- [x] Display each Routine Schedule only on its selected calendar date.
- [x] Allow Administrator to select a calendar date and open the create form with date/day prefilled.
- [x] Add previous month, next month, and current month navigation.
- [x] Add responsive mobile calendar and selected-date agenda.
- [x] Add loading, empty, and error states to Routine Schedule calendar.
- [x] Show only Coach nickname without time in calendar entries and use the configured Coach color.
- [x] Color Routine Schedule calendar dates Emerald, use a dark-green selected border for scheduled dates, and gray for empty selected dates.
- [x] Add Routine Schedule search/filter/pagination.
- [x] Add loading, empty, and error states to Routine Schedule list.
- [x] Create Routine Schedule create form.
- [x] Create Routine Schedule edit form.
- [x] Add confirmed delete actions to the Routine Schedule calendar and list.
- [x] Add Coach selector using active coaches only.
- [x] Derive Routine Schedule name from the selected date without exposing confusing inputs.
- [x] Add start/end time inputs with defaults of 18:30 and 20:30.
- [x] Add a single training-date input prefilled from the selected calendar date.
- [x] Do not repeat a Routine Schedule beyond the single selected date.
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
- [x] Create Private Training monthly calendar view with a selected-date mobile agenda.
- [x] Color Private Session calendar dates blue, use a dark-blue selected border for session dates, and gray for empty selected dates.
- [x] Display Coach color/nickname, start-end time, and athlete count on Private Training calendar entries.
- [x] Allow Administrator to select a calendar date and open the create form with the date prefilled.
- [x] Add Private Training search/filter/pagination.
- [x] Add loading, empty, and error states to Private Training list.
- [x] Create Private Training create form.
- [x] Create Private Training edit form.
- [x] Add active Coach selector.
- [x] Display Private Training coach options as nickname followed by full name.
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

- [x] Create Coach Home page (`FR-CDASH-001`–`003`).
- [x] Create Today's Sessions section.
- [x] Create Upcoming Sessions section.
- [x] Display Routine/Private type for each session.
- [x] Display scheduled time and current status.
- [x] Display required next action for actionable sessions.
- [x] Add completed/remaining session summary.
- [x] Display a distinct monthly teaching-day summary instead of teaching hours as requested by the user.
- [x] Add pending/incomplete training-record summary.
- [x] Add direct navigation from session card/list item to session detail.
- [x] Add loading state.
- [x] Add empty state when no sessions are scheduled.
- [x] Add API error state and retry action.
- [x] Optimize Coach Home for mobile-first use.
- [x] Display past actionable sessions on Coach Home so Coaches can complete overdue teaching records.
- [x] Add a responsive monthly calendar and selected-date agenda for the signed-in Coach's sessions.
- [x] Split Coach Home into Overview and Calendar tabs with touch-friendly responsive controls.
- [x] Allow a Coach to create only the Coach's own Routine Training schedule from Coach Home without conflict override permission.
- [ ] Allow a Coach to create Routine Training over a start/end date range by selecting one or more weekdays. <!-- Implemented and tested; awaiting successful frontend production build (current ng build exits 134 without diagnostics). -->
- [ ] Allow a Coach to create Routine Training on every calendar date in a selected range without choosing weekdays. <!-- Implemented and tested; awaiting successful frontend production build. -->
- [ ] Validate all selected occurrences for schedule conflicts before batch creation. <!-- Implemented and backend tests pass; close with the same frontend production-build validation above. -->
- [x] Open the Coach's Routine Training create form with the selected date after a calendar-day long press.
- [x] Allow a Coach to delete only the Coach's own untouched Scheduled Routine Training from the calendar.
- [x] Use distinct calendar colors and legends for Routine and Private Training on desktop and mobile.
- [x] Add distinct calendar background colors, including a split background for dates containing both training types.
- [x] Use a gray selected-day background only when empty while preserving training-type backgrounds on populated dates.
- [x] Match the selected calendar-day border to a darker shade of its Routine or Private Training background.
- [x] Show nickname-only colleague presence on dates shared with the signed-in Coach without exposing other session details.
- [x] Add distinct colors to Coach Home summary statuses and consistently color Routine/Private type badges.
- [x] Allow Coach self-service Routine Training creation across a date range filtered by selected weekdays.

### 5.6 Coach Session Frontend

- [x] Create shared Coach Session page for Routine and Private sessions.
- [x] Create Session Summary section.
- [x] Display scheduled start/end separately from actual start/end.
- [x] Display assigned coach separately from actual coach.
- [x] Add Start/Check-in action.
- [x] Add End/Check-out action.
- [x] Display calculated actual teaching duration.
- [x] Disable invalid actions based on current session status.
- [x] Add Attendance section appropriate to training type.
- [x] Add Training Log section.
- [x] Add Complete action.
- [x] Add Submit action when applicable.
- [x] Display validation errors blocking completion/submission.
- [x] Display locked/read-only state for finalized records.
- [x] Add loading and API error states.
- [x] Make Coach Session flow responsive and optimized for mobile.
- [x] Require confirmation before starting a training session.
- [x] Allow an Administrator to reset an In Progress session to Scheduled with a required audited reason.

### 5.7 Routine Attendance Frontend

- [x] Create Routine Attendance component.
- [x] Add athlete search from active Athlete Master.
- [x] Add selected athlete to attendance list.
- [x] Prevent already-added athletes from duplicate selection.
- [x] Add Present status option.
- [x] Add Late status option.
- [x] Add arrival-time input for Late status.
- [x] Add attendance remark input.
- [x] Add editable attendance removal while session is editable.
- [x] Do not render a fixed roster.
- [x] Do not label unselected athletes as Absent.
- [x] Add saving/loading/error feedback.
- [x] Optimize attendance controls for touch/mobile use.

### 5.8 Private Attendance Frontend

- [x] Create Private Attendance component.
- [x] Display all athletes assigned to the Private session.
- [x] Add Present status option.
- [x] Add Absent status option.
- [x] Add Late status option.
- [x] Add Leave / Excused status option.
- [x] Add arrival-time input for Late status.
- [x] Add remark input for Absent and Leave / Excused.
- [x] Highlight assigned athletes missing attendance status.
- [x] Prevent final submission until all assigned athletes have attendance status.
- [x] Add saving/loading/error feedback.
- [x] Optimize attendance controls for touch/mobile use.

### 5.9 Training Log Frontend

- [x] Create Training Log form component.
- [x] Add Training Topic input.
- [x] Add Training Objective input.
- [x] Add Exercise / Drill input.
- [x] Add Training Focus input.
- [x] Add Training Intensity input.
- [x] Add Coach Notes input.
- [x] Add Athlete Notes input.
- [x] Add General Remarks input.
- [x] Disable editing when session is non-editable.
- [x] Add loading/saving/error feedback.
- [x] Make Training Log form responsive.

### 5.10 Substitute Coach Frontend

- [x] Add substitute-coach action to eligible session screens.
- [x] Create substitute-coach dialog/form.
- [x] Add active substitute Coach selector.
- [x] Require substitution reason.
- [x] Display schedule-conflict validation feedback.
- [x] Display original assigned coach after substitution.
- [x] Display actual/substitute coach after substitution.
- [x] Display substitution history in session detail for authorized users.

### 5.11 Cancellation Frontend

- [x] Add Cancel action to eligible session screens.
- [x] Create cancellation confirmation dialog.
- [x] Require cancellation reason.
- [x] Display Cancelled status clearly after success.
- [x] Remove invalid operational actions from Cancelled sessions.
- [x] Add cancellation API loading/error feedback.

### 5.12 Rescheduling Frontend

- [x] Add Reschedule action to eligible session screens.
- [x] Create reschedule form/dialog.
- [x] Add new date/start/end time inputs.
- [x] Display coach conflict feedback.
- [x] Display athlete conflict feedback for Private Training.
- [x] Display authorized conflict-override controls when permitted.
- [x] Require override reason when override is used.
- [x] Display link/reference between original and replacement sessions.
- [x] Add reschedule API loading/error feedback.

### 5.13 Administrative Review Frontend

- [x] Create Submitted Training Records list page.
- [x] Add review-list search/filter/pagination.
- [x] Add loading, empty, and error states to review list.
- [x] Create Administrative Review detail page.
- [x] Display scheduled teaching information.
- [x] Display actual teaching information.
- [x] Display assigned coach and actual coach.
- [x] Display athlete attendance.
- [x] Display training log.
- [x] Display session status.
- [x] Display submission/approval history.
- [x] Add Approve action.
- [x] Add Reject action.
- [x] Add Request Revision action.
- [x] Add Unlock action for authorized Administrator.
- [x] Require unlock reason.
- [x] Refresh displayed status after workflow action.
- [x] Make review pages responsive.

### 5.14 Administrator Dashboard Frontend

- [x] Create Administrator Dashboard page (`FR-ADASH-001`–`004`).
- [x] Create Training Sessions Today KPI card.
- [x] Create Completed Sessions KPI card.
- [x] Create Upcoming Sessions KPI card.
- [x] Create Cancelled Sessions KPI card.
- [x] Create Routine Sessions KPI card.
- [x] Create Private Sessions KPI card.
- [x] Create Coaches Teaching Today summary component.
- [x] Create Athlete Attendance Summary component.
- [x] Create Coach Teaching Hours summary component.
- [x] Add date/date-range filter.
- [x] Add Coach filter.
- [x] Add Training Type filter.
- [x] Add navigation from applicable dashboard items to related records.
- [x] Add loading state for dashboard data.
- [x] Add empty state where summary data is unavailable.
- [x] Add dashboard API error state and retry action.
- [x] Make dashboard responsive without horizontal overflow.
- [x] Default Administrator Dashboard filters to the current calendar month, show coach nicknames/colors split by training type, show participation-only athlete names/totals split by training type, and remove coach teaching hours from this page as requested by the user.
- [x] Remove the Administrator Dashboard KPI card row as requested by the user.
- [x] Add date-by-athlete attendance matrix tables with daily totals, period totals, and coach names for Routine and Private Training on the Administrator Dashboard.

### 5.15 Coach Teaching-Hour Report Frontend

- [x] Create Coach Teaching-Hour Report page.
- [x] Add Coach filter.
- [x] Add date-range filter.
- [x] Add Training Type filter.
- [x] Display Routine teaching hours.
- [x] Display Private teaching hours.
- [x] Display total teaching hours.
- [x] Distinguish actual coach from originally assigned coach where relevant.
- [x] Add report loading state.
- [x] Add report empty state.
- [x] Add report error state.
- [x] Make report layout responsive.
- [x] Display distinct coach teaching-day totals instead of hourly totals, with same-day sessions counted once.
- [x] Default teaching-day filters to the current month and show coach nickname/color in report controls and results.

### 5.16 Athlete Attendance Report Frontend

- [x] Create Athlete Attendance Report page.
- [x] Add Athlete filter.
- [x] Add date-range filter.
- [x] Distinguish Routine and Private attendance records.
- [x] Display Private attendance status counts.
- [x] Display Routine recorded-attendance history without inferred absence.
- [x] Add report loading state.
- [x] Add report empty state.
- [x] Add report error state.
- [x] Make report layout responsive.
- [x] Show participation-only summaries, athlete nickname followed by full name, and default filters to the current month as requested by the user.
- [x] Use a compact athlete summary table with on-demand expandable attendance history.
- [x] Present athlete attendance details in a responsive monthly calendar modal, including a seven-day mobile calendar with green Routine and blue Private markers.

### 5.17 History and Audit Frontend

- [x] Create session history/timeline component.
- [x] Display schedule changes.
- [x] Display cancellation/rescheduling history.
- [x] Display substitute-coach history.
- [x] Display attendance change history when authorized.
- [x] Display completion/submission/approval history.
- [x] Display conflict-override history.
- [x] Display responsible user and action date/time.
- [x] Add loading and error states for history data.
- [x] Make history/timeline responsive.

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
- [x] Validate active Coach selection for new schedules and substitutions.
- [ ] Validate active Athlete selection for new Private assignments and Routine attendance.
- [ ] Validate unique Coach Code.
- [ ] Validate unique Athlete Code.
- [ ] Validate no duplicate Private athlete assignment.
- [ ] Validate no duplicate attendance entry per athlete/session.
- [ ] Validate Private Training has at least one athlete.
- [x] Validate Private attendance completeness before final submission. <!-- Enforced in TrainingApprovalService.SubmitAsync (4.15) -->
- [x] Validate Routine attendance uses only allowed statuses.
- [x] Validate Private attendance uses only allowed statuses.
- [x] Validate cancellation eligibility and required reason.
- [x] Validate rescheduling eligibility.
- [x] Validate substitution reason.
- [x] Validate conflict-override reason when override is used.
- [x] Validate unlock reason.
- [x] Validate session status transitions centrally.
- [x] Validate Locked record edit restrictions in service layer. <!-- ISessionStatusService.IsEditableByCoach excludes Locked; enforced by Attendance/TrainingLog/CoachTeaching services -->

### 7.2 Schedule Conflict Validation

- [x] Add unit-tested overlap rule for coach vs coach schedule. <!-- ScheduleConflictServiceTests -->
- [x] Add unit-tested overlap rule for athlete vs athlete Private schedule. <!-- ScheduleConflictServiceTests -->
- [x] Add unit-tested overlap rule for Private Training vs coach Routine Training. <!-- ScheduleConflictServiceTests -->
- [x] Apply conflict checks consistently on create, update, substitution, and rescheduling.
- [x] Return conflict responses using appropriate `409 Conflict` behavior where applicable.

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

- [x] Enforce Administrator-only access to administrative master-data changes. <!-- Coach/Athlete/RoutineSchedules/PrivateSessions controllers are [Authorize(Roles = Administrator)] -->
- [x] Enforce Administrator-only approval/review actions unless another role is explicitly authorized. <!-- Approve/Reject/RequestRevision/Unlock are Administrator-only; Submit explicitly also allows the acting Coach (FR-APPROVAL-001) -->
- [x] Enforce authorized-user checks for cancellation, rescheduling, substitution, and conflict override. <!-- CancellationsController/ReschedulingController/SubstituteCoachesController/conflict-override paths are all Administrator-only -->
- [x] Restrict Coach operational APIs to sessions relevant to the signed-in Coach.
- [x] Restrict Coach history and teaching-hour APIs to the signed-in Coach unless elevated permission exists. <!-- CoachDashboardController scopes to _currentUser.CoachId; HistoryController/AuditService reject a Coach viewing another coach's session; Coach Teaching-Hour Report is Administrator/ManagementViewer only -->
- [x] Keep Management / Viewer operations read-only. <!-- ManagementViewer role is never granted on a write endpoint anywhere in the API -->
- [x] Enforce permission checks in backend services/controllers; do not rely only on frontend guards.

### 8.2 Data and Transport Security

- [ ] Require HTTPS in deployed environments.
- [ ] Keep JWT signing keys and database credentials outside source control.
- [x] Keep email credentials outside source control and protect password-reset URLs in logs and telemetry.
- [x] Rate-limit forgot-password requests and apply abuse protection without enabling account enumeration.
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
- [x] Test password change ownership, current-password verification, and minimum 8-character validation.
- [x] Test forgot-password neutral responses for known, unknown, and inactive account emails.
- [x] Test reset-token hashing, 10-minute expiry, single use, and invalidation after a newer request.
- [x] Test successful password reset and rejection of invalid, expired, or already-used tokens.
- [x] Test password-reset email uses the registered account email and configured frontend reset URL.
- [x] Test JwtTokenService issues correct identity/role claims.
- [x] Test User username/email uniqueness validation.
- [x] Test Coach Code uniqueness validation.
- [x] Test Athlete Code uniqueness validation.
- [x] Test single-date Routine Session generation.
- [x] Test completed Routine history remains unchanged after schedule edits.
- [x] Test coach schedule conflict detection.
- [x] Test athlete Private schedule conflict detection.
- [x] Test Private-vs-Routine coach conflict detection.
- [x] Test conflict override requires authorization and reason.
- [x] Test actual teaching duration calculation.
- [x] Test invalid/negative actual duration rejection.
- [x] Test Routine attendance duplicate prevention.
- [x] Test Routine attendance never infers absent athletes.
- [x] Test Private attendance duplicate prevention.
- [x] Test Private attendance completeness before submission. <!-- completeness computation tested; the submission trigger itself is Approval (4.15) -->
- [x] Test substitute coach preserves original coach.
- [x] Test substitute coach receives actual teaching-hour credit.
- [x] Test cancellation exclusion from teaching hours.
- [x] Test rescheduled-original exclusion from teaching hours and attendance totals.
- [x] Test Coach Absent teaching-hour rule.
- [x] Test session status transition rules.
- [x] Test Locked record edit restrictions.
- [x] Test approval/reject/revision/unlock history creation.
- [x] Test coach dashboard data is scoped to signed-in Coach.
- [x] Test teaching-hour report filters and totals.
- [x] Test athlete attendance report filters and totals.
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
- [x] Run `dotnet build` after password change/reset backend implementation.
- [x] Run `ng build` after password change/reset frontend implementation.
- [x] Run `dotnet build` after Coach module backend implementation.
- [x] Run `ng build` after Coach module frontend implementation.
- [x] Run `dotnet build` after Athlete module backend implementation.
- [x] Run `ng build` after Athlete module frontend implementation.
- [x] Run `dotnet build` after Routine Training backend implementation.
- [x] Run `ng build` after Routine Training frontend implementation.
- [x] Run `dotnet build` after Private Training backend implementation.
- [x] Run `ng build` after Private Training frontend implementation.
- [x] Run `dotnet build` after Training Session/Teaching backend implementation.
- [x] Run `ng build` after Coach Session frontend implementation.
- [x] Run `dotnet build` after Attendance/Training Log backend implementation.
- [x] Run `ng build` after Attendance/Training Log frontend implementation.
- [x] Run `dotnet build` after substitution/cancellation/rescheduling/conflict implementation.
- [x] Run `ng build` after substitution/cancellation/rescheduling UI implementation.
- [x] Run `dotnet build` after approval/audit implementation.
- [x] Run `ng build` after administrative review/history UI implementation.
- [x] Run `dotnet build` after dashboard/report backend implementation.
- [x] Run `ng build` after dashboard/report frontend implementation.

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
