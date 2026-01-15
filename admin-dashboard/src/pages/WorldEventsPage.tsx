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
  MenuItem,
  Select,
  FormControl,
  InputLabel,
  LinearProgress,
  IconButton,
} from '@mui/material';
import { DataGrid, GridColDef, GridRenderCellParams } from '@mui/x-data-grid';
import {
  Add as AddIcon,
  Refresh as RefreshIcon,
  Edit as EditIcon,
  Delete as DeleteIcon,
  PlayArrow as StartIcon,
  Stop as StopIcon,
} from '@mui/icons-material';
import {
  collection,
  getDocs,
  doc,
  addDoc,
  updateDoc,
  deleteDoc,
  query,
  orderBy,
  Timestamp,
} from 'firebase/firestore';
import { db } from '../config/firebase';

interface WorldEvent {
  id: string;
  name: string;
  description: string;
  eventType: string;
  status: 'scheduled' | 'active' | 'ended';
  startTime: Date;
  endTime: Date;
  rewards: {
    gold: number;
    xp: number;
    gems: number;
  };
  participantCount: number;
  config: Record<string, unknown>;
}

const EVENT_TYPES = [
  { value: 'world_boss', label: 'World Boss', color: '#ef4444' },
  { value: 'territory_rush', label: 'Territory Rush', color: '#f59e0b' },
  { value: 'resource_surge', label: 'Resource Surge', color: '#22c55e' },
  { value: 'alliance_war_weekend', label: 'Alliance War Weekend', color: '#8b5cf6' },
  { value: 'double_xp', label: 'Double XP', color: '#3b82f6' },
  { value: 'treasure_hunt', label: 'Treasure Hunt', color: '#f59e0b' },
  { value: 'defend_the_realm', label: 'Defend the Realm', color: '#ef4444' },
];

