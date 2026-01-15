/**
 * Apex Citadels - Alliance System Cloud Functions
 * 
 * Handles:
 * - Alliance creation/management
 * - Member management
 * - Alliance wars
 * - Alliance leaderboards
 */

import * as functions from 'firebase-functions';
import * as admin from 'firebase-admin';
import {
  Alliance,
  AllianceMember,
  AllianceInvitation,
  AllianceWar,
  User,
  WAR_TIMELINE,
  WarStatus,
  WarPhase
} from './types';
import { sendNotification } from './notifications';
import { checkAchievements } from './progression';

const db = admin.firestore();

// ============================================================================
// Configuration
// ============================================================================

const ALLIANCE_CREATION_COST = 1000; // Gems
const MAX_ALLIANCE_MEMBERS = 50;
const INVITATION_EXPIRY_DAYS = 7;

// ============================================================================
// Create Alliance
// ============================================================================

export const createAlliance = functions.https.onCall(async (data, context) => {
  if (!context.auth) {
    throw new functions.https.HttpsError('unauthenticated', 'Must be authenticated');
  }

  const { name, tag, description, isOpen } = data as {
    name: string;
    tag: string;
    description?: string;
    isOpen?: boolean;
  };

  const userId = context.auth.uid;

  // Validate inputs
  if (!name || name.length < 3 || name.length > 20) {
    throw new functions.https.HttpsError('invalid-argument', 'Name must be 3-20 characters');
  }

  if (!tag || tag.length < 2 || tag.length > 4) {
    throw new functions.https.HttpsError('invalid-argument', 'Tag must be 2-4 characters');
  }

  // Check if tag is taken
  const existingTag = await db.collection('alliances')
    .where('tag', '==', tag.toUpperCase())
    .limit(1)
    .get();

  if (!existingTag.empty) {
    throw new functions.https.HttpsError('already-exists', 'Tag already in use');
  }

  // Get user and check if already in alliance
  const userDoc = await db.collection('users').doc(userId).get();
  const user = userDoc.data() as User;

  if (user.allianceId) {
    throw new functions.https.HttpsError('failed-precondition', 'Already in an alliance');
  }

  // Check gems
  if ((user.resources.gems || 0) < ALLIANCE_CREATION_COST) {
    throw new functions.https.HttpsError(
      'failed-precondition',
      `Need ${ALLIANCE_CREATION_COST} gems to create alliance`
    );
  }

  // Create alliance
  const allianceId = db.collection('alliances').doc().id;

  const alliance: Alliance = {
    id: allianceId,
    name,
    tag: tag.toUpperCase(),
    description: description || '',
    leaderId: userId,
    leaderName: user.displayName,
    memberCount: 1,
    maxMembers: MAX_ALLIANCE_MEMBERS,
    isOpen: isOpen ?? true,
    requiredLevel: 1,
    totalTerritories: user.stats.territoriesClaimed || 0,
    totalXp: user.stats.totalXp || 0,
    warsWon: 0,
    warsLost: 0,
    createdAt: admin.firestore.Timestamp.now(),
    emblemId: 'default',
    settings: {
      allowInvites: true,
      minLevelToJoin: 1
    }
  };

  const member: AllianceMember = {
    userId,
    displayName: user.displayName,
    role: 'leader',
    joinedAt: admin.firestore.Timestamp.now(),
    xpContributed: user.stats.totalXp || 0,
    territoriesContributed: user.stats.territoriesClaimed || 0,
    lastActive: admin.firestore.Timestamp.now()
  };

  // Transaction to create alliance and update user
  await db.runTransaction(async (transaction) => {
    // Deduct gems
    transaction.update(db.collection('users').doc(userId), {
      'resources.gems': admin.firestore.FieldValue.increment(-ALLIANCE_CREATION_COST),
      allianceId,
      allianceRole: 'leader',
      'stats.alliancesJoined': admin.firestore.FieldValue.increment(1)
    });

    // Create alliance
    transaction.set(db.collection('alliances').doc(allianceId), alliance);

    // Add member
    transaction.set(
      db.collection('alliances').doc(allianceId).collection('members').doc(userId),
      member
    );
  });

  // Check achievements
  await checkAchievements(userId, 'social');

  return { success: true, allianceId, alliance };
});

