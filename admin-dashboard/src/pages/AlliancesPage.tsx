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
  Avatar,
  AvatarGroup,
} from '@mui/material';
import { DataGrid, GridColDef, GridRenderCellParams } from '@mui/x-data-grid';
import {
  Search as SearchIcon,
  Refresh as RefreshIcon,
  Groups as GroupsIcon,
} from '@mui/icons-material';
import {
  collection,
  getDocs,
  query,
  orderBy,
  limit,
} from 'firebase/firestore';
import { db } from '../config/firebase';

interface Alliance {
  id: string;
  name: string;
  tag: string;
  description: string;
  leaderId: string;
  leaderName: string;
  memberCount: number;
  maxMembers: number;
  level: number;
  xp: number;
  territoriesControlled: number;
  totalPower: number;
  warWins: number;
  warLosses: number;
  createdAt: Date;
  isRecruiting: boolean;
}

export default function AlliancesPage() {
  const [alliances, setAlliances] = useState<Alliance[]>([]);
  const [loading, setLoading] = useState(true);
  const [searchQuery, setSearchQuery] = useState('');
  const [stats, setStats] = useState({
    total: 0,
    totalMembers: 0,
    avgSize: 0,
    activeWars: 0,
  });

  useEffect(() => {
    fetchAlliances();
  }, []);

  const fetchAlliances = async () => {
    setLoading(true);
    try {
      const alliancesQuery = query(
        collection(db, 'alliances'),
        orderBy('level', 'desc'),
        limit(500)
      );
      const snapshot = await getDocs(alliancesQuery);
      const alliancesData = snapshot.docs.map((doc) => ({
        id: doc.id,
        name: doc.data().name || 'Unknown Alliance',
        tag: doc.data().tag || '???',
        description: doc.data().description || '',
        leaderId: doc.data().leaderId || '',
        leaderName: doc.data().leaderName || 'Unknown',
        memberCount: doc.data().memberCount || 1,
        maxMembers: doc.data().maxMembers || 50,
        level: doc.data().level || 1,
        xp: doc.data().xp || 0,
        territoriesControlled: doc.data().territoriesControlled || 0,
        totalPower: doc.data().totalPower || 0,
        warWins: doc.data().warWins || 0,
        warLosses: doc.data().warLosses || 0,
        createdAt: doc.data().createdAt?.toDate() || new Date(),
        isRecruiting: doc.data().isRecruiting ?? true,
      }));

      setAlliances(alliancesData);

      // Calculate stats
      const total = alliancesData.length;
      const totalMembers = alliancesData.reduce((sum, a) => sum + a.memberCount, 0);
      const avgSize = total > 0 ? totalMembers / total : 0;

      setStats({
        total,
        totalMembers,
        avgSize: Math.round(avgSize * 10) / 10,
        activeWars: Math.floor(total / 5), // Simulated
      });
    } catch (error) {
      console.error('Error fetching alliances:', error);
    } finally {
      setLoading(false);
    }
  };

  const filteredAlliances = alliances.filter(
    (alliance) =>
      alliance.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
      alliance.tag.toLowerCase().includes(searchQuery.toLowerCase()) ||
      alliance.leaderName.toLowerCase().includes(searchQuery.toLowerCase())
  );

  const columns: GridColDef[] = [
    {
      field: 'name',
      headerName: 'Alliance',
      flex: 1,
      minWidth: 220,
      renderCell: (params: GridRenderCellParams) => (
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
          <Avatar sx={{ bgcolor: 'primary.main', width: 40, height: 40 }}>
            <GroupsIcon />
          </Avatar>
          <Box>
            <Typography variant="body2" sx={{ fontWeight: 600 }}>
              {params.value}
            </Typography>
            <Chip label={`[${params.row.tag}]`} size="small" sx={{ height: 20, fontSize: '0.7rem' }} />
          </Box>
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
            fontWeight: 600,
          }}
        />
      ),
    },
    {
      field: 'memberCount',
      headerName: 'Members',
      width: 120,
      renderCell: (params: GridRenderCellParams) => (
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
          <AvatarGroup max={3} sx={{ '& .MuiAvatar-root': { width: 24, height: 24, fontSize: 12 } }}>
            {Array.from({ length: Math.min(params.value, 3) }).map((_, i) => (
              <Avatar key={i} sx={{ bgcolor: 'secondary.main' }} />
            ))}
          </AvatarGroup>
          <Typography variant="body2">
            {params.value}/{params.row.maxMembers}
          </Typography>
        </Box>
      ),
    },
    {
      field: 'totalPower',
      headerName: 'Power',
      width: 100,
      align: 'right',
      headerAlign: 'right',
      renderCell: (params: GridRenderCellParams) => (
        <Typography variant="body2" sx={{ color: '#f59e0b', fontWeight: 600 }}>
          {params.value?.toLocaleString()}
        </Typography>
      ),
    },
    {
      field: 'territoriesControlled',
      headerName: 'Territories',
      width: 100,
      align: 'center',
      headerAlign: 'center',
    },
    {
      field: 'warRecord',
      headerName: 'War Record',
      width: 120,
      renderCell: (params: GridRenderCellParams) => (
        <Typography variant="body2">
          <span style={{ color: '#22c55e' }}>{params.row.warWins}W</span>
          {' / '}
          <span style={{ color: '#ef4444' }}>{params.row.warLosses}L</span>
        </Typography>
      ),
    },
    {
      field: 'leaderName',
      headerName: 'Leader',
      width: 140,
    },
    {
      field: 'isRecruiting',
      headerName: 'Status',
      width: 110,
      renderCell: (params: GridRenderCellParams) => (
        <Chip
          label={params.value ? 'Recruiting' : 'Closed'}
          size="small"
          color={params.value ? 'success' : 'default'}
          variant="outlined"
        />
      ),
    },
    {
      field: 'createdAt',
      headerName: 'Founded',
      width: 120,
      renderCell: (params: GridRenderCellParams) => (
        <Typography variant="body2" sx={{ color: 'text.secondary' }}>
          {params.value?.toLocaleDateString()}
        </Typography>
      ),
    },
  ];

  return (
    <Box className="animate-fadeIn">
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <Typography variant="h4" sx={{ fontWeight: 700 }}>
          Alliance Management
        </Typography>
        <Button
          variant="outlined"
          startIcon={<RefreshIcon />}
          onClick={fetchAlliances}
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
                Total Alliances
              </Typography>
            </CardContent>
          </Card>
        </Grid>
        <Grid item xs={6} md={3}>
          <Card>
            <CardContent sx={{ textAlign: 'center' }}>
              <Typography variant="h4" sx={{ color: '#22c55e' }}>
                {stats.totalMembers}
              </Typography>
              <Typography variant="body2" sx={{ color: 'text.secondary' }}>
                Total Members
              </Typography>
            </CardContent>
          </Card>
        </Grid>
        <Grid item xs={6} md={3}>
          <Card>
            <CardContent sx={{ textAlign: 'center' }}>
              <Typography variant="h4" sx={{ color: '#3b82f6' }}>
                {stats.avgSize}
              </Typography>
              <Typography variant="body2" sx={{ color: 'text.secondary' }}>
                Avg Size
              </Typography>
            </CardContent>
          </Card>
        </Grid>
        <Grid item xs={6} md={3}>
          <Card>
            <CardContent sx={{ textAlign: 'center' }}>
              <Typography variant="h4" sx={{ color: '#ef4444' }}>
                {stats.activeWars}
              </Typography>
              <Typography variant="body2" sx={{ color: 'text.secondary' }}>
                Active Wars
              </Typography>
            </CardContent>
          </Card>
        </Grid>
      </Grid>

      <Card>
        <CardContent>
          {/* Search */}
          <TextField
            placeholder="Search alliances by name, tag, or leader..."
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
              rows={filteredAlliances}
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
