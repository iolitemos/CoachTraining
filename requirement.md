# Coach Training & Athlete Attendance Management System — Requirements

## 1. Project Overview

The system manages coach teaching schedules, actual teaching records, and athlete attendance for a sports gym.

The system shall support two training types:

- **Routine Training** — recurring coach teaching schedules conducted at the gym's regular training venue.
- **Private Training** — scheduled sessions assigned to one or more specific athletes.

Coaches are responsible for recording actual teaching activity and athlete attendance. Administrators manage master data, schedules, corrections, approvals, and reporting. Management users have read-only access to operational summaries and reports.

This document defines the business behavior and expected system functionality for the initial implementation. Engineering standards, architecture rules, technology choices, coding standards, and shared UI conventions are governed by `skill.md` and are intentionally not repeated here.

---

## 2. Business Goals

- Maintain a reliable record of when each coach is scheduled to teach and when each coach actually teaches.
- Separate Routine Training and Private Training while keeping reporting consistent across both training types.
- Allow coaches to record athlete attendance directly for each training session.
- Provide a clear history of completed, cancelled, rescheduled, and substituted training sessions.
- Prevent schedule conflicts for coaches and athletes.
- Provide accurate coach teaching-hour and athlete attendance reports.
- Support an approval and record-locking process for finalized teaching records.
- Preserve clean historical data for future extensions such as packages, compensation, athlete performance, booking, payment, and notifications.

---

## 3. User Roles

### 3.1 Administrator

The Administrator shall be able to:

- Manage user access and role assignments.
- Manage coach records.
- Manage athlete records.
- Create and maintain Routine Training schedules.
- Create and maintain Private Training schedules.
- Cancel or reschedule training sessions.
- Assign substitute coaches.
- Review and correct attendance records when authorized.
- Review submitted teaching records.
- Approve, reject, request revision, or unlock training records.
- View all dashboards and reports.
- View audit history.

### 3.2 Coach

The Coach shall be able to:

- View only the training sessions relevant to the coach.
- View today's and upcoming sessions.
- Record actual teaching start and end information.
- Record athlete attendance.
- Record training notes.
- Complete and submit training records.
- View the coach's own historical teaching records.
- View the coach's own teaching-hour summary.

### 3.3 Management / Viewer

The Management / Viewer role shall be able to:

- View dashboards.
- View coach teaching records.
- View athlete attendance information.
- View coach workload and teaching-hour reports.
- View summary and historical reports.

The Management / Viewer role shall not modify operational records unless explicitly granted an additional role with edit permissions.

---

## 4. Modules

### 4.1 User & Access Management

Purpose:

- Manage system users and business roles.
- Control access to administrative, coach, and management functions.
- Allow account owners to change their password and securely reset a forgotten password through their registered email address.

### 4.2 Coach Management

Purpose:

- Maintain coach master data and active status.
- Link coaches to system users when applicable.
- Preserve historical teaching records when a coach becomes inactive.

Core data:

- Coach Code
- Full Name
- Nickname
- Contact Information
- Email
- Bank Account Details (bank name, account number, account name) — reference data for teaching-compensation payment only; no rate calculation or payment processing (Coach Compensation Management remains out of scope)
- Active / Inactive Status
- Remarks

### 4.3 Athlete Management

Purpose:

- Maintain athlete master data used by Routine and Private Training attendance.

Core data:

- Athlete Code
- Full Name
- Nickname
- Date of Birth
- Contact Information
- Parent / Guardian Information, when applicable
- Athlete Level, when applicable
- Join Date
- Active / Inactive Status
- Remarks

### 4.4 Routine Training Management

Purpose:

- Maintain recurring teaching schedules for coaches.
- Create individual Routine Training sessions from recurring schedules.

Routine Training does **not** require:

- Athlete Group
- Team
- Training Location

Core schedule data:

- Routine Schedule Name
- Coach
- Day of Week
- Start Time
- End Time
- Effective Start Date
- Effective End Date, when defined
- Recurrence Pattern
- Active / Inactive Status
- Remarks

### 4.5 Private Training Management

Purpose:

- Manage scheduled training assigned to specific athlete(s).

Private Training shall support:

- One coach to one athlete.
- One coach to multiple athletes.

Core data:

