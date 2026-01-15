/**
 * Apex Citadels - Friends & Social System
 * 
 * Social features drive viral growth and retention:
 * - Friend requests and connections
 * - Activity feed
 * - Social features (gifting, visiting)
 * - Friend leaderboards
 */

import * as functions from 'firebase-functions';
import * as admin from 'firebase-admin';
import { sendNotification } from './notifications';

const db = admin.firestore();

// ============================================================================
// Types
// ============================================================================

export interface FriendRequest {
  id: string;
  fromUserId: string;
  fromUsername: string;
  toUserId: string;
  toUsername: string;
  status: 'pending' | 'accepted' | 'rejected';
  message?: string;
  createdAt: admin.firestore.Timestamp;
  respondedAt?: admin.firestore.Timestamp;
}

export interface Friendship {
  id: string;
  users: string[]; // [userId1, userId2] - sorted alphabetically
  user1: string;
  user2: string;
  user1Username: string;
  user2Username: string;
  createdAt: admin.firestore.Timestamp;
  
  // Social tracking
  lastGiftSent?: {
    from: string;
    at: admin.firestore.Timestamp;
  };
  lastVisit?: {
    by: string;
    at: admin.firestore.Timestamp;
  };
}

export interface FriendProfile {
  userId: string;
  username: string;
  level: number;
  avatarUrl?: string;
  avatarFrame?: string;
  currentTitle?: string;
  alliance?: {
    id: string;
    name: string;
    tag: string;
  };
  
  // Stats
  territoriesOwned: number;
  totalConquests: number;
  defensesWon: number;
  
  // Online status
  lastActive: admin.firestore.Timestamp;
  isOnline: boolean;
  
  // Social features
  canSendGift: boolean;
  canVisit: boolean;
}

export interface SocialActivity {
  id: string;
  userId: string;
  username: string;
  type: 'territory_claimed' | 'territory_conquered' | 'level_up' | 'achievement' | 
        'alliance_joined' | 'defense_won' | 'building_completed' | 'season_rank';
  description: string;
  data?: Record<string, unknown>;
  createdAt: admin.firestore.Timestamp;
  
  // Reactions
  likes: string[]; // userIds who liked
  comments: SocialComment[];
}

export interface SocialComment {
  userId: string;
  username: string;
  text: string;
  createdAt: admin.firestore.Timestamp;
}

export interface GiftTransaction {
  id: string;
  fromUserId: string;
  fromUsername: string;
  toUserId: string;
  toUsername: string;
  giftType: 'daily_gift' | 'special_gift';
  resources: {
    stone?: number;
    wood?: number;
    iron?: number;
    gems?: number;
    arcaneEssence?: number;
  };
  message?: string;
  claimed: boolean;
  createdAt: admin.firestore.Timestamp;
  claimedAt?: admin.firestore.Timestamp;
}

// ============================================================================
// Send Friend Request
// ============================================================================

