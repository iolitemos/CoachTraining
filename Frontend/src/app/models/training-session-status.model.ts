/**
 * Training Session business statuses (requirement.md section 6.7).
 * Full status-transition and locking rules are implemented in the Training
 * Session backend module (todo.md section 4.6) — this file only defines the
 * shared display shape used by the status badge component.
 */
export type TrainingSessionStatus =
  | 'Scheduled'
  | 'InProgress'
  | 'Completed'
  | 'Submitted'
  | 'Approved'
  | 'Locked'
  | 'Cancelled'
  | 'Rescheduled'
  | 'CoachAbsent';

export interface StatusDisplay {
  label: string;
  className: string;
}

const STATUS_DISPLAY: Record<TrainingSessionStatus, StatusDisplay> = {
  Scheduled: { label: 'กำหนดการ', className: 'bg-secondary-100 text-secondary-700' },
  InProgress: { label: 'กำลังฝึกซ้อม', className: 'bg-primary-100 text-primary-700' },
  Completed: { label: 'เสร็จสิ้น', className: 'bg-primary-100 text-primary-700' },
  Submitted: { label: 'ส่งตรวจแล้ว', className: 'bg-warning-surface text-warning' },
  Approved: { label: 'อนุมัติแล้ว', className: 'bg-success-surface text-success' },
  Locked: { label: 'ล็อกแล้ว', className: 'bg-secondary-200 text-secondary-700' },
  Cancelled: { label: 'ยกเลิก', className: 'bg-danger-surface text-danger' },
  Rescheduled: { label: 'เลื่อนตาราง', className: 'bg-warning-surface text-warning' },
  CoachAbsent: { label: 'โค้ชขาด', className: 'bg-danger-surface text-danger' },
};

export function getStatusDisplay(status: TrainingSessionStatus): StatusDisplay {
  return STATUS_DISPLAY[status];
}
