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
  Alert,
  CircularProgress,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  IconButton,
  Tabs,
  Tab,
  Slider,
  Select,
  MenuItem,
  FormControl,
  InputLabel,
} from '@mui/material';
import { DataGrid, GridColDef, GridRenderCellParams } from '@mui/x-data-grid';
import {
  Search as SearchIcon,
  Refresh as RefreshIcon,
  Map as MapIcon,
  AddLocation as SeedIcon,
  Close as CloseIcon,
  Edit as EditIcon,
  Save as SaveIcon,
  TableChart as TableIcon,
  Public as GlobeIcon,
} from '@mui/icons-material';
import {
  collection,
  getDocs,
  query,
  orderBy,
  limit,
  doc,
  updateDoc,
} from 'firebase/firestore';
import { getFunctions, httpsCallable } from 'firebase/functions';
import { db, app } from '../config/firebase';
import { MapContainer, TileLayer, Marker, Popup, useMap } from 'react-leaflet';
import L from 'leaflet';
import 'leaflet/dist/leaflet.css';

// Fix for default marker icons in React-Leaflet
delete (L.Icon.Default.prototype as any)._getIconUrl;
L.Icon.Default.mergeOptions({
  iconRetinaUrl: 'https://cdnjs.cloudflare.com/ajax/libs/leaflet/1.7.1/images/marker-icon-2x.png',
  iconUrl: 'https://cdnjs.cloudflare.com/ajax/libs/leaflet/1.7.1/images/marker-icon.png',
  shadowUrl: 'https://cdnjs.cloudflare.com/ajax/libs/leaflet/1.7.1/images/marker-shadow.png',
});

// Custom marker icons by level
const createLevelIcon = (level: number, resourceType: string) => {
  const color = resourceType === 'gold' ? '#f59e0b' : 
                resourceType === 'wood' ? '#22c55e' : 
                resourceType === 'stone' ? '#94a3b8' : '#8b5cf6';
  const size = 20 + level * 4;
  
  return L.divIcon({
    className: 'custom-marker',
    html: `<div style="
      background: ${color};
      border: 3px solid white;
      border-radius: 50%;
      width: ${size}px;
      height: ${size}px;
      display: flex;
      align-items: center;
      justify-content: center;
      color: white;
      font-weight: bold;
      font-size: ${10 + level}px;
      box-shadow: 0 2px 8px rgba(0,0,0,0.3);
    ">${level}</div>`,
    iconSize: [size, size],
    iconAnchor: [size / 2, size / 2],
  });
};