export const sendFriendRequest = functions.https.onCall(async (data, context) => {
  if (!context.auth) {
    throw new functions.https.HttpsError('unauthenticated', 'Must be authenticated');
  }
  
  const { targetUserId, message } = data as { targetUserId: string; message?: string };
  const userId = context.auth.uid;
  
  if (userId === targetUserId) {
    throw new functions.https.HttpsError('invalid-argument', 'Cannot send friend request to yourself');
  }
  
  // Get both users
  const [userDoc, targetDoc] = await Promise.all([
    db.collection('users').doc(userId).get(),
    db.collection('users').doc(targetUserId).get()
  ]);
  
  if (!userDoc.exists || !targetDoc.exists) {
    throw new functions.https.HttpsError('not-found', 'User not found');
  }
  
  const user = userDoc.data()!;
  const target = targetDoc.data()!;
  
  // Check if already friends
  const existingFriendship = await db.collection('friendships')
    .where('users', 'array-contains', userId)
    .get();
  
  const alreadyFriends = existingFriendship.docs.some(doc => {
    const f = doc.data() as Friendship;
    return f.users.includes(targetUserId);
  });
  
  if (alreadyFriends) {
    throw new functions.https.HttpsError('already-exists', 'Already friends');
  }
  
  // Check for existing pending request
  const existingRequest = await db.collection('friend_requests')
    .where('fromUserId', '==', userId)
    .where('toUserId', '==', targetUserId)
    .where('status', '==', 'pending')
    .get();
  
  if (!existingRequest.empty) {
    throw new functions.https.HttpsError('already-exists', 'Request already pending');
  }
  
  // Check for reverse request (they sent us one)
  const reverseRequest = await db.collection('friend_requests')
    .where('fromUserId', '==', targetUserId)
    .where('toUserId', '==', userId)
    .where('status', '==', 'pending')
    .get();
  
  if (!reverseRequest.empty) {
    // Auto-accept
    const request = reverseRequest.docs[0];
    await acceptFriendRequestInternal(request.id, userId);
    return { success: true, autoAccepted: true };
  }
  
  // Create friend request
  const requestRef = db.collection('friend_requests').doc();
  const request: FriendRequest = {
    id: requestRef.id,
    fromUserId: userId,
    fromUsername: user.username || 'Unknown',
    toUserId: targetUserId,
    toUsername: target.username || 'Unknown',
    status: 'pending',
    message,
    createdAt: admin.firestore.Timestamp.now()
  };
  
  await requestRef.set(request);
  
  // Send notification
  await sendNotification(targetUserId, {
    type: 'friend_request',
    title: 'New Friend Request! 👋',
    body: `${user.username} wants to be your friend`,
    data: { 
      requestId: requestRef.id,
      fromUserId: userId 
    }
  });
  
  return { success: true, requestId: requestRef.id };
});

// ============================================================================
// Respond to Friend Request
// ============================================================================

export const respondToFriendRequest = functions.https.onCall(async (data, context) => {
  if (!context.auth) {
    throw new functions.https.HttpsError('unauthenticated', 'Must be authenticated');
  }
  
  const { requestId, accept } = data as { requestId: string; accept: boolean };
  const userId = context.auth.uid;
  
  const requestDoc = await db.collection('friend_requests').doc(requestId).get();
  if (!requestDoc.exists) {
    throw new functions.https.HttpsError('not-found', 'Request not found');
  }
  
  const request = requestDoc.data() as FriendRequest;
  
  if (request.toUserId !== userId) {
    throw new functions.https.HttpsError('permission-denied', 'Not your request');
  }
  
  if (request.status !== 'pending') {
    throw new functions.https.HttpsError('failed-precondition', 'Request already processed');
  }
  
  if (accept) {
    await acceptFriendRequestInternal(requestId, userId);
  } else {
    await db.collection('friend_requests').doc(requestId).update({
      status: 'rejected',
      respondedAt: admin.firestore.Timestamp.now()
    });
  }
  
  return { success: true, accepted: accept };
});

async function acceptFriendRequestInternal(requestId: string, acceptingUserId: string): Promise<void> {
  const requestDoc = await db.collection('friend_requests').doc(requestId).get();
  const request = requestDoc.data() as FriendRequest;
  
  // Create friendship
  const users = [request.fromUserId, request.toUserId].sort();
  const friendshipRef = db.collection('friendships').doc();
  
  const friendship: Friendship = {
    id: friendshipRef.id,
    users,
    user1: users[0],
    user2: users[1],
    user1Username: request.fromUserId === users[0] ? request.fromUsername : request.toUsername,
    user2Username: request.fromUserId === users[1] ? request.fromUsername : request.toUsername,
    createdAt: admin.firestore.Timestamp.now()
  };
  
  await db.runTransaction(async (transaction) => {
    // Create friendship
    transaction.set(friendshipRef, friendship);
    
    // Update request
    transaction.update(db.collection('friend_requests').doc(requestId), {
      status: 'accepted',
      respondedAt: admin.firestore.Timestamp.now()
    });
    
    // Update friend counts
    transaction.update(db.collection('users').doc(request.fromUserId), {
      'stats.friendCount': admin.firestore.FieldValue.increment(1)
    });
    transaction.update(db.collection('users').doc(request.toUserId), {
      'stats.friendCount': admin.firestore.FieldValue.increment(1)
    });
  });
  
  // Notify the sender
  await sendNotification(request.fromUserId, {
    type: 'friend_request_accepted',
    title: 'Friend Request Accepted! 🎉',
    body: `${request.toUsername} accepted your friend request`,
    data: { friendId: acceptingUserId }
  });
}

