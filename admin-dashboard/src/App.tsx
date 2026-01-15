import { Routes, Route, Navigate } from 'react-router-dom';
import { useState, useEffect } from 'react';
import { onAuthStateChanged, User } from 'firebase/auth';
import { auth } from './config/firebase';
import DashboardLayout from './layouts/DashboardLayout';
import LoginPage from './pages/LoginPage';
import DashboardPage from './pages/DashboardPage';
import PlayersPage from './pages/PlayersPage';
import TerritoriesPage from './pages/TerritoriesPage';
import AlliancesPage from './pages/AlliancesPage';
import WorldEventsPage from './pages/WorldEventsPage';
import SeasonPassPage from './pages/SeasonPassPage';
import AnalyticsPage from './pages/AnalyticsPage';
import ModerationPage from './pages/ModerationPage';
import SettingsPage from './pages/SettingsPage';
import { Box, CircularProgress } from '@mui/material';

function App() {
  const [user, setUser] = useState<User | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const unsubscribe = onAuthStateChanged(auth, (currentUser) => {
      setUser(currentUser);
      setLoading(false);
    });

    return () => unsubscribe();
  }, []);

  if (loading) {
    return (
      <Box
        sx={{
          display: 'flex',
          justifyContent: 'center',
          alignItems: 'center',
          height: '100vh',
          backgroundColor: 'background.default',
        }}
      >
        <CircularProgress />
      </Box>
    );
  }

  if (!user) {
    return (
      <Routes>
        <Route path="/login" element={<LoginPage />} />
        <Route path="*" element={<Navigate to="/login" replace />} />
      </Routes>
    );
  }

  return (
    <Routes>
      <Route path="/" element={<DashboardLayout />}>
        <Route index element={<Navigate to="/dashboard" replace />} />
        <Route path="dashboard" element={<DashboardPage />} />
        <Route path="players" element={<PlayersPage />} />
        <Route path="territories" element={<TerritoriesPage />} />
        <Route path="alliances" element={<AlliancesPage />} />
        <Route path="world-events" element={<WorldEventsPage />} />
        <Route path="season-pass" element={<SeasonPassPage />} />
        <Route path="analytics" element={<AnalyticsPage />} />
        <Route path="moderation" element={<ModerationPage />} />
        <Route path="settings" element={<SettingsPage />} />
      </Route>
      <Route path="/login" element={<Navigate to="/dashboard" replace />} />
    </Routes>
  );
}

export default App;
