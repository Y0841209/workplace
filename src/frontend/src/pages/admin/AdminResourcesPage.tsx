import React from 'react';
import { useQuery } from '@tanstack/react-query';
import {
  Grid,
  Card,
  CardContent,
  Typography,
  Box,
  Chip,
  Button,
  IconButton,
  Tooltip,
  Paper,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Pagination,
  TextField,
  Select,
  MenuItem,
  FormControl,
  InputLabel,
  Alert,
  Skeleton,
} from '@mui/material';
import {
  Add as AddIcon,
  Edit as EditIcon,
  Delete as DeleteIcon,
  Visibility as ViewIcon,
  Search as SearchIcon,
  FilterList as FilterIcon,
  Download as DownloadIcon,
  Event as EventIcon,
  MeetingRoom as MeetingRoomIcon,
  Business as BusinessIcon,
} from '@mui/icons-material';
import { useAuth } from '../contexts/AuthContext';
import { apiClient } from '../services/apiClient';
import { Resource, ResourceQueryParams, ResourceTypeCode } from '../types';

export function AdminResourcesPage() {
  const { hasRole } = useAuth();
  const [page, setPage] = React.useState(0);
  const [pageSize, setPageSize] = React.useState(10);
  const [search, setSearch] = React.useState('');
  const [typeFilter, setTypeFilter] = React.useState<ResourceTypeCode | ''>('');
  const [floorFilter, setFloorFilter] = React.useState('');
  const [sortBy, setSortBy] = React.useState('code');
  const [sortDirection, setSortDirection] = React.useState<'asc' | 'desc'>('asc');

  const params: ResourceQueryParams = {
    page: page + 1,
    pageSize,
    search: search || undefined,
    resourceTypeCode: typeFilter || undefined,
    floorId: floorFilter || undefined,
    active: true,
    reservable: true,
    sortBy,
    sortDirection,
  };

  const { data, isLoading, error, refetch } = useQuery({
    queryKey: ['admin', 'resources', params],
    queryFn: () => apiClient.get<PaginatedResponse<Resource>>('/admin/resources', { params }),
  });

  const handleSort = (column: string) => {
    if (sortBy === column) {
      setSortDirection(prev => prev === 'asc' ? 'desc' : 'asc');
    } else {
      setSortBy(column);
      setSortDirection('asc');
    }
  };

  const handleDelete = async (resourceId: string) => {
    if (!window.confirm('¿Estás seguro de eliminar este recurso?')) return;
    
    try {
      await apiClient.delete(`/admin/resources/${resourceId}`);
      refetch();
    } catch (err) {
      console.error('Delete failed:', err);
    }
  };

  const typeColors: Record<ResourceTypeCode, 'default' | 'primary' | 'secondary' | 'success' | 'warning' | 'error'> = {
    OPEN_WORKSPACE: 'primary',
    CLOSED_OFFICE: 'secondary',
    MEETING_ROOM: 'success',
  };

  const typeIcons: Record<ResourceTypeCode, React.ReactNode> = {
    OPEN_WORKSPACE: <BusinessIcon fontSize="small" />,
    CLOSED_OFFICE: <BusinessIcon fontSize="small" />,
    MEETING_ROOM: <MeetingRoomIcon fontSize="small" />,
  };

  if (isLoading && !data) {
    return (
      <Box sx={{ p: 3 }}>
        {[...Array(5)].map((_, i) => (
          <Card key={i} sx={{ mb: 2 }}>
            <CardContent>
              <Skeleton variant="rectangular" width="60%" height={24} />
              <Skeleton variant="rectangular" width="40%" height={16} />
            </CardContent>
          </Card>
        ))}
      </Box>
    );
  }

  if (error) {
    return (
      <Alert severity="error" sx={{ mb: 3 }}>
        Error al cargar recursos: {error instanceof Error ? error.message : 'Error desconocido'}
      </Alert>
    );
  }

  return (
    <Box sx={{ p: 1 }}>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3, flexWrap: 'wrap', gap: 2 }}>
        <Box>
          <Typography variant="h4" fontWeight={700} color="text.primary">
            Gestión de Recursos
          </Typography>
          <Typography variant="body1" color="text.secondary">
            Administra oficinas abiertas, oficinas cerradas y salas de juntas
          </Typography>
        </Box>
        {hasRole('GLOBAL_ADMIN') && (
          <Tooltip title="Crear recurso">
            <Button
              variant="contained"
              startIcon={<AddIcon />}
              onClick={() => console.log('Create resource')}
            >
              Nuevo Recurso
            </Button>
          </Tooltip>
        )}
      </Box>

      {/* Filters */}
      <Paper elevation={1} sx={{ p: 2, mb: 3, border: 1, borderColor: 'divider' }}>
        <Grid container spacing={2} alignItems="flex-end">
          <Grid item xs={12} sm={4}>
            <TextField
              fullWidth
              placeholder="Buscar por código o nombre"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              size="small"
              variant="outlined"
              InputProps={{
                startAdornment: (
                  <Box sx={{ position: 'absolute', left: 10, top: '50%', transform: 'translateY(-50%)', pointerEvents: 'none' }}>
                    <SearchIcon color="action" fontSize="small" />
                  </Box>
                ),
              }}
              inputProps={{ style: { paddingLeft: 40 } }}
            />
          </Grid>
          <Grid item xs={12} sm={2}>
            <FormControl fullWidth size="small">
              <InputLabel id="type-label">Tipo</InputLabel>
              <Select
                labelId="type-label"
                value={typeFilter}
                label="Tipo"
                onChange={(e) => setTypeFilter(e.target.value as ResourceTypeCode | '')}
              >
                <MenuItem value="">Todos</MenuItem>
                <MenuItem value="OPEN_WORKSPACE">Oficina Abierta</MenuItem>
                <MenuItem value="CLOSED_OFFICE">Oficina Cerrada</MenuItem>
                <MenuItem value="MEETING_ROOM">Sala de Juntas</MenuItem>
              </Select>
            </FormControl>
          </Grid>
          <Grid item xs={12} sm={2}>
            <FormControl fullWidth size="small">
              <InputLabel id="floor-label">Piso</InputLabel>
              <Select
                labelId="floor-label"
                value={floorFilter}
                label="Piso"
                onChange={(e) => setFloorFilter(e.target.value)}
              >
                <MenuItem value="">Todos</MenuItem>
                <MenuItem value="floor-3">Piso 3</MenuItem>
                <MenuItem value="floor-6">Piso 6</MenuItem>
                <MenuItem value="floor-10">Piso 10</MenuItem>
              </Select>
            </FormControl>
          </Grid>
          <Grid item xs={12} sm={2}>
            <FormControl fullWidth size="small">
              <InputLabel id="pagesize-label">Por página</InputLabel>
              <Select
                labelId="pagesize-label"
                value={pageSize}
                label="Por página"
                onChange={(e) => { setPageSize(Number(e.target.value)); setPage(0); }}
              >
                <MenuItem value={5}>5</MenuItem>
                <MenuItem value={10}>10</MenuItem>
                <MenuItem value={25}>25</MenuItem>
                <MenuItem value={50}>50</MenuItem>
              </Select>
            </FormControl>
          </Grid>
          <Grid item xs={12} sm={2}>
            <Button
              variant="outlined"
              fullWidth
              startIcon={<DownloadIcon />}
              onClick={() => console.log('Export CSV')}
            >
              Exportar
            </Button>
          </Grid>
        </Grid>
      </Paper>

      {/* Table */}
      <Paper elevation={1} sx={{ border: 1, borderColor: 'divider' }}>
        <TableContainer>
          <Table>
            <TableHead>
              <TableRow>
                {[
                  { key: 'code', label: 'Código', sortable: true },
                  { key: 'name', label: 'Nombre', sortable: true },
                  { key: 'resourceTypeCode', label: 'Tipo', sortable: true },
                  { key: 'floor', label: 'Piso', sortable: true },
                  { key: 'zone', label: 'Zona', sortable: true },
                  { key: 'capacity', label: 'Capacidad', sortable: true, align: 'center' },
                  { key: 'active', label: 'Estado', sortable: true, align: 'center' },
                  { key: 'actions', label: 'Acciones', align: 'center' },
                ].map((col) => (
                  <TableCell
                    key={col.key}
                    align={col.align}
                    sortDirection={sortBy === col.key ? sortDirection : false}
                    onClick={col.sortable ? () => handleSort(col.key) : undefined}
                    sx={{ fontWeight: 600, cursor: col.sortable ? 'pointer' : 'default', userSelect: 'none' }}
                  >
                    {col.label}
                  </TableCell>
                ))}
              </TableRow>
            </TableHead>
            <TableBody>
              {data?.items.map((resource) => (
                <TableRow key={resource.id} hover>
                  <TableCell>
                    <Typography variant="body2" fontFamily="monospace" fontWeight={500}>
                      {resource.code}
                    </Typography>
                  </TableCell>
                  <TableCell>
                    <Typography variant="body2">{resource.name}</Typography>
                  </TableCell>
                  <TableCell>
                    <Chip
                      label={resource.resourceTypeCode}
                      icon={typeIcons[resource.resourceTypeCode]}
                      color={typeColors[resource.resourceTypeCode]}
                      size="small"
                      variant="outlined"
                    />
                  </TableCell>
                  <TableCell>{resource.floor?.name || resource.floorId}</TableCell>
                  <TableCell>{resource.zone?.name || resource.zoneId || '-'}</TableCell>
                  <TableCell align="center">
                    <Typography variant="body2">{resource.capacity}</Typography>
                  </TableCell>
                  <TableCell align="center">
                    <Chip
                      label={resource.active ? 'Activo' : 'Inactivo'}
                      color={resource.active ? 'success' : 'default'}
                      size="small"
                    />
                  </TableCell>
                  <TableCell align="center">
                    <Tooltip title="Ver detalles">
                      <IconButton size="small" onClick={() => console.log('View', resource.id)}>
                        <ViewIcon fontSize="small" />
                      </IconButton>
                    </Tooltip>
                    {hasRole('GLOBAL_ADMIN') && (
                      <>
                        <Tooltip title="Editar">
                          <IconButton size="small" onClick={() => console.log('Edit', resource.id)}>
                            <EditIcon fontSize="small" />
                          </IconButton>
                        </Tooltip>
                        <Tooltip title="Eliminar">
                          <IconButton size="small" color="error" onClick={() => handleDelete(resource.id)}>
                            <DeleteIcon fontSize="small" />
                          </IconButton>
                        </Tooltip>
                      </>
                    )}
                  </TableCell>
                </TableRow>
              ))}
              {!data?.items.length && (
                <TableRow>
                  <TableCell colSpan={8} align="center" sx={{ py: 4 }}>
                    <Typography color="text.secondary">No se encontraron recursos</Typography>
                  </TableCell>
                </TableRow>
              )}
            </TableBody>
          </Table>
        </TableContainer>

        {/* Pagination */}
        {data && data.totalPages > 1 && (
          <Box sx={{ p: 2, borderTop: 1, borderColor: 'divider' }}>
            <Pagination
              count={data.totalPages}
              page={page + 1}
              onChange={(_, value) => setPage(value - 1)}
              color="primary"
              size="small"
              shape="rounded"
              showFirstButton
              showLastButton
              boundaryCount={1}
              siblingCount={1}
            />
          </Box>
        )}
      </Paper>
    </Box>
  );
}