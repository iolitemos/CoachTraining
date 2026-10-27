export type CompensationStatementStatus = 'Draft' | 'Confirmed' | 'Paid';

export interface CoachCompensationPeriodRequest {
  coachId: number;
  year: number;
  month: number;
}

export interface CoachCompensationItem {
  coachCompensationItemId: number;
  trainingSessionId: number;
  sessionDate: string;
  scheduledStartDateTime: string;
  scheduledEndDateTime: string;
  coachName: string;
  participantNames: string;
  privateRate: number;
  amount: number;
}

export interface CoachCompensationStatement {
  coachCompensationStatementId: number;
  coachId: number;
  coachCode: string;
  coachFullName: string;
  coachNickname: string | null;
  bankName: string | null;
  bankAccountNumber: string | null;
  bankAccountName: string | null;
  year: number;
  month: number;
  status: CompensationStatementStatus;
  baseSalary: number;
  privateSessionCount: number;
  privateCompensation: number;
  adjustmentAmount: number;
  adjustmentReason: string | null;
  netAmount: number;
  confirmedDate: string | null;
  paidDate: string | null;
  items: CoachCompensationItem[];
}