- Training Date
- Start Time
- End Time
- Coach
- Athlete(s)
- Location, when applicable
- Remarks
- Session Status

### 4.6 Training Session Management

Purpose:

- Provide the central operational record for both Routine Training and Private Training.

Each session shall identify:

- Training Type
- Training Date
- Scheduled Start Time
- Scheduled End Time
- Assigned Coach
- Actual Coach
- Actual Start Time
- Actual End Time
- Session Status
- Remarks

### 4.7 Coach Teaching Record

Purpose:

- Record the difference between the planned schedule and actual teaching activity.

The system shall support recording:

- Check-in / actual teaching start
- Check-out / actual teaching end
- Actual teaching duration
- Teaching status
- Teaching remarks

### 4.8 Athlete Attendance

Purpose:

- Record athlete participation for each training session.

Attendance behavior differs between Routine Training and Private Training and is defined in Section 6.

### 4.9 Training Log

Purpose:

- Allow the coach to record what was taught during a completed session.

Training log data may include:

- Training Topic
- Training Objective
- Exercise / Drill
- Training Focus
- Training Intensity
- Coach Notes
- Athlete Notes
- General Remarks

### 4.10 Substitute Coach Management

Purpose:

- Preserve the originally assigned coach while recording the coach who actually taught the session.

### 4.11 Cancellation & Rescheduling

Purpose:

- Manage sessions that cannot proceed as originally scheduled.
- Preserve the relationship between an original session and its rescheduled replacement.

### 4.12 Approval & Record Locking

Purpose:

- Control when coach teaching records become final.
- Prevent unauthorized changes to approved historical records.

### 4.13 Dashboard

Purpose:

- Provide operational visibility for coaches and administrators.

### 4.14 Reports

Purpose:

- Provide coach teaching-hour reports.
- Provide athlete attendance reports.
- Provide training history and status summaries.

### 4.15 Audit History

Purpose:

- Track important business changes to schedules, attendance, coach assignments, and approvals.

---

## 5. Business Flow

### 5.1 Routine Training Schedule Setup

1. Administrator creates a Routine Training schedule.
2. Administrator selects the coach.
3. Administrator defines the recurring day and teaching time.
4. Administrator defines the effective date range when applicable.
5. The system validates the schedule against existing coach commitments.
6. If no blocking conflict exists, the schedule becomes active.
7. The system makes the corresponding Routine Training sessions available for coach operations.
8. Changes to the recurring schedule apply only to applicable future sessions and must not alter completed historical sessions.

### 5.2 Routine Training Execution

1. Coach opens today's Routine Training session.
2. Coach starts or checks in to the session.
3. The system records the actual coach and actual start information.
4. Coach selects the athletes who attended the Routine Training session from the Athlete Master.
5. Coach records attendance information for each selected athlete.
6. Coach enters training notes when required.
7. Coach completes the session.
8. The system records the actual end information and teaching duration.
9. Coach submits the completed training record for review when approval is required.
10. Administrator reviews and finalizes the submitted record.

**Routine Attendance Rule:** Routine Training has no pre-assigned athlete roster. The system shall not automatically infer that unselected athletes are absent.

### 5.3 Private Training Creation

1. Administrator or authorized user creates a Private Training session.
2. User selects the coach.
3. User selects one or more athletes.
4. User defines the date, start time, and end time.
5. User enters optional location and remarks when applicable.
6. The system validates coach and athlete schedule conflicts.
7. If no blocking conflict exists, the Private Training session is scheduled.

### 5.4 Private Training Execution

1. Coach opens the scheduled Private Training session.
2. Coach starts or checks in to the session.
3. The system records the actual coach and actual start information.
4. The system displays the athletes assigned to the Private Training session.
5. Coach records attendance status for each assigned athlete.
6. Coach records training notes when required.
7. Coach completes the session.
8. The system records the actual end information and teaching duration.
9. Coach submits the completed training record for review when approval is required.
10. Administrator reviews and finalizes the submitted record.

### 5.5 Substitute Coach Flow

1. An authorized user identifies that the assigned coach cannot teach the session.
2. User assigns a substitute coach.
3. User records the substitution reason.
4. The system retains the original assigned coach.
5. The substitute coach becomes the actual coach for the session when the session is taught.
6. The session history shows both the originally assigned coach and the actual coach.

