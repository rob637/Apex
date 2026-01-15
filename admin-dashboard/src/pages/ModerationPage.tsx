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
  IconButton,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Tabs,
  Tab,
} from '@mui/material';
import { DataGrid, GridColDef, GridRenderCellParams } from '@mui/x-data-grid';
import {
  Search as SearchIcon,
  Refresh as RefreshIcon,
  Check as ApproveIcon,
  Close as RejectIcon,
  Visibility as ViewIcon,
  Warning as WarningIcon,
  Report as ReportIcon,
  Gavel as BanIcon,
} from '@mui/icons-material';
import {
  collection,
  getDocs,
  doc,
  updateDoc,
  query,
  where,
  orderBy,
  limit,
} from 'firebase/firestore';
import { db } from '../config/firebase';

interface SuspiciousActivity {
  id: string;
  userId: string;
  userName: string;
  type: string;
  severity: 'low' | 'medium' | 'high' | 'critical';
  description: string;
  evidence: Record<string, unknown>;
  reviewed: boolean;
  action: string | null;
  createdAt: Date;
}

interface ChatReport {
  id: string;
  messageId: string;
  reporterId: string;
  reporterName: string;
  reportedUserId: string;
  reportedUserName: string;
  reason: string;
  messageContent: string;
  status: 'pending' | 'reviewed' | 'actioned';
  createdAt: Date;
}

