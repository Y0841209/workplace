import React from 'react';
import { Outlet, NavLink, useLocation } from 'react-router-dom';
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
  Divider,
  Avatar,
  Menu,
  MenuItem,
  Tooltip,
  Badge,
  useMediaQuery,
  useTheme,
} from '@mui/material';
import {
  Menu as MenuIcon,
  Dashboard as DashboardIcon,
  Storage as StorageIcon,
  People as PeopleIcon,
  History as HistoryIcon,
  Settings as SettingsIcon,
  Logout as LogoutIcon,
  Notifications as NotificationsIcon,
  AdminPanelSettings as AdminIcon,
  AccountCircle,
} from '@mui/icons-material';
import { useAuth } from '../contexts/AuthContext';

const drawerWidth = 280;

export function AdminLayout({ children }: { children: React.ReactNode }) {
  const theme = useTheme();
  const isMobile = useMediaQuery(theme.breakpoints.down('md'));
  const [mobileOpen, setMobileOpen] = useState(false);
  const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null);
  const { user, logout } = useAuth();
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
    { path: '/admin', label: 'Dashboard', icon: <DashboardIcon /> },
    { path: '/admin/resources', label: 'Recursos', icon: <StorageIcon /> },
    { path: '/admin/users', label: 'Usuarios', icon: <PeopleIcon /> },
    { path: '/admin/audit', label: 'Auditoría', icon: <HistoryIcon /> },
  ];

  const drawer = (
    <Box sx={{ textAlign: 'center', py: 2, px: 1, borderBottom: 1, borderColor: 'divider' }}>
      <Typography variant="h6" fontWeight={700} color="primary.main" sx={{ letterSpacing: '-0.5px' }}>
        Admin Panel
      </Typography>
      <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mt: 0.5 }}>
        Workplace Booking
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
          <Typography variant="h6" noWrap component={NavLink} to="/admin" sx={{ flexGrow: 1, textDecoration: 'none', color: 'inherit', fontWeight: 700 }}>
            Administración
          </Typography>
          
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
            <Tooltip title="Notificaciones">
              <IconButton color="inherit">
                <Badge badgeContent={0} color="error">
                  <NotificationsIcon />
                </Badge>
              </IconButton>
            </Tooltip>
            
            <Tooltip title={user?.displayName || 'Admin'}>
              <IconButton onClick={handleProfileMenuOpen} sx={{ ml: 1 }}>
                <Avatar
                  sx={{ width: 36, height: 36, bgcolor: 'error.main', color: 'error.contrastText' }}
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
                selected={location.pathname === item.path}
                sx={{
                  borderRadius: '8px',
                  margin: '4px 12px',
                  '&.Mui-selected': {
                    backgroundColor: 'error.light',
                    color: 'error.contrastText',
                    '&:hover': { backgroundColor: 'error.main' },
                    '& .MuiListItemIcon-root': { color: 'error.contrastText' },
                  },
                }}
              >
                <ListItemIcon sx={{ minWidth: 40 }}>{item.icon}</ListItemIcon>
                <ListItemText primary={item.label} />
              </ListItem>
            ))}
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
                selected={location.pathname === item.path}
                sx={{
                  borderRadius: '8px',
                  margin: '4px 12px',
                  '&.Mui-selected': {
                    backgroundColor: 'error.light',
                    color: 'error.contrastText',
                    '&:hover': { backgroundColor: 'error.main' },
                    '& .MuiListItemIcon-root': { color: 'error.contrastText' },
                  },
                }}
              >
                <ListItemIcon sx={{ minWidth: 40 }}>{item.icon}</ListItemIcon>
                <ListItemText primary={item.label} />
              </ListItem>
            ))}
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
    </Box>
  );
}