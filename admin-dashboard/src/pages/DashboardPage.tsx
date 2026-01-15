import { useState, useEffect } from 'react';
import {
  Box,
  Grid,
  Card,
  CardContent,
  Typography,
  Chip,
  LinearProgress,
} from '@mui/material';
import {
  People as PeopleIcon,
  Map as MapIcon,
  Groups as GroupsIcon,
  TrendingUp as TrendingUpIcon,
  AttachMoney as MoneyIcon,
  EmojiEvents as TrophyIcon,
} from '@mui/icons-material';
import {
  Chart as ChartJS,
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  BarElement,
  ArcElement,
  Title,
  Tooltip,
  Legend,
  Filler,
} from 'chart.js';
import { Line, Doughnut } from 'react-chartjs-2';
import { collection, getDocs, query, orderBy, limit } from 'firebase/firestore';
import { db } from '../config/firebase';

// Register Chart.js components
ChartJS.register(
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  BarElement,
  ArcElement,
  Title,
  Tooltip,
  Legend,
  Filler
);

interface StatCardProps {
  title: string;
  value: string | number;
  change?: string;
  changeType?: 'positive' | 'negative' | 'neutral';
  icon: React.ReactNode;
  color: string;
}

function StatCard({ title, value, change, changeType, icon, color }: StatCardProps) {
  return (
    <Card sx={{ height: '100%' }}>
      <CardContent>
        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
          <Box>
            <Typography variant="body2" sx={{ color: 'text.secondary', mb: 1 }}>
              {title}
            </Typography>
            <Typography variant="h4" sx={{ fontWeight: 700, mb: 1 }}>
              {value}
            </Typography>
            {change && (
              <Chip
                label={change}
                size="small"
                sx={{
                  backgroundColor:
                    changeType === 'positive'
                      ? 'rgba(34, 197, 94, 0.1)'
                      : changeType === 'negative'
                      ? 'rgba(239, 68, 68, 0.1)'
                      : 'rgba(148, 163, 184, 0.1)',
                  color:
                    changeType === 'positive'
                      ? 'success.main'
                      : changeType === 'negative'
                      ? 'error.main'
                      : 'text.secondary',
                }}
              />
            )}
          </Box>
          <Box
            sx={{
              width: 48,
              height: 48,
              borderRadius: 2,
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              backgroundColor: `${color}20`,
              color: color,
            }}
          >
            {icon}
          </Box>
        </Box>
      </CardContent>
    </Card>
  );
}