export default function ModerationPage() {
  const [currentTab, setCurrentTab] = useState(0);
  const [suspiciousActivities, setSuspiciousActivities] = useState<SuspiciousActivity[]>([]);
  const [chatReports, setChatReports] = useState<ChatReport[]>([]);
  const [loading, setLoading] = useState(true);
  const [searchQuery, setSearchQuery] = useState('');
  const [selectedActivity, setSelectedActivity] = useState<SuspiciousActivity | null>(null);
  const [detailsDialogOpen, setDetailsDialogOpen] = useState(false);

  useEffect(() => {
    fetchModerationData();
  }, []);

  const fetchModerationData = async () => {
    setLoading(true);
    try {
      // Fetch suspicious activities
      const activitiesQuery = query(
        collection(db, 'suspicious_activities'),
        orderBy('createdAt', 'desc'),
        limit(500)
      );
      const activitiesSnapshot = await getDocs(activitiesQuery);
      const activities = activitiesSnapshot.docs.map((doc) => ({
        id: doc.id,
        userId: doc.data().userId || '',
        userName: doc.data().userName || 'Unknown',
        type: doc.data().type || 'unknown',
        severity: doc.data().severity || 'low',
        description: doc.data().description || '',
        evidence: doc.data().evidence || {},
        reviewed: doc.data().reviewed || false,
        action: doc.data().action || null,
        createdAt: doc.data().createdAt?.toDate() || new Date(),
      }));
      setSuspiciousActivities(activities);

      // Fetch chat reports
      const reportsQuery = query(
        collection(db, 'chat_reports'),
        orderBy('createdAt', 'desc'),
        limit(500)
      );
      const reportsSnapshot = await getDocs(reportsQuery);
      const reports = reportsSnapshot.docs.map((doc) => ({
        id: doc.id,
        messageId: doc.data().messageId || '',
        reporterId: doc.data().reporterId || '',
        reporterName: doc.data().reporterName || 'Unknown',
        reportedUserId: doc.data().reportedUserId || '',
        reportedUserName: doc.data().reportedUserName || 'Unknown',
        reason: doc.data().reason || '',
        messageContent: doc.data().messageContent || '',
        status: doc.data().status || 'pending',
        createdAt: doc.data().createdAt?.toDate() || new Date(),
      }));
      setChatReports(reports);
    } catch (error) {
      console.error('Error fetching moderation data:', error);
    } finally {
      setLoading(false);
    }
  };

  const handleReviewActivity = async (
    activityId: string,
    action: 'dismiss' | 'warn' | 'ban'
  ) => {
    try {
      await updateDoc(doc(db, 'suspicious_activities', activityId), {
        reviewed: true,
        action,
        reviewedAt: new Date(),
      });

      // If ban, update user
      if (action === 'ban') {
        const activity = suspiciousActivities.find((a) => a.id === activityId);
        if (activity) {
          await updateDoc(doc(db, 'users', activity.userId), {
            isBanned: true,
            bannedAt: new Date(),
            banReason: activity.description,
          });
        }
      }

      fetchModerationData();
    } catch (error) {
      console.error('Error reviewing activity:', error);
    }
  };

  const handleReviewReport = async (
    reportId: string,
    status: 'reviewed' | 'actioned'
  ) => {
    try {
      await updateDoc(doc(db, 'chat_reports', reportId), {
        status,
        reviewedAt: new Date(),
      });
      fetchModerationData();
    } catch (error) {
      console.error('Error reviewing report:', error);
    }
  };

  const getSeverityColor = (severity: string) => {
    switch (severity) {
      case 'critical':
        return '#ef4444';
      case 'high':
        return '#f59e0b';
      case 'medium':
        return '#eab308';
      case 'low':
        return '#22c55e';
      default:
        return '#94a3b8';
    }
  };

  const activityColumns: GridColDef[] = [
    {
      field: 'userName',
      headerName: 'User',
      width: 150,
    },
    {
      field: 'type',
      headerName: 'Type',
      width: 140,
      renderCell: (params: GridRenderCellParams) => (
        <Chip
          label={params.value?.replace(/_/g, ' ')}
          size="small"
          sx={{
            backgroundColor: 'rgba(99, 102, 241, 0.1)',
            color: 'primary.main',
          }}
        />
      ),
    },
    {
      field: 'severity',
      headerName: 'Severity',
      width: 100,
      renderCell: (params: GridRenderCellParams) => (
        <Chip
          label={params.value}
          size="small"
          sx={{
            backgroundColor: `${getSeverityColor(params.value)}20`,
            color: getSeverityColor(params.value),
            fontWeight: 600,
          }}
        />
      ),
    },
    {
      field: 'description',
      headerName: 'Description',
      flex: 1,
      minWidth: 200,
      renderCell: (params: GridRenderCellParams) => (
        <Typography variant="body2" sx={{ color: 'text.secondary' }} noWrap>
          {params.value}
        </Typography>
      ),
    },
    {
      field: 'reviewed',
      headerName: 'Status',
      width: 100,
      renderCell: (params: GridRenderCellParams) => (
        <Chip
          label={params.value ? 'Reviewed' : 'Pending'}
          size="small"
          color={params.value ? 'default' : 'warning'}
          variant="outlined"
        />
      ),
    },
    {
      field: 'createdAt',
      headerName: 'Date',
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
      renderCell: (params: GridRenderCellParams) =>
        !params.row.reviewed ? (
          <Box>
            <IconButton
              size="small"
              onClick={() => {
                setSelectedActivity(params.row);
                setDetailsDialogOpen(true);
              }}
              title="View Details"
            >
              <ViewIcon fontSize="small" />
            </IconButton>
            <IconButton
              size="small"
              onClick={() => handleReviewActivity(params.row.id, 'dismiss')}
              title="Dismiss"
              color="success"
            >
              <ApproveIcon fontSize="small" />
            </IconButton>
            <IconButton
              size="small"
              onClick={() => handleReviewActivity(params.row.id, 'warn')}
              title="Warn User"
              color="warning"
            >
              <WarningIcon fontSize="small" />
            </IconButton>
            <IconButton
              size="small"
              onClick={() => handleReviewActivity(params.row.id, 'ban')}
              title="Ban User"
              color="error"
            >
              <BanIcon fontSize="small" />
            </IconButton>
          </Box>
        ) : (
          <Chip label={params.row.action} size="small" />
        ),
    },
  ];

  const reportColumns: GridColDef[] = [
    {
      field: 'reporterName',
      headerName: 'Reporter',
      width: 130,
    },
    {
      field: 'reportedUserName',
      headerName: 'Reported User',
      width: 130,
    },
    {
      field: 'reason',
      headerName: 'Reason',
      width: 140,
      renderCell: (params: GridRenderCellParams) => (
        <Chip label={params.value} size="small" color="error" variant="outlined" />
      ),
    },
    {
      field: 'messageContent',
      headerName: 'Message',
      flex: 1,
      minWidth: 200,
      renderCell: (params: GridRenderCellParams) => (
        <Typography variant="body2" sx={{ color: 'text.secondary' }} noWrap>
          {params.value}
        </Typography>
      ),
    },
    {
      field: 'status',
      headerName: 'Status',
      width: 100,
      renderCell: (params: GridRenderCellParams) => (
        <Chip
          label={params.value}
          size="small"
          color={
            params.value === 'pending'
              ? 'warning'
              : params.value === 'actioned'
              ? 'error'
              : 'default'
          }
          variant="outlined"
        />
      ),
    },
    {
      field: 'createdAt',
      headerName: 'Date',
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
      width: 120,
      sortable: false,
      renderCell: (params: GridRenderCellParams) =>
        params.row.status === 'pending' ? (
          <Box>
            <IconButton
              size="small"
              onClick={() => handleReviewReport(params.row.id, 'reviewed')}
              title="Mark Reviewed"
              color="success"
            >
              <ApproveIcon fontSize="small" />
            </IconButton>
            <IconButton
              size="small"
              onClick={() => handleReviewReport(params.row.id, 'actioned')}
              title="Take Action"
              color="error"
            >
              <BanIcon fontSize="small" />
            </IconButton>
          </Box>
        ) : null,
    },
  ];

  const pendingActivities = suspiciousActivities.filter((a) => !a.reviewed);
  const pendingReports = chatReports.filter((r) => r.status === 'pending');

  const filteredActivities = suspiciousActivities.filter(
    (a) =>
      a.userName.toLowerCase().includes(searchQuery.toLowerCase()) ||
      a.type.toLowerCase().includes(searchQuery.toLowerCase()) ||
      a.description.toLowerCase().includes(searchQuery.toLowerCase())
  );

  const filteredReports = chatReports.filter(
    (r) =>
      r.reporterName.toLowerCase().includes(searchQuery.toLowerCase()) ||
      r.reportedUserName.toLowerCase().includes(searchQuery.toLowerCase()) ||
      r.reason.toLowerCase().includes(searchQuery.toLowerCase())
  );

  return (
    <Box className="animate-fadeIn">
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <Typography variant="h4" sx={{ fontWeight: 700 }}>
          Moderation
        </Typography>
        <Button
          variant="outlined"
          startIcon={<RefreshIcon />}
          onClick={fetchModerationData}
        >
          Refresh
        </Button>
      </Box>

      {/* Stats */}
      <Grid container spacing={2} sx={{ mb: 3 }}>
        <Grid item xs={6} md={3}>
          <Card>
            <CardContent sx={{ textAlign: 'center' }}>
              <Typography variant="h4" sx={{ color: '#ef4444' }}>
                {pendingActivities.length}
              </Typography>
              <Typography variant="body2" sx={{ color: 'text.secondary' }}>
                Pending Reviews
              </Typography>
            </CardContent>
          </Card>
        </Grid>
        <Grid item xs={6} md={3}>
          <Card>
            <CardContent sx={{ textAlign: 'center' }}>
              <Typography variant="h4" sx={{ color: '#f59e0b' }}>
                {pendingReports.length}
              </Typography>
              <Typography variant="body2" sx={{ color: 'text.secondary' }}>
                Chat Reports
              </Typography>
            </CardContent>
          </Card>
        </Grid>
        <Grid item xs={6} md={3}>
          <Card>
            <CardContent sx={{ textAlign: 'center' }}>
              <Typography variant="h4" sx={{ color: '#8b5cf6' }}>
                {suspiciousActivities.filter((a) => a.severity === 'critical').length}
              </Typography>
              <Typography variant="body2" sx={{ color: 'text.secondary' }}>
                Critical Alerts
              </Typography>
            </CardContent>
          </Card>
        </Grid>
        <Grid item xs={6} md={3}>
          <Card>
            <CardContent sx={{ textAlign: 'center' }}>
              <Typography variant="h4" sx={{ color: '#22c55e' }}>
                {suspiciousActivities.filter((a) => a.action === 'ban').length}
              </Typography>
              <Typography variant="body2" sx={{ color: 'text.secondary' }}>
                Bans Today
              </Typography>
            </CardContent>
          </Card>
        </Grid>
      </Grid>

      <Card>
        <CardContent>
          <Tabs
            value={currentTab}
            onChange={(_, v) => setCurrentTab(v)}
            sx={{ mb: 2 }}
          >
            <Tab
              label={
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                  <WarningIcon fontSize="small" />
                  Suspicious Activity
                  {pendingActivities.length > 0 && (
                    <Chip label={pendingActivities.length} size="small" color="error" />
                  )}
                </Box>
              }
            />
            <Tab
              label={
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                  <ReportIcon fontSize="small" />
                  Chat Reports
                  {pendingReports.length > 0 && (
                    <Chip label={pendingReports.length} size="small" color="warning" />
                  )}
                </Box>
              }
            />
          </Tabs>

          <TextField
            placeholder={`Search ${currentTab === 0 ? 'activities' : 'reports'}...`}
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

          <Box sx={{ height: 500, width: '100%' }}>
            <DataGrid
              rows={currentTab === 0 ? filteredActivities : filteredReports}
              columns={currentTab === 0 ? activityColumns : reportColumns}
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

      {/* Activity Details Dialog */}
      <Dialog
        open={detailsDialogOpen}
        onClose={() => setDetailsDialogOpen(false)}
        maxWidth="md"
        fullWidth
      >
        {selectedActivity && (
          <>
            <DialogTitle>
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
                <WarningIcon sx={{ color: getSeverityColor(selectedActivity.severity) }} />
                Suspicious Activity Details
              </Box>
            </DialogTitle>
            <DialogContent>
              <Grid container spacing={2} sx={{ mt: 1 }}>
                <Grid item xs={6}>
                  <Typography variant="body2" sx={{ color: 'text.secondary' }}>
                    User
                  </Typography>
                  <Typography variant="body1">{selectedActivity.userName}</Typography>
                </Grid>
                <Grid item xs={6}>
                  <Typography variant="body2" sx={{ color: 'text.secondary' }}>
                    User ID
                  </Typography>
                  <Typography variant="body1" sx={{ fontFamily: 'monospace' }}>
                    {selectedActivity.userId}
                  </Typography>
                </Grid>
                <Grid item xs={6}>
                  <Typography variant="body2" sx={{ color: 'text.secondary' }}>
                    Type
                  </Typography>
                  <Chip label={selectedActivity.type.replace(/_/g, ' ')} size="small" />
                </Grid>
                <Grid item xs={6}>
                  <Typography variant="body2" sx={{ color: 'text.secondary' }}>
                    Severity
                  </Typography>
                  <Chip
                    label={selectedActivity.severity}
                    size="small"
                    sx={{
                      backgroundColor: `${getSeverityColor(selectedActivity.severity)}20`,
                      color: getSeverityColor(selectedActivity.severity),
                    }}
                  />
                </Grid>
                <Grid item xs={12}>
                  <Typography variant="body2" sx={{ color: 'text.secondary' }}>
                    Description
                  </Typography>
                  <Typography variant="body1">{selectedActivity.description}</Typography>
                </Grid>
                <Grid item xs={12}>
                  <Typography variant="body2" sx={{ color: 'text.secondary' }}>
                    Evidence
                  </Typography>
                  <Card variant="outlined" sx={{ p: 2, mt: 1 }}>
                    <pre style={{ margin: 0, overflow: 'auto', fontSize: '0.85rem' }}>
                      {JSON.stringify(selectedActivity.evidence, null, 2)}
                    </pre>
                  </Card>
                </Grid>
              </Grid>
            </DialogContent>
            <DialogActions>
              <Button
                startIcon={<ApproveIcon />}
                onClick={() => {
                  handleReviewActivity(selectedActivity.id, 'dismiss');
                  setDetailsDialogOpen(false);
                }}
                color="success"
              >
                Dismiss
              </Button>
              <Button
                startIcon={<WarningIcon />}
                onClick={() => {
                  handleReviewActivity(selectedActivity.id, 'warn');
                  setDetailsDialogOpen(false);
                }}
                color="warning"
              >
                Warn User
              </Button>
              <Button
                startIcon={<BanIcon />}
                onClick={() => {
                  handleReviewActivity(selectedActivity.id, 'ban');
                  setDetailsDialogOpen(false);
                }}
                color="error"
                variant="contained"
              >
                Ban User
              </Button>
            </DialogActions>
          </>
        )}
      </Dialog>
    </Box>
  );
}
