/** Mirrors the backend PagedResponse<T> envelope. */
export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

/** Mirrors the backend ApiResponse<T> / ApiErrorResponse envelopes. */
export interface ApiSuccessBody<T> {
  message: string;
  data: T;
}

export interface ApiFieldError {
  field: string;
  message: string;
}

export interface ApiErrorBody {
  message: string;
  errors: ApiFieldError[];
}
