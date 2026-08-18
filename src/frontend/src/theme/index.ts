import { createTheme, ThemeOptions } from '@mui/material/styles';

const palette = {
  primary: {
    main: '#FFD800',
    light: '#FFE033',
    dark: '#CCAD00',
    contrastText: '#0E0E0E',
  },
  secondary: {
    main: '#0E0E0E',
    light: '#2A2A2A',
    dark: '#000000',
    contrastText: '#FFD800',
  },
  background: {
    default: '#F5F5F5',
    paper: '#FFFFFF',
    accent: '#F6F0CB',
  },
  text: {
    primary: '#0E0E0E',
    secondary: '#2A2A2A',
    disabled: 'rgba(14, 14, 14, 0.38)',
  },
  error: {
    main: '#D32F2F',
    light: '#EF5350',
    dark: '#C62828',
  },
  warning: {
    main: '#FF8F00',
    light: '#FFAB40',
    dark: '#FF6F00',
  },
  info: {
    main: '#0288D1',
    light: '#03A9F4',
    dark: '#01579B',
  },
  success: {
    main: '#388E3C',
    light: '#66BB6A',
    dark: '#2E7D32',
  },
  divider: 'rgba(14, 14, 14, 0.12)',
};

const typography = {
  fontFamily: '"Inter", "Roboto", "Helvetica", "Arial", sans-serif',
  h1: { fontWeight: 700, fontSize: '2.5rem', lineHeight: 1.2 },
  h2: { fontWeight: 600, fontSize: '2rem', lineHeight: 1.3 },
  h3: { fontWeight: 600, fontSize: '1.75rem', lineHeight: 1.3 },
  h4: { fontWeight: 600, fontSize: '1.5rem', lineHeight: 1.4 },
  h5: { fontWeight: 600, fontSize: '1.25rem', lineHeight: 1.4 },
  h6: { fontWeight: 600, fontSize: '1rem', lineHeight: 1.4 },
  subtitle1: { fontWeight: 500, fontSize: '1rem', lineHeight: 1.5 },
  subtitle2: { fontWeight: 500, fontSize: '0.875rem', lineHeight: 1.5 },
  body1: { fontWeight: 400, fontSize: '1rem', lineHeight: 1.6 },
  body2: { fontWeight: 400, fontSize: '0.875rem', lineHeight: 1.6 },
  button: { fontWeight: 600, fontSize: '0.875rem', textTransform: 'none' },
  caption: { fontWeight: 400, fontSize: '0.75rem', lineHeight: 1.5 },
  overline: { fontWeight: 500, fontSize: '0.75rem', lineHeight: 1.5, textTransform: 'uppercase' },
};

const shape = {
  borderRadius: 8,
};

const shadows = [
  'none',
  '0 1px 3px rgba(14,14,14,0.08), 0 1px 2px rgba(14,14,14,0.06)',
  '0 4px 6px rgba(14,14,14,0.07), 0 2px 4px rgba(14,14,14,0.06)',
  '0 10px 15px rgba(14,14,14,0.1), 0 4px 6px rgba(14,14,14,0.05)',
  '0 20px 25px rgba(14,14,14,0.1), 0 10px 10px rgba(14,14,14,0.04)',
  '0 25px 50px rgba(14,14,14,0.15)',
  ...Array(19).fill('none'),
];

