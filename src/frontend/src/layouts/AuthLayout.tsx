import React from 'react';
import { Outlet } from 'react-router-dom';
import { Box, Container, Paper, Typography, CssBaseline } from '@mui/material';
import { theme } from '../theme';

export function AuthLayout({ children }: { children: React.ReactNode }) {
  return (
    <>
      <CssBaseline />
      <Box
        sx={{
          minHeight: '100vh',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          background: 'linear-gradient(135deg, #0E0E0E 0%, #2A2A2A 100%)',
          padding: 3,
        }}
      >
        <Container maxWidth="sm">
          <Paper
            elevation={3}
            sx={{
              padding: 4,
              backgroundColor: 'background.paper',
              borderRadius: 3,
              border: '1px solid',
              borderColor: 'divider',
            }}
          >
            <Box sx={{ textAlign: 'center', mb: 4 }}>
              <Typography variant="h4" fontWeight={700} color="primary.main" gutterBottom>
                Workplace Booking
              </Typography>
              <Typography variant="body1" color="text.secondary">
                Inicia sesión para continuar
              </Typography>
            </Box>
            {children}
          </Paper>
          <Typography variant="caption" color="text.secondary" sx={{ display: 'block', textAlign: 'center', mt: 3 }}>
            © 2026 Workplace Booking Platform. Todos los derechos reservados.
          </Typography>
        </Container>
      </Box>
    </>
  );
}