export default function WorldEventsPage() {
  const [events, setEvents] = useState<WorldEvent[]>([]);
  const [loading, setLoading] = useState(true);
  const [createDialogOpen, setCreateDialogOpen] = useState(false);
  const [newEvent, setNewEvent] = useState({
    name: '',
    description: '',
    eventType: 'world_boss',
    duration: 24,
    goldReward: 1000,
    xpReward: 500,
    gemsReward: 10,
  });

  useEffect(() => {
    fetchEvents();
  }, []);

  const fetchEvents = async () => {
    setLoading(true);
    try {
      const eventsQuery = query(
        collection(db, 'world_events'),
        orderBy('startTime', 'desc')
      );
      const snapshot = await getDocs(eventsQuery);
      const eventsData = snapshot.docs.map((doc) => ({
        id: doc.id,
        name: doc.data().name || 'Unknown Event',
        description: doc.data().description || '',
        eventType: doc.data().eventType || 'world_boss',
        status: doc.data().status || 'scheduled',
        startTime: doc.data().startTime?.toDate() || new Date(),
        endTime: doc.data().endTime?.toDate() || new Date(),
        rewards: doc.data().rewards || { gold: 0, xp: 0, gems: 0 },
        participantCount: doc.data().participantCount || 0,
        config: doc.data().config || {},
      }));
      setEvents(eventsData);
    } catch (error) {
      console.error('Error fetching events:', error);
    } finally {
      setLoading(false);
    }
  };

  const handleCreateEvent = async () => {
    try {
      const startTime = new Date();
      const endTime = new Date(startTime.getTime() + newEvent.duration * 60 * 60 * 1000);

      await addDoc(collection(db, 'world_events'), {
        name: newEvent.name,
        description: newEvent.description,
        eventType: newEvent.eventType,
        status: 'scheduled',
        startTime: Timestamp.fromDate(startTime),
        endTime: Timestamp.fromDate(endTime),
        rewards: {
          gold: newEvent.goldReward,
          xp: newEvent.xpReward,
          gems: newEvent.gemsReward,
        },
        participantCount: 0,
        config: {},
        createdAt: Timestamp.now(),
      });

      setCreateDialogOpen(false);
      setNewEvent({
        name: '',
        description: '',
        eventType: 'world_boss',
        duration: 24,
        goldReward: 1000,
        xpReward: 500,
        gemsReward: 10,
      });
      fetchEvents();
    } catch (error) {
      console.error('Error creating event:', error);
    }
  };

  const handleStartEvent = async (eventId: string) => {
    try {
      await updateDoc(doc(db, 'world_events', eventId), {
        status: 'active',
        startTime: Timestamp.now(),
      });
      fetchEvents();
    } catch (error) {
      console.error('Error starting event:', error);
    }
  };

  const handleEndEvent = async (eventId: string) => {
    try {
      await updateDoc(doc(db, 'world_events', eventId), {
        status: 'ended',
        endTime: Timestamp.now(),
      });
      fetchEvents();
    } catch (error) {
      console.error('Error ending event:', error);
    }
  };

  const handleDeleteEvent = async (eventId: string) => {
    if (window.confirm('Are you sure you want to delete this event?')) {
      try {
        await deleteDoc(doc(db, 'world_events', eventId));
        fetchEvents();
      } catch (error) {
        console.error('Error deleting event:', error);
      }
    }
  };

  const getEventTypeInfo = (type: string) => {
    return EVENT_TYPES.find((t) => t.value === type) || { label: type, color: '#6366f1' };
  };

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'active':
        return 'success';
      case 'scheduled':
        return 'warning';
      case 'ended':
        return 'default';
      default:
        return 'default';
    }
  };

  const columns: GridColDef[] = [
    {
      field: 'name',
      headerName: 'Event',
      flex: 1,
      minWidth: 200,
      renderCell: (params: GridRenderCellParams) => (
        <Box>
          <Typography variant="body2" sx={{ fontWeight: 600 }}>
            {params.value}
          </Typography>
          <Typography variant="caption" sx={{ color: 'text.secondary' }}>
            {params.row.description?.slice(0, 50)}...
          </Typography>
        </Box>
      ),
    },
    {
      field: 'eventType',
      headerName: 'Type',
      width: 160,
      renderCell: (params: GridRenderCellParams) => {
        const typeInfo = getEventTypeInfo(params.value);
        return (
          <Chip
            label={typeInfo.label}
            size="small"
            sx={{
              backgroundColor: `${typeInfo.color}20`,
              color: typeInfo.color,
            }}
          />
        );
      },
    },
    {
      field: 'status',
      headerName: 'Status',
      width: 100,
      renderCell: (params: GridRenderCellParams) => (
        <Chip
          label={params.value}
          size="small"
          color={getStatusColor(params.value)}
          variant="outlined"
        />
      ),
    },
    {
      field: 'participantCount',
      headerName: 'Participants',
      width: 110,
      align: 'center',
      headerAlign: 'center',
    },
    {
      field: 'rewards',
      headerName: 'Rewards',
      width: 180,
      renderCell: (params: GridRenderCellParams) => (
        <Box sx={{ display: 'flex', gap: 1 }}>
          <Typography variant="caption" sx={{ color: '#f59e0b' }}>
            {params.value?.gold}g
          </Typography>
          <Typography variant="caption" sx={{ color: '#22c55e' }}>
            {params.value?.xp}xp
          </Typography>
          <Typography variant="caption" sx={{ color: '#8b5cf6' }}>
            {params.value?.gems}💎
          </Typography>
        </Box>
      ),
    },
    {
      field: 'startTime',
      headerName: 'Start',
      width: 140,
      renderCell: (params: GridRenderCellParams) => (
        <Typography variant="body2" sx={{ color: 'text.secondary' }}>
          {params.value?.toLocaleString()}
        </Typography>
      ),
    },
    {
      field: 'actions',
      headerName: 'Actions',
      width: 160,
      sortable: false,
      renderCell: (params: GridRenderCellParams) => (
        <Box>
          {params.row.status === 'scheduled' && (
            <IconButton
              size="small"
              onClick={() => handleStartEvent(params.row.id)}
              title="Start Event"
              color="success"
            >
              <StartIcon fontSize="small" />
            </IconButton>
          )}
          {params.row.status === 'active' && (
            <IconButton
              size="small"
              onClick={() => handleEndEvent(params.row.id)}
              title="End Event"
              color="error"
            >
              <StopIcon fontSize="small" />
            </IconButton>
          )}
          <IconButton size="small" title="Edit">
            <EditIcon fontSize="small" />
          </IconButton>
          <IconButton
            size="small"
            onClick={() => handleDeleteEvent(params.row.id)}
            title="Delete"
            color="error"
          >
            <DeleteIcon fontSize="small" />
          </IconButton>
        </Box>
      ),
    },
  ];

  const activeEvents = events.filter((e) => e.status === 'active').length;
  const scheduledEvents = events.filter((e) => e.status === 'scheduled').length;

  return (
    <Box className="animate-fadeIn">
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <Typography variant="h4" sx={{ fontWeight: 700 }}>
          World Events
        </Typography>
        <Box sx={{ display: 'flex', gap: 1 }}>
          <Button
            variant="outlined"
            startIcon={<RefreshIcon />}
            onClick={fetchEvents}
          >
            Refresh
          </Button>
          <Button
            variant="contained"
            startIcon={<AddIcon />}
            onClick={() => setCreateDialogOpen(true)}
          >
            Create Event
          </Button>
        </Box>
      </Box>

      {/* Stats */}
      <Grid container spacing={2} sx={{ mb: 3 }}>
        <Grid item xs={6} md={3}>
          <Card>
            <CardContent sx={{ textAlign: 'center' }}>
              <Typography variant="h4" sx={{ color: '#22c55e' }}>
                {activeEvents}
              </Typography>
              <Typography variant="body2" sx={{ color: 'text.secondary' }}>
                Active Events
              </Typography>
            </CardContent>
          </Card>
        </Grid>
        <Grid item xs={6} md={3}>
          <Card>
            <CardContent sx={{ textAlign: 'center' }}>
              <Typography variant="h4" sx={{ color: '#f59e0b' }}>
                {scheduledEvents}
              </Typography>
              <Typography variant="body2" sx={{ color: 'text.secondary' }}>
                Scheduled
              </Typography>
            </CardContent>
          </Card>
        </Grid>
        <Grid item xs={6} md={3}>
          <Card>
            <CardContent sx={{ textAlign: 'center' }}>
              <Typography variant="h4" sx={{ color: 'primary.main' }}>
                {events.reduce((sum, e) => sum + e.participantCount, 0)}
              </Typography>
              <Typography variant="body2" sx={{ color: 'text.secondary' }}>
                Total Participants
              </Typography>
            </CardContent>
          </Card>
        </Grid>
        <Grid item xs={6} md={3}>
          <Card>
            <CardContent sx={{ textAlign: 'center' }}>
              <Typography variant="h4" sx={{ color: '#94a3b8' }}>
                {events.length}
              </Typography>
              <Typography variant="body2" sx={{ color: 'text.secondary' }}>
                Total Events
              </Typography>
            </CardContent>
          </Card>
        </Grid>
      </Grid>

      <Card>
        <CardContent>
          {loading ? (
            <LinearProgress />
          ) : (
            <Box sx={{ height: 600, width: '100%' }}>
              <DataGrid
                rows={events}
                columns={columns}
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
          )}
        </CardContent>
      </Card>

      {/* Create Event Dialog */}
      <Dialog
        open={createDialogOpen}
        onClose={() => setCreateDialogOpen(false)}
        maxWidth="sm"
        fullWidth
      >
        <DialogTitle>Create World Event</DialogTitle>
        <DialogContent>
          <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2, mt: 2 }}>
            <TextField
              label="Event Name"
              value={newEvent.name}
              onChange={(e) => setNewEvent({ ...newEvent, name: e.target.value })}
              fullWidth
            />
            <TextField
              label="Description"
              value={newEvent.description}
              onChange={(e) => setNewEvent({ ...newEvent, description: e.target.value })}
              fullWidth
              multiline
              rows={3}
            />
            <FormControl fullWidth>
              <InputLabel>Event Type</InputLabel>
              <Select
                value={newEvent.eventType}
                label="Event Type"
                onChange={(e) => setNewEvent({ ...newEvent, eventType: e.target.value })}
              >
                {EVENT_TYPES.map((type) => (
                  <MenuItem key={type.value} value={type.value}>
                    {type.label}
                  </MenuItem>
                ))}
              </Select>
            </FormControl>
            <TextField
              label="Duration (hours)"
              type="number"
              value={newEvent.duration}
              onChange={(e) => setNewEvent({ ...newEvent, duration: parseInt(e.target.value) })}
              fullWidth
            />
            <Typography variant="subtitle2" sx={{ mt: 1 }}>
              Rewards
            </Typography>
            <Grid container spacing={2}>
              <Grid item xs={4}>
                <TextField
                  label="Gold"
                  type="number"
                  value={newEvent.goldReward}
                  onChange={(e) => setNewEvent({ ...newEvent, goldReward: parseInt(e.target.value) })}
                  fullWidth
                  size="small"
                />
              </Grid>
              <Grid item xs={4}>
                <TextField
                  label="XP"
                  type="number"
                  value={newEvent.xpReward}
                  onChange={(e) => setNewEvent({ ...newEvent, xpReward: parseInt(e.target.value) })}
                  fullWidth
                  size="small"
                />
              </Grid>
              <Grid item xs={4}>
                <TextField
                  label="Gems"
                  type="number"
                  value={newEvent.gemsReward}
                  onChange={(e) => setNewEvent({ ...newEvent, gemsReward: parseInt(e.target.value) })}
                  fullWidth
                  size="small"
                />
              </Grid>
            </Grid>
          </Box>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setCreateDialogOpen(false)}>Cancel</Button>
          <Button
            variant="contained"
            onClick={handleCreateEvent}
            disabled={!newEvent.name}
          >
            Create Event
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
}