### 5.6 Cancellation Flow

1. Authorized user selects a scheduled session.
2. User chooses Cancel.
3. User records a cancellation reason.
4. The system changes the session status to Cancelled.
5. The cancelled session remains in history.
6. A cancelled session shall not count as completed teaching time.

### 5.7 Rescheduling Flow

1. Authorized user selects a scheduled session.
2. User chooses Reschedule.
3. User defines the new date and/or time.
4. The system validates the new schedule for conflicts.
5. The original session is retained as rescheduled history.
6. A new or replacement session is linked to the original session.
7. Only the completed replacement session counts toward teaching-hour and attendance reporting.

### 5.8 Training Record Approval Flow

1. Coach records the required session data.
2. Coach marks the session as Completed.
3. Coach submits the record.
4. Administrator reviews the submitted record.
5. Administrator chooses one of the following:
   - Approve
   - Reject
   - Request Revision
6. Approved records become Locked.
7. Locked records cannot be edited by the coach.
8. If a locked record requires correction, an authorized Administrator may unlock it.
9. The system records the approval, rejection, revision, and unlock history.

### 5.9 Password Change and Reset Flow

1. A signed-in account owner may change only the password of the owner's own account.
2. The system verifies the owner's current password before accepting a password change.
3. If the owner forgets the password, the owner requests a reset link using the email address registered to the account.
4. The system sends a single-use password-reset link to that registered email address without disclosing whether an account exists for the submitted address.
5. The password-reset token expires 10 minutes after issuance.
6. The owner sets a new password containing at least 8 characters.
7. A successfully used token becomes invalid and cannot be reused.

---

## 6. Functional Requirements

### 6.1 User & Role Requirements

- **FR-USER-001** The system shall support Administrator, Coach, and Management / Viewer roles.
- **FR-USER-002** The system shall restrict features and data according to the signed-in user's authorized role.
- **FR-USER-003** A Coach shall access only the coach's relevant operational records unless additional permission is granted.
- **FR-USER-004** Inactive coaches and athletes shall remain visible in historical records but shall not be selectable for new active schedules unless reactivated.
- **FR-USER-005** A signed-in user shall be able to change only the password of the user's own account after providing the correct current password.
- **FR-USER-006** An account owner shall be able to request a password-reset link through the email address registered to the account.
- **FR-USER-007** A password-reset link shall contain a single-use token that expires 10 minutes after issuance.
- **FR-USER-008** New passwords created through password change or password reset shall contain at least 8 characters.
- **FR-USER-009** Password-reset requests shall return a neutral response that does not reveal whether the submitted email address belongs to an account.
- **FR-USER-010** A password-reset token shall be invalid after successful use, expiration, or issuance of a newer reset token for the same account.

### 6.2 Coach Requirements

- **FR-COACH-001** Administrator shall be able to create, edit, activate, and deactivate coach records.
- **FR-COACH-002** Each coach record shall have a unique business identifier.
- **FR-COACH-003** Deactivating a coach shall not delete or alter historical teaching records.
- **FR-COACH-004** A coach may be linked to a system user account.

### 6.3 Athlete Requirements

- **FR-ATHLETE-001** Administrator shall be able to create, edit, activate, and deactivate athlete records.
- **FR-ATHLETE-002** Each athlete record shall have a unique business identifier.
- **FR-ATHLETE-003** Deactivating an athlete shall not delete or alter historical attendance records.
- **FR-ATHLETE-004** Active athletes shall be selectable for new Private Training sessions and Routine Training attendance.

### 6.4 Routine Training Requirements

- **FR-ROUTINE-001** Administrator shall be able to create recurring Routine Training schedules.
- **FR-ROUTINE-002** A Routine Training schedule shall require a coach, recurrence rule, start time, and end time.
- **FR-ROUTINE-003** Routine Training shall not require Group, Team, or Location.
- **FR-ROUTINE-004** Routine Training shall support an effective start date.
- **FR-ROUTINE-005** Routine Training may support an effective end date.
- **FR-ROUTINE-006** Administrator shall be able to activate or deactivate a recurring schedule.
- **FR-ROUTINE-007** Changes to a recurring schedule shall not modify completed historical sessions.
- **FR-ROUTINE-008** The system shall identify coach schedule conflicts before accepting a new or changed Routine Training schedule.
- **FR-ROUTINE-009** Routine Training sessions shall be distinguishable from Private Training sessions throughout the system.

