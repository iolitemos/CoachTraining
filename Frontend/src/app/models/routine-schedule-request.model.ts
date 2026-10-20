export type RoutineScheduleRequestStatus =
  'Pending' | 'Approved' | 'RevisionRequested' | 'Rejected' | 'Cancelled';

export interface RoutineScheduleRequest {
  routineScheduleRequestId: number;
  submissionBatchId: string;
  coachId: number;
  coachCode: string;
  coachName: string;
  coachNickname: string | null;
  coachColorHex: string;
  proposedDate: string;
  proposedStartTime: string;
  proposedEndTime: string;
  proposedRemarks: string | null;
  status: RoutineScheduleRequestStatus;
  approvedDate: string | null;
  approvedStartTime: string | null;
  approvedEndTime: string | null;
  approvedRemarks: string | null;
  reviewReason: string | null;
  reviewedDate: string | null;
  routineScheduleId: number | null;
  currentScheduleCoachId: number | null;
  currentScheduleCoachCode: string | null;
  currentScheduleCoachName: string | null;
  currentScheduleCoachNickname: string | null;
  currentScheduleCoachColorHex: string | null;
  currentScheduleDate: string | null;
  currentScheduleStartTime: string | null;
  currentScheduleEndTime: string | null;
  currentScheduleRemarks: string | null;
  createdDate: string;
}

export interface RoutineScheduleRequestReview {
  approvedDate: string;
  approvedStartTime: string;
  approvedEndTime: string;
  approvedRemarks: string | null;
  overrideConflict: boolean;
  overrideReason: string | null;
}
