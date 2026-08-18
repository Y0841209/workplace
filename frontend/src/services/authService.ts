import axios, { AxiosInstance, InternalAxiosRequestConfig } from 'axios';
import { jwtDecode } from 'jwt-decode';

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || '/api/v1';

interface JWTPayload {
  sub: string;
  email: string;
  name: string;
  jobTitle?: string;
  department?: string;
  roles: string[];
  businessProfiles: string[];
  exp: number;
  iat: number;
}

export interface User {
  id: string;
  entraObjectId: string;
  email: string;
  displayName: string;
  jobTitle?: string;
  department?: string;
  roles: string[];
  businessProfiles: string[];
  permissions?: Record<string, boolean>;
}

interface LoginResponse {
  accessToken: string;
  refreshToken: string;
  user: User;
}

class AuthService {
  private accessToken: string | null = null;
  private refreshToken: string | null = null;
  private user: User | null = null;
  private tokenRefreshPromise: Promise<boolean> | null = null;

  constructor() {
    this.loadFromStorage();
  }

  private loadFromStorage() {
    try {
      const stored = localStorage.getItem('booking_auth');
      if (stored) {
        const { accessToken, refreshToken, user } = JSON.parse(stored);
        if (accessToken && this.isTokenValid(accessToken)) {
          this.accessToken = accessToken;
          this.refreshToken = refreshToken;
          this.user = user;
        } else if (refreshToken) {
          this.refreshToken = refreshToken;
        }
      }
    } catch (error) {
      console.error('Failed to load auth from storage:', error);
      this.clearStorage();
    }
  }

  private saveToStorage() {
    if (this.accessToken && this.refreshToken && this.user) {
      localStorage.setItem('booking_auth', JSON.stringify({
        accessToken: this.accessToken,
        refreshToken: this.refreshToken,
        user: this.user,
      }));
    }
  }

  private clearStorage() {
    this.accessToken = null;
    this.refreshToken = null;
    this.user = null;
    localStorage.removeItem('booking_auth');
  }

  isTokenValid(token?: string): boolean {
    const t = token || this.accessToken;
    if (!t) return false;
    try {
      const decoded = jwtDecode<JWTPayload>(t);
      return decoded.exp * 1000 > Date.now() + 30000; // 30s buffer
    } catch {
      return false;
    }
  }

  getAccessToken(): string | null {
    return this.accessToken;
  }

  getRefreshToken(): string | null {
    return this.refreshToken;
  }

  getStoredUser(): User | null {
    return this.user;
  }

  async login(redirectUrl: string): Promise<void> {
    const authUrl = `${API_BASE_URL.replace('/api/v1', '')}/auth/login?redirect=${encodeURIComponent(redirectUrl)}`;
    window.location.href = authUrl;
  }

  async handleCallback(code: string, state: string): Promise<LoginResponse> {
    const response = await axios.post<LoginResponse>(`${API_BASE_URL}/auth/callback`, { code, state });
    const { accessToken, refreshToken, user } = response.data;
    
    this.accessToken = accessToken;
    this.refreshToken = refreshToken;
    this.user = user;
    this.saveToStorage();
    
    return response.data;
  }

  async logout(): Promise<void> {
    try {
      if (this.refreshToken) {
        await axios.post(`${API_BASE_URL}/auth/logout`, { refreshToken: this.refreshToken });
      }
    } catch (error) {
      console.error('Logout error:', error);
    } finally {
      this.clearStorage();
    }
  }

  async refreshToken(): Promise<boolean> {
    if (this.tokenRefreshPromise) return this.tokenRefreshPromise;
    
    if (!this.refreshToken) return false;
    
    this.tokenRefreshPromise = this.performTokenRefresh();
    const result = await this.tokenRefreshPromise;
    this.tokenRefreshPromise = null;
    return result;
  }

  private async performTokenRefresh(): Promise<boolean> {
    try {
      const response = await axios.post<{ accessToken: string; refreshToken: string }>(
        `${API_BASE_URL}/auth/refresh`,
        { refreshToken: this.refreshToken }
      );
      
      this.accessToken = response.data.accessToken;
      this.refreshToken = response.data.refreshToken;
      this.saveToStorage();
      return true;
    } catch (error) {
      console.error('Token refresh failed:', error);
      this.clearStorage();
      return false;
    }
  }

  async getCurrentUser(): Promise<User> {
    const response = await this.createAuthorizedClient().get<User>(`${API_BASE_URL}/auth/me`);
    this.user = response.data;
    this.saveToStorage();
    return response.data;
  }

  canProfileReserve(profileCode: string, resourceType: string): boolean {
    const policyMatrix: Record<string, Record<string, boolean>> = {
      COLLABORATOR: { OPEN_WORKSPACE: true, CLOSED_OFFICE: false, MEETING_ROOM: true },
      ASSOCIATE: { OPEN_WORKSPACE: true, CLOSED_OFFICE: false, MEETING_ROOM: true },
      LEADER: { OPEN_WORKSPACE: true, CLOSED_OFFICE: true, MEETING_ROOM: true },
      DIRECTOR: { OPEN_WORKSPACE: true, CLOSED_OFFICE: true, MEETING_ROOM: true },
      PARTNER: { OPEN_WORKSPACE: true, CLOSED_OFFICE: true, MEETING_ROOM: true },
    };
    return policyMatrix[profileCode]?.[resourceType] ?? false;
  }

  createAuthorizedClient(): AxiosInstance {
    const client = axios.create({ baseURL: API_BASE_URL });
    
    client.interceptors.request.use((config: InternalAxiosRequestConfig) => {
      if (this.accessToken) {
        config.headers.Authorization = `Bearer ${this.accessToken}`;
      }
      config.headers['X-Correlation-ID'] = crypto.randomUUID();
      return config;
    });
    
    client.interceptors.response.use(
      (response) => response,
      async (error) => {
        const originalRequest = error.config as InternalAxiosRequestConfig & { _retry?: boolean };
        
        if (error.response?.status === 401 && !originalRequest._retry && this.refreshToken) {
          originalRequest._retry = true;
          
          const refreshed = await this.refreshToken();
          if (refreshed) {
            originalRequest.headers.Authorization = `Bearer ${this.accessToken}`;
            return client(originalRequest);
          }
          
          await this.logout();
          window.location.href = '/login';
        }
        
        return Promise.reject(error);
      }
    );
    
    return client;
  }
}

export const authService = new AuthService();

export function createApiClient(): AxiosInstance {
  return authService.createAuthorizedClient();
}