### 6.5 Private Training Requirements

- **FR-PRIVATE-001** Authorized users shall be able to create a Private Training session.
- **FR-PRIVATE-002** A Private Training session shall require one coach.
- **FR-PRIVATE-003** A Private Training session shall require at least one athlete.
- **FR-PRIVATE-004** A Private Training session shall support multiple athletes.
- **FR-PRIVATE-005** A Private Training session shall require a training date, start time, and end time.
- **FR-PRIVATE-006** Location shall be optional for Private Training.
- **FR-PRIVATE-007** The system shall identify coach schedule conflicts before saving or rescheduling a Private Training session.
- **FR-PRIVATE-008** The system shall identify athlete schedule conflicts before saving or rescheduling a Private Training session.
- **FR-PRIVATE-009** Administrator or another authorized user shall be able to edit future Private Training sessions.

### 6.6 Training Session Requirements

- **FR-SESSION-001** Every operational training occurrence shall exist as a Training Session.
- **FR-SESSION-002** Every Training Session shall have exactly one Training Type: Routine or Private.
- **FR-SESSION-003** Each Training Session shall retain its scheduled start and end time.
- **FR-SESSION-004** Each Training Session shall retain its originally assigned coach.
- **FR-SESSION-005** Each Training Session shall support a separate actual coach.
- **FR-SESSION-006** Actual start and actual end information shall be recorded separately from scheduled time.
- **FR-SESSION-007** Actual teaching duration shall not be negative.
- **FR-SESSION-008** A session shall not be reported as completed teaching time unless it reaches Completed status.
- **FR-SESSION-009** Cancelled sessions shall remain available in history.
- **FR-SESSION-010** Rescheduled sessions shall preserve reference to the original session.

### 6.7 Session Status Requirements

The system shall support at least the following business statuses:

- Scheduled
- In Progress
- Completed
- Submitted
- Approved
- Locked
- Cancelled
- Rescheduled
- Coach Absent

Requirements:

- **FR-STATUS-001** Status transitions shall follow the applicable business flow.
- **FR-STATUS-002** A Cancelled session cannot be completed unless it is restored by an authorized user.
- **FR-STATUS-003** A Locked session cannot be edited by a Coach.
- **FR-STATUS-004** A Rescheduled original session shall not be counted as a completed session.
- **FR-STATUS-005** A session marked Coach Absent shall not count as completed teaching time unless an actual substitute coach completes the session.

### 6.8 Coach Teaching Record Requirements

- **FR-TEACH-001** Coach shall be able to record the actual start of a session.
- **FR-TEACH-002** Coach shall be able to record the actual end of a session.
- **FR-TEACH-003** The system shall calculate or display actual teaching duration from the recorded session information.
- **FR-TEACH-004** The system shall preserve both scheduled and actual teaching information.
- **FR-TEACH-005** The system shall associate the teaching record with the actual coach.
- **FR-TEACH-006** A Coach shall not submit an incomplete session record when required session information is missing.

### 6.9 Routine Attendance Requirements

- **FR-RATT-001** Coach shall be able to add active athletes to a Routine Training attendance record.
- **FR-RATT-002** Routine Training shall not require a pre-assigned athlete roster.
- **FR-RATT-003** The system shall not automatically mark athletes as absent when they were not selected for a Routine Training session.
- **FR-RATT-004** A Routine Training session shall prevent duplicate attendance entries for the same athlete.
- **FR-RATT-005** Coach shall be able to record Present or Late status for athletes who attended.
- **FR-RATT-006** Late attendance may include an arrival time and remark.

### 6.10 Private Attendance Requirements

Private Training attendance statuses shall support:

- Present
- Absent
- Late
- Leave / Excused

Requirements:

- **FR-PATT-001** The system shall display all athletes assigned to the Private Training session.
- **FR-PATT-002** Coach shall record an attendance status for each assigned athlete before final submission.
- **FR-PATT-003** Late attendance may include an arrival time and remark.
- **FR-PATT-004** Absence or Leave / Excused may include a remark.
- **FR-PATT-005** A Private Training session shall prevent duplicate attendance entries for the same athlete.

