import React from 'react';
import ReactDOM from 'react-dom/client';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ReactQueryDevtools } from '@tanstack/react-query-devtools';
import { BrowserRouter } from 'react-router-dom';
import { ThemeProvider, CssBaseline } from '@mui/material';
import { SnackbarProvider } from 'react-hot-toast';
import theme from './theme';
import App from './App';
import { AuthProvider } from './contexts/AuthContext';
import './styles/globals.css';

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 1000 * 60 * 2,
      gcTime: 1000 * 60 * 10,
      retry: (failureCount, error: any) => {
        if (error?.response?.status === 401) return false;
        return failureCount < 3;
      },
      refetchOnWindowFocus: false,
      refetchOnReconnect: true,
    },
  },
});

ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <ThemeProvider theme={theme}>
          <CssBaseline />
          <SnackbarProvider
            position="bottom-right"
            toastOptions={{
              duration: 4000,
              style: {
                background: '#0E0E0E',
                color: '#FFD800',
                borderRadius: '8px',
                padding: '12px 16px',
              },
              success: {
                iconTheme: {
                  primary: '#FFD800',
                  secondary: '#0E0E0E',
                },
              },
              error: {
                iconTheme: {
                  primary: '#FF4444',
                  secondary: '#FFFFFF',
                },
              },
            }}
          >
            <AuthProvider>
              <App />
            </AuthProvider>
          </SnackbarProvider>
        </ThemeProvider>
      </BrowserRouter>
      <ReactQueryDevtools initialIsOpen={false} />
    </QueryClientProvider>
  </React.StrictMode>
);