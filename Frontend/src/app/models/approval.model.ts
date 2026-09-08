import { TrainingSessionDetail } from './training-session.model';

/** requirement.md section 5.8 / FR-APPROVAL-* workflow actions. */
export type ApprovalActionType = 'Submit' | 'Approve' | 'Reject' | 'RequestRevision' | 'Unlock';

/** Immutable business details recorded for one approval workflow action. */
export interface TrainingApprovalHistory {
  trainingApprovalHistoryId: number;
  trainingSessionId: number;
  actionType: ApprovalActionType;
  reason: string | null;
  actionByUserId: number;
  actionDate: string;
}

export interface TrainingApprovalActionResponse {
  session: TrainingSessionDetail;
  history: TrainingApprovalHistory;
}

const APPROVAL_ACTION_LABELS_TH: Record<ApprovalActionType, string> = {
  Submit: 'ส่งตรวจ',
  Approve: 'อนุมัติ',
  Reject: 'ตีกลับ',
  RequestRevision: 'ขอให้แก้ไข',
  Unlock: 'ปลดล็อก',
};

export function getApprovalActionLabel(action: ApprovalActionType): string {
  return APPROVAL_ACTION_LABELS_TH[action];
}
