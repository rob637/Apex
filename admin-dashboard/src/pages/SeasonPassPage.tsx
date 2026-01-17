import { useState, useEffect } from 'react';
import {
  Box,
  Card,
  CardContent,
  Typography,
  TextField,
  Button,
  Grid,
  Chip,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  LinearProgress,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  IconButton,
} from '@mui/material';
import {
  Add as AddIcon,
  Refresh as RefreshIcon,
  Edit as EditIcon,
  Stars as StarsIcon,
} from '@mui/icons-material';
import {
  collection,
  getDocs,
  doc,
  addDoc,
  updateDoc,
  query,
  orderBy,
  where,
  Timestamp,
} from 'firebase/firestore';
import { db } from '../config/firebase';

interface Season {
  id: string;
  name: string;
  number: number;
  status: 'upcoming' | 'active' | 'ended';
  startDate: Date;
  endDate: Date;
  totalLevels: number;
  premiumPrice: number;
  totalPlayers: number;
  premiumPlayers: number;
  rewards: Array<{
    level: number;
    freeReward: string;
    premiumReward: string;
  }>;
}

export default function SeasonPassPage() {
  const [seasons, setSeasons] = useState<Season[]>([]);
  const [loading, setLoading] = useState(true);
  const [createDialogOpen, setCreateDialogOpen] = useState(false);
  const [selectedSeason, setSelectedSeason] = useState<Season | null>(null);
  const [newSeason, setNewSeason] = useState({
    name: '',
    number: 1,
    durationDays: 90,
    totalLevels: 100,
    premiumPrice: 999, // In cents
  });

  useEffect(() => {
    fetchSeasons();
  }, []);

  const fetchSeasons = async () => {
    setLoading(true);
    try {
      const seasonsQuery = query(
        collection(db, 'seasons'),
        orderBy('number', 'desc')
      );
      const snapshot = await getDocs(seasonsQuery);
      const seasonsData: Season[] = [];

      for (const docSnapshot of snapshot.docs) {
        const data = docSnapshot.data();

        // Get player count for this season
        const progressQuery = query(
          collection(db, 'season_progress'),
          where('seasonId', '==', docSnapshot.id)
        );
        const progressSnapshot = await getDocs(progressQuery);
        const totalPlayers = progressSnapshot.size;
        const premiumPlayers = progressSnapshot.docs.filter(
          (d) => d.data().hasPremium
        ).length;

        seasonsData.push({
          id: docSnapshot.id,
          name: data.name || `Season ${data.number}`,
          number: data.number || 1,
          status: data.status || 'upcoming',
          startDate: data.startDate?.toDate() || new Date(),
          endDate: data.endDate?.toDate() || new Date(),
          totalLevels: data.totalLevels || 100,
          premiumPrice: data.premiumPrice || 999,
          totalPlayers,
          premiumPlayers,
          rewards: data.rewards || [],
        });
      }

      setSeasons(seasonsData);
    } catch (error) {
      console.error('Error fetching seasons:', error);
    } finally {
      setLoading(false);
    }
  };

  const handleCreateSeason = async () => {
    try {
      const startDate = new Date();
      const endDate = new Date(
        startDate.getTime() + newSeason.durationDays * 24 * 60 * 60 * 1000
      );

      // Generate default rewards
      const rewards = [];
      for (let i = 1; i <= newSeason.totalLevels; i++) {
        rewards.push({
          level: i,
          freeReward:
            i % 10 === 0 ? '500 gems' : i % 5 === 0 ? '1000 gold' : '200 gold',
          premiumReward:
            i % 10 === 0
              ? 'Exclusive Skin'
              : i % 5 === 0
              ? '1000 gems'
              : '500 gold',
        });
      }

      await addDoc(collection(db, 'seasons'), {
        name: newSeason.name || `Season ${newSeason.number}`,
        number: newSeason.number,
        status: 'upcoming',
        startDate: Timestamp.fromDate(startDate),
        endDate: Timestamp.fromDate(endDate),
        totalLevels: newSeason.totalLevels,
        premiumPrice: newSeason.premiumPrice,
        rewards,
        createdAt: Timestamp.now(),
      });

      setCreateDialogOpen(false);
      setNewSeason({
        name: '',
        number: seasons.length + 1,
        durationDays: 90,
        totalLevels: 100,
        premiumPrice: 999,
      });
      fetchSeasons();
    } catch (error) {
      console.error('Error creating season:', error);
    }
  };

  const handleActivateSeason = async (seasonId: string) => {
    try {
      // Deactivate any currently active season
      const activeSeasons = seasons.filter((s) => s.status === 'active');
      for (const season of activeSeasons) {
        await updateDoc(doc(db, 'seasons', season.id), { status: 'ended' });
      }

      // Activate the selected season
      await updateDoc(doc(db, 'seasons', seasonId), {
        status: 'active',
        startDate: Timestamp.now(),
      });
      fetchSeasons();
    } catch (error) {
      console.error('Error activating season:', error);
    }
  };

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'active':
        return 'success';
      case 'upcoming':
        return 'warning';
      case 'ended':
        return 'default';
      default:
        return 'default';
    }
  };

  const activeSeason = seasons.find((s) => s.status === 'active');
  const totalRevenue = seasons.reduce(
    (sum, s) => sum + s.premiumPlayers * (s.premiumPrice / 100),
    0
  );

  return (
    <Box className="animate-fadeIn">
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <Typography variant="h4" sx={{ fontWeight: 700 }}>
          Season Pass Management
        </Typography>
        <Box sx={{ display: 'flex', gap: 1 }}>
          <Button
            variant="outlined"
            startIcon={<RefreshIcon />}
            onClick={fetchSeasons}
          >
            Refresh
          </Button>
          <Button
            variant="contained"
            startIcon={<AddIcon />}
            onClick={() => setCreateDialogOpen(true)}
          >
            New Season
          </Button>
        </Box>
      </Box>

      {/* Stats */}
      <Grid container spacing={2} sx={{ mb: 3 }}>
        <Grid item xs={6} md={3}>
          <Card>
            <CardContent sx={{ textAlign: 'center' }}>
              <Typography variant="h4" sx={{ color: 'primary.main' }}>
                {activeSeason?.name || 'None'}
              </Typography>
              <Typography variant="body2" sx={{ color: 'text.secondary' }}>
                Active Season
              </Typography>
            </CardContent>
          </Card>
        </Grid>
        <Grid item xs={6} md={3}>
          <Card>
            <CardContent sx={{ textAlign: 'center' }}>
              <Typography variant="h4" sx={{ color: '#22c55e' }}>
                {activeSeason?.totalPlayers || 0}
              </Typography>
              <Typography variant="body2" sx={{ color: 'text.secondary' }}>
                Active Players
              </Typography>
            </CardContent>
          </Card>
        </Grid>
        <Grid item xs={6} md={3}>
          <Card>
            <CardContent sx={{ textAlign: 'center' }}>
              <Typography variant="h4" sx={{ color: '#8b5cf6' }}>
                {activeSeason?.premiumPlayers || 0}
              </Typography>
              <Typography variant="body2" sx={{ color: 'text.secondary' }}>
                Premium Holders
              </Typography>
            </CardContent>
          </Card>
        </Grid>
        <Grid item xs={6} md={3}>
          <Card>
            <CardContent sx={{ textAlign: 'center' }}>
              <Typography variant="h4" sx={{ color: '#f59e0b' }}>
                ${totalRevenue.toLocaleString()}
              </Typography>
              <Typography variant="body2" sx={{ color: 'text.secondary' }}>
                Total Revenue
              </Typography>
            </CardContent>
          </Card>
        </Grid>
      </Grid>

      {/* Seasons List */}
      <Card>
        <CardContent>
          {loading ? (
            <LinearProgress />
          ) : (
            <TableContainer>
              <Table>
                <TableHead>
                  <TableRow>
                    <TableCell>Season</TableCell>
                    <TableCell>Status</TableCell>
                    <TableCell>Duration</TableCell>
                    <TableCell align="center">Levels</TableCell>
                    <TableCell align="center">Players</TableCell>
                    <TableCell align="center">Premium</TableCell>
                    <TableCell align="right">Revenue</TableCell>
                    <TableCell>Actions</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {seasons.map((season) => (
                    <TableRow key={season.id} hover>
                      <TableCell>
                        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                          <StarsIcon sx={{ color: '#f59e0b' }} />
                          <Box>
                            <Typography variant="body2" sx={{ fontWeight: 600 }}>
                              {season.name}
                            </Typography>
                            <Typography variant="caption" sx={{ color: 'text.secondary' }}>
                              Season #{season.number}
                            </Typography>
                          </Box>
                        </Box>
                      </TableCell>
                      <TableCell>
                        <Chip
                          label={season.status}
                          size="small"
                          color={getStatusColor(season.status)}
                          variant="outlined"
                        />
                      </TableCell>
                      <TableCell>
                        <Typography variant="body2">
                          {season.startDate.toLocaleDateString()} -{' '}
                          {season.endDate.toLocaleDateString()}
                        </Typography>
                      </TableCell>
                      <TableCell align="center">{season.totalLevels}</TableCell>
                      <TableCell align="center">{season.totalPlayers}</TableCell>
                      <TableCell align="center">
                        <Typography variant="body2" sx={{ color: '#8b5cf6' }}>
                          {season.premiumPlayers} (
                          {season.totalPlayers > 0
                            ? Math.round(
                                (season.premiumPlayers / season.totalPlayers) * 100
                              )
                            : 0}
                          %)
                        </Typography>
                      </TableCell>
                      <TableCell align="right">
                        <Typography variant="body2" sx={{ color: '#22c55e', fontWeight: 600 }}>
                          ${((season.premiumPlayers * season.premiumPrice) / 100).toLocaleString()}
                        </Typography>
                      </TableCell>
                      <TableCell>
                        <Box sx={{ display: 'flex', gap: 1 }}>
                          {season.status === 'upcoming' && (
                            <Button
                              size="small"
                              variant="contained"
                              color="success"
                              onClick={() => handleActivateSeason(season.id)}
                            >
                              Activate
                            </Button>
                          )}
                          <IconButton
                            size="small"
                            onClick={() => setSelectedSeason(season)}
                          >
                            <EditIcon fontSize="small" />
                          </IconButton>
                        </Box>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </TableContainer>
          )}
        </CardContent>
      </Card>

      {/* Create Season Dialog */}
      <Dialog
        open={createDialogOpen}
        onClose={() => setCreateDialogOpen(false)}
        maxWidth="sm"
        fullWidth
      >
        <DialogTitle>Create New Season</DialogTitle>
        <DialogContent>
          <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2, mt: 2 }}>
            <TextField
              label="Season Name"
              value={newSeason.name}
              onChange={(e) => setNewSeason({ ...newSeason, name: e.target.value })}
              fullWidth
              placeholder={`Season ${newSeason.number}`}
            />
            <TextField
              label="Season Number"
              type="number"
              value={newSeason.number}
              onChange={(e) =>
                setNewSeason({ ...newSeason, number: parseInt(e.target.value) })
              }
              fullWidth
            />
            <TextField
              label="Duration (days)"
              type="number"
              value={newSeason.durationDays}
              onChange={(e) =>
                setNewSeason({ ...newSeason, durationDays: parseInt(e.target.value) })
              }
              fullWidth
            />
            <TextField
              label="Total Levels"
              type="number"
              value={newSeason.totalLevels}
              onChange={(e) =>
                setNewSeason({ ...newSeason, totalLevels: parseInt(e.target.value) })
              }
              fullWidth
            />
            <TextField
              label="Premium Price (cents)"
              type="number"
              value={newSeason.premiumPrice}
              onChange={(e) =>
                setNewSeason({ ...newSeason, premiumPrice: parseInt(e.target.value) })
              }
              fullWidth
              helperText={`$${(newSeason.premiumPrice / 100).toFixed(2)} USD`}
            />
          </Box>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setCreateDialogOpen(false)}>Cancel</Button>
          <Button variant="contained" onClick={handleCreateSeason}>
            Create Season
          </Button>
        </DialogActions>
      </Dialog>

      {/* Edit Season Dialog */}
      <Dialog
        open={!!selectedSeason}
        onClose={() => setSelectedSeason(null)}
        maxWidth="md"
        fullWidth
      >
        {selectedSeason && (
          <>
            <DialogTitle>
              Edit {selectedSeason.name} - Rewards
            </DialogTitle>
            <DialogContent>
              <Typography variant="body2" sx={{ mb: 2, color: 'text.secondary' }}>
                Configure rewards for each level of the season pass.
              </Typography>
              <TableContainer sx={{ maxHeight: 400 }}>
                <Table size="small" stickyHeader>
                  <TableHead>
                    <TableRow>
                      <TableCell>Level</TableCell>
                      <TableCell>Free Reward</TableCell>
                      <TableCell>Premium Reward</TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {selectedSeason.rewards.slice(0, 20).map((reward) => (
                      <TableRow key={reward.level}>
                        <TableCell>
                          <Chip label={reward.level} size="small" />
                        </TableCell>
                        <TableCell>{reward.freeReward}</TableCell>
                        <TableCell>
                          <Typography sx={{ color: '#8b5cf6' }}>
                            {reward.premiumReward}
                          </Typography>
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </TableContainer>
              <Typography variant="caption" sx={{ color: 'text.secondary', mt: 1, display: 'block' }}>
                Showing first 20 levels. Full editor coming soon.
              </Typography>
            </DialogContent>
            <DialogActions>
              <Button onClick={() => setSelectedSeason(null)}>Close</Button>
            </DialogActions>
          </>
        )}
      </Dialog>
    </Box>
  );
}
