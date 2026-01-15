import { useState, useEffect } from 'react';
import {
  Box,
  Card,
  CardContent,
  Typography,
  TextField,
  InputAdornment,
  Chip,
  Button,
  Grid,
} from '@mui/material';
import { DataGrid, GridColDef, GridRenderCellParams } from '@mui/x-data-grid';
import {
  Search as SearchIcon,
  Refresh as RefreshIcon,
  Map as MapIcon,
} from '@mui/icons-material';
import {
  collection,
  getDocs,
  query,
  orderBy,
  limit,
} from 'firebase/firestore';
import { db } from '../config/firebase';

interface Territory {
  id: string;
  name: string;
  geohash: string;
  latitude: number;
  longitude: number;
  level: number;
  controlledBy: string | null;
  controllerName: string;
  resourceType: string;
  resourceOutput: number;
  lastConquered: Date | null;
  totalBattles: number;
}

export default function TerritoriesPage() {
  const [territories, setTerritories] = useState<Territory[]>([]);
  const [loading, setLoading] = useState(true);
  const [searchQuery, setSearchQuery] = useState('');
  const [stats, setStats] = useState({
    total: 0,
    contested: 0,
    unclaimed: 0,
    avgLevel: 0,
  });

  useEffect(() => {
    fetchTerritories();
  }, []);

  const fetchTerritories = async () => {
    setLoading(true);
    try {
      const territoriesQuery = query(
        collection(db, 'territories'),
        orderBy('level', 'desc'),
        limit(500)
      );
      const snapshot = await getDocs(territoriesQuery);
      const territoriesData = snapshot.docs.map((doc) => ({
        id: doc.id,
        name: doc.data().name || `Territory ${doc.id.slice(0, 6)}`,
        geohash: doc.data().geohash || '',
        latitude: doc.data().latitude || 0,
        longitude: doc.data().longitude || 0,
        level: doc.data().level || 1,
        controlledBy: doc.data().controlledBy || null,
        controllerName: doc.data().controllerName || 'Unclaimed',
        resourceType: doc.data().resourceType || 'gold',
        resourceOutput: doc.data().resourceOutput || 100,
        lastConquered: doc.data().lastConquered?.toDate() || null,
        totalBattles: doc.data().totalBattles || 0,
      }));

      setTerritories(territoriesData);

      // Calculate stats
      const total = territoriesData.length;
      const contested = territoriesData.filter((t) => t.totalBattles > 5).length;
      const unclaimed = territoriesData.filter((t) => !t.controlledBy).length;
      const avgLevel =
        total > 0
          ? territoriesData.reduce((sum, t) => sum + t.level, 0) / total
          : 0;

      setStats({ total, contested, unclaimed, avgLevel: Math.round(avgLevel * 10) / 10 });
    } catch (error) {
      console.error('Error fetching territories:', error);
    } finally {
      setLoading(false);
    }
  };

  const filteredTerritories = territories.filter(
    (territory) =>
      territory.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
      territory.geohash.toLowerCase().includes(searchQuery.toLowerCase()) ||
      territory.controllerName.toLowerCase().includes(searchQuery.toLowerCase())
  );

  const columns: GridColDef[] = [
    {
      field: 'name',
      headerName: 'Territory',
      flex: 1,
      minWidth: 180,
      renderCell: (params: GridRenderCellParams) => (
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
          <MapIcon sx={{ color: 'primary.main', fontSize: 20 }} />
          <Typography variant="body2">{params.value}</Typography>
        </Box>
      ),
    },
    {
      field: 'level',
      headerName: 'Level',
      width: 80,
      align: 'center',
      headerAlign: 'center',
      renderCell: (params: GridRenderCellParams) => (
        <Chip
          label={params.value}
          size="small"
          sx={{
            backgroundColor: 'rgba(99, 102, 241, 0.1)',
            color: 'primary.main',
          }}
        />
      ),
    },
    {
      field: 'controllerName',
      headerName: 'Controlled By',
      flex: 1,
      minWidth: 150,
      renderCell: (params: GridRenderCellParams) => (
        <Typography
          variant="body2"
          sx={{ color: params.value === 'Unclaimed' ? 'text.secondary' : 'text.primary' }}
        >
          {params.value}
        </Typography>
      ),
    },
    {
      field: 'resourceType',
      headerName: 'Resource',
      width: 100,
      renderCell: (params: GridRenderCellParams) => (
        <Chip
          label={params.value}
          size="small"
          sx={{
            backgroundColor:
              params.value === 'gold'
                ? 'rgba(245, 158, 11, 0.1)'
                : params.value === 'wood'
                ? 'rgba(34, 197, 94, 0.1)'
                : params.value === 'stone'
                ? 'rgba(148, 163, 184, 0.1)'
                : 'rgba(139, 92, 246, 0.1)',
            color:
              params.value === 'gold'
                ? '#f59e0b'
                : params.value === 'wood'
                ? '#22c55e'
                : params.value === 'stone'
                ? '#94a3b8'
                : '#8b5cf6',
          }}
        />
      ),
    },
    {
      field: 'resourceOutput',
      headerName: 'Output/hr',
      width: 100,
      align: 'right',
      headerAlign: 'right',
    },
    {
      field: 'totalBattles',
      headerName: 'Battles',
      width: 80,
      align: 'center',
      headerAlign: 'center',
    },
    {
      field: 'geohash',
      headerName: 'Geohash',
      width: 120,
      renderCell: (params: GridRenderCellParams) => (
        <Typography variant="body2" sx={{ color: 'text.secondary', fontFamily: 'monospace' }}>
          {params.value?.slice(0, 8)}
        </Typography>
      ),
    },
    {
      field: 'lastConquered',
      headerName: 'Last Conquered',
      width: 140,
      renderCell: (params: GridRenderCellParams) => (
        <Typography variant="body2" sx={{ color: 'text.secondary' }}>
          {params.value ? params.value.toLocaleDateString() : 'Never'}
        </Typography>
      ),
    },
  ];

  return (
    <Box className="animate-fadeIn">
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <Typography variant="h4" sx={{ fontWeight: 700 }}>
          Territory Management
        </Typography>
        <Button
          variant="outlined"
          startIcon={<RefreshIcon />}
          onClick={fetchTerritories}
        >
          Refresh
        </Button>
      </Box>

      {/* Stats */}
      <Grid container spacing={2} sx={{ mb: 3 }}>
        <Grid item xs={6} md={3}>
          <Card>
            <CardContent sx={{ textAlign: 'center' }}>
              <Typography variant="h4" sx={{ color: 'primary.main' }}>
                {stats.total}
              </Typography>
              <Typography variant="body2" sx={{ color: 'text.secondary' }}>
                Total Territories
              </Typography>
            </CardContent>
          </Card>
        </Grid>
        <Grid item xs={6} md={3}>
          <Card>
            <CardContent sx={{ textAlign: 'center' }}>
              <Typography variant="h4" sx={{ color: '#ef4444' }}>
                {stats.contested}
              </Typography>
              <Typography variant="body2" sx={{ color: 'text.secondary' }}>
                Highly Contested
              </Typography>
            </CardContent>
          </Card>
        </Grid>
        <Grid item xs={6} md={3}>
          <Card>
            <CardContent sx={{ textAlign: 'center' }}>
              <Typography variant="h4" sx={{ color: '#94a3b8' }}>
                {stats.unclaimed}
              </Typography>
              <Typography variant="body2" sx={{ color: 'text.secondary' }}>
                Unclaimed
              </Typography>
            </CardContent>
          </Card>
        </Grid>
        <Grid item xs={6} md={3}>
          <Card>
            <CardContent sx={{ textAlign: 'center' }}>
              <Typography variant="h4" sx={{ color: '#22c55e' }}>
                {stats.avgLevel}
              </Typography>
              <Typography variant="body2" sx={{ color: 'text.secondary' }}>
                Avg Level
              </Typography>
            </CardContent>
          </Card>
        </Grid>
      </Grid>

      <Card>
        <CardContent>
          {/* Search */}
          <TextField
            placeholder="Search territories by name, geohash, or controller..."
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            fullWidth
            sx={{ mb: 3 }}
            InputProps={{
              startAdornment: (
                <InputAdornment position="start">
                  <SearchIcon sx={{ color: 'text.secondary' }} />
                </InputAdornment>
              ),
            }}
          />

          {/* Data Grid */}
          <Box sx={{ height: 600, width: '100%' }}>
            <DataGrid
              rows={filteredTerritories}
              columns={columns}
              loading={loading}
              pageSizeOptions={[25, 50, 100]}
              initialState={{
                pagination: { paginationModel: { pageSize: 25 } },
              }}
              disableRowSelectionOnClick
              sx={{
                border: 'none',
                '& .MuiDataGrid-cell': {
                  borderColor: 'rgba(148, 163, 184, 0.1)',
                },
                '& .MuiDataGrid-columnHeaders': {
                  backgroundColor: 'rgba(30, 41, 59, 0.5)',
                  borderColor: 'rgba(148, 163, 184, 0.1)',
                },
              }}
            />
          </Box>
        </CardContent>
      </Card>
    </Box>
  );
}