// ============================================================================
// Join Alliance
// ============================================================================

export const joinAlliance = functions.https.onCall(async (data, context) => {
  if (!context.auth) {
    throw new functions.https.HttpsError('unauthenticated', 'Must be authenticated');
  }

  const { allianceId } = data as { allianceId: string };
  const userId = context.auth.uid;

  // Get alliance
  const allianceDoc = await db.collection('alliances').doc(allianceId).get();
  if (!allianceDoc.exists) {
    throw new functions.https.HttpsError('not-found', 'Alliance not found');
  }

  const alliance = allianceDoc.data() as Alliance;

  // Check if open
  if (!alliance.isOpen) {
    throw new functions.https.HttpsError('failed-precondition', 'Alliance is invite-only');
  }

  // Check member count
  if (alliance.memberCount >= alliance.maxMembers) {
    throw new functions.https.HttpsError('failed-precondition', 'Alliance is full');
  }

  // Get user
  const userDoc = await db.collection('users').doc(userId).get();
  const user = userDoc.data() as User;

  // Check if already in alliance
  if (user.allianceId) {
    throw new functions.https.HttpsError('failed-precondition', 'Already in an alliance');
  }

  // Check level requirement
  if (user.stats.level < alliance.requiredLevel) {
    throw new functions.https.HttpsError(
      'failed-precondition',
      `Requires level ${alliance.requiredLevel}`
    );
  }

  const member: AllianceMember = {
    userId,
    displayName: user.displayName,
    role: 'member',
    joinedAt: admin.firestore.Timestamp.now(),
    xpContributed: 0,
    territoriesContributed: 0,
    lastActive: admin.firestore.Timestamp.now()
  };

  await db.runTransaction(async (transaction) => {
    // Update user
    transaction.update(db.collection('users').doc(userId), {
      allianceId,
      allianceRole: 'member',
      'stats.alliancesJoined': admin.firestore.FieldValue.increment(1)
    });

    // Update alliance
    transaction.update(db.collection('alliances').doc(allianceId), {
      memberCount: admin.firestore.FieldValue.increment(1),
      totalXp: admin.firestore.FieldValue.increment(user.stats.totalXp || 0)
    });

    // Add member
    transaction.set(
      db.collection('alliances').doc(allianceId).collection('members').doc(userId),
      member
    );
  });

  // Notify alliance leader
  await sendNotification(alliance.leaderId, {
    type: 'system_announcement',
    title: 'New Member!',
    body: `${user.displayName} joined ${alliance.name}`,
    data: { allianceId, userId }
  });

  // Check achievements
  await checkAchievements(userId, 'social');

  return { success: true, alliance };
});

// ============================================================================
// Leave Alliance
// ============================================================================

export const leaveAlliance = functions.https.onCall(async (data, context) => {
  if (!context.auth) {
    throw new functions.https.HttpsError('unauthenticated', 'Must be authenticated');
  }

  const userId = context.auth.uid;

  const userDoc = await db.collection('users').doc(userId).get();
  const user = userDoc.data() as User;

  if (!user.allianceId) {
    throw new functions.https.HttpsError('failed-precondition', 'Not in an alliance');
  }

  const allianceDoc = await db.collection('alliances').doc(user.allianceId).get();
  const alliance = allianceDoc.data() as Alliance;

  // Leaders must transfer or disband
  if (alliance.leaderId === userId) {
    throw new functions.https.HttpsError(
      'failed-precondition',
      'Leaders must transfer leadership or disband alliance'
    );
  }

  // Get member data for stats
  const memberDoc = await db
    .collection('alliances')
    .doc(user.allianceId)
    .collection('members')
    .doc(userId)
    .get();
  const member = memberDoc.data() as AllianceMember;

  await db.runTransaction(async (transaction) => {
    // Update user
    transaction.update(db.collection('users').doc(userId), {
      allianceId: admin.firestore.FieldValue.delete(),
      allianceRole: admin.firestore.FieldValue.delete()
    });

    // Update alliance
    transaction.update(db.collection('alliances').doc(user.allianceId!), {
      memberCount: admin.firestore.FieldValue.increment(-1),
      totalXp: admin.firestore.FieldValue.increment(-(member.xpContributed || 0))
    });

    // Remove member
    transaction.delete(
      db.collection('alliances').doc(user.allianceId!).collection('members').doc(userId)
    );
  });

  return { success: true };
});