export default function DashboardPage() {
  const [loading, setLoading] = useState(true);
  const [stats, setStats] = useState({
    totalPlayers: 0,
    activePlayers: 0,
    totalTerritories: 0,
    totalAlliances: 0,
    revenue: 0,
    battlesToday: 0,
  });
  const [recentActivity, setRecentActivity] = useState<Array<{
    id: string;
    type: string;
    message: string;
    time: string;
  }>>([]);

  useEffect(() => {
    fetchDashboardData();
  }, []);

  const fetchDashboardData = async () => {
    try {
      // Fetch users count
      const usersSnapshot = await getDocs(collection(db, 'users'));
      const totalPlayers = usersSnapshot.size;

      // Fetch territories count
      const territoriesSnapshot = await getDocs(collection(db, 'territories'));
      const totalTerritories = territoriesSnapshot.size;

      // Fetch alliances count
      const alliancesSnapshot = await getDocs(collection(db, 'alliances'));
      const totalAlliances = alliancesSnapshot.size;

      // Fetch recent activities
      const activitiesQuery = query(
        collection(db, 'social_activities'),
        orderBy('createdAt', 'desc'),
        limit(5)
      );
      const activitiesSnapshot = await getDocs(activitiesQuery);
      const activities = activitiesSnapshot.docs.map((doc) => ({
        id: doc.id,
        type: doc.data().activityType,
        message: doc.data().message || 'Activity recorded',
        time: formatTime(doc.data().createdAt?.toDate()),
      }));

      setStats({
        totalPlayers,
        activePlayers: Math.floor(totalPlayers * 0.3), // Simulated
        totalTerritories,
        totalAlliances,
        revenue: 12847, // Simulated
        battlesToday: 156, // Simulated
      });

      setRecentActivity(activities);
    } catch (error) {
      console.error('Error fetching dashboard data:', error);
    } finally {
      setLoading(false);
    }
  };

  const formatTime = (date: Date | undefined): string => {
    if (!date) return 'Recently';
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffMins = Math.floor(diffMs / 60000);
    if (diffMins < 1) return 'Just now';
    if (diffMins < 60) return `${diffMins}m ago`;
    const diffHours = Math.floor(diffMins / 60);
    if (diffHours < 24) return `${diffHours}h ago`;
    return `${Math.floor(diffHours / 24)}d ago`;
  };

  // Chart data
  const playerGrowthData = {
    labels: ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'],
    datasets: [
      {
        label: 'Daily Active Users',
        data: [1200, 1350, 1280, 1420, 1550, 1680, 1850],
        borderColor: '#6366f1',
        backgroundColor: 'rgba(99, 102, 241, 0.1)',
        fill: true,
        tension: 0.4,
      },
      {
        label: 'New Signups',
        data: [150, 180, 165, 195, 210, 240, 280],
        borderColor: '#22c55e',
        backgroundColor: 'rgba(34, 197, 94, 0.1)',
        fill: true,
        tension: 0.4,
      },
    ],
  };

  const playerDistributionData = {
    labels: ['Casual', 'Regular', 'Hardcore', 'Whale'],
    datasets: [
      {
        data: [45, 30, 20, 5],
        backgroundColor: ['#3b82f6', '#6366f1', '#8b5cf6', '#f59e0b'],
        borderWidth: 0,
      },
    ],
  };

  const chartOptions = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: {
        position: 'bottom' as const,
        labels: {
          color: '#94a3b8',
        },
      },
    },
    scales: {
      x: {
        grid: {
          color: 'rgba(148, 163, 184, 0.1)',
        },
        ticks: {
          color: '#94a3b8',
        },
      },
      y: {
        grid: {
          color: 'rgba(148, 163, 184, 0.1)',
        },
        ticks: {
          color: '#94a3b8',
        },
      },
    },
  };

  if (loading) {
    return (
      <Box sx={{ width: '100%' }}>
        <LinearProgress />
      </Box>
    );
  }

  return (
    <Box className="animate-fadeIn">
      <Typography variant="h4" sx={{ fontWeight: 700, mb: 3 }}>
        Dashboard Overview
      </Typography>

      {/* Stats Grid */}
      <Grid container spacing={3} sx={{ mb: 4 }}>
        <Grid item xs={12} sm={6} md={4} lg={2}>
          <StatCard
            title="Total Players"
            value={stats.totalPlayers.toLocaleString()}
            change="+12.5%"
            changeType="positive"
            icon={<PeopleIcon />}
            color="#6366f1"
          />
        </Grid>
        <Grid item xs={12} sm={6} md={4} lg={2}>
          <StatCard
            title="Active Now"
            value={stats.activePlayers.toLocaleString()}
            change="+8.2%"
            changeType="positive"
            icon={<TrendingUpIcon />}
            color="#22c55e"
          />
        </Grid>
        <Grid item xs={12} sm={6} md={4} lg={2}>
          <StatCard
            title="Territories"
            value={stats.totalTerritories.toLocaleString()}
            change="+3 today"
            changeType="neutral"
            icon={<MapIcon />}
            color="#3b82f6"
          />
        </Grid>
        <Grid item xs={12} sm={6} md={4} lg={2}>
          <StatCard
            title="Alliances"
            value={stats.totalAlliances.toLocaleString()}
            change="+2 today"
            changeType="positive"
            icon={<GroupsIcon />}
            color="#8b5cf6"
          />
        </Grid>
        <Grid item xs={12} sm={6} md={4} lg={2}>
          <StatCard
            title="Revenue (24h)"
            value={`$${stats.revenue.toLocaleString()}`}
            change="+15.3%"
            changeType="positive"
            icon={<MoneyIcon />}
            color="#f59e0b"
          />
        </Grid>
        <Grid item xs={12} sm={6} md={4} lg={2}>
          <StatCard
            title="Battles Today"
            value={stats.battlesToday.toLocaleString()}
            change="+24"
            changeType="positive"
            icon={<TrophyIcon />}
            color="#ef4444"
          />
        </Grid>
      </Grid>

      {/* Charts */}
      <Grid container spacing={3}>
        <Grid item xs={12} lg={8}>
          <Card sx={{ height: 400 }}>
            <CardContent sx={{ height: '100%' }}>
              <Typography variant="h6" sx={{ mb: 2 }}>
                Player Activity (Last 7 Days)
              </Typography>
              <Box sx={{ height: 'calc(100% - 40px)' }}>
                <Line data={playerGrowthData} options={chartOptions} />
              </Box>
            </CardContent>
          </Card>
        </Grid>
        <Grid item xs={12} lg={4}>
          <Card sx={{ height: 400 }}>
            <CardContent sx={{ height: '100%' }}>
              <Typography variant="h6" sx={{ mb: 2 }}>
                Player Distribution
              </Typography>
              <Box sx={{ height: 'calc(100% - 40px)', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
                <Doughnut
                  data={playerDistributionData}
                  options={{
                    ...chartOptions,
                    cutout: '60%',
                  }}
                />
              </Box>
            </CardContent>
          </Card>
        </Grid>
      </Grid>

      {/* Recent Activity */}
      <Card sx={{ mt: 3 }}>
        <CardContent>
          <Typography variant="h6" sx={{ mb: 2 }}>
            Recent Activity
          </Typography>
          {recentActivity.length > 0 ? (
            recentActivity.map((activity) => (
              <Box
                key={activity.id}
                sx={{
                  display: 'flex',
                  justifyContent: 'space-between',
                  alignItems: 'center',
                  py: 1.5,
                  borderBottom: '1px solid rgba(148, 163, 184, 0.1)',
                  '&:last-child': { borderBottom: 'none' },
                }}
              >
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
                  <Chip
                    label={activity.type}
                    size="small"
                    sx={{ minWidth: 80 }}
                  />
                  <Typography variant="body2">{activity.message}</Typography>
                </Box>
                <Typography variant="caption" sx={{ color: 'text.secondary' }}>
                  {activity.time}
                </Typography>
              </Box>
            ))
          ) : (
            <Typography variant="body2" sx={{ color: 'text.secondary', textAlign: 'center', py: 4 }}>
              No recent activity to display
            </Typography>
          )}
        </CardContent>
      </Card>
    </Box>
  );
}
