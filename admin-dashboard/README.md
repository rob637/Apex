# Apex Citadels Admin Dashboard

A powerful React-based admin dashboard for managing the Apex Citadels game.

## Features

- **Dashboard Overview**: Real-time stats on players, territories, revenue, and activity
- **Player Management**: View, search, ban/unban players, see detailed player profiles
- **Territory Management**: Monitor all territories, ownership, and resource output
- **Alliance Management**: View alliances, membership, war records, and stats
- **World Events**: Create, activate, and manage time-limited game events
- **Season Pass**: Create seasons, configure rewards, track premium conversions
- **Analytics**: DAU/MAU charts, retention metrics, revenue tracking, session analytics
- **Moderation**: Review suspicious activities, chat reports, take actions (warn/ban)
- **Settings**: Configure game parameters, feature toggles, economy multipliers

## Tech Stack

- **React 18** with TypeScript
- **Vite** for fast development and building
- **Material-UI (MUI)** for components
- **Chart.js** for data visualization
- **Firebase** for authentication and Firestore

## Quick Start

1. **Install dependencies**:
   ```bash
   npm install
   ```

2. **Configure Firebase**:
   ```bash
   cp .env.example .env
   # Edit .env with your Firebase credentials
   ```

3. **Start development server**:
   ```bash
   npm run dev
   ```

4. **Build for production**:
   ```bash
   npm run build
   ```

## Project Structure

```
src/
├── config/
│   ├── firebase.ts     # Firebase initialization
│   └── theme.ts        # MUI theme configuration
├── layouts/
│   └── DashboardLayout.tsx  # Main layout with sidebar
├── pages/
│   ├── LoginPage.tsx        # Authentication
│   ├── DashboardPage.tsx    # Overview & stats
│   ├── PlayersPage.tsx      # Player management
│   ├── TerritoriesPage.tsx  # Territory management
│   ├── AlliancesPage.tsx    # Alliance management
│   ├── WorldEventsPage.tsx  # World events
│   ├── SeasonPassPage.tsx   # Season/Battle pass
│   ├── AnalyticsPage.tsx    # Analytics & metrics
│   ├── ModerationPage.tsx   # Moderation tools
│   └── SettingsPage.tsx     # Game settings
├── App.tsx
├── main.tsx
└── index.css
```

## Firebase Security

Ensure your admin users have the appropriate Firestore rules. The dashboard requires:
- Read access to: `users`, `territories`, `alliances`, `world_events`, `seasons`, `analytics_*`, `suspicious_activities`, `chat_reports`
- Write access to: `world_events`, `seasons`, `suspicious_activities`, `users` (for banning)

## Deployment

### Firebase Hosting

1. Install Firebase CLI:
   ```bash
   npm install -g firebase-tools
   ```

2. Initialize hosting:
   ```bash
   firebase init hosting
   ```

3. Build and deploy:
   ```bash
   npm run build
   firebase deploy --only hosting
   ```

### Vercel

```bash
npm install -g vercel
vercel
```

## Screenshots

The dashboard features a dark gaming aesthetic with:
- Indigo primary colors (#6366f1)
- Amber/gold accents (#f59e0b)
- Slate dark backgrounds (#0f172a, #1e293b)
- Responsive design for desktop and mobile

## License

Proprietary - Apex Citadels
