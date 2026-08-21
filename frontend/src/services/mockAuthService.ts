import { User } from '../types';

export const mockUser: User = {
  id: '11111111-1111-1111-1111-111111111111',
  entraObjectId: '11111111-1111-1111-1111-111111111111',
  email: 'dev@local.com',
  displayName: 'Developer Local',
  jobTitle: 'Software Engineer',
  department: 'Engineering',
  roles: ['GLOBAL_ADMIN', 'ROOM_ADMIN', 'SUPPORT', 'USER'],
  businessProfiles: ['GLOBAL_ADMIN', 'ROOM_ADMIN', 'SUPPORT', 'LEADER', 'PARTNER', 'DIRECTOR', 'ASSOCIATE', 'COLLABORATOR'],
  permissions: {
    canReserve: {
      OPEN_WORKSPACE: true,
      CLOSED_OFFICE: true,
      MEETING_ROOM: true
    }
  }
};

export const mockAuthService = {
  async login(): Promise<User> {
    // Simulate network delay
    await new Promise(resolve => setTimeout(resolve, 500));
    
    // Store in localStorage for persistence
    localStorage.setItem('booking_auth', JSON.stringify({
      accessToken: 'dev-token-' + Date.now(),
      refreshToken: 'dev-refresh-token',
      user: mockUser
    }));
    
    return mockUser;
  },

  async logout(): Promise<void> {
    localStorage.removeItem('booking_auth');
  },

  getCurrentUser(): User | null {
    const stored = localStorage.getItem('booking_auth');
    if (stored) {
      try {
        return JSON.parse(stored).user;
      } catch {
        return null;
      }
    }
    return null;
  },

  isAuthenticated(): boolean {
    return !!localStorage.getItem('booking_auth');
  },

  getAccessToken(): string | null {
    const stored = localStorage.getItem('booking_auth');
    if (stored) {
      try {
        return JSON.parse(stored).accessToken;
      } catch {
        return null;
      }
    }
    return null;
  },

  async refreshToken(): Promise<boolean> {
    // In dev, always refresh successfully
    return true;
  }
};

// Hook for using dev auth in components
export function useDevAuth() {
  const [user, setUser] = useState<User | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const user = mockAuthService.getCurrentUser();
    setUser(user);
    setLoading(false);
  }, []);

  const login = async () => {
    setLoading(true);
    const user = await mockAuthService.login();
    setUser(user);
    setLoading(false);
    return user;
  };

  const logout = async () => {
    await mockAuthService.logout();
    setUser(null);
  };

  return { user, loading, login, logout, isAuthenticated: !!mockAuthService.getCurrentUser() };
}

// Need to import useState and useEffect
import { useState, useEffect } from 'react';