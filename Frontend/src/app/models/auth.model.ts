/** Canonical role names — must match Backend/CoachTraining.Api/Constants/Roles.cs. */
export const AppRole = {
  Administrator: 'Administrator',
  Coach: 'Coach',
  ManagementViewer: 'ManagementViewer',
} as const;

export type AppRole = (typeof AppRole)[keyof typeof AppRole];

export interface CurrentUser {
  userId: number;
  username: string;
  fullName: string;
  email: string;
  roles: string[];
  coachId: number | null;
}

export interface LoginRequest {
  username: string;
  password: string;
}

export interface LoginResponse {
  token: string;
  expiresAtUtc: string;
  user: CurrentUser;
}

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
}

export interface ForgotPasswordRequest {
  email: string;
}

export interface ResetPasswordRequest {
  token: string;
  newPassword: string;
}

const ROLE_LABELS_TH: Record<string, string> = {
  [AppRole.Administrator]: 'ผู้ดูแลระบบ',
  [AppRole.Coach]: 'โค้ช',
  [AppRole.ManagementViewer]: 'ผู้บริหาร/ผู้ชม',
};

export function getRoleLabel(role: string): string {
  return ROLE_LABELS_TH[role] ?? role;
}