### 6.11 Training Log Requirements

- **FR-LOG-001** Coach shall be able to record a training log for a session.
- **FR-LOG-002** The training log shall remain linked to the training session.
- **FR-LOG-003** Training log content shall remain visible in historical session review.
- **FR-LOG-004** Training log data may be corrected only while the session is editable.

### 6.12 Substitute Coach Requirements

- **FR-SUB-001** Authorized users shall be able to assign a substitute coach.
- **FR-SUB-002** A substitute assignment shall require a reason.
- **FR-SUB-003** The original assigned coach shall remain preserved.
- **FR-SUB-004** The system shall use the actual coach for actual teaching-hour reporting.
- **FR-SUB-005** Assigning a substitute coach shall be validated against the substitute coach's schedule conflicts.

### 6.13 Cancellation & Rescheduling Requirements

- **FR-CR-001** Authorized users shall be able to cancel a future or uncompleted session.
- **FR-CR-002** Cancellation shall require a reason.
- **FR-CR-003** Cancelled sessions shall remain visible in history.
- **FR-CR-004** Authorized users shall be able to reschedule a future or uncompleted session.
- **FR-CR-005** A rescheduled date/time shall be validated for coach and athlete conflicts as applicable.
- **FR-CR-006** The original session shall remain traceable after rescheduling.
- **FR-CR-007** Rescheduling shall not duplicate teaching-hour or attendance totals.

### 6.14 Schedule Conflict Requirements

- **FR-CONFLICT-001** The system shall detect overlapping training sessions for the same coach.
- **FR-CONFLICT-002** The system shall detect overlapping Private Training sessions for the same athlete.
- **FR-CONFLICT-003** Private Training shall be validated against the coach's Routine Training sessions.
- **FR-CONFLICT-004** When an authorized override is permitted, the system shall require an override reason.
- **FR-CONFLICT-005** An overridden conflict shall remain identifiable in history.

### 6.15 Approval & Locking Requirements

- **FR-APPROVAL-001** Coach shall be able to submit a Completed training record.
- **FR-APPROVAL-002** Administrator shall be able to Approve, Reject, or Request Revision for a submitted record.
- **FR-APPROVAL-003** Approved records shall become Locked.
- **FR-APPROVAL-004** Locked records shall not be editable by Coaches.
- **FR-APPROVAL-005** Authorized Administrator shall be able to unlock a Locked record when correction is necessary.
- **FR-APPROVAL-006** Unlocking a record shall require a reason.
- **FR-APPROVAL-007** Approval, rejection, revision, and unlock actions shall remain visible in history.

### 6.16 Coach Dashboard Requirements

The Coach Dashboard shall show at least:

- Today's Training Sessions
- Upcoming Training Sessions
- Completed Sessions
- Remaining Sessions
- Routine Sessions
- Private Sessions
- Pending or incomplete training records
- Monthly teaching-hour summary

Requirements:

- **FR-CDASH-001** Coach shall be able to open a session directly from the dashboard.
- **FR-CDASH-002** Today's sessions shall clearly indicate training type and session status.
- **FR-CDASH-003** Sessions requiring coach action shall be distinguishable from finalized sessions.

### 6.17 Administrator Dashboard Requirements

The Administrator Dashboard shall show at least:

- Training Sessions Today
- Completed Sessions
- Upcoming Sessions
- Cancelled Sessions
- Routine Sessions
- Private Sessions
- Coaches Teaching Today
- Athlete Attendance Summary
- Coach Teaching Hours

Requirements:

- **FR-ADASH-001** Administrator shall be able to filter dashboard information by date or date range.
- **FR-ADASH-002** Administrator shall be able to filter relevant information by coach.
- **FR-ADASH-003** Administrator shall be able to filter relevant information by training type.
- **FR-ADASH-004** Administrator shall be able to open the related operational records from dashboard summaries where applicable.

### 6.18 Coach Teaching-Hour Report Requirements

