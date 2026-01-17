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
import { collection, getDocs, query, orderBy, limit, where, Timestamp } from 'firebase/firestore';
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
  const [chartData, setChartData] = useState<{
    labels: string[];
    dau: number[];
    newUsers: number[];
  }>({
    labels: ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'],
    dau: [0, 0, 0, 0, 0, 0, 0],
    newUsers: [0, 0, 0, 0, 0, 0, 0],
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
      // Get date boundaries
      const now = new Date();
      const todayStart = new Date(now.getFullYear(), now.getMonth(), now.getDate());
      const last24Hours = new Date(now.getTime() - 24 * 60 * 60 * 1000);

      // Fetch users count
      const usersSnapshot = await getDocs(collection(db, 'users'));
      const totalPlayers = usersSnapshot.size;

      // Count active players (last 24 hours)
      let activePlayers = 0;
      usersSnapshot.docs.forEach((doc) => {
        const lastActive = doc.data().lastActiveAt?.toDate();
        if (lastActive && lastActive > last24Hours) {
          activePlayers++;
        }
      });

      // Fetch territories count
      const territoriesSnapshot = await getDocs(collection(db, 'territories'));
      const totalTerritories = territoriesSnapshot.size;

      // Fetch alliances count
      const alliancesSnapshot = await getDocs(collection(db, 'alliances'));
      const totalAlliances = alliancesSnapshot.size;

      // Fetch battles today from combat_logs
      let battlesToday = 0;
      try {
        const battlesQuery = query(
          collection(db, 'combat_logs'),
          where('timestamp', '>=', Timestamp.fromDate(todayStart))
        );
        const battlesSnapshot = await getDocs(battlesQuery);
        battlesToday = battlesSnapshot.size;
      } catch {
        // Collection may not exist yet
        console.log('No combat_logs collection yet');
      }

      // Fetch 24h revenue from purchases
      let revenue = 0;
      try {
        const purchasesQuery = query(
          collection(db, 'purchases'),
          where('purchasedAt', '>=', Timestamp.fromDate(last24Hours)),
          where('status', '==', 'completed')
        );
        const purchasesSnapshot = await getDocs(purchasesQuery);
        purchasesSnapshot.docs.forEach((doc) => {
          revenue += doc.data().priceUSD || 0;
        });
      } catch {
        // Collection may not exist yet
        console.log('No purchases collection yet');
      }

      // Fetch daily metrics for charts (last 7 days)
      const dailyMetricsData: { date: string; dau: number; newUsers: number }[] = [];
      try {
        const metricsQuery = query(
          collection(db, 'daily_metrics'),
          orderBy('date', 'desc'),
          limit(7)
        );
        const metricsSnapshot = await getDocs(metricsQuery);
        metricsSnapshot.docs.forEach((doc) => {
          dailyMetricsData.push({
            date: doc.data().date,
            dau: doc.data().dau || 0,
            newUsers: doc.data().newUsers || 0,
          });
        });
      } catch {
        console.log('No daily_metrics collection yet');
      }

      // Update chart data if we have metrics
      if (dailyMetricsData.length > 0) {
        const sortedMetrics = dailyMetricsData.reverse();
        setChartData({
          labels: sortedMetrics.map((m) => {
            const d = new Date(m.date);
            return d.toLocaleDateString('en-US', { weekday: 'short' });
          }),
          dau: sortedMetrics.map((m) => m.dau),
          newUsers: sortedMetrics.map((m) => m.newUsers),
        });
      }

      // Fetch recent activities
      const activitiesQuery = query(
        collection(db, 'social_activities'),
        orderBy('createdAt', 'desc'),
        limit(5)
      );
      const activitiesSnapshot = await getDocs(activitiesQuery);
      const activities = activitiesSnapshot.docs.map((doc) => ({
        id: doc.id,
        type: doc.data().activityType || 'activity',
        message: doc.data().message || 'Activity recorded',
        time: formatTime(doc.data().createdAt?.toDate()),
      }));

      setStats({
        totalPlayers,
        activePlayers,
        totalTerritories,
        totalAlliances,
        revenue,
        battlesToday,
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

  // Chart data - uses real data from state
  const playerGrowthData = {
    labels: chartData.labels,
    datasets: [
      {
        label: 'Daily Active Users',
        data: chartData.dau,
        borderColor: '#6366f1',
        backgroundColor: 'rgba(99, 102, 241, 0.1)',
        fill: true,
        tension: 0.4,
      },
      {
        label: 'New Signups',
        data: chartData.newUsers,
        borderColor: '#22c55e',
        backgroundColor: 'rgba(34, 197, 94, 0.1)',
        fill: true,
        tension: 0.4,
      },
    ],
  };

  const playerDistributionData = {
    labels: stats.totalPlayers > 0 ? ['Active', 'Inactive'] : ['No Data'],
    datasets: [
      {
        data: stats.totalPlayers > 0 
          ? [stats.activePlayers, stats.totalPlayers - stats.activePlayers] 
          : [1],
        backgroundColor: stats.totalPlayers > 0 
          ? ['#22c55e', '#64748b']
          : ['#1e293b'],
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
            icon={<PeopleIcon />}
            color="#6366f1"
          />
        </Grid>
        <Grid item xs={12} sm={6} md={4} lg={2}>
          <StatCard
            title="Active (24h)"
            value={stats.activePlayers.toLocaleString()}
            icon={<TrendingUpIcon />}
            color="#22c55e"
          />
        </Grid>
        <Grid item xs={12} sm={6} md={4} lg={2}>
          <StatCard
            title="Territories"
            value={stats.totalTerritories.toLocaleString()}
            icon={<MapIcon />}
            color="#3b82f6"
          />
        </Grid>
        <Grid item xs={12} sm={6} md={4} lg={2}>
          <StatCard
            title="Alliances"
            value={stats.totalAlliances.toLocaleString()}
            icon={<GroupsIcon />}
            color="#8b5cf6"
          />
        </Grid>
        <Grid item xs={12} sm={6} md={4} lg={2}>
          <StatCard
            title="Revenue (24h)"
            value={`$${stats.revenue.toLocaleString()}`}
            icon={<MoneyIcon />}
            color="#f59e0b"
          />
        </Grid>
        <Grid item xs={12} sm={6} md={4} lg={2}>
          <StatCard
            title="Battles Today"
            value={stats.battlesToday.toLocaleString()}
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
