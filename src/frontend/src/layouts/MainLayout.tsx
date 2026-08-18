import React, { useState } from 'react';
import { Outlet, NavLink, useNavigate, useLocation } from 'react-router-dom';
import {
  AppBar,
  Toolbar,
  Typography,
  IconButton,
  Drawer,
  List,
  ListItem,
  ListItemIcon,
  ListItemText,
  Box,
  Avatar,
  Menu,
  MenuItem,
  Divider,
  Tooltip,
  Badge,
  useMediaQuery,
  useTheme,
} from '@mui/material';
import {
  Menu as MenuIcon,
  Dashboard as DashboardIcon,
  Event as EventIcon,
  Person as PersonIcon,
  Settings as SettingsIcon,
  Logout as LogoutIcon,
  Notifications as NotificationsIcon,
  Home as HomeIcon,
  AdminPanelSettings as AdminIcon,
  AccountCircle,
  ChevronLeft,
  ChevronRight,
} from '@mui/icons-material';
import { useAuth } from '../contexts/AuthContext';

const drawerWidth = 280;

interface MainLayoutProps {
  children?: React.ReactNode;
}

export function MainLayout({ children }: MainLayoutProps) {
  const theme = useTheme();
  const isMobile = useMediaQuery(theme.breakpoints.down('md'));
  const [mobileOpen, setMobileOpen] = useState(false);
  const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null);
  const { user, logout, hasRole } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();

  const handleDrawerToggle = () => {
    setMobileOpen(!mobileOpen);
  };

  const handleProfileMenuOpen = (event: React.MouseEvent<HTMLElement>) => {
    setAnchorEl(event.currentTarget);
  };

  const handleMenuClose = () => {
    setAnchorEl(null);
  };

  const handleLogout = async () => {
    handleMenuClose();
    await logout();
  };

  const navigation = [
    { path: '/dashboard', label: 'Dashboard', icon: <DashboardIcon />, exact: true },
    { path: '/book', label: 'Reservar', icon: <EventIcon /> },
    { path: '/reservations', label: 'Mis Reservas', icon: <EventIcon /> },
    { path: '/profile', label: 'Perfil', icon: <PersonIcon /> },
  ];

  const adminNavigation = [
    { path: '/admin', label: 'Panel Admin', icon: <AdminIcon /> },
    { path: '/admin/resources', label: 'Recursos', icon: <HomeIcon /> },
    { path: '/admin/users', label: 'Usuarios', icon: <PersonIcon /> },
    { path: '/admin/audit', label: 'Auditoría', icon: <SettingsIcon /> },
  ];

  const drawer = (
    <Box onClick={handleDrawerToggle} sx={{ textAlign: 'center', py: 2, px: 1, borderBottom: 1, borderColor: 'divider' }}>
      <Typography variant="h6" fontWeight={700} color="primary.main" sx={{ letterSpacing: '-0.5px' }}>
        Workplace Booking
      </Typography>
      <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mt: 0.5 }}>
        Platform
      </Typography>
    </Box>
  );

  return (
    <Box sx={{ display: 'flex', minHeight: '100vh' }}>
      <AppBar
        position="fixed"
        sx={{
          width: { md: `calc(100% - ${drawerWidth}px)` },
          ml: { md: `${drawerWidth}px` },
          backgroundColor: 'background.paper',
          color: 'text.primary',
          borderBottom: 1,
          borderColor: 'divider',
          zIndex: (theme) => theme.zIndex.drawer + 1,
        }}
      >
        <Toolbar>
          <IconButton
            color="inherit"
            aria-label="open drawer"
            edge="start"
            onClick={handleDrawerToggle}
            sx={{ mr: 2, display: { md: 'none' } }}
          >
            <MenuIcon />
          </IconButton>
          <Typography
            variant="h6"
            noWrap
            component={NavLink}
            to="/dashboard"
            sx={{ flexGrow: 1, textDecoration: 'none', color: 'inherit', fontWeight: 700 }}
          >
            Workspace Booking
          </Typography>
          
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
            <Tooltip title="Notificaciones">
              <IconButton color="inherit">
                <Badge badgeContent={3} color="error">
                  <NotificationsIcon />
                </Badge>
              </IconButton>
            </Tooltip>
            
            <Tooltip title={user?.displayName || 'Usuario'}>
              <IconButton onClick={handleProfileMenuOpen} sx={{ ml: 1 }}>
                <Avatar
                  sx={{ width: 36, height: 36, bgcolor: 'primary.main', color: 'primary.contrastText' }}
                  src={user?.email ? `https://ui-avatars.com/api/?name=${encodeURIComponent(user.displayName)}&background=0E0E0E&color=FFD800` : undefined}
                >
                  {user?.displayName?.charAt(0).toUpperCase()}
                </Avatar>
              </IconButton>
            </Tooltip>
          </Box>
        </Toolbar>
      </AppBar>

      <Box
        component="nav"
        sx={{ width: { md: drawerWidth }, flexShrink: { md: 0 } }}
        aria-label="main navigation"
      >
        <Drawer
          variant="temporary"
          open={mobileOpen}
          onClose={handleDrawerToggle}
          ModalProps={{ keepMounted: true }}
          sx={{
            display: { xs: 'block', md: 'none' },
            '& .MuiDrawer-paper': { boxSizing: 'border-box', width: drawerWidth },
          }}
        >
          {drawer}
          <Divider />
          <List>
            {navigation.map((item) => (
              <ListItem
                button
                key={item.path}
                component={NavLink}
                to={item.path}
                selected={location.pathname === item.path || (location.pathname.startsWith(item.path) && !item.exact)}
                sx={{
                  borderRadius: '8px',
                  margin: '4px 12px',
                  '&.Mui-selected': {
                    backgroundColor: 'primary.light',
                    color: 'primary.contrastText',
                    '&:hover': { backgroundColor: 'primary.main' },
                    '& .MuiListItemIcon-root': { color: 'primary.contrastText' },
                  },
                }}
              >
                <ListItemIcon sx={{ minWidth: 40 }}>{item.icon}</ListItemIcon>
                <ListItemText primary={item.label} />
              </ListItem>
            ))}
            {hasRole('GLOBAL_ADMIN') && (
              <>
                <Divider sx={{ my: 1 }} />
                <ListItem disablePadding sx={{ px: 2, py: 1 }}>
                  <Typography variant="caption" color="text.secondary" textTransform="uppercase" fontWeight={600}>
                    Administración
                  </Typography>
                </ListItem>
                {adminNavigation.map((item) => (
                  <ListItem
                    button
                    key={item.path}
                    component={NavLink}
                    to={item.path}
                    selected={location.pathname === item.path}
                    sx={{
                      borderRadius: '8px',
                      margin: '4px 12px',
                      '&.Mui-selected': {
                        backgroundColor: 'primary.light',
                        color: 'primary.contrastText',
                        '&:hover': { backgroundColor: 'primary.main' },
                        '& .MuiListItemIcon-root': { color: 'primary.contrastText' },
                      },
                    }}
                  >
                    <ListItemIcon sx={{ minWidth: 40 }}>{item.icon}</ListItemIcon>
                    <ListItemText primary={item.label} />
                  </ListItem>
                ))}
              </>
            )}
          </List>
        </Drawer>

        <Drawer
          variant="permanent"
          sx={{
            display: { xs: 'none', md: 'block' },
            '& .MuiDrawer-paper': {
              boxSizing: 'border-box',
              width: drawerWidth,
              borderRight: 1,
              borderColor: 'divider',
              backgroundColor: 'background.paper',
            },
          }}
          open
        >
          {drawer}
          <Divider />
          <List>
            {navigation.map((item) => (
              <ListItem
                button
                key={item.path}
                component={NavLink}
                to={item.path}
                selected={location.pathname === item.path || (location.pathname.startsWith(item.path) && !item.exact)}
                sx={{
                  borderRadius: '8px',
                  margin: '4px 12px',
                  '&.Mui-selected': {
                    backgroundColor: 'primary.light',
                    color: 'primary.contrastText',
                    '&:hover': { backgroundColor: 'primary.main' },
                    '& .MuiListItemIcon-root': { color: 'primary.contrastText' },
                  },
                }}
              >
                <ListItemIcon sx={{ minWidth: 40 }}>{item.icon}</ListItemIcon>
                <ListItemText primary={item.label} />
              </ListItem>
            ))}
            {hasRole('GLOBAL_ADMIN') && (
              <>
                <Divider sx={{ my: 1 }} />
                <ListItem disablePadding sx={{ px: 2, py: 1 }}>
                  <Typography variant="caption" color="text.secondary" textTransform="uppercase" fontWeight={600}>
                    Administración
                  </Typography>
                </ListItem>
                {adminNavigation.map((item) => (
                  <ListItem
                    button
                    key={item.path}
                    component={NavLink}
                    to={item.path}
                    selected={location.pathname === item.path}
                    sx={{
                      borderRadius: '8px',
                      margin: '4px 12px',
                      '&.Mui-selected': {
                        backgroundColor: 'primary.light',
                        color: 'primary.contrastText',
                        '&:hover': { backgroundColor: 'primary.main' },
                        '& .MuiListItemIcon-root': { color: 'primary.contrastText' },
                      },
                    }}
                  >
                    <ListItemIcon sx={{ minWidth: 40 }}>{item.icon}</ListItemIcon>
                    <ListItemText primary={item.label} />
                  </ListItem>
                ))}
              </>
            )}
          </List>
        </Drawer>
      </Box>

      <Box
        component="main"
        sx={{
          flexGrow: 1,
          p: 3,
          width: { md: `calc(100% - ${drawerWidth}px)` },
          mt: '64px',
          bgcolor: 'background.default',
          minHeight: 'calc(100vh - 64px)',
        }}
      >
        {children}
      </Box>

      <Menu
        anchorEl={anchorEl}
        open={Boolean(anchorEl)}
        onClose={handleMenuClose}
        transformOrigin={{ horizontal: 'right', vertical: 'top' }}
        anchorOrigin={{ horizontal: 'right', vertical: 'bottom' }}
        PaperProps={{ sx: { mt: 1 } }}
      >
        <Box sx={{ px: 2, py: 1, borderBottom: 1, borderColor: 'divider' }}>
          <Typography variant="subtitle1" fontWeight={600}>
            {user?.displayName}
          </Typography>
          <Typography variant="body2" color="text.secondary">
            {user?.email}
          </Typography>
          {user?.jobTitle && (
            <Typography variant="caption" color="text.secondary">
              {user.jobTitle}
            </Typography>
          )}
        </Box>
        <MenuItem onClick={handleMenuClose}>
          <AccountCircle sx={{ mr: 1, fontSize: 20 }} />
          Mi perfil
        </MenuItem>
        <MenuItem onClick={() => { handleMenuClose(); navigate('/settings'); }}>
          <SettingsIcon sx={{ mr: 1, fontSize: 20 }} />
          Configuración
        </MenuItem>
        <Divider />
        <MenuItem onClick={handleLogout} sx={{ color: 'error.main' }}>
          <LogoutIcon sx={{ mr: 1, fontSize: 20 }} />
          Cerrar sesión
        </MenuItem>
      </Menu>
    </Box>
  );
}