- **FR-RPT-COACH-001** The system shall report Routine Training teaching hours.
- **FR-RPT-COACH-002** The system shall report Private Training teaching hours.
- **FR-RPT-COACH-003** The system shall report total teaching hours.
- **FR-RPT-COACH-004** The report shall support filtering by coach.
- **FR-RPT-COACH-005** The report shall support filtering by date range.
- **FR-RPT-COACH-006** The report shall support filtering by training type.
- **FR-RPT-COACH-007** Actual coach shall receive teaching-hour credit when a substitute coach taught the session.
- **FR-RPT-COACH-008** Cancelled, rescheduled-original, and Coach Absent sessions shall not count as completed teaching hours.
- **FR-RPT-COACH-009** Teaching-hour totals shall use finalized session records according to the approval rules in use.

### 6.19 Athlete Attendance Report Requirements

- **FR-RPT-ATH-001** The system shall provide attendance history per athlete.
- **FR-RPT-ATH-002** The report shall distinguish Routine and Private Training.
- **FR-RPT-ATH-003** The report shall support filtering by athlete.
- **FR-RPT-ATH-004** The report shall support filtering by date range.
- **FR-RPT-ATH-005** Private Training attendance summaries may include Present, Absent, Late, and Leave / Excused counts.
- **FR-RPT-ATH-006** Routine attendance shall report recorded attendance only and shall not infer absent sessions from sessions where the athlete was not selected.
- **FR-RPT-ATH-007** The report shall not double-count attendance from rescheduled-original sessions.

### 6.20 History & Audit Requirements

The system shall retain business history for at least:

- Schedule creation and changes
- Session cancellation
- Session rescheduling
- Coach substitution
- Attendance changes
- Session completion
- Submission
- Approval
- Rejection
- Revision request
- Unlock actions

Requirements:

- **FR-AUDIT-001** Each tracked action shall identify the action type, action date/time, and responsible user.
- **FR-AUDIT-002** Historical records shall remain available after related coaches or athletes become inactive.
- **FR-AUDIT-003** Material changes to finalized operational records shall remain traceable.

---

## 7. Non-Functional Requirements

This section contains only requirements specific to this project. Shared engineering, security, performance, UI, API, deployment, and coding standards are defined in `skill.md`.

### 7.1 Historical Integrity

- **NFR-001** Completed and finalized training history shall remain stable when master data or future recurring schedules are changed.
- **NFR-002** Historical records shall preserve the business identity of the coach and athlete involved at the time of the transaction.

### 7.2 Traceability

- **NFR-003** Important operational changes shall be traceable to the responsible user and action time.
- **NFR-004** Approval and unlock actions shall be traceable for administrative review.

### 7.3 Reporting Consistency

- **NFR-005** Dashboard totals and reports shall use consistent session and attendance status rules.
- **NFR-006** A single business event shall not be counted more than once because of cancellation, rescheduling, or substitution history.

### 7.4 Operational Usability

- **NFR-007** The primary coach workflow for today's session shall require minimal navigation between schedule, attendance, training log, completion, and submission.
- **NFR-008** Session status and required next actions shall be clear to the responsible user.

---

## 8. Project-Specific Requirements

### 8.1 Routine Training Rules

- Routine Training represents recurring teaching at the gym's regular venue.
- Routine Training shall not contain Group, Team, or Location fields.
- Routine Training shall not maintain a fixed athlete roster.
- Athlete participation in Routine Training is recorded by the coach at the session.
- Unselected athletes shall not be treated as absent.

### 8.2 Private Training Rules

- Private Training shall always identify the athlete(s) expected to attend.
- Private Training may contain one or multiple athletes.
- Location is optional for Private Training.
- Attendance shall be recorded against the athletes assigned to the session.

### 8.3 Coach Assignment Rules

- Scheduled Coach and Actual Coach are separate business concepts.
- The original coach assignment shall remain visible after substitution.
- Actual teaching-hour reporting shall use the coach who actually taught the session.

### 8.4 Time and Duration Rules

- End time shall be later than start time for scheduled and actual session data.
- Actual teaching duration shall not be negative.
- Cancelled sessions shall not contribute to actual teaching hours.
- Rescheduled-original sessions shall not contribute to actual teaching hours.
- A substitute coach's completed session shall count only once.

### 8.5 Record Finalization Rules

- A coach may edit the session while the record is in an editable state.
- A submitted record requires administrative review when the approval workflow is enabled.
- An approved record becomes Locked.
- Locked records require authorized administrative action before correction.
- Unlock actions require a reason and remain in history.

### 8.6 Future Compatibility Requirements

Future modules are not part of the initial implementation, but existing operational data shall remain usable if the system is later extended with:

- Private Training Packages
- Coach Compensation
- Athlete Performance Tracking
- Training Program / Exercise Library
- Athlete / Parent Portal
- Private Training Booking
- Payment Management
- Notifications
- Advanced Management Analytics

Adding future modules shall not require changing the meaning of existing Routine Training, Private Training, Training Session, Coach Teaching, or Athlete Attendance historical records.

---

## 9. UI/UX Requirements

Shared visual style, responsive behavior, form behavior, list behavior, theme, and other general UI standards are governed by `skill.md`. This section defines only project-specific screens and interactions.

### 9.1 Coach Home

The Coach Home shall prioritize:

- Today's sessions
- Upcoming sessions
- Session type
- Scheduled time
- Current status
- Required next action

Coach shall be able to open the relevant session directly from this view.

### 9.2 Coach Session Screen

The session screen shall present the operational flow in a clear sequence:

1. Session Summary
2. Start / Actual Teaching Information
3. Athlete Attendance
4. Training Log
5. Complete Session
6. Submit Record, when required

Routine and Private sessions shall clearly display different attendance behavior.

### 9.3 Routine Attendance Screen

- Coach shall be able to search and select athletes from the Athlete Master.
- Already-added athletes shall not be selectable as duplicate attendance entries.
- The screen shall not display a mandatory pre-assigned athlete roster.
- The screen shall not imply that unselected athletes are absent.

### 9.4 Private Attendance Screen

- The screen shall display all athletes assigned to the Private Training session.
- Attendance status shall be selectable for each athlete.
- Missing required attendance status shall be clearly identifiable before submission.

### 9.5 Routine Schedule Screen

The Routine Schedule form shall contain only business-relevant routine fields.

It shall not display:

- Group
- Team
- Location

### 9.6 Private Training Screen

The Private Training form shall clearly support:

- Coach selection
- Multiple athlete selection
- Date
- Start time
- End time
- Optional location
- Remarks
- Session status

### 9.7 Administrative Review Screen

The review screen shall clearly show:

- Scheduled teaching information
- Actual teaching information
- Assigned coach
- Actual coach
- Athlete attendance
- Training log
- Session status
- Submission history
- Available approval actions

### 9.8 Reports

Reports shall clearly distinguish:

- Routine Training
- Private Training
- Scheduled Coach
- Actual Coach
- Session status
- Attendance status where applicable

---

## 10. Out of Scope

The following capabilities are not included in the initial implementation unless separately approved as additional scope:

### 10.1 Private Training Package Management

Including:

- Package sales
- Package session balance
- Package expiry
- Automatic package deduction

### 10.2 Coach Compensation Management

Including:

- Rate per hour
- Rate per session
- Allowances
- Monthly compensation calculation
- Payroll integration

### 10.3 Athlete Performance Tracking

Including:

- Skill assessments
- Physical assessments
- Development goals
- Progress scoring
- Performance charts

### 10.4 Training Program / Exercise Library

Including:

- Exercise master
- Drill library
- Training videos
- Standard training programs

### 10.5 Athlete / Parent Portal

Including:

- Athlete self-service
- Parent access
- Attendance viewing
- Coach feedback viewing
- Package balance viewing

### 10.6 Self-Service Private Training Booking

Including:

- Athlete or parent booking requests
- Coach availability booking
- Booking approval flow

### 10.7 Payment Management

Including:

- Online payment
- PromptPay
- Receipts
- Payment status tracking
- Refund processing

### 10.8 External Notification Integrations

Including:

- General operational email notifications; the transactional password-reset email required by `FR-USER-006` is in scope
- LINE notifications
- Messaging platform integrations
- Automated reminder campaigns

### 10.9 Advanced Management Analytics

Including:

- Revenue analytics
- Coach compensation analytics
- Athlete retention analytics
- Package utilization analytics
- Athlete development analytics

### 10.10 Routine Group / Location Management

The initial Routine Training scope does not include:

- Routine Training groups
- Routine Training teams
- Routine Training location assignment
- Routine athlete roster assignment

---
