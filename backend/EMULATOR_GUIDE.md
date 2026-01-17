# Firebase Emulator Guide

Local development environment for Apex Citadels using Firebase Emulators.

## Quick Start

```bash
# From the backend directory
cd /workspaces/Apex/backend

# Install dependencies
cd functions && npm install && cd ..

# Start all emulators
cd functions && npm run emulators
```

## Emulator Ports

| Service | Port | URL |
|---------|------|-----|
| Emulator UI | 4000 | http://localhost:4000 |
| Auth | 9099 | http://localhost:9099 |
| Firestore | 8080 | http://localhost:8080 |
| Functions | 5001 | http://localhost:5001 |
| Hosting | 5002 | http://localhost:5002 |
| Storage | 9199 | http://localhost:9199 |

## Available Commands

```bash
# Start all emulators
npm run emulators

# Start with debug output
npm run emulators:debug

# Start with previously saved data
npm run emulators:import

# Export current emulator data
npm run emulators:export

# Seed database with test data
npm run seed
```

## Seeding Test Data

After starting emulators, seed the database with test data:

```bash
cd functions
npm run seed
```

This creates:
- **3 test users** with credentials (password: `testpass123`)
  - `player1@test.com` - Level 10, alliance leader
  - `player2@test.com` - Level 8, rival player
  - `alliance@test.com` - Level 5, alliance member

- **3 territories** around San Francisco
  - SF Downtown (owned by player1)
  - Embarcadero (owned by player2)
  - Mission District (unclaimed)

- **4 resource nodes**
  - Stone, Wood, Metal, Crystal deposits

- **1 test alliance** - "Test Alliance" [TEST]

- **Sample buildings** - Walls and towers

## Unity Integration

### Connecting to Emulators

In Unity, the `FirebaseManager.cs` can be configured to use emulators:

```csharp
// In FirebaseManager.InitializeFirebase()
#if UNITY_EDITOR
    // Use emulators in editor
    FirebaseFirestore.DefaultInstance.Settings = new FirestoreSettings
    {
        Host = "localhost:8080",
        SslEnabled = false,
        PersistenceEnabled = false
    };
    
    FirebaseAuth.DefaultInstance.UseEmulator("localhost", 9099);
    FirebaseFunctions.DefaultInstance.UseFunctionsEmulator("localhost", 5001);
#endif
```

### Test Without Device

1. Start emulators: `npm run emulators`
2. Seed data: `npm run seed`
3. Open Unity project
4. Use Debug Console teleport: `teleport sf`
5. Test building, resources, combat

## Admin Dashboard with Emulators

```bash
# Terminal 1: Start emulators
cd backend/functions && npm run emulators

# Terminal 2: Start admin dashboard
cd admin-dashboard && npm run dev
```

The dashboard will detect emulators via `connectFirestoreEmulator()` in development mode.

## Persisting Data

### Export Data
```bash
npm run emulators:export
```
Saves current state to `./emulator-data/`

### Import Data
```bash
npm run emulators:import
```
Starts emulators with previously saved state.

## Emulator UI Features

Access at http://localhost:4000

### Firestore Tab
- Browse all collections
- Add/edit/delete documents
- Run queries
- View real-time updates

### Auth Tab
- View all users
- Create new users
- Edit user properties
- Delete users

### Functions Tab
- View function logs
- See invocation history
- Debug function errors

### Request History
- See all emulator requests
- Debug API calls
- Monitor performance

## Testing Cloud Functions

### Direct HTTP Testing

```bash
# Health check
curl http://localhost:5001/apex-citadels-dev/us-central1/healthCheck

# Callable function (with auth)
curl -X POST http://localhost:5001/apex-citadels-dev/us-central1/claimTerritory \
  -H "Content-Type: application/json" \
  -d '{"data":{"territoryId":"territory-sf-mission","latitude":37.7599,"longitude":-122.4148}}'
```

### Using Functions Shell

```bash
npm run shell

# In the shell:
> claimTerritory({territoryId: 'territory-sf-mission'}, {auth: {uid: 'test-user-1'}})
```

## Troubleshooting

### Port Already in Use
```bash
# Find process using port
lsof -i :8080

# Kill it
kill -9 <PID>
```

### Emulators Won't Start
```bash
# Clear Java cache
firebase emulators:start --clear-java-cache

# Or try specific project
firebase emulators:start --project apex-citadels-dev
```

### Auth Not Working
Make sure you're setting the emulator host BEFORE initializing Firebase:
```javascript
process.env.FIREBASE_AUTH_EMULATOR_HOST = 'localhost:9099';
```

### Data Not Persisting
- Export before stopping: `npm run emulators:export`
- Start with import: `npm run emulators:import`

## Development Workflow

1. **Morning**: Start emulators with previous data
   ```bash
   npm run emulators:import
   ```

2. **Develop**: Make changes, test in real-time
   - Cloud Functions auto-reload on save with `build:watch`
   - Firestore changes visible in UI

3. **End of Day**: Export your data
   ```bash
   npm run emulators:export
   ```

4. **Fresh Start**: Reset with seed data
   ```bash
   npm run emulators
   npm run seed
   ```

## Security Rules Testing

Emulators respect your `firestore.rules`. Test rules by:

1. Edit `firestore.rules`
2. Restart emulators (rules reload automatically)
3. Test operations in Emulator UI or via code

## Differences from Production

| Feature | Emulator | Production |
|---------|----------|------------|
| Data persistence | In-memory (export to save) | Permanent |
| Auth tokens | Simplified, no verification | Full OAuth |
| Function cold starts | None | Yes |
| Rate limiting | None | Enforced |
| Billing | Free | Pay-as-you-go |

## Next Steps

- [ ] Add more comprehensive seed data
- [ ] Create test scenarios for combat
- [ ] Set up automated integration tests
- [ ] Add performance testing scripts