// ============================================================================
// Get Friends List
// ============================================================================

export const getFriendsList = functions.https.onCall(async (data, context) => {
  if (!context.auth) {
    throw new functions.https.HttpsError('unauthenticated', 'Must be authenticated');
  }
  
  const userId = context.auth.uid;
  const { limit = 50 } = data as { limit?: number };
  
  // Get all friendships
  const friendshipSnapshot = await db.collection('friendships')
    .where('users', 'array-contains', userId)
    .limit(limit)
    .get();
  
  // Get friend user IDs
  const friendIds = friendshipSnapshot.docs.map(doc => {
    const f = doc.data() as Friendship;
    return f.user1 === userId ? f.user2 : f.user1;
  });
  
  if (friendIds.length === 0) {
    return { friends: [], totalCount: 0 };
  }
  
  // Get friend profiles
  const friendDocs = await Promise.all(
    friendIds.map(id => db.collection('users').doc(id).get())
  );
  
  const now = Date.now();
  const fiveMinutesAgo = now - 5 * 60 * 1000;
  
  const friends: FriendProfile[] = friendDocs
    .filter(doc => doc.exists)
    .map(doc => {
      const data = doc.data()!;
      const friendshipDoc = friendshipSnapshot.docs.find(f => {
        const friendship = f.data() as Friendship;
        return friendship.users.includes(doc.id);
      });
      const friendship = friendshipDoc?.data() as Friendship | undefined;
      
      const lastActive = data.lastActive?.toMillis() || 0;
      const lastGiftTime = friendship?.lastGiftSent?.from === userId 
        ? friendship.lastGiftSent.at.toMillis() 
        : 0;
      const canSendGift = now - lastGiftTime > 24 * 60 * 60 * 1000; // 24 hours
      
      return {
        userId: doc.id,
        username: data.username,
        level: data.level || 1,
        avatarUrl: data.avatarUrl,
        avatarFrame: data.equippedAvatarFrame,
        currentTitle: data.equippedTitle,
        alliance: data.allianceId ? {
          id: data.allianceId,
          name: data.allianceName,
          tag: data.allianceTag
        } : undefined,
        territoriesOwned: data.stats?.territoriesOwned || 0,
        totalConquests: data.stats?.totalConquests || 0,
        defensesWon: data.stats?.defensesWon || 0,
        lastActive: data.lastActive,
        isOnline: lastActive > fiveMinutesAgo,
        canSendGift,
        canVisit: true
      } as FriendProfile;
    })
    .sort((a, b) => {
      // Online friends first, then by last active
      if (a.isOnline !== b.isOnline) return a.isOnline ? -1 : 1;
      return (b.lastActive?.toMillis() || 0) - (a.lastActive?.toMillis() || 0);
    });
  
  return { friends, totalCount: friends.length };
});

// ============================================================================
// Get Friend Requests
// ============================================================================

export const getFriendRequests = functions.https.onCall(async (data, context) => {
  if (!context.auth) {
    throw new functions.https.HttpsError('unauthenticated', 'Must be authenticated');
  }
  
  const userId = context.auth.uid;
  
  const [incoming, outgoing] = await Promise.all([
    db.collection('friend_requests')
      .where('toUserId', '==', userId)
      .where('status', '==', 'pending')
      .orderBy('createdAt', 'desc')
      .limit(50)
      .get(),
    db.collection('friend_requests')
      .where('fromUserId', '==', userId)
      .where('status', '==', 'pending')
      .orderBy('createdAt', 'desc')
      .limit(50)
      .get()
  ]);
  
  return {
    incoming: incoming.docs.map(doc => doc.data()),
    outgoing: outgoing.docs.map(doc => doc.data())
  };
});

// ============================================================================
// Send Gift
// ============================================================================

export const sendGift = functions.https.onCall(async (data, context) => {
  if (!context.auth) {
    throw new functions.https.HttpsError('unauthenticated', 'Must be authenticated');
  }
  
  const { friendId, message } = data as { friendId: string; message?: string };
  const userId = context.auth.uid;
  
  // Verify friendship
  const friendshipSnapshot = await db.collection('friendships')
    .where('users', 'array-contains', userId)
    .get();
  
  const friendship = friendshipSnapshot.docs.find(doc => {
    const f = doc.data() as Friendship;
    return f.users.includes(friendId);
  });
  
  if (!friendship) {
    throw new functions.https.HttpsError('not-found', 'Not friends with this user');
  }
  
  const friendshipData = friendship.data() as Friendship;
  
  // Check cooldown (24 hours)
  const now = Date.now();
  if (friendshipData.lastGiftSent?.from === userId) {
    const lastGiftTime = friendshipData.lastGiftSent.at.toMillis();
    const hoursSince = (now - lastGiftTime) / (1000 * 60 * 60);
    if (hoursSince < 24) {
      throw new functions.https.HttpsError(
        'failed-precondition',
        `Can send another gift in ${Math.ceil(24 - hoursSince)} hours`
      );
    }
  }
  
  // Get sender info
  const userDoc = await db.collection('users').doc(userId).get();
  const user = userDoc.data()!;
  const friendDoc = await db.collection('users').doc(friendId).get();
  const friend = friendDoc.data()!;
  
  // Daily gift resources
  const giftResources = {
    stone: 100,
    wood: 100,
    iron: 25
  };
  
  // Create gift
  const giftRef = db.collection('gifts').doc();
  const gift: GiftTransaction = {
    id: giftRef.id,
    fromUserId: userId,
    fromUsername: user.username,
    toUserId: friendId,
    toUsername: friend.username,
    giftType: 'daily_gift',
    resources: giftResources,
    message,
    claimed: false,
    createdAt: admin.firestore.Timestamp.now()
  };
  
  await db.runTransaction(async (transaction) => {
    transaction.set(giftRef, gift);
    transaction.update(friendship.ref, {
      lastGiftSent: {
        from: userId,
        at: admin.firestore.Timestamp.now()
      }
    });
    transaction.update(db.collection('users').doc(userId), {
      'stats.giftsSent': admin.firestore.FieldValue.increment(1)
    });
  });
  
  // Notify friend
  await sendNotification(friendId, {
    type: 'gift_received',
    title: 'Gift from a Friend! 🎁',
    body: `${user.username} sent you a gift!`,
    data: { giftId: giftRef.id, fromUserId: userId }
  });
  
  return { success: true, giftId: giftRef.id };
});

// ============================================================================
// Claim Gift
// ============================================================================

export const claimGift = functions.https.onCall(async (data, context) => {
  if (!context.auth) {
    throw new functions.https.HttpsError('unauthenticated', 'Must be authenticated');
  }
  
  const { giftId } = data as { giftId: string };
  const userId = context.auth.uid;
  
  const giftDoc = await db.collection('gifts').doc(giftId).get();
  if (!giftDoc.exists) {
    throw new functions.https.HttpsError('not-found', 'Gift not found');
  }
  
  const gift = giftDoc.data() as GiftTransaction;
  
  if (gift.toUserId !== userId) {
    throw new functions.https.HttpsError('permission-denied', 'Not your gift');
  }
  
  if (gift.claimed) {
    throw new functions.https.HttpsError('already-exists', 'Gift already claimed');
  }
  
  // Apply resources
  const userUpdate: Record<string, unknown> = {
    'stats.giftsReceived': admin.firestore.FieldValue.increment(1)
  };
  
  if (gift.resources.stone) {
    userUpdate['resources.stone'] = admin.firestore.FieldValue.increment(gift.resources.stone);
  }
  if (gift.resources.wood) {
    userUpdate['resources.wood'] = admin.firestore.FieldValue.increment(gift.resources.wood);
  }
  if (gift.resources.iron) {
    userUpdate['resources.iron'] = admin.firestore.FieldValue.increment(gift.resources.iron);
  }
  if (gift.resources.gems) {
    userUpdate['resources.gems'] = admin.firestore.FieldValue.increment(gift.resources.gems);
  }
  if (gift.resources.arcaneEssence) {
    userUpdate['resources.arcaneEssence'] = admin.firestore.FieldValue.increment(gift.resources.arcaneEssence);
  }
  
  await db.runTransaction(async (transaction) => {
    transaction.update(db.collection('users').doc(userId), userUpdate);
    transaction.update(giftDoc.ref, {
      claimed: true,
      claimedAt: admin.firestore.Timestamp.now()
    });
  });
  
  return { success: true, resources: gift.resources };
});

// ============================================================================
// Get Pending Gifts
// ============================================================================

export const getPendingGifts = functions.https.onCall(async (data, context) => {
  if (!context.auth) {
    throw new functions.https.HttpsError('unauthenticated', 'Must be authenticated');
  }
  
  const userId = context.auth.uid;
  
  const giftsSnapshot = await db.collection('gifts')
    .where('toUserId', '==', userId)
    .where('claimed', '==', false)
    .orderBy('createdAt', 'desc')
    .limit(50)
    .get();
  
  return {
    gifts: giftsSnapshot.docs.map(doc => doc.data()),
    count: giftsSnapshot.size
  };
});

// ============================================================================
// Activity Feed
// ============================================================================

export const getActivityFeed = functions.https.onCall(async (data, context) => {
  if (!context.auth) {
    throw new functions.https.HttpsError('unauthenticated', 'Must be authenticated');
  }
  
  const userId = context.auth.uid;
  const { limit = 50, feedType = 'friends' } = data as { 
    limit?: number; 
    feedType?: 'friends' | 'alliance' | 'global';
  };
  
  let query: admin.firestore.Query = db.collection('social_activities');
  
  if (feedType === 'friends') {
    // Get friend IDs
    const friendshipSnapshot = await db.collection('friendships')
      .where('users', 'array-contains', userId)
      .get();
    
    const friendIds = friendshipSnapshot.docs.map(doc => {
      const f = doc.data() as Friendship;
      return f.user1 === userId ? f.user2 : f.user1;
    });
    
    // Include own activities
    friendIds.push(userId);
    
    if (friendIds.length > 0) {
      // Firestore 'in' query limited to 30 items
      const limitedIds = friendIds.slice(0, 30);
      query = query.where('userId', 'in', limitedIds);
    }
  } else if (feedType === 'alliance') {
    const userDoc = await db.collection('users').doc(userId).get();
    const user = userDoc.data();
    
    if (user?.allianceId) {
      query = query.where('allianceId', '==', user.allianceId);
    } else {
      return { activities: [], hasMore: false };
    }
  }
  // 'global' shows all activities
  
  const activitiesSnapshot = await query
    .orderBy('createdAt', 'desc')
    .limit(limit + 1)
    .get();
  
  const activities = activitiesSnapshot.docs.slice(0, limit).map(doc => doc.data());
  const hasMore = activitiesSnapshot.size > limit;
  
  return { activities, hasMore };
});

// ============================================================================
// Post Activity
// ============================================================================

export async function postActivity(
  userId: string,
  type: SocialActivity['type'],
  description: string,
  activityData?: Record<string, unknown>
): Promise<void> {
  const userDoc = await db.collection('users').doc(userId).get();
  const user = userDoc.data();
  
  if (!user) return;
  
  const activityRef = db.collection('social_activities').doc();
  const activity: SocialActivity = {
    id: activityRef.id,
    userId,
    username: user.username,
    type,
    description,
    data: activityData,
    createdAt: admin.firestore.Timestamp.now(),
    likes: [],
    comments: []
  };
  
  await activityRef.set(activity);
}

// ============================================================================
// Like Activity
// ============================================================================

export const likeActivity = functions.https.onCall(async (data, context) => {
  if (!context.auth) {
    throw new functions.https.HttpsError('unauthenticated', 'Must be authenticated');
  }
  
  const { activityId } = data as { activityId: string };
  const userId = context.auth.uid;
  
  const activityDoc = await db.collection('social_activities').doc(activityId).get();
  if (!activityDoc.exists) {
    throw new functions.https.HttpsError('not-found', 'Activity not found');
  }
  
  const activity = activityDoc.data() as SocialActivity;
  
  if (activity.likes.includes(userId)) {
    // Unlike
    await activityDoc.ref.update({
      likes: admin.firestore.FieldValue.arrayRemove(userId)
    });
    return { success: true, liked: false };
  } else {
    // Like
    await activityDoc.ref.update({
      likes: admin.firestore.FieldValue.arrayUnion(userId)
    });
    return { success: true, liked: true };
  }
});

// ============================================================================
// Remove Friend
// ============================================================================

export const removeFriend = functions.https.onCall(async (data, context) => {
  if (!context.auth) {
    throw new functions.https.HttpsError('unauthenticated', 'Must be authenticated');
  }
  
  const { friendId } = data as { friendId: string };
  const userId = context.auth.uid;
  
  // Find friendship
  const friendshipSnapshot = await db.collection('friendships')
    .where('users', 'array-contains', userId)
    .get();
  
  const friendship = friendshipSnapshot.docs.find(doc => {
    const f = doc.data() as Friendship;
    return f.users.includes(friendId);
  });
  
  if (!friendship) {
    throw new functions.https.HttpsError('not-found', 'Friendship not found');
  }
  
  await db.runTransaction(async (transaction) => {
    transaction.delete(friendship.ref);
    transaction.update(db.collection('users').doc(userId), {
      'stats.friendCount': admin.firestore.FieldValue.increment(-1)
    });
    transaction.update(db.collection('users').doc(friendId), {
      'stats.friendCount': admin.firestore.FieldValue.increment(-1)
    });
  });
  
  return { success: true };
});

// ============================================================================
// Search Users
// ============================================================================

export const searchUsers = functions.https.onCall(async (data, context) => {
  if (!context.auth) {
    throw new functions.https.HttpsError('unauthenticated', 'Must be authenticated');
  }
  
  const { query, limit = 20 } = data as { query: string; limit?: number };
  
  if (!query || query.length < 3) {
    throw new functions.https.HttpsError('invalid-argument', 'Query must be at least 3 characters');
  }
  
  // Search by username (case-insensitive prefix)
  const queryLower = query.toLowerCase();
  
  const usersSnapshot = await db.collection('users')
    .where('usernameLower', '>=', queryLower)
    .where('usernameLower', '<=', queryLower + '\uf8ff')
    .limit(limit)
    .get();
  
  const users = usersSnapshot.docs.map(doc => {
    const data = doc.data();
    return {
      userId: doc.id,
      username: data.username,
      level: data.level || 1,
      avatarUrl: data.avatarUrl,
      alliance: data.allianceId ? {
        id: data.allianceId,
        name: data.allianceName,
        tag: data.allianceTag
      } : undefined
    };
  });
  
  return { users };
});

// ============================================================================
// Friend Leaderboard
// ============================================================================

export const getFriendLeaderboard = functions.https.onCall(async (data, context) => {
  if (!context.auth) {
    throw new functions.https.HttpsError('unauthenticated', 'Must be authenticated');
  }
  
  const userId = context.auth.uid;
  const { metric = 'territories' } = data as { metric?: 'territories' | 'conquests' | 'level' | 'defenses' };
  
  // Get friend IDs
  const friendshipSnapshot = await db.collection('friendships')
    .where('users', 'array-contains', userId)
    .get();
  
  const friendIds = friendshipSnapshot.docs.map(doc => {
    const f = doc.data() as Friendship;
    return f.user1 === userId ? f.user2 : f.user1;
  });
  
  // Include self
  friendIds.push(userId);
  
  if (friendIds.length === 0) {
    return { leaderboard: [], userRank: 1 };
  }
  
  // Get all friend data
  const friendDocs = await Promise.all(
    friendIds.map(id => db.collection('users').doc(id).get())
  );
  
  const leaderboard = friendDocs
    .filter(doc => doc.exists)
    .map(doc => {
      const data = doc.data()!;
      let value: number;
      
      switch (metric) {
        case 'territories':
          value = data.stats?.territoriesOwned || 0;
          break;
        case 'conquests':
          value = data.stats?.totalConquests || 0;
          break;
        case 'level':
          value = data.level || 1;
          break;
        case 'defenses':
          value = data.stats?.defensesWon || 0;
          break;
        default:
          value = 0;
      }
      
      return {
        userId: doc.id,
        username: data.username,
        avatarUrl: data.avatarUrl,
        value,
        isCurrentUser: doc.id === userId
      };
    })
    .sort((a, b) => b.value - a.value)
    .map((entry, index) => ({ ...entry, rank: index + 1 }));
  
  const userRank = leaderboard.findIndex(e => e.userId === userId) + 1;
  
  return { leaderboard, userRank };
});
