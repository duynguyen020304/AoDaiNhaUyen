export interface UserProfile {
  id: number;
  fullName: string;
  email: string | null;
  phone: string | null;
  dateOfBirth: string | null;
  gender: string | null;
  avatarUrl: string | null;
  address: string | null;
}

export interface UpdateProfilePayload {
  fullName: string;
  phone: string;
  dateOfBirth: string;
  gender: string;
  address: string;
}
