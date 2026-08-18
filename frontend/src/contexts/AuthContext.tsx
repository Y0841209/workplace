import React, { createContext, useContext, useState, useEffect, useCallback, ReactNode } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import { authService, User, LoginResponse } from '../services/authService';

interface AuthContextType {
  user: User | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  login: (redirectUrl?: string) => Promise<void>;
  logout: () => Promise<void>;
  refreshUser: () => Promise<void>;
  hasRole: (role: string) => boolean;
  hasAnyRole: (roles: string[]) => boolean;
  canReserve: (resourceType: string) => boolean;
  getAccessToken: () => string | null;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const navigate = useNavigate();
  const location = useLocation();

  const checkAuth = useCallback(async () => {
    try {
      const storedUser = authService.getStoredUser();
      if (storedUser && authService.isTokenValid()) {
        setUser(storedUser);
      } else {
        // Try to refresh token
        const refreshed = await authService.refreshToken();
        if (refreshed) {
          const newUser = authService.getStoredUser();
          if (newUser) setUser(newUser);
        }
      }
    } catch (error) {
      console.error('Auth check failed:', error);
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    checkAuth();
  }, [checkAuth]);

  const login = async (redirectUrl?: string) => {
    const from = redirectUrl || location.state?.from?.pathname || '/dashboard';
    await authService.login(from);
  };

  const logout = async () => {
    await authService.logout();
    setUser(null);
    navigate('/login', { replace: true });
  };

  const refreshUser = async () => {
    try {
      const updatedUser = await authService.getCurrentUser();
      setUser(updatedUser);
    } catch (error) {
      console.error('Failed to refresh user:', error);
    }
  };

  const hasRole = (role: string): boolean => {
    return user?.roles?.includes(role) ?? false;
  };

  const hasAnyRole = (roles: string[]): boolean => {
    return roles.some(role => hasRole(role));
  };

  const canReserve = (resourceType: string): boolean => {
    if (hasRole('GLOBAL_ADMIN')) return true;
    if (hasRole('ROOM_ADMIN') && resourceType === 'MEETING_ROOM') return true;
    return user?.businessProfiles?.some(p => 
      authService.canProfileReserve(p.profileCode, resourceType)
    ) ?? false;
  };

  const getAccessToken = (): string | null => {
    return authService.getAccessToken();
  };

  const value: AuthContextType = {
    user,
    isAuthenticated: !!user,
    isLoading,
    login,
    logout,
    refreshUser,
    hasRole,
    hasAnyRole,
    canReserve,
    getAccessToken,
  };

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (context === undefined) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
}