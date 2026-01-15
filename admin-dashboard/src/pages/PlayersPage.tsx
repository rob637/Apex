import { useState, useEffect, useCallback } from 'react';
import {
  Box,
  Card,
  CardContent,
  Typography,
  TextField,
  InputAdornment,
  Chip,
  IconButton,
  Button,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Grid,
  Avatar,
  Tabs,
  Tab,
} from '@mui/material';
import { DataGrid, GridColDef, GridRenderCellParams } from '@mui/x-data-grid';
import {
  Search as SearchIcon,
  Visibility as ViewIcon,
  Block as BanIcon,
  Refresh as RefreshIcon,
  Edit as EditIcon,
} from '@mui/icons-material';
import {
  collection,
  getDocs,
  doc,
  getDoc,
  updateDoc,
  query,
  where,
  orderBy,
  limit,
} from 'firebase/firestore';
import { db } from '../config/firebase';

interface Player {
  id: string;
  displayName: string;
  email: string;
  level: number;
  xp: number;
  gold: number;
  gems: number;
  allianceId: string | null;
  createdAt: Date;
  lastSeen: Date;
  isBanned: boolean;
  trustScore: number;
}

interface PlayerDetails extends Player {
  citadelsCount: number;
  territoriesControlled: number;
  battlesWon: number;
  battlesLost: number;
  totalSpent: number;
  referralCode: string | null;
  achievements: string[];
}

export default function PlayersPage() {
  const [players, setPlayers] = useState<Player[]>([]);
  const [loading, setLoading] = useState(true);
  const [searchQuery, setSearchQuery] = useState('');
  const [selectedPlayer, setSelectedPlayer] = useState<PlayerDetails | null>(null);
  const [detailsDialogOpen, setDetailsDialogOpen] = useState(false);
  const [currentTab, setCurrentTab] = useState(0);

  useEffect(() => {
    fetchPlayers();
  }, []);

  const fetchPlayers = async () => {
    setLoading(true);
    try {
      const usersQuery = query(
        collection(db, 'users'),
        orderBy('createdAt', 'desc'),
        limit(500)
      );
      const snapshot = await getDocs(usersQuery);
      const playersData = snapshot.docs.map((doc) => ({
        id: doc.id,
        displayName: doc.data().displayName || 'Unknown',
        email: doc.data().email || '',
        level: doc.data().level || 1,
        xp: doc.data().xp || 0,
        gold: doc.data().gold || 0,
        gems: doc.data().gems || 0,
        allianceId: doc.data().allianceId || null,
        createdAt: doc.data().createdAt?.toDate() || new Date(),
        lastSeen: doc.data().lastSeen?.toDate() || new Date(),
        isBanned: doc.data().isBanned || false,
        trustScore: doc.data().trustScore || 100,
      }));
      setPlayers(playersData);
    } catch (error) {
      console.error('Error fetching players:', error);
    } finally {
      setLoading(false);
    }
  };

  const fetchPlayerDetails = async (playerId: string) => {
    try {
      const userDoc = await getDoc(doc(db, 'users', playerId));
      if (!userDoc.exists()) return null;

      const userData = userDoc.data();

      // Fetch additional data
      const citadelsQuery = query(
        collection(db, 'citadels'),
        where('ownerId', '==', playerId)
      );
      const citadelsSnapshot = await getDocs(citadelsQuery);

      const territoriesQuery = query(
        collection(db, 'territories'),
        where('controllerId', '==', playerId)
      );
      const territoriesSnapshot = await getDocs(territoriesQuery);

      // Fetch referral code
      const referralQuery = query(
        collection(db, 'referral_codes'),
        where('userId', '==', playerId)
      );
      const referralSnapshot = await getDocs(referralQuery);

      return {
        id: playerId,
        displayName: userData.displayName || 'Unknown',
        email: userData.email || '',
        level: userData.level || 1,
        xp: userData.xp || 0,
        gold: userData.gold || 0,
        gems: userData.gems || 0,
        allianceId: userData.allianceId || null,
        createdAt: userData.createdAt?.toDate() || new Date(),
        lastSeen: userData.lastSeen?.toDate() || new Date(),
        isBanned: userData.isBanned || false,
        trustScore: userData.trustScore || 100,
        citadelsCount: citadelsSnapshot.size,
        territoriesControlled: territoriesSnapshot.size,
        battlesWon: userData.stats?.battlesWon || 0,
        battlesLost: userData.stats?.battlesLost || 0,
        totalSpent: userData.totalSpent || 0,
        referralCode: referralSnapshot.empty ? null : referralSnapshot.docs[0].id,
        achievements: userData.achievements || [],
      } as PlayerDetails;
    } catch (error) {
      console.error('Error fetching player details:', error);
      return null;
    }
  };

  const handleViewPlayer = async (playerId: string) => {
    const details = await fetchPlayerDetails(playerId);
    if (details) {
      setSelectedPlayer(details);
      setDetailsDialogOpen(true);
    }
  };

  const handleBanPlayer = async (playerId: string, isBanned: boolean) => {
    try {
      await updateDoc(doc(db, 'users', playerId), {
        isBanned: !isBanned,
      });
      fetchPlayers();
    } catch (error) {
      console.error('Error updating player ban status:', error);
    }
  };

  const filteredPlayers = players.filter(
    (player) =>
      player.displayName.toLowerCase().includes(searchQuery.toLowerCase()) ||
      player.email.toLowerCase().includes(searchQuery.toLowerCase()) ||
      player.id.toLowerCase().includes(searchQuery.toLowerCase())
  );

  const columns: GridColDef[] = [
    {
      field: 'displayName',
      headerName: 'Player',
      flex: 1,
      minWidth: 200,
      renderCell: (params: GridRenderCellParams) => (
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
          <Avatar sx={{ width: 32, height: 32, bgcolor: 'primary.main' }}>
            {params.value?.charAt(0).toUpperCase()}
          </Avatar>
          <Box>
            <Typography variant="body2">{params.value}</Typography>
            <Typography variant="caption" sx={{ color: 'text.secondary' }}>
              {params.row.email}
            </Typography>
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
    },
    {
      field: 'gold',
      headerName: 'Gold',
      width: 100,
      renderCell: (params: GridRenderCellParams) => (
        <Typography variant="body2" sx={{ color: '#f59e0b' }}>
          {params.value?.toLocaleString()}
        </Typography>
      ),
    },
    {
      field: 'gems',
      headerName: 'Gems',
      width: 80,
      renderCell: (params: GridRenderCellParams) => (
        <Typography variant="body2" sx={{ color: '#8b5cf6' }}>
          {params.value?.toLocaleString()}
        </Typography>
      ),
    },
    {
      field: 'trustScore',
      headerName: 'Trust',
      width: 100,
      renderCell: (params: GridRenderCellParams) => (
        <Chip
          label={params.value}
          size="small"
          sx={{
            backgroundColor:
              params.value >= 80
                ? 'rgba(34, 197, 94, 0.1)'
                : params.value >= 50
                ? 'rgba(245, 158, 11, 0.1)'
                : 'rgba(239, 68, 68, 0.1)',
            color:
              params.value >= 80
                ? 'success.main'
                : params.value >= 50
                ? 'warning.main'
                : 'error.main',
          }}
        />
      ),
    },
    {
      field: 'lastSeen',
      headerName: 'Last Seen',
      width: 150,
      renderCell: (params: GridRenderCellParams) => (
        <Typography variant="body2" sx={{ color: 'text.secondary' }}>
          {params.value?.toLocaleDateString()}
        </Typography>
      ),
    },
    {
      field: 'isBanned',
      headerName: 'Status',
      width: 100,
      renderCell: (params: GridRenderCellParams) => (
        <Chip
          label={params.value ? 'Banned' : 'Active'}
          size="small"
          color={params.value ? 'error' : 'success'}
          variant="outlined"
        />
      ),
    },
    {
      field: 'actions',
      headerName: 'Actions',
      width: 120,
      sortable: false,
      renderCell: (params: GridRenderCellParams) => (
        <Box>
          <IconButton
            size="small"
            onClick={() => handleViewPlayer(params.row.id)}
            title="View Details"
          >
            <ViewIcon fontSize="small" />
          </IconButton>
          <IconButton
            size="small"
            onClick={() => handleBanPlayer(params.row.id, params.row.isBanned)}
            title={params.row.isBanned ? 'Unban Player' : 'Ban Player'}
            color={params.row.isBanned ? 'success' : 'error'}
          >
            <BanIcon fontSize="small" />
          </IconButton>
        </Box>
      ),
    },
  ];

  const handleTabChange = useCallback((_event: React.SyntheticEvent, newValue: number) => {
    setCurrentTab(newValue);
  }, []);

  return (
    <Box className="animate-fadeIn">
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <Typography variant="h4" sx={{ fontWeight: 700 }}>
          Player Management
        </Typography>
        <Button
          variant="outlined"
          startIcon={<RefreshIcon />}
          onClick={fetchPlayers}
        >
          Refresh
        </Button>
      </Box>

      <Card>
        <CardContent>
          {/* Search */}
          <TextField
            placeholder="Search players by name, email, or ID..."
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
              rows={filteredPlayers}
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

      {/* Player Details Dialog */}
      <Dialog
        open={detailsDialogOpen}
        onClose={() => setDetailsDialogOpen(false)}
        maxWidth="md"
        fullWidth
      >
        {selectedPlayer && (
          <>
            <DialogTitle>
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
                <Avatar sx={{ width: 48, height: 48, bgcolor: 'primary.main' }}>
                  {selectedPlayer.displayName.charAt(0).toUpperCase()}
                </Avatar>
                <Box>
                  <Typography variant="h6">{selectedPlayer.displayName}</Typography>
                  <Typography variant="body2" sx={{ color: 'text.secondary' }}>
                    {selectedPlayer.email}
                  </Typography>
                </Box>
              </Box>
            </DialogTitle>
            <DialogContent>
              <Tabs value={currentTab} onChange={handleTabChange} sx={{ mb: 2 }}>
                <Tab label="Overview" />
                <Tab label="Stats" />
                <Tab label="Economy" />
              </Tabs>

              {currentTab === 0 && (
                <Grid container spacing={2}>
                  <Grid item xs={6} md={3}>
                    <Card variant="outlined">
                      <CardContent sx={{ textAlign: 'center' }}>
                        <Typography variant="h4" sx={{ color: 'primary.main' }}>
                          {selectedPlayer.level}
                        </Typography>
                        <Typography variant="body2" sx={{ color: 'text.secondary' }}>
                          Level
                        </Typography>
                      </CardContent>
                    </Card>
                  </Grid>
                  <Grid item xs={6} md={3}>
                    <Card variant="outlined">
                      <CardContent sx={{ textAlign: 'center' }}>
                        <Typography variant="h4" sx={{ color: '#22c55e' }}>
                          {selectedPlayer.xp.toLocaleString()}
                        </Typography>
                        <Typography variant="body2" sx={{ color: 'text.secondary' }}>
                          XP
                        </Typography>
                      </CardContent>
                    </Card>
                  </Grid>
                  <Grid item xs={6} md={3}>
                    <Card variant="outlined">
                      <CardContent sx={{ textAlign: 'center' }}>
                        <Typography variant="h4" sx={{ color: '#3b82f6' }}>
                          {selectedPlayer.citadelsCount}
                        </Typography>
                        <Typography variant="body2" sx={{ color: 'text.secondary' }}>
                          Citadels
                        </Typography>
                      </CardContent>
                    </Card>
                  </Grid>
                  <Grid item xs={6} md={3}>
                    <Card variant="outlined">
                      <CardContent sx={{ textAlign: 'center' }}>
                        <Typography variant="h4" sx={{ color: '#8b5cf6' }}>
                          {selectedPlayer.territoriesControlled}
                        </Typography>
                        <Typography variant="body2" sx={{ color: 'text.secondary' }}>
                          Territories
                        </Typography>
                      </CardContent>
                    </Card>
                  </Grid>
                  <Grid item xs={12}>
                    <Typography variant="body2" sx={{ color: 'text.secondary', mt: 2 }}>
                      <strong>User ID:</strong> {selectedPlayer.id}
                    </Typography>
                    <Typography variant="body2" sx={{ color: 'text.secondary' }}>
                      <strong>Joined:</strong> {selectedPlayer.createdAt.toLocaleDateString()}
                    </Typography>
                    <Typography variant="body2" sx={{ color: 'text.secondary' }}>
                      <strong>Last Active:</strong> {selectedPlayer.lastSeen.toLocaleDateString()}
                    </Typography>
                    <Typography variant="body2" sx={{ color: 'text.secondary' }}>
                      <strong>Referral Code:</strong> {selectedPlayer.referralCode || 'None'}
                    </Typography>
                  </Grid>
                </Grid>
              )}

              {currentTab === 1 && (
                <Grid container spacing={2}>
                  <Grid item xs={6}>
                    <Card variant="outlined">
                      <CardContent>
                        <Typography variant="h4" sx={{ color: '#22c55e' }}>
                          {selectedPlayer.battlesWon}
                        </Typography>
                        <Typography variant="body2" sx={{ color: 'text.secondary' }}>
                          Battles Won
                        </Typography>
                      </CardContent>
                    </Card>
                  </Grid>
                  <Grid item xs={6}>
                    <Card variant="outlined">
                      <CardContent>
                        <Typography variant="h4" sx={{ color: '#ef4444' }}>
                          {selectedPlayer.battlesLost}
                        </Typography>
                        <Typography variant="body2" sx={{ color: 'text.secondary' }}>
                          Battles Lost
                        </Typography>
                      </CardContent>
                    </Card>
                  </Grid>
                  <Grid item xs={12}>
                    <Typography variant="body2" sx={{ color: 'text.secondary', mt: 2 }}>
                      <strong>Win Rate:</strong>{' '}
                      {selectedPlayer.battlesWon + selectedPlayer.battlesLost > 0
                        ? (
                            (selectedPlayer.battlesWon /
                              (selectedPlayer.battlesWon + selectedPlayer.battlesLost)) *
                            100
                          ).toFixed(1)
                        : 0}
                      %
                    </Typography>
                    <Typography variant="body2" sx={{ color: 'text.secondary' }}>
                      <strong>Trust Score:</strong> {selectedPlayer.trustScore}
                    </Typography>
                  </Grid>
                </Grid>
              )}

              {currentTab === 2 && (
                <Grid container spacing={2}>
                  <Grid item xs={6} md={4}>
                    <Card variant="outlined">
                      <CardContent sx={{ textAlign: 'center' }}>
                        <Typography variant="h4" sx={{ color: '#f59e0b' }}>
                          {selectedPlayer.gold.toLocaleString()}
                        </Typography>
                        <Typography variant="body2" sx={{ color: 'text.secondary' }}>
                          Gold
                        </Typography>
                      </CardContent>
                    </Card>
                  </Grid>
                  <Grid item xs={6} md={4}>
                    <Card variant="outlined">
                      <CardContent sx={{ textAlign: 'center' }}>
                        <Typography variant="h4" sx={{ color: '#8b5cf6' }}>
                          {selectedPlayer.gems.toLocaleString()}
                        </Typography>
                        <Typography variant="body2" sx={{ color: 'text.secondary' }}>
                          Gems
                        </Typography>
                      </CardContent>
                    </Card>
                  </Grid>
                  <Grid item xs={12} md={4}>
                    <Card variant="outlined">
                      <CardContent sx={{ textAlign: 'center' }}>
                        <Typography variant="h4" sx={{ color: '#22c55e' }}>
                          ${selectedPlayer.totalSpent.toLocaleString()}
                        </Typography>
                        <Typography variant="body2" sx={{ color: 'text.secondary' }}>
                          Total Spent
                        </Typography>
                      </CardContent>
                    </Card>
                  </Grid>
                </Grid>
              )}
            </DialogContent>
            <DialogActions>
              <Button
                startIcon={<EditIcon />}
                onClick={() => {
                  /* TODO: Edit player */
                }}
              >
                Edit Player
              </Button>
              <Button onClick={() => setDetailsDialogOpen(false)}>Close</Button>
            </DialogActions>
          </>
        )}
      </Dialog>
    </Box>
  );
}