// ============================================================================
// Invite to Alliance
// ============================================================================

export const inviteToAlliance = functions.https.onCall(async (data, context) => {
  if (!context.auth) {
    throw new functions.https.HttpsError('unauthenticated', 'Must be authenticated');
  }

  const { targetUserId } = data as { targetUserId: string };
  const inviterId = context.auth.uid;

  // Get inviter
  const inviterDoc = await db.collection('users').doc(inviterId).get();
  const inviter = inviterDoc.data() as User;

  if (!inviter.allianceId) {
    throw new functions.https.HttpsError('failed-precondition', 'Not in an alliance');
  }

  // Check permissions (leader or officer)
  if (inviter.allianceRole !== 'leader' && inviter.allianceRole !== 'officer') {
    throw new functions.https.HttpsError('permission-denied', 'Only leaders and officers can invite');
  }

  // Get alliance
  const allianceDoc = await db.collection('alliances').doc(inviter.allianceId).get();
  const alliance = allianceDoc.data() as Alliance;

  // Check if full
  if (alliance.memberCount >= alliance.maxMembers) {
    throw new functions.https.HttpsError('failed-precondition', 'Alliance is full');
  }

  // Get target user
  const targetDoc = await db.collection('users').doc(targetUserId).get();
  if (!targetDoc.exists) {
    throw new functions.https.HttpsError('not-found', 'User not found');
  }
  const target = targetDoc.data() as User;

  if (target.allianceId) {
    throw new functions.https.HttpsError('failed-precondition', 'User is already in an alliance');
  }

  // Check for existing pending invitation
  const existingInvite = await db.collection('alliance_invitations')
    .where('allianceId', '==', inviter.allianceId)
    .where('invitedUserId', '==', targetUserId)
    .where('status', '==', 'pending')
    .limit(1)
    .get();

  if (!existingInvite.empty) {
    throw new functions.https.HttpsError('already-exists', 'Invitation already pending');
  }

  // Create invitation
  const invitationId = db.collection('alliance_invitations').doc().id;
  const expiresAt = new Date();
  expiresAt.setDate(expiresAt.getDate() + INVITATION_EXPIRY_DAYS);

  const invitation: AllianceInvitation = {
    id: invitationId,
    allianceId: inviter.allianceId,
    allianceName: alliance.name,
    invitedUserId: targetUserId,
    invitedBy: inviterId,
    invitedByName: inviter.displayName,
    createdAt: admin.firestore.Timestamp.now(),
    expiresAt: admin.firestore.Timestamp.fromDate(expiresAt),
    status: 'pending'
  };

  await db.collection('alliance_invitations').doc(invitationId).set(invitation);

  // Notify target user
  await sendNotification(targetUserId, {
    type: 'alliance_invite',
    title: 'Alliance Invitation',
    body: `${inviter.displayName} invited you to join ${alliance.name}`,
    data: { invitationId, allianceId: inviter.allianceId }
  });

  return { success: true, invitationId };
});

// ============================================================================
// Respond to Invitation
// ============================================================================

export const respondToInvitation = functions.https.onCall(async (data, context) => {
  if (!context.auth) {
    throw new functions.https.HttpsError('unauthenticated', 'Must be authenticated');
  }

  const { invitationId, accept } = data as { invitationId: string; accept: boolean };
  const userId = context.auth.uid;

  const inviteDoc = await db.collection('alliance_invitations').doc(invitationId).get();
  if (!inviteDoc.exists) {
    throw new functions.https.HttpsError('not-found', 'Invitation not found');
  }

  const invitation = inviteDoc.data() as AllianceInvitation;

  // Verify recipient
  if (invitation.invitedUserId !== userId) {
    throw new functions.https.HttpsError('permission-denied', 'Not your invitation');
  }

  // Check status
  if (invitation.status !== 'pending') {
    throw new functions.https.HttpsError('failed-precondition', 'Invitation already processed');
  }

  // Check expiry
  if (invitation.expiresAt.toDate() < new Date()) {
    await db.collection('alliance_invitations').doc(invitationId).update({
      status: 'expired'
    });
    throw new functions.https.HttpsError('failed-precondition', 'Invitation expired');
  }

  if (accept) {
    // Join alliance (reuse join logic)
    const userDoc = await db.collection('users').doc(userId).get();
    const user = userDoc.data() as User;

    if (user.allianceId) {
      throw new functions.https.HttpsError('failed-precondition', 'Already in an alliance');
    }

    const allianceDoc = await db.collection('alliances').doc(invitation.allianceId).get();
    if (!allianceDoc.exists) {
      throw new functions.https.HttpsError('not-found', 'Alliance no longer exists');
    }

    const alliance = allianceDoc.data() as Alliance;

    if (alliance.memberCount >= alliance.maxMembers) {
      throw new functions.https.HttpsError('failed-precondition', 'Alliance is now full');
    }

    const member: AllianceMember = {
      userId,
      displayName: user.displayName,
      role: 'member',
      joinedAt: admin.firestore.Timestamp.now(),
      xpContributed: 0,
      territoriesContributed: 0,
      lastActive: admin.firestore.Timestamp.now()
    };

    await db.runTransaction(async (transaction) => {
      transaction.update(db.collection('users').doc(userId), {
        allianceId: invitation.allianceId,
        allianceRole: 'member',
        'stats.alliancesJoined': admin.firestore.FieldValue.increment(1)
      });

      transaction.update(db.collection('alliances').doc(invitation.allianceId), {
        memberCount: admin.firestore.FieldValue.increment(1)
      });

      transaction.set(
        db.collection('alliances').doc(invitation.allianceId).collection('members').doc(userId),
        member
      );

      transaction.update(db.collection('alliance_invitations').doc(invitationId), {
        status: 'accepted'
      });
    });

    // Check achievements
    await checkAchievements(userId, 'social');

    return { success: true, joined: true };
  } else {
    // Decline
    await db.collection('alliance_invitations').doc(invitationId).update({
      status: 'declined'
    });

    return { success: true, joined: false };
  }
});

// ============================================================================
// Alliance War System (Enhanced with 24hr Warning → 48hr War → 72hr Peace)
// ============================================================================

/**
 * Declare war on another alliance
 * Timeline: 24hr warning → 48hr active war → 72hr peace treaty
 */
export const declareAllianceWar = functions.https.onCall(async (data, context) => {
  if (!context.auth) {
    throw new functions.https.HttpsError('unauthenticated', 'Must be authenticated');
  }

  const { targetAllianceId } = data as { targetAllianceId: string };
  const userId = context.auth.uid;

  // Get challenger
  const userDoc = await db.collection('users').doc(userId).get();
  const user = userDoc.data() as User;

  if (!user.allianceId) {
    throw new functions.https.HttpsError('failed-precondition', 'Not in an alliance');
  }

  // Only leaders can start wars
  if (user.allianceRole !== 'leader') {
    throw new functions.https.HttpsError('permission-denied', 'Only leaders can declare war');
  }

  // Can't war yourself
  if (user.allianceId === targetAllianceId) {
    throw new functions.https.HttpsError('invalid-argument', 'Cannot war your own alliance');
  }

  // Get both alliances
  const [challengerDoc, defenderDoc] = await Promise.all([
    db.collection('alliances').doc(user.allianceId).get(),
    db.collection('alliances').doc(targetAllianceId).get()
  ]);

  if (!defenderDoc.exists) {
    throw new functions.https.HttpsError('not-found', 'Target alliance not found');
  }

  const challenger = challengerDoc.data() as Alliance;
  const defender = defenderDoc.data() as Alliance;

  // Check minimum member requirement
  if (challenger.memberCount < WAR_TIMELINE.MIN_MEMBERS_TO_WAR) {
    throw new functions.https.HttpsError(
      'failed-precondition',
      `Need at least ${WAR_TIMELINE.MIN_MEMBERS_TO_WAR} members to declare war`
    );
  }

  // Check for existing active/pending war between these alliances
  const existingWar = await db.collection('alliance_wars')
    .where('status', 'in', ['pending', 'active'])
    .get();

  for (const doc of existingWar.docs) {
    const war = doc.data() as AllianceWar;
    const involvesBoth = 
      (war.challengerAllianceId === user.allianceId || war.defenderAllianceId === user.allianceId) &&
      (war.challengerAllianceId === targetAllianceId || war.defenderAllianceId === targetAllianceId);
    
    if (involvesBoth) {
      throw new functions.https.HttpsError('failed-precondition', 'Already at war with this alliance');
    }
  }

  // Check peace treaty cooldown
  const recentWars = await db.collection('alliance_wars')
    .where('status', '==', 'completed')
    .orderBy('endsAt', 'desc')
    .limit(10)
    .get();

  for (const doc of recentWars.docs) {
    const war = doc.data() as AllianceWar;
    const involvesBoth = 
      (war.challengerAllianceId === user.allianceId || war.defenderAllianceId === user.allianceId) &&
      (war.challengerAllianceId === targetAllianceId || war.defenderAllianceId === targetAllianceId);
    
    if (involvesBoth && war.peaceTreatyEndsAt) {
      const peaceTreatyEnds = war.peaceTreatyEndsAt.toDate();
      if (peaceTreatyEnds > new Date()) {
        const hoursRemaining = Math.ceil((peaceTreatyEnds.getTime() - Date.now()) / 3600000);
        throw new functions.https.HttpsError(
          'failed-precondition',
          `Peace treaty in effect. Can declare war again in ${hoursRemaining} hours.`
        );
      }
    }
  }

  // Calculate war timeline
  const now = new Date();
  const warningEnds = new Date(now.getTime() + WAR_TIMELINE.WARNING_HOURS * 3600000);
  const warEnds = new Date(warningEnds.getTime() + WAR_TIMELINE.DURATION_HOURS * 3600000);
  const peaceTreatyEnds = new Date(warEnds.getTime() + WAR_TIMELINE.PEACE_TREATY_HOURS * 3600000);

  // Create war
  const warId = db.collection('alliance_wars').doc().id;

  const war: AllianceWar = {
    id: warId,
    challengerAllianceId: user.allianceId,
    challengerAllianceName: challenger.name,
    defenderAllianceId: targetAllianceId,
    defenderAllianceName: defender.name,
    status: 'pending',
    phase: 'warning',
    declaredAt: admin.firestore.Timestamp.now(),
    warningEndsAt: admin.firestore.Timestamp.fromDate(warningEnds),
    startsAt: admin.firestore.Timestamp.fromDate(warningEnds),
    endsAt: admin.firestore.Timestamp.fromDate(warEnds),
    peaceTreatyEndsAt: admin.firestore.Timestamp.fromDate(peaceTreatyEnds),
    challengerScore: 0,
    defenderScore: 0,
    challengerParticipants: [],
    defenderParticipants: [],
    totalBattles: 0,
    challengerBattlesWon: 0,
    defenderBattlesWon: 0
  };

  await db.collection('alliance_wars').doc(warId).set(war);

  // Notify both alliances about upcoming war
  await notifyAllianceMembers(user.allianceId, {
    type: 'alliance_war_declared',
    title: '⚔️ War Declared!',
    body: `War against ${defender.name} begins in 24 hours! Prepare your forces.`,
    data: { warId, phase: 'warning', enemyAllianceId: targetAllianceId }
  });

  await notifyAllianceMembers(targetAllianceId, {
    type: 'alliance_war_incoming',
    title: '🚨 War Incoming!',
    body: `${challenger.name} declared war! Fighting begins in 24 hours.`,
    data: { warId, phase: 'warning', enemyAllianceId: user.allianceId }
  });

  return { 
    success: true, 
    warId, 
    war,
    timeline: {
      warningEnds: warningEnds.toISOString(),
      warStarts: warningEnds.toISOString(),
      warEnds: warEnds.toISOString(),
      peaceTreatyEnds: peaceTreatyEnds.toISOString()
    }
  };
});

/**
 * Legacy function name for backwards compatibility
 */
export const startAllianceWar = declareAllianceWar;

/**
 * Cancel a pending war (only during warning phase)
 */
export const cancelAllianceWar = functions.https.onCall(async (data, context) => {
  if (!context.auth) {
    throw new functions.https.HttpsError('unauthenticated', 'Must be authenticated');
  }

  const { warId } = data as { warId: string };
  const userId = context.auth.uid;

  // Get user
  const userDoc = await db.collection('users').doc(userId).get();
  const user = userDoc.data() as User;

  if (!user.allianceId || user.allianceRole !== 'leader') {
    throw new functions.https.HttpsError('permission-denied', 'Only leaders can cancel wars');
  }

  // Get war
  const warDoc = await db.collection('alliance_wars').doc(warId).get();
  if (!warDoc.exists) {
    throw new functions.https.HttpsError('not-found', 'War not found');
  }

  const war = warDoc.data() as AllianceWar;

  // Verify user's alliance is the challenger
  if (war.challengerAllianceId !== user.allianceId) {
    throw new functions.https.HttpsError('permission-denied', 'Only the declaring alliance can cancel');
  }

  // Can only cancel during warning phase
  if (war.phase !== 'warning') {
    throw new functions.https.HttpsError('failed-precondition', 'Can only cancel during warning phase');
  }

  // Cancel the war
  await warDoc.ref.update({
    status: 'cancelled' as WarStatus,
    phase: 'cancelled' as WarPhase,
    cancelledAt: admin.firestore.Timestamp.now(),
    cancelledBy: userId
  });

  // Notify both alliances
  await notifyAllianceMembers(war.challengerAllianceId, {
    type: 'alliance_war_cancelled',
    title: 'War Cancelled',
    body: `The war against ${war.defenderAllianceName} has been cancelled.`,
    data: { warId }
  });

  await notifyAllianceMembers(war.defenderAllianceId, {
    type: 'alliance_war_cancelled',
    title: 'War Cancelled',
    body: `${war.challengerAllianceName} has cancelled their war declaration.`,
    data: { warId }
  });

  return { success: true, message: 'War cancelled' };
});

/**
 * Get current war status for an alliance
 */
export const getAllianceWarStatus = functions.https.onCall(async (data, context) => {
  if (!context.auth) {
    throw new functions.https.HttpsError('unauthenticated', 'Must be authenticated');
  }

  const { allianceId } = data as { allianceId?: string };
  const userId = context.auth.uid;

  // Get user's alliance if not specified
  let targetAllianceId = allianceId;
  if (!targetAllianceId) {
    const userDoc = await db.collection('users').doc(userId).get();
    const user = userDoc.data() as User;
    targetAllianceId = user.allianceId;
  }

  if (!targetAllianceId) {
    return { success: true, hasActiveWar: false };
  }

  // Find active/pending wars
  const wars = await db.collection('alliance_wars')
    .where('status', 'in', ['pending', 'active'])
    .get();

  const activeWars: AllianceWar[] = [];
  for (const doc of wars.docs) {
    const war = doc.data() as AllianceWar;
    if (war.challengerAllianceId === targetAllianceId || war.defenderAllianceId === targetAllianceId) {
      activeWars.push(war);
    }
  }

  if (activeWars.length === 0) {
    return { success: true, hasActiveWar: false };
  }

  const war = activeWars[0];
  const isChallenger = war.challengerAllianceId === targetAllianceId;
  const now = new Date();

  return {
    success: true,
    hasActiveWar: true,
    war,
    isChallenger,
    enemyAllianceId: isChallenger ? war.defenderAllianceId : war.challengerAllianceId,
    enemyAllianceName: isChallenger ? war.defenderAllianceName : war.challengerAllianceName,
    yourScore: isChallenger ? war.challengerScore : war.defenderScore,
    enemyScore: isChallenger ? war.defenderScore : war.challengerScore,
    phase: war.phase,
    timeRemaining: getTimeRemaining(war, now)
  };
});

/**
 * Process war phase transitions (scheduled function)
 */
export const processWarPhases = functions.pubsub
  .schedule('every 5 minutes')
  .onRun(async () => {
    const now = admin.firestore.Timestamp.now();

    // Process pending wars that should start (warning period ended)
    const pendingWars = await db.collection('alliance_wars')
      .where('status', '==', 'pending')
      .where('phase', '==', 'warning')
      .where('warningEndsAt', '<=', now)
      .get();

    for (const doc of pendingWars.docs) {
      const war = doc.data() as AllianceWar;
      
      await doc.ref.update({
        status: 'active' as WarStatus,
        phase: 'active' as WarPhase
      });

      // Notify both alliances that war has begun
      await notifyAllianceMembers(war.challengerAllianceId, {
        type: 'alliance_war_started',
        title: '⚔️ WAR HAS BEGUN!',
        body: `The war against ${war.defenderAllianceName} is now active! Attack enemy territories for bonus points.`,
        data: { warId: war.id, phase: 'active' }
      });

      await notifyAllianceMembers(war.defenderAllianceId, {
        type: 'alliance_war_started',
        title: '⚔️ WAR HAS BEGUN!',
        body: `The war with ${war.challengerAllianceName} is now active! Defend your territories!`,
        data: { warId: war.id, phase: 'active' }
      });

      console.log(`War ${war.id} transitioned from warning to active`);
    }

    // Process active wars that should end
    const activeWars = await db.collection('alliance_wars')
      .where('status', '==', 'active')
      .where('phase', '==', 'active')
      .where('endsAt', '<=', now)
      .get();

    for (const doc of activeWars.docs) {
      const war = doc.data() as AllianceWar;
      
      // Determine winner
      let winnerId: string | undefined;
      let winnerName: string | undefined;
      
      if (war.challengerScore > war.defenderScore) {
        winnerId = war.challengerAllianceId;
        winnerName = war.challengerAllianceName;
      } else if (war.defenderScore > war.challengerScore) {
        winnerId = war.defenderAllianceId;
        winnerName = war.defenderAllianceName;
      }
      // If tied, no winner (draw)

      await doc.ref.update({
        status: 'completed' as WarStatus,
        phase: 'peace_treaty' as WarPhase,
        winnerId,
        winnerName,
        rewards: calculateWarRewards(war, winnerId)
      });

      // Notify results
      const resultMessage = winnerId 
        ? `${winnerName} wins!`
        : 'The war ended in a draw!';

      await notifyAllianceMembers(war.challengerAllianceId, {
        type: 'alliance_war_ended',
        title: '🏁 War Ended!',
        body: `${resultMessage} Final score: ${war.challengerScore} - ${war.defenderScore}. Peace treaty active for 72 hours.`,
        data: { warId: war.id, phase: 'peace_treaty', won: winnerId === war.challengerAllianceId }
      });

      await notifyAllianceMembers(war.defenderAllianceId, {
        type: 'alliance_war_ended',
        title: '🏁 War Ended!',
        body: `${resultMessage} Final score: ${war.defenderScore} - ${war.challengerScore}. Peace treaty active for 72 hours.`,
        data: { warId: war.id, phase: 'peace_treaty', won: winnerId === war.defenderAllianceId }
      });

      // Award rewards to winner
      if (winnerId) {
        await awardWarRewards(winnerId, war);
      }

      console.log(`War ${war.id} completed. Winner: ${winnerName || 'Draw'}`);
    }

    return null;
  });

/**
 * Record a battle result for war scoring
 */
export async function recordWarBattle(
  warId: string,
  winnerId: string,
  loserId: string,
  winnerAllianceId: string,
  points: number
): Promise<void> {
  const warRef = db.collection('alliance_wars').doc(warId);
  const warDoc = await warRef.get();
  
  if (!warDoc.exists) return;
  
  const war = warDoc.data() as AllianceWar;
  
  if (war.phase !== 'active') return;
  
  const isChallenger = war.challengerAllianceId === winnerAllianceId;
  
  await warRef.update({
    totalBattles: admin.firestore.FieldValue.increment(1),
    [isChallenger ? 'challengerScore' : 'defenderScore']: admin.firestore.FieldValue.increment(points),
    [isChallenger ? 'challengerBattlesWon' : 'defenderBattlesWon']: admin.firestore.FieldValue.increment(1)
  });
}

// ============================================================================
// Helper Functions
// ============================================================================

function getTimeRemaining(war: AllianceWar, now: Date): { hours: number; minutes: number; phase: string } {
  let targetTime: Date;
  let phase: string;

  switch (war.phase) {
    case 'warning':
      targetTime = war.warningEndsAt.toDate();
      phase = 'until war starts';
      break;
    case 'active':
      targetTime = war.endsAt.toDate();
      phase = 'until war ends';
      break;
    case 'peace_treaty':
      targetTime = war.peaceTreatyEndsAt?.toDate() || now;
      phase = 'of peace treaty';
      break;
    default:
      return { hours: 0, minutes: 0, phase: 'ended' };
  }

  const diff = targetTime.getTime() - now.getTime();
  const hours = Math.floor(diff / 3600000);
  const minutes = Math.floor((diff % 3600000) / 60000);

  return { hours: Math.max(0, hours), minutes: Math.max(0, minutes), phase };
}

function calculateWarRewards(war: AllianceWar, winnerId?: string): { winnerXp: number; winnerGems: number; loserXp: number } {
  const baseXp = 500;
  const baseGems = 100;
  const battleBonus = (war.totalBattles || 0) * 10;
  
  return {
    winnerXp: winnerId ? baseXp + battleBonus : baseXp / 2, // Draw gets half
    winnerGems: winnerId ? baseGems : baseGems / 2,
    loserXp: baseXp / 4
  };
}

async function awardWarRewards(winnerAllianceId: string, war: AllianceWar): Promise<void> {
  const rewards = war.rewards;
  if (!rewards) return;

  const members = await db
    .collection('alliances')
    .doc(winnerAllianceId)
    .collection('members')
    .get();

  const batch = db.batch();
  
  for (const member of members.docs) {
    const userRef = db.collection('users').doc(member.id);
    batch.update(userRef, {
      'resources.gems': admin.firestore.FieldValue.increment(Math.floor(rewards.winnerGems / members.size)),
      'stats.totalXp': admin.firestore.FieldValue.increment(Math.floor(rewards.winnerXp / members.size)),
      'stats.allianceWarsWon': admin.firestore.FieldValue.increment(1)
    });
  }

  await batch.commit();
}

async function notifyAllianceMembers(
  allianceId: string,
  notification: { 
    type: 'alliance_war_declared' | 'alliance_war_incoming' | 'alliance_war_started' | 'alliance_war_ended' | 'alliance_war_cancelled'; 
    title: string; 
    body: string; 
    data: Record<string, unknown> 
  }
): Promise<void> {
  const members = await db
    .collection('alliances')
    .doc(allianceId)
    .collection('members')
    .get();

  for (const member of members.docs) {
    await sendNotification(member.id, notification as Parameters<typeof sendNotification>[1]);
  }
}

// ============================================================================
// Get Alliance Details
// ============================================================================

export const getAllianceDetails = functions.https.onCall(async (data, context) => {
  const { allianceId } = data as { allianceId: string };

  const allianceDoc = await db.collection('alliances').doc(allianceId).get();
  if (!allianceDoc.exists) {
    throw new functions.https.HttpsError('not-found', 'Alliance not found');
  }

  const alliance = allianceDoc.data() as Alliance;

  // Get members
  const membersSnapshot = await db
    .collection('alliances')
    .doc(allianceId)
    .collection('members')
    .orderBy('joinedAt', 'asc')
    .get();

  const members = membersSnapshot.docs.map(doc => doc.data() as AllianceMember);

  // Get active wars
  const warsSnapshot = await db.collection('alliance_wars')
    .where('status', '==', 'active')
    .where('challengerAllianceId', '==', allianceId)
    .get();

  const wars = warsSnapshot.docs.map(doc => doc.data() as AllianceWar);

  return {
    alliance,
    members,
    activeWars: wars
  };
});

// ============================================================================
// Search Alliances
// ============================================================================

export const searchAlliances = functions.https.onCall(async (data, context) => {
  const { query, openOnly = false, limit = 20 } = data as {
    query?: string;
    openOnly?: boolean;
    limit?: number;
  };

  let dbQuery = db.collection('alliances')
    .orderBy('memberCount', 'desc')
    .limit(limit);

  if (openOnly) {
    dbQuery = dbQuery.where('isOpen', '==', true);
  }

  const snapshot = await dbQuery.get();
  let alliances = snapshot.docs.map(doc => doc.data() as Alliance);

  // Client-side filter by name/tag if query provided
  if (query) {
    const lowerQuery = query.toLowerCase();
    alliances = alliances.filter(a =>
      a.name.toLowerCase().includes(lowerQuery) ||
      a.tag.toLowerCase().includes(lowerQuery)
    );
  }

  return { alliances };
});