const components = {
  MuiButton: {
    styleOverrides: {
      root: { borderRadius: 8, textTransform: 'none', fontWeight: 600 },
      containedPrimary: { boxShadow: 'none', '&:hover': { boxShadow: '0 4px 12px rgba(255,216,0,0.4)' } },
      containedSecondary: { boxShadow: 'none', '&:hover': { boxShadow: '0 4px 12px rgba(14,14,14,0.4)' } },
      outlined: { borderWidth: 2, '&:hover': { borderWidth: 2 } },
    },
  },
  MuiCard: {
    styleOverrides: {
      root: { borderRadius: 12, boxShadow: shadows[1], border: '1px solid rgba(14,14,14,0.06)' },
    },
  },
  MuiPaper: {
    styleOverrides: {
      root: { borderRadius: 12 },
      elevation1: { boxShadow: shadows[1] },
      elevation2: { boxShadow: shadows[2] },
      elevation3: { boxShadow: shadows[3] },
    },
  },
  MuiTextField: {
    styleOverrides: {
      root: { '& .MuiOutlinedInput-root': { borderRadius: 8 } },
    },
    defaultProps: { variant: 'outlined', size: 'small' },
  },
  MuiSelect: {
    defaultProps: { variant: 'outlined', size: 'small' },
  },
  MuiMenuItem: {
    styleOverrides: { root: { fontSize: '0.875rem' } },
  },
  MuiChip: {
    styleOverrides: { root: { borderRadius: 6, fontWeight: 500 } },
  },
  MuiTableCell: {
    styleOverrides: {
      head: { fontWeight: 600, backgroundColor: '#F5F5F5', borderBottom: '2px solid rgba(14,14,14,0.12)' },
      body: { borderBottom: '1px solid rgba(14,14,14,0.06)' },
    },
  },
  MuiTableRow: {
    styleOverrides: {
      root: { '&:last-child td': { borderBottom: 'none' }, '&:hover': { backgroundColor: '#F6F0CB' } },
    },
  },
  MuiAppBar: {
    styleOverrides: {
      root: { boxShadow: shadows[1], backgroundColor: '#FFFFFF', color: '#0E0E0E', borderBottom: '1px solid rgba(14,14,14,0.06)' },
    },
  },
  MuiDrawer: {
    styleOverrides: {
      paper: { borderRight: '1px solid rgba(14,14,14,0.06)', backgroundColor: '#FFFFFF' },
    },
  },
  MuiListItemButton: {
    styleOverrides: {
      root: { borderRadius: 8, margin: '4px 8px', '&.Mui-selected': { backgroundColor: '#FFF8E1', '&:hover': { backgroundColor: '#FFECB3' } } },
    },
  },
  MuiTooltip: {
    styleOverrides: { tooltip: { backgroundColor: '#0E0E0E', color: '#FFD800', fontSize: '0.75rem', borderRadius: 6 } },
  },
  MuiSnackbarContent: {
    styleOverrides: { root: { backgroundColor: '#0E0E0E', color: '#FFD800' } },
  },
  MuiAlert: {
    styleOverrides: {
      root: { borderRadius: 8 },
      standardSuccess: { backgroundColor: '#E8F5E9', color: '#2E7D32' },
      standardError: { backgroundColor: '#FDEDEC', color: '#C62828' },
      standardWarning: { backgroundColor: '#FFF8E1', color: '#FF6F00' },
      standardInfo: { backgroundColor: '#E3F2FD', color: '#01579B' },
    },
  },
  MuiDialog: {
    styleOverrides: { paper: { borderRadius: 12 } },
  },
  MuiTabs: {
    styleOverrides: { indicator: { backgroundColor: '#FFD800', height: 3 } },
  },
  MuiTab: {
    styleOverrides: { root: { fontWeight: 500, textTransform: 'none', minWidth: 'auto', padding: '8px 16px' } },
  },
  MuiAvatar: {
    styleOverrides: { root: { fontWeight: 600 } },
  },
  MuiPaginationItem: {
    styleOverrides: { root: { borderRadius: 8, '&.Mui-selected': { backgroundColor: '#FFD800', color: '#0E0E0E' } } },
  },
  MuiDataGrid: {
    styleOverrides: {
      root: { border: 'none', '& .MuiDataGrid-cell': { borderBottom: '1px solid rgba(14,14,14,0.06)' } },
      columnHeaders: { backgroundColor: '#F5F5F5', borderBottom: '2px solid rgba(14,14,14,0.12)' },
      row: { '&:hover': { backgroundColor: '#F6F0CB' } },
    },
  },
};

const themeOptions: ThemeOptions = {
  palette,
  typography,
  shape,
  shadows,
  components,
  breakpoints: {
    values: { xs: 0, sm: 600, md: 900, lg: 1200, xl: 1536 },
  },
  spacing: 8,
};

const theme = createTheme(themeOptions);

export default theme;