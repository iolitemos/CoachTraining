export interface UserListItem {
  userId: number;
  username: string;
  fullName: string;
  email: string;
  isActive: boolean;
  roles: string[];
  coachCode: string | null;
}

export interface UserDetail {
  userId: number;
  username: string;
  fullName: string;
  email: string;
  isActive: boolean;
  roleIds: number[];
  roles: string[];
  coachId: number | null;
  coachCode: string | null;
}

export interface UserCreateRequest {
  username: string;
  email: string;
  fullName: string;
  password: string;
  roleIds: number[];
}

export interface UserUpdateRequest {
  email: string;
  fullName: string;
}

export interface RoleOption {
  roleId: number;
  name: string;
}

export interface CoachOption {
  coachId: number;
  coachCode: string;
  fullName: string;
}
