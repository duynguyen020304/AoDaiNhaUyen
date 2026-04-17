export interface ApiError {
  code: string;
  message: string;
}

export interface ApiEnvelope<T> {
  success: boolean;
  message: string;
  data: T;
  errors: ApiError[] | null;
  timestamp: string;
}