// Component to fit map bounds
function FitBounds({ territories }: { territories: Territory[] }) {
  const map = useMap();
  
  useEffect(() => {
    if (territories.length > 0) {
      const bounds = L.latLngBounds(
        territories.map(t => [t.latitude, t.longitude] as [number, number])
      );
      map.fitBounds(bounds, { padding: [50, 50] });
    }
  }, [territories, map]);
  
  return null;
}

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
  const [seeding, setSeeding] = useState(false);
  const [seedResult, setSeedResult] = useState<{ type: 'success' | 'error'; message: string } | null>(null);
  const [viewMode, setViewMode] = useState<'table' | 'map'>('table');
  const [selectedTerritory, setSelectedTerritory] = useState<Territory | null>(null);
  const [editMode, setEditMode] = useState(false);
  const [editedTerritory, setEditedTerritory] = useState<Territory | null>(null);
  const [saving, setSaving] = useState(false);
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

  const seedTestData = async () => {
    setSeeding(true);
    setSeedResult(null);
    try {
      const functions = getFunctions(app, 'us-central1');
      const seedTerritories = httpsCallable(functions, 'seedTerritories');
      const result = await seedTerritories();
      const data = result.data as { success: boolean; message: string; count?: number };
      
      if (data.success) {
        setSeedResult({ 
          type: 'success', 
          message: `${data.message} (${data.count} territories created)` 
        });
        // Refresh the list after seeding
        await fetchTerritories();
      } else {
        setSeedResult({ type: 'error', message: data.message });
      }
    } catch (error: any) {
      console.error('Error seeding territories:', error);
      setSeedResult({ 
        type: 'error', 
        message: error.message || 'Failed to seed territories' 
      });
    } finally {
      setSeeding(false);
    }
  };

  const handleTerritoryClick = (territory: Territory) => {
    setSelectedTerritory(territory);
    setEditedTerritory({ ...territory });
    setEditMode(false);
  };

  const handleCloseDialog = () => {
    setSelectedTerritory(null);
    setEditedTerritory(null);
    setEditMode(false);
  };

  const handleSaveTerritory = async () => {
    if (!editedTerritory || !selectedTerritory) return;
    
    setSaving(true);
    try {
      const territoryRef = doc(db, 'territories', selectedTerritory.id);
      await updateDoc(territoryRef, {
        name: editedTerritory.name,
        level: editedTerritory.level,
        resourceType: editedTerritory.resourceType,
        resourceOutput: editedTerritory.resourceOutput,
      });
      
      // Update local state
      setTerritories(prev => 
        prev.map(t => t.id === selectedTerritory.id ? editedTerritory : t)
      );
      setSelectedTerritory(editedTerritory);
      setEditMode(false);
      setSeedResult({ type: 'success', message: `Territory "${editedTerritory.name}" updated successfully` });
    } catch (error: any) {
      console.error('Error updating territory:', error);
      setSeedResult({ type: 'error', message: error.message || 'Failed to update territory' });
    } finally {
      setSaving(false);
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
        <Box sx={{ display: 'flex', gap: 1 }}>
          <Button
            variant="contained"
            color="secondary"
            startIcon={seeding ? <CircularProgress size={20} color="inherit" /> : <SeedIcon />}
            onClick={seedTestData}
            disabled={seeding}
          >
            {seeding ? 'Seeding...' : 'Seed Test Data'}
          </Button>
          <Button
            variant="outlined"
            startIcon={<RefreshIcon />}
            onClick={fetchTerritories}
          >
            Refresh
          </Button>
        </Box>
      </Box>

      {/* Seed Result Alert */}
      {seedResult && (
        <Alert 
          severity={seedResult.type} 
          onClose={() => setSeedResult(null)}
          sx={{ mb: 3 }}
        >
          {seedResult.message}
        </Alert>
      )}

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
          {/* View Mode Tabs */}
          <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
            <Tabs value={viewMode} onChange={(_, v) => setViewMode(v)}>
              <Tab icon={<TableIcon />} label="Table" value="table" />
              <Tab icon={<GlobeIcon />} label="Map" value="map" />
            </Tabs>
          </Box>

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

          {/* Table View */}
          {viewMode === 'table' && (
            <Box sx={{ height: 600, width: '100%' }}>
              <DataGrid
                rows={filteredTerritories}
                columns={columns}
                loading={loading}
                pageSizeOptions={[25, 50, 100]}
                initialState={{
                  pagination: { paginationModel: { pageSize: 25 } },
                }}
                onRowClick={(params) => handleTerritoryClick(params.row as Territory)}
                sx={{
                  border: 'none',
                  cursor: 'pointer',
                  '& .MuiDataGrid-cell': {
                    borderColor: 'rgba(148, 163, 184, 0.1)',
                  },
                  '& .MuiDataGrid-columnHeaders': {
                    backgroundColor: 'rgba(30, 41, 59, 0.5)',
                    borderColor: 'rgba(148, 163, 184, 0.1)',
                  },
                  '& .MuiDataGrid-row:hover': {
                    backgroundColor: 'rgba(99, 102, 241, 0.1)',
                  },
                }}
              />
            </Box>
          )}

          {/* Map View */}
          {viewMode === 'map' && (
            <Box sx={{ height: 600, width: '100%', borderRadius: 2, overflow: 'hidden' }}>
              {loading ? (
                <Box sx={{ display: 'flex', justifyContent: 'center', alignItems: 'center', height: '100%' }}>
                  <CircularProgress />
                </Box>
              ) : (
                <MapContainer
                  center={[38.9, -77.26]}
                  zoom={12}
                  style={{ height: '100%', width: '100%' }}
                >
                  <TileLayer
                    attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>'
                    url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
                  />
                  <FitBounds territories={filteredTerritories} />
                  {filteredTerritories.map((territory) => (
                    <Marker
                      key={territory.id}
                      position={[territory.latitude, territory.longitude]}
                      icon={createLevelIcon(territory.level, territory.resourceType)}
                      eventHandlers={{
                        click: () => handleTerritoryClick(territory),
                      }}
                    >
                      <Popup>
                        <Box sx={{ minWidth: 150 }}>
                          <Typography variant="subtitle2" sx={{ fontWeight: 'bold' }}>
                            {territory.name}
                          </Typography>
                          <Typography variant="body2">Level: {territory.level}</Typography>
                          <Typography variant="body2">Resource: {territory.resourceType}</Typography>
                          <Typography variant="body2">Status: {territory.controllerName}</Typography>
                          <Button
                            size="small"
                            onClick={() => handleTerritoryClick(territory)}
                            sx={{ mt: 1 }}
                          >
                            View Details
                          </Button>
                        </Box>
                      </Popup>
                    </Marker>
                  ))}
                </MapContainer>
              )}
            </Box>
          )}
        </CardContent>
      </Card>

      {/* Territory Detail Dialog */}
      <Dialog 
        open={!!selectedTerritory} 
        onClose={handleCloseDialog}
        maxWidth="sm"
        fullWidth
      >
        {selectedTerritory && editedTerritory && (
          <>
            <DialogTitle sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                <MapIcon sx={{ color: 'primary.main' }} />
                {editMode ? 'Edit Territory' : selectedTerritory.name}
              </Box>
              <Box>
                {!editMode && (
                  <IconButton onClick={() => setEditMode(true)} color="primary">
                    <EditIcon />
                  </IconButton>
                )}
                <IconButton onClick={handleCloseDialog}>
                  <CloseIcon />
                </IconButton>
              </Box>
            </DialogTitle>
            <DialogContent dividers>
              {editMode ? (
                <Box sx={{ display: 'flex', flexDirection: 'column', gap: 3, pt: 1 }}>
                  <TextField
                    label="Name"
                    value={editedTerritory.name}
                    onChange={(e) => setEditedTerritory({ ...editedTerritory, name: e.target.value })}
                    fullWidth
                  />
                  <Box>
                    <Typography gutterBottom>Level: {editedTerritory.level}</Typography>
                    <Slider
                      value={editedTerritory.level}
                      onChange={(_, v) => setEditedTerritory({ ...editedTerritory, level: v as number })}
                      min={1}
                      max={10}
                      marks
                      valueLabelDisplay="auto"
                    />
                  </Box>
                  <FormControl fullWidth>
                    <InputLabel>Resource Type</InputLabel>
                    <Select
                      value={editedTerritory.resourceType}
                      label="Resource Type"
                      onChange={(e) => setEditedTerritory({ ...editedTerritory, resourceType: e.target.value })}
                    >
                      <MenuItem value="gold">Gold</MenuItem>
                      <MenuItem value="wood">Wood</MenuItem>
                      <MenuItem value="stone">Stone</MenuItem>
                      <MenuItem value="crystal">Crystal</MenuItem>
                    </Select>
                  </FormControl>
                  <TextField
                    label="Resource Output per Hour"
                    type="number"
                    value={editedTerritory.resourceOutput}
                    onChange={(e) => setEditedTerritory({ ...editedTerritory, resourceOutput: parseInt(e.target.value) || 0 })}
                    fullWidth
                  />
                </Box>
              ) : (
                <Grid container spacing={2}>
                  <Grid item xs={6}>
                    <Typography variant="body2" color="text.secondary">Level</Typography>
                    <Typography variant="h6">{selectedTerritory.level}</Typography>
                  </Grid>
                  <Grid item xs={6}>
                    <Typography variant="body2" color="text.secondary">Status</Typography>
                    <Typography variant="h6">{selectedTerritory.controllerName}</Typography>
                  </Grid>
                  <Grid item xs={6}>
                    <Typography variant="body2" color="text.secondary">Resource</Typography>
                    <Chip 
                      label={selectedTerritory.resourceType} 
                      size="small"
                      sx={{ 
                        mt: 0.5,
                        backgroundColor: selectedTerritory.resourceType === 'gold' ? 'rgba(245, 158, 11, 0.2)' : 'rgba(34, 197, 94, 0.2)',
                        color: selectedTerritory.resourceType === 'gold' ? '#f59e0b' : '#22c55e',
                      }}
                    />
                  </Grid>
                  <Grid item xs={6}>
                    <Typography variant="body2" color="text.secondary">Output/hr</Typography>
                    <Typography variant="h6">{selectedTerritory.resourceOutput}</Typography>
                  </Grid>
                  <Grid item xs={6}>
                    <Typography variant="body2" color="text.secondary">Total Battles</Typography>
                    <Typography variant="h6">{selectedTerritory.totalBattles}</Typography>
                  </Grid>
                  <Grid item xs={6}>
                    <Typography variant="body2" color="text.secondary">Last Conquered</Typography>
                    <Typography variant="h6">
                      {selectedTerritory.lastConquered?.toLocaleDateString() || 'Never'}
                    </Typography>
                  </Grid>
                  <Grid item xs={12}>
                    <Typography variant="body2" color="text.secondary">Location</Typography>
                    <Typography variant="body1" sx={{ fontFamily: 'monospace' }}>
                      {selectedTerritory.latitude.toFixed(6)}, {selectedTerritory.longitude.toFixed(6)}
                    </Typography>
                    <Typography variant="body2" color="text.secondary" sx={{ mt: 0.5 }}>
                      Geohash: {selectedTerritory.geohash}
                    </Typography>
                  </Grid>
                </Grid>
              )}
            </DialogContent>
            <DialogActions>
              {editMode ? (
                <>
                  <Button onClick={() => {
                    setEditMode(false);
                    setEditedTerritory({ ...selectedTerritory });
                  }}>
                    Cancel
                  </Button>
                  <Button
                    variant="contained"
                    startIcon={saving ? <CircularProgress size={20} color="inherit" /> : <SaveIcon />}
                    onClick={handleSaveTerritory}
                    disabled={saving}
                  >
                    {saving ? 'Saving...' : 'Save Changes'}
                  </Button>
                </>
              ) : (
                <Button onClick={handleCloseDialog}>Close</Button>
              )}
            </DialogActions>
          </>
        )}
      </Dialog>
    </Box>
  );
}
