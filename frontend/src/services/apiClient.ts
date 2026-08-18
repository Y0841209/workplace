import axios, { AxiosInstance, AxiosError, InternalAxiosRequestConfig } from 'axios';
import { authService } from './authService';

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || '/api/v1';

class ApiClient {
  private client: AxiosInstance;

  constructor() {
    this.client = axios.create({
      baseURL: API_BASE_URL,
      headers: {
        'Content-Type': 'application/json',
      },
      timeout: 30000,
    });

    this.setupInterceptors();
  }

  private setupInterceptors() {
    this.client.interceptors.request.use(
      async (config: InternalAxiosRequestConfig) => {
        const token = authService.getAccessToken();
        if (token) {
          config.headers.Authorization = `Bearer ${token}`;
        }
        config.headers['X-Correlation-ID'] = crypto.randomUUID();
        return config;
      },
      (error) => Promise.reject(error)
    );

    this.client.interceptors.response.use(
      (response) => response,
      async (error: AxiosError) => {
        const originalRequest = error.config as InternalAxiosRequestConfig & { _retry?: boolean };

        if (error.response?.status === 401 && !originalRequest._retry && authService.getRefreshToken()) {
          originalRequest._retry = true;

          const refreshed = await authService.refreshToken();
          if (refreshed) {
            originalRequest.headers.Authorization = `Bearer ${authService.getAccessToken()}`;
            return this.client(originalRequest);
          }

          await authService.logout();
          window.location.href = '/login';
        }

        const apiError = this.transformError(error);
        return Promise.reject(apiError);
      }
    );
  }

  private transformError(error: AxiosError): ApiError {
    if (error.response) {
      const data = error.response.data as any;
      return new ApiError(
        error.response.status,
        data?.title || error.message,
        data?.detail || error.message,
        data?.errors || [],
        data?.type
      );
    }

    if (error.request) {
      return new ApiError(0, 'Network Error', 'No se pudo conectar al servidor', [], 'network');
    }

    return new ApiError(0, 'Unknown Error', error.message, [], 'unknown');
  }

  async get<T>(url: string, params?: Record<string, any>): Promise<T> {
    const response = await this.client.get<T>(url, { params });
    return response.data;
  }

  async post<T>(url: string, data?: any): Promise<T> {
    const response = await this.client.post<T>(url, data);
    return response.data;
  }

  async put<T>(url: string, data?: any): Promise<T> {
    const response = await this.client.put<T>(url, data);
    return response.data;
  }

  async patch<T>(url: string, data?: any): Promise<T> {
    const response = await this.client.patch<T>(url, data);
    return response.data;
  }

  async delete<T>(url: string): Promise<T> {
    const response = await this.client.delete<T>(url);
    return response.data;
  }

  getClient(): AxiosInstance {
    return this.client;
  }
}

export class ApiError extends Error {
  constructor(
    public status: number,
    public title: string,
    public detail: string,
    public validationErrors: Array<{ identifier: string; errorMessage: string }>,
    public type?: string
  ) {
    super(detail);
    this.name = 'ApiError';
  }

  get isValidationError(): boolean {
    return this.status === 400 && this.validationErrors.length > 0;
  }

  get isNotFound(): boolean {
    return this.status === 404;
  }

  get isConflict(): boolean {
    return this.status === 409;
  }

  get isUnauthorized(): boolean {
    return this.status === 401;
  }

  get isForbidden(): boolean {
    return this.status === 403;
  }

  getValidationError(field: string): string | undefined {
    return this.validationErrors.find(e => e.identifier === field)?.errorMessage;
  }
}

export const apiClient = new ApiClient();