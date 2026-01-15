import { useState } from 'react';
import {
  Box,
  Card,
  CardContent,
  Typography,
  TextField,
  Button,
  Grid,
  Switch,
  FormControlLabel,
  Divider,
  Alert,
  Slider,
  Select,
  MenuItem,
  FormControl,
  InputLabel,
} from '@mui/material';
import {
  Save as SaveIcon,
  Refresh as RefreshIcon,
} from '@mui/icons-material';

interface GameSettings {
  // Economy
  goldMultiplier: number;
  xpMultiplier: number;
  buildCostMultiplier: number;
  
  // Combat
  attackCooldownMinutes: number;
  maxAttacksPerDay: number;
  defenderBonus: number;
  
  // Social
  maxFriendsPerUser: number;
  dailyGiftLimit: number;
  maxAllianceSize: number;
  
  // Events
  eventXpBoost: number;
  eventGoldBoost: number;
  
  // Anticheat
  maxSpeedKmh: number;
  minActionIntervalMs: number;
  trustScoreThreshold: number;
  
  // Features
  maintenanceMode: boolean;
  chatEnabled: boolean;
  eventsEnabled: boolean;
  purchasesEnabled: boolean;
}

export default function SettingsPage() {
  const [settings, setSettings] = useState<GameSettings>({
    goldMultiplier: 1.0,
    xpMultiplier: 1.0,
    buildCostMultiplier: 1.0,
    attackCooldownMinutes: 15,
    maxAttacksPerDay: 20,
    defenderBonus: 20,
    maxFriendsPerUser: 100,
    dailyGiftLimit: 5,
    maxAllianceSize: 50,
    eventXpBoost: 2.0,
    eventGoldBoost: 2.0,
    maxSpeedKmh: 150,
    minActionIntervalMs: 500,
    trustScoreThreshold: 30,
    maintenanceMode: false,
    chatEnabled: true,
    eventsEnabled: true,
    purchasesEnabled: true,
  });

  const [saved, setSaved] = useState(false);

  const handleSave = () => {
    // TODO: Save to Firebase Remote Config
    console.log('Saving settings:', settings);
    setSaved(true);
    setTimeout(() => setSaved(false), 3000);
  };

  const handleReset = () => {
    setSettings({
      goldMultiplier: 1.0,
      xpMultiplier: 1.0,
      buildCostMultiplier: 1.0,
      attackCooldownMinutes: 15,
      maxAttacksPerDay: 20,
      defenderBonus: 20,
      maxFriendsPerUser: 100,
      dailyGiftLimit: 5,
      maxAllianceSize: 50,
      eventXpBoost: 2.0,
      eventGoldBoost: 2.0,
      maxSpeedKmh: 150,
      minActionIntervalMs: 500,
      trustScoreThreshold: 30,
      maintenanceMode: false,
      chatEnabled: true,
      eventsEnabled: true,
      purchasesEnabled: true,
    });
  };

  return (
    <Box className="animate-fadeIn">
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <Typography variant="h4" sx={{ fontWeight: 700 }}>
          Game Settings
        </Typography>
        <Box sx={{ display: 'flex', gap: 1 }}>
          <Button
            variant="outlined"
            startIcon={<RefreshIcon />}
            onClick={handleReset}
          >
            Reset to Defaults
          </Button>
          <Button
            variant="contained"
            startIcon={<SaveIcon />}
            onClick={handleSave}
          >
            Save Changes
          </Button>
        </Box>
      </Box>

      {saved && (
        <Alert severity="success" sx={{ mb: 3 }}>
          Settings saved successfully!
        </Alert>
      )}

      <Grid container spacing={3}>
        {/* Feature Toggles */}
        <Grid item xs={12} lg={6}>
          <Card>
            <CardContent>
              <Typography variant="h6" sx={{ mb: 2 }}>
                Feature Toggles
              </Typography>
              <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
                <FormControlLabel
                  control={
                    <Switch
                      checked={settings.maintenanceMode}
                      onChange={(e) =>
                        setSettings({ ...settings, maintenanceMode: e.target.checked })
                      }
                      color="error"
                    />
                  }
                  label={
                    <Box>
                      <Typography>Maintenance Mode</Typography>
                      <Typography variant="caption" sx={{ color: 'text.secondary' }}>
                        Blocks all player access to the game
                      </Typography>
                    </Box>
                  }
                />
                <FormControlLabel
                  control={
                    <Switch
                      checked={settings.chatEnabled}
                      onChange={(e) =>
                        setSettings({ ...settings, chatEnabled: e.target.checked })
                      }
                    />
                  }
                  label={
                    <Box>
                      <Typography>Chat System</Typography>
                      <Typography variant="caption" sx={{ color: 'text.secondary' }}>
                        Enable/disable global and alliance chat
                      </Typography>
                    </Box>
                  }
                />
                <FormControlLabel
                  control={
                    <Switch
                      checked={settings.eventsEnabled}
                      onChange={(e) =>
                        setSettings({ ...settings, eventsEnabled: e.target.checked })
                      }
                    />
                  }
                  label={
                    <Box>
                      <Typography>World Events</Typography>
                      <Typography variant="caption" sx={{ color: 'text.secondary' }}>
                        Enable/disable world events system
                      </Typography>
                    </Box>
                  }
                />
                <FormControlLabel
                  control={
                    <Switch
                      checked={settings.purchasesEnabled}
                      onChange={(e) =>
                        setSettings({ ...settings, purchasesEnabled: e.target.checked })
                      }
                    />
                  }
                  label={
                    <Box>
                      <Typography>In-App Purchases</Typography>
                      <Typography variant="caption" sx={{ color: 'text.secondary' }}>
                        Enable/disable all purchases
                      </Typography>
                    </Box>
                  }
                />
              </Box>
            </CardContent>
          </Card>
        </Grid>

        {/* Economy Settings */}
        <Grid item xs={12} lg={6}>
          <Card>
            <CardContent>
              <Typography variant="h6" sx={{ mb: 2 }}>
                Economy Multipliers
              </Typography>
              <Box sx={{ display: 'flex', flexDirection: 'column', gap: 3 }}>
                <Box>
                  <Typography variant="body2" sx={{ mb: 1 }}>
                    Gold Multiplier: {settings.goldMultiplier}x
                  </Typography>
                  <Slider
                    value={settings.goldMultiplier}
                    onChange={(_, value) =>
                      setSettings({ ...settings, goldMultiplier: value as number })
                    }
                    min={0.5}
                    max={5}
                    step={0.1}
                    marks={[
                      { value: 0.5, label: '0.5x' },
                      { value: 1, label: '1x' },
                      { value: 2, label: '2x' },
                      { value: 5, label: '5x' },
                    ]}
                  />
                </Box>
                <Box>
                  <Typography variant="body2" sx={{ mb: 1 }}>
                    XP Multiplier: {settings.xpMultiplier}x
                  </Typography>
                  <Slider
                    value={settings.xpMultiplier}
                    onChange={(_, value) =>
                      setSettings({ ...settings, xpMultiplier: value as number })
                    }
                    min={0.5}
                    max={5}
                    step={0.1}
                    marks={[
                      { value: 0.5, label: '0.5x' },
                      { value: 1, label: '1x' },
                      { value: 2, label: '2x' },
                      { value: 5, label: '5x' },
                    ]}
                  />
                </Box>
                <Box>
                  <Typography variant="body2" sx={{ mb: 1 }}>
                    Build Cost Multiplier: {settings.buildCostMultiplier}x
                  </Typography>
                  <Slider
                    value={settings.buildCostMultiplier}
                    onChange={(_, value) =>
                      setSettings({ ...settings, buildCostMultiplier: value as number })
                    }
                    min={0.5}
                    max={3}
                    step={0.1}
                    marks={[
                      { value: 0.5, label: '0.5x' },
                      { value: 1, label: '1x' },
                      { value: 2, label: '2x' },
                      { value: 3, label: '3x' },
                    ]}
                  />
                </Box>
              </Box>
            </CardContent>
          </Card>
        </Grid>

        {/* Combat Settings */}
        <Grid item xs={12} lg={6}>
          <Card>
            <CardContent>
              <Typography variant="h6" sx={{ mb: 2 }}>
                Combat Settings
              </Typography>
              <Grid container spacing={2}>
                <Grid item xs={6}>
                  <TextField
                    label="Attack Cooldown (minutes)"
                    type="number"
                    value={settings.attackCooldownMinutes}
                    onChange={(e) =>
                      setSettings({
                        ...settings,
                        attackCooldownMinutes: parseInt(e.target.value),
                      })
                    }
                    fullWidth
                  />
                </Grid>
                <Grid item xs={6}>
                  <TextField
                    label="Max Attacks Per Day"
                    type="number"
                    value={settings.maxAttacksPerDay}
                    onChange={(e) =>
                      setSettings({
                        ...settings,
                        maxAttacksPerDay: parseInt(e.target.value),
                      })
                    }
                    fullWidth
                  />
                </Grid>
                <Grid item xs={12}>
                  <Typography variant="body2" sx={{ mb: 1 }}>
                    Defender Bonus: {settings.defenderBonus}%
                  </Typography>
                  <Slider
                    value={settings.defenderBonus}
                    onChange={(_, value) =>
                      setSettings({ ...settings, defenderBonus: value as number })
                    }
                    min={0}
                    max={50}
                    step={5}
                    marks={[
                      { value: 0, label: '0%' },
                      { value: 20, label: '20%' },
                      { value: 50, label: '50%' },
                    ]}
                  />
                </Grid>
              </Grid>
            </CardContent>
          </Card>
        </Grid>

        {/* Social Settings */}
        <Grid item xs={12} lg={6}>
          <Card>
            <CardContent>
              <Typography variant="h6" sx={{ mb: 2 }}>
                Social Settings
              </Typography>
              <Grid container spacing={2}>
                <Grid item xs={6}>
                  <TextField
                    label="Max Friends Per User"
                    type="number"
                    value={settings.maxFriendsPerUser}
                    onChange={(e) =>
                      setSettings({
                        ...settings,
                        maxFriendsPerUser: parseInt(e.target.value),
                      })
                    }
                    fullWidth
                  />
                </Grid>
                <Grid item xs={6}>
                  <TextField
                    label="Daily Gift Limit"
                    type="number"
                    value={settings.dailyGiftLimit}
                    onChange={(e) =>
                      setSettings({
                        ...settings,
                        dailyGiftLimit: parseInt(e.target.value),
                      })
                    }
                    fullWidth
                  />
                </Grid>
                <Grid item xs={6}>
                  <TextField
                    label="Max Alliance Size"
                    type="number"
                    value={settings.maxAllianceSize}
                    onChange={(e) =>
                      setSettings({
                        ...settings,
                        maxAllianceSize: parseInt(e.target.value),
                      })
                    }
                    fullWidth
                  />
                </Grid>
              </Grid>
            </CardContent>
          </Card>
        </Grid>

        {/* Anti-cheat Settings */}
        <Grid item xs={12} lg={6}>
          <Card>
            <CardContent>
              <Typography variant="h6" sx={{ mb: 2, color: 'error.main' }}>
                Anti-Cheat Settings
              </Typography>
              <Grid container spacing={2}>
                <Grid item xs={6}>
                  <TextField
                    label="Max Speed (km/h)"
                    type="number"
                    value={settings.maxSpeedKmh}
                    onChange={(e) =>
                      setSettings({
                        ...settings,
                        maxSpeedKmh: parseInt(e.target.value),
                      })
                    }
                    fullWidth
                    helperText="Max allowed movement speed"
                  />
                </Grid>
                <Grid item xs={6}>
                  <TextField
                    label="Min Action Interval (ms)"
                    type="number"
                    value={settings.minActionIntervalMs}
                    onChange={(e) =>
                      setSettings({
                        ...settings,
                        minActionIntervalMs: parseInt(e.target.value),
                      })
                    }
                    fullWidth
                    helperText="Minimum time between actions"
                  />
                </Grid>
                <Grid item xs={12}>
                  <Typography variant="body2" sx={{ mb: 1 }}>
                    Trust Score Ban Threshold: {settings.trustScoreThreshold}
                  </Typography>
                  <Slider
                    value={settings.trustScoreThreshold}
                    onChange={(_, value) =>
                      setSettings({ ...settings, trustScoreThreshold: value as number })
                    }
                    min={0}
                    max={100}
                    step={5}
                    marks={[
                      { value: 0, label: '0' },
                      { value: 30, label: '30' },
                      { value: 50, label: '50' },
                      { value: 100, label: '100' },
                    ]}
                    color="error"
                  />
                  <Typography variant="caption" sx={{ color: 'text.secondary' }}>
                    Players with trust score below this will be auto-flagged
                  </Typography>
                </Grid>
              </Grid>
            </CardContent>
          </Card>
        </Grid>

        {/* Event Settings */}
        <Grid item xs={12} lg={6}>
          <Card>
            <CardContent>
              <Typography variant="h6" sx={{ mb: 2 }}>
                Event Boost Settings
              </Typography>
              <Grid container spacing={2}>
                <Grid item xs={6}>
                  <Typography variant="body2" sx={{ mb: 1 }}>
                    Event XP Boost: {settings.eventXpBoost}x
                  </Typography>
                  <Slider
                    value={settings.eventXpBoost}
                    onChange={(_, value) =>
                      setSettings({ ...settings, eventXpBoost: value as number })
                    }
                    min={1}
                    max={5}
                    step={0.5}
                    marks={[
                      { value: 1, label: '1x' },
                      { value: 2, label: '2x' },
                      { value: 3, label: '3x' },
                      { value: 5, label: '5x' },
                    ]}
                  />
                </Grid>
                <Grid item xs={6}>
                  <Typography variant="body2" sx={{ mb: 1 }}>
                    Event Gold Boost: {settings.eventGoldBoost}x
                  </Typography>
                  <Slider
                    value={settings.eventGoldBoost}
                    onChange={(_, value) =>
                      setSettings({ ...settings, eventGoldBoost: value as number })
                    }
                    min={1}
                    max={5}
                    step={0.5}
                    marks={[
                      { value: 1, label: '1x' },
                      { value: 2, label: '2x' },
                      { value: 3, label: '3x' },
                      { value: 5, label: '5x' },
                    ]}
                  />
                </Grid>
              </Grid>
            </CardContent>
          </Card>
        </Grid>
      </Grid>
    </Box>
  );
}
