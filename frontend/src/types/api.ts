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

export interface PaginatedApiEnvelope<T> extends ApiEnvelope<T> {
  hasNextPage: boolean;
  hasPreviousPage: boolean;
  totalPage: number;
  totalItem: number;
}
