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
  Scheduled: { label: 'กำหนดการ', className: 'bg-blue-100 text-blue-800' },
  InProgress: { label: 'กำลังฝึกซ้อม', className: 'bg-amber-100 text-amber-800' },
  Completed: { label: 'เสร็จสิ้น', className: 'bg-emerald-100 text-emerald-800' },
  Submitted: { label: 'ส่งตรวจแล้ว', className: 'bg-violet-100 text-violet-800' },
  Approved: { label: 'อนุมัติแล้ว', className: 'bg-green-100 text-green-800' },
  Locked: { label: 'ล็อกแล้ว', className: 'bg-secondary-200 text-secondary-700' },
  Cancelled: { label: 'ยกเลิก', className: 'bg-danger-surface text-danger' },
  Rescheduled: { label: 'เลื่อนตาราง', className: 'bg-orange-100 text-orange-800' },
  CoachAbsent: { label: 'โค้ชขาด', className: 'bg-rose-100 text-rose-800' },
};

export function getStatusDisplay(status: TrainingSessionStatus): StatusDisplay {
  return STATUS_DISPLAY[status];
}
