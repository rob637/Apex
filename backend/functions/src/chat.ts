/**
 * Apex Citadels - Chat System Infrastructure
 * 
 * Real-time communication is key for social games:
 * - Global chat
 * - Alliance chat
 * - Territory chat (proximity)
 * - Direct messages
 * - Moderation tools
 */

import * as functions from 'firebase-functions';
import * as admin from 'firebase-admin';
import { sendNotification } from './notifications';

const db = admin.firestore();

// ============================================================================
// Types
// ============================================================================

export interface ChatChannel {
  id: string;
  type: 'global' | 'alliance' | 'territory' | 'direct';
  name: string;
  description?: string;
  
  // For alliance/territory chats
  ownerId?: string; // Alliance ID or Territory ID
  
  // For DMs
  participants?: string[];
  
  // Settings
  isActive: boolean;
  isModerated: boolean;
  slowModeSeconds?: number;
  
  // Stats
  memberCount: number;
  messageCount: number;
  lastMessageAt?: admin.firestore.Timestamp;
  
  createdAt: admin.firestore.Timestamp;
}

export interface ChatMessage {
  id: string;
  channelId: string;
  channelType: ChatChannel['type'];
  
  // Sender
  senderId: string;
  senderName: string;
  senderLevel: number;
  senderTitle?: string;
  senderBadge?: string;
  senderAllianceTag?: string;
  
  // Content
  content: string;
  type: 'text' | 'emote' | 'system' | 'achievement' | 'conquest' | 'level_up';
  
  // Rich content
  attachments?: MessageAttachment[];
  mentions?: string[]; // User IDs
  
  // Moderation
  isDeleted: boolean;
  deletedBy?: string;
  deletedAt?: admin.firestore.Timestamp;
  isHidden: boolean; // Shadow ban
  
  // Reactions
  reactions: Record<string, string[]>; // emoji: userIds[]
  
  createdAt: admin.firestore.Timestamp;
  editedAt?: admin.firestore.Timestamp;
}

export interface MessageAttachment {
  type: 'territory' | 'achievement' | 'player' | 'alliance' | 'event';
  id: string;
  name: string;
  preview?: string;
}

export interface ChatMember {
  userId: string;
  channelId: string;
  joinedAt: admin.firestore.Timestamp;
  lastReadAt: admin.firestore.Timestamp;
  unreadCount: number;
  
  // Roles
  role: 'member' | 'moderator' | 'admin' | 'owner';
  isMuted: boolean;
  mutedUntil?: admin.firestore.Timestamp;
  isBanned: boolean;
}

export interface ChatReport {
  id: string;
  messageId: string;
  channelId: string;
  reporterId: string;
  reporterName: string;
  reason: 'spam' | 'harassment' | 'offensive' | 'cheating' | 'other';
  description?: string;
  
  // Status
  status: 'pending' | 'reviewed' | 'actioned' | 'dismissed';
  reviewedBy?: string;
  reviewedAt?: admin.firestore.Timestamp;
  action?: 'none' | 'delete_message' | 'warn_user' | 'mute_user' | 'ban_user';
  
  createdAt: admin.firestore.Timestamp;
}

// ============================================================================
// Word Filter
// ============================================================================

const BANNED_WORDS: string[] = [
  // This would contain actual banned words in production
  // Keeping empty for the example
];

const WORD_REPLACEMENTS: Record<string, string> = {
  // Replace common offensive words with safe versions
  // 'badword': '****'
};

function filterMessage(content: string): { filtered: string; wasCensored: boolean } {
  let filtered = content;
  let wasCensored = false;
  
  // Convert to lowercase for checking
  const lowerContent = content.toLowerCase();
  
  for (const word of BANNED_WORDS) {
    if (lowerContent.includes(word.toLowerCase())) {
      const regex = new RegExp(word, 'gi');
      filtered = filtered.replace(regex, '*'.repeat(word.length));
      wasCensored = true;
    }
  }
  
  for (const [word, replacement] of Object.entries(WORD_REPLACEMENTS)) {
    const regex = new RegExp(word, 'gi');
    if (regex.test(filtered)) {
      filtered = filtered.replace(regex, replacement);
      wasCensored = true;
    }
  }
  
  return { filtered, wasCensored };
}

// ============================================================================
// Send Message
// ============================================================================

export const sendMessage = functions.https.onCall(async (data, context) => {
  if (!context.auth) {
    throw new functions.https.HttpsError('unauthenticated', 'Must be authenticated');
  }
  
  const { channelId, content, type = 'text', attachments } = data as {
    channelId: string;
    content: string;
    type?: ChatMessage['type'];
    attachments?: MessageAttachment[];
  };
  const userId = context.auth.uid;
  
  // Validate content
  if (!content || content.trim().length === 0) {
    throw new functions.https.HttpsError('invalid-argument', 'Message cannot be empty');
  }
  
  if (content.length > 500) {
    throw new functions.https.HttpsError('invalid-argument', 'Message too long (max 500 characters)');
  }
  
  // Get channel
  const channelDoc = await db.collection('chat_channels').doc(channelId).get();
  if (!channelDoc.exists) {
    throw new functions.https.HttpsError('not-found', 'Channel not found');
  }
  const channel = channelDoc.data() as ChatChannel;
  
  if (!channel.isActive) {
    throw new functions.https.HttpsError('failed-precondition', 'Channel is not active');
  }
  
  // Check membership for non-global channels
  if (channel.type !== 'global') {
    const memberDoc = await db.collection('chat_members')
      .doc(`${channelId}_${userId}`)
      .get();
    
    if (!memberDoc.exists) {
      throw new functions.https.HttpsError('permission-denied', 'Not a member of this channel');
    }
    
    const member = memberDoc.data() as ChatMember;
    
    if (member.isBanned) {
      throw new functions.https.HttpsError('permission-denied', 'You are banned from this channel');
    }
    
    if (member.isMuted && member.mutedUntil && member.mutedUntil.toMillis() > Date.now()) {
      const remaining = Math.ceil((member.mutedUntil.toMillis() - Date.now()) / 1000);
      throw new functions.https.HttpsError(
        'failed-precondition', 
        `You are muted for ${remaining} more seconds`
      );
    }
  }
  
  // Check slow mode
  if (channel.slowModeSeconds && channel.slowModeSeconds > 0) {
    const lastMessage = await db.collection('chat_messages')
      .where('channelId', '==', channelId)
      .where('senderId', '==', userId)
      .orderBy('createdAt', 'desc')
      .limit(1)
      .get();
    
    if (!lastMessage.empty) {
      const lastTime = lastMessage.docs[0].data().createdAt.toMillis();
      const elapsed = (Date.now() - lastTime) / 1000;
      
      if (elapsed < channel.slowModeSeconds) {
        throw new functions.https.HttpsError(
          'resource-exhausted',
          `Slow mode: wait ${Math.ceil(channel.slowModeSeconds - elapsed)} seconds`
        );
      }
    }
  }
  
  // Get user info
  const userDoc = await db.collection('users').doc(userId).get();
  const user = userDoc.data()!;
  
  // Filter content
  const { filtered, wasCensored } = filterMessage(content);
  
  // Parse mentions
  const mentionRegex = /@(\w+)/g;
  const mentionMatches = filtered.match(mentionRegex) || [];
  const mentions: string[] = [];
  
  for (const match of mentionMatches) {
    const username = match.substring(1);
    const mentionedUser = await db.collection('users')
      .where('username', '==', username)
      .limit(1)
      .get();
    
    if (!mentionedUser.empty) {
      mentions.push(mentionedUser.docs[0].id);
    }
  }
  
  // Create message
  const messageRef = db.collection('chat_messages').doc();
  const message: ChatMessage = {
    id: messageRef.id,
    channelId,
    channelType: channel.type,
    senderId: userId,
    senderName: user.username,
    senderLevel: user.level || 1,
    senderTitle: user.equippedTitle,
    senderBadge: user.equippedBadge,
    senderAllianceTag: user.allianceTag,
    content: filtered,
    type,
    attachments,
    mentions,
    isDeleted: false,
    isHidden: false,
    reactions: {},
    createdAt: admin.firestore.Timestamp.now()
  };
  
  await db.runTransaction(async (transaction) => {
    transaction.set(messageRef, message);
    
    // Update channel
    transaction.update(channelDoc.ref, {
      messageCount: admin.firestore.FieldValue.increment(1),
      lastMessageAt: admin.firestore.Timestamp.now()
    });
  });
  
  // Send notifications for mentions
  for (const mentionedUserId of mentions) {
    if (mentionedUserId !== userId) {
      await sendNotification(mentionedUserId, {
        type: 'chat_mention',
        title: `${user.username} mentioned you`,
        body: filtered.substring(0, 100),
        data: { channelId, messageId: messageRef.id }
      });
    }
  }
  
  // Send notifications for DMs
  if (channel.type === 'direct' && channel.participants) {
    const otherParticipants = channel.participants.filter(p => p !== userId);
    for (const participantId of otherParticipants) {
      await sendNotification(participantId, {
        type: 'direct_message',
        title: `Message from ${user.username}`,
        body: filtered.substring(0, 100),
        data: { channelId, messageId: messageRef.id }
      });
    }
  }
  
  return { 
    success: true, 
    messageId: messageRef.id,
    wasCensored
  };
});

// ============================================================================
// Get Messages
// ============================================================================

export const getMessages = functions.https.onCall(async (data, context) => {
  const { channelId, limit = 50, before } = data as {
    channelId: string;
    limit?: number;
    before?: string;
  };
  
  let query = db.collection('chat_messages')
    .where('channelId', '==', channelId)
    .where('isDeleted', '==', false)
    .where('isHidden', '==', false)
    .orderBy('createdAt', 'desc')
    .limit(limit);
  
  if (before) {
    const beforeDoc = await db.collection('chat_messages').doc(before).get();
    if (beforeDoc.exists) {
      query = query.startAfter(beforeDoc);
    }
  }
  
  const messagesSnapshot = await query.get();
  const messages = messagesSnapshot.docs.map(doc => doc.data());
  
  return { messages: messages.reverse(), hasMore: messagesSnapshot.size === limit };
});

// ============================================================================
// Create Channel
// ============================================================================

export const createChannel = functions.https.onCall(async (data, context) => {
  if (!context.auth) {
    throw new functions.https.HttpsError('unauthenticated', 'Must be authenticated');
  }
  
  const { type, name, description, ownerId, participants } = data as {
    type: ChatChannel['type'];
    name?: string;
    description?: string;
    ownerId?: string;
    participants?: string[];
  };
  const userId = context.auth.uid;
  
  // Validate based on type
  if (type === 'direct') {
    if (!participants || participants.length !== 2) {
      throw new functions.https.HttpsError('invalid-argument', 'DM requires exactly 2 participants');
    }
    
    // Check if DM already exists
    const sortedParticipants = participants.sort();
    const existingDm = await db.collection('chat_channels')
      .where('type', '==', 'direct')
      .where('participants', '==', sortedParticipants)
      .limit(1)
      .get();
    
    if (!existingDm.empty) {
      return { channelId: existingDm.docs[0].id, existing: true };
    }
  }
  
  // Get user info for DM naming
  let channelName = name;
  if (type === 'direct' && participants) {
    const otherUserId = participants.find(p => p !== userId)!;
    const otherUser = await db.collection('users').doc(otherUserId).get();
    channelName = otherUser.exists ? otherUser.data()!.username : 'Direct Message';
  }
  
  const channelRef = db.collection('chat_channels').doc();
  const channel: ChatChannel = {
    id: channelRef.id,
    type,
    name: channelName || 'Chat',
    description,
    ownerId,
    participants: type === 'direct' ? participants?.sort() : undefined,
    isActive: true,
    isModerated: type !== 'direct',
    slowModeSeconds: type === 'global' ? 5 : 0,
    memberCount: type === 'direct' ? 2 : 1,
    messageCount: 0,
    createdAt: admin.firestore.Timestamp.now()
  };
  
  await channelRef.set(channel);
  
  // Add members
  if (type === 'direct' && participants) {
    for (const participantId of participants) {
      await db.collection('chat_members').doc(`${channelRef.id}_${participantId}`).set({
        userId: participantId,
        channelId: channelRef.id,
        joinedAt: admin.firestore.Timestamp.now(),
        lastReadAt: admin.firestore.Timestamp.now(),
        unreadCount: 0,
        role: 'member',
        isMuted: false,
        isBanned: false
      });
    }
  }
  
  return { channelId: channelRef.id, existing: false };
});

// ============================================================================
// Join Channel
// ============================================================================

export const joinChannel = functions.https.onCall(async (data, context) => {
  if (!context.auth) {
    throw new functions.https.HttpsError('unauthenticated', 'Must be authenticated');
  }
  
  const { channelId } = data as { channelId: string };
  const userId = context.auth.uid;
  
  const channelDoc = await db.collection('chat_channels').doc(channelId).get();
  if (!channelDoc.exists) {
    throw new functions.https.HttpsError('not-found', 'Channel not found');
  }
  
  const channel = channelDoc.data() as ChatChannel;
  
  if (channel.type === 'direct') {
    throw new functions.https.HttpsError('invalid-argument', 'Cannot join DM channels');
  }
  
  // Check if already member
  const memberDoc = await db.collection('chat_members').doc(`${channelId}_${userId}`).get();
  if (memberDoc.exists) {
    return { success: true, alreadyMember: true };
  }
  
  // Check alliance requirement for alliance chats
  if (channel.type === 'alliance' && channel.ownerId) {
    const userDoc = await db.collection('users').doc(userId).get();
    if (userDoc.data()?.allianceId !== channel.ownerId) {
      throw new functions.https.HttpsError('permission-denied', 'Must be in alliance to join');
    }
  }
  
  await db.runTransaction(async (transaction) => {
    transaction.set(db.collection('chat_members').doc(`${channelId}_${userId}`), {
      userId,
      channelId,
      joinedAt: admin.firestore.Timestamp.now(),
      lastReadAt: admin.firestore.Timestamp.now(),
      unreadCount: 0,
      role: 'member',
      isMuted: false,
      isBanned: false
    });
    
    transaction.update(channelDoc.ref, {
      memberCount: admin.firestore.FieldValue.increment(1)
    });
  });
  
  return { success: true, alreadyMember: false };
});

// ============================================================================
// Leave Channel
// ============================================================================

export const leaveChannel = functions.https.onCall(async (data, context) => {
  if (!context.auth) {
    throw new functions.https.HttpsError('unauthenticated', 'Must be authenticated');
  }
  
  const { channelId } = data as { channelId: string };
  const userId = context.auth.uid;
  
  const memberRef = db.collection('chat_members').doc(`${channelId}_${userId}`);
  const memberDoc = await memberRef.get();
  
  if (!memberDoc.exists) {
    return { success: true };
  }
  
  await db.runTransaction(async (transaction) => {
    transaction.delete(memberRef);
    transaction.update(db.collection('chat_channels').doc(channelId), {
      memberCount: admin.firestore.FieldValue.increment(-1)
    });
  });
  
  return { success: true };
});

// ============================================================================
// React to Message
// ============================================================================

export const reactToMessage = functions.https.onCall(async (data, context) => {
  if (!context.auth) {
    throw new functions.https.HttpsError('unauthenticated', 'Must be authenticated');
  }
  
  const { messageId, emoji } = data as { messageId: string; emoji: string };
  const userId = context.auth.uid;
  
  // Validate emoji (basic check)
  const allowedEmojis = ['👍', '❤️', '😂', '😮', '😢', '😡', '🔥', '💎', '⚔️', '🏰'];
  if (!allowedEmojis.includes(emoji)) {
    throw new functions.https.HttpsError('invalid-argument', 'Invalid emoji');
  }
  
  const messageRef = db.collection('chat_messages').doc(messageId);
  const messageDoc = await messageRef.get();
  
  if (!messageDoc.exists) {
    throw new functions.https.HttpsError('not-found', 'Message not found');
  }
  
  const message = messageDoc.data() as ChatMessage;
  const reactions = message.reactions || {};
  
  if (!reactions[emoji]) {
    reactions[emoji] = [];
  }
  
  const userIndex = reactions[emoji].indexOf(userId);
  
  if (userIndex >= 0) {
    // Remove reaction
    reactions[emoji].splice(userIndex, 1);
    if (reactions[emoji].length === 0) {
      delete reactions[emoji];
    }
  } else {
    // Add reaction
    reactions[emoji].push(userId);
  }
  
  await messageRef.update({ reactions });
  
  return { success: true, reactions };
});

// ============================================================================
// Report Message
// ============================================================================

export const reportMessage = functions.https.onCall(async (data, context) => {
  if (!context.auth) {
    throw new functions.https.HttpsError('unauthenticated', 'Must be authenticated');
  }
  
  const { messageId, reason, description } = data as {
    messageId: string;
    reason: ChatReport['reason'];
    description?: string;
  };
  const userId = context.auth.uid;
  
  const messageDoc = await db.collection('chat_messages').doc(messageId).get();
  if (!messageDoc.exists) {
    throw new functions.https.HttpsError('not-found', 'Message not found');
  }
  
  const message = messageDoc.data() as ChatMessage;
  
  // Get reporter info
  const userDoc = await db.collection('users').doc(userId).get();
  const user = userDoc.data()!;
  
  const reportRef = db.collection('chat_reports').doc();
  const report: ChatReport = {
    id: reportRef.id,
    messageId,
    channelId: message.channelId,
    reporterId: userId,
    reporterName: user.username,
    reason,
    description,
    status: 'pending',
    createdAt: admin.firestore.Timestamp.now()
  };
  
  await reportRef.set(report);
  
  functions.logger.info('Chat message reported', {
    reportId: reportRef.id,
    messageId,
    reason
  });
  
  return { success: true, reportId: reportRef.id };
});

// ============================================================================
// Moderation: Delete Message
// ============================================================================

export const deleteMessage = functions.https.onCall(async (data, context) => {
  if (!context.auth) {
    throw new functions.https.HttpsError('unauthenticated', 'Must be authenticated');
  }
  
  const { messageId, reason } = data as { messageId: string; reason?: string };
  const userId = context.auth.uid;
  
  const messageDoc = await db.collection('chat_messages').doc(messageId).get();
  if (!messageDoc.exists) {
    throw new functions.https.HttpsError('not-found', 'Message not found');
  }
  
  const message = messageDoc.data() as ChatMessage;
  
  // Check permission
  const isOwner = message.senderId === userId;
  
  if (!isOwner) {
    // Check if moderator
    const memberDoc = await db.collection('chat_members')
      .doc(`${message.channelId}_${userId}`)
      .get();
    
    if (!memberDoc.exists) {
      throw new functions.https.HttpsError('permission-denied', 'Not authorized');
    }
    
    const member = memberDoc.data() as ChatMember;
    if (member.role !== 'moderator' && member.role !== 'admin' && member.role !== 'owner') {
      throw new functions.https.HttpsError('permission-denied', 'Not authorized');
    }
  }
  
  await messageDoc.ref.update({
    isDeleted: true,
    deletedBy: userId,
    deletedAt: admin.firestore.Timestamp.now(),
    deleteReason: reason
  });
  
  return { success: true };
});

// ============================================================================
// Moderation: Mute User
// ============================================================================

export const muteUser = functions.https.onCall(async (data, context) => {
  if (!context.auth) {
    throw new functions.https.HttpsError('unauthenticated', 'Must be authenticated');
  }
  
  const { channelId, targetUserId, durationMinutes, reason } = data as {
    channelId: string;
    targetUserId: string;
    durationMinutes: number;
    reason?: string;
  };
  const userId = context.auth.uid;
  
  // Check permission
  const modMemberDoc = await db.collection('chat_members')
    .doc(`${channelId}_${userId}`)
    .get();
  
  if (!modMemberDoc.exists) {
    throw new functions.https.HttpsError('permission-denied', 'Not authorized');
  }
  
  const modMember = modMemberDoc.data() as ChatMember;
  if (modMember.role !== 'moderator' && modMember.role !== 'admin' && modMember.role !== 'owner') {
    throw new functions.https.HttpsError('permission-denied', 'Not authorized');
  }
  
  const muteUntil = new Date();
  muteUntil.setMinutes(muteUntil.getMinutes() + durationMinutes);
  
  await db.collection('chat_members').doc(`${channelId}_${targetUserId}`).update({
    isMuted: true,
    mutedUntil: admin.firestore.Timestamp.fromDate(muteUntil)
  });
  
  // Notify user
  await sendNotification(targetUserId, {
    type: 'chat_muted',
    title: 'Chat Muted',
    body: `You have been muted for ${durationMinutes} minutes. ${reason || ''}`,
    data: { channelId }
  });
  
  return { success: true, mutedUntil: muteUntil.toISOString() };
});

// ============================================================================
// Get User's Channels
// ============================================================================

export const getUserChannels = functions.https.onCall(async (data, context) => {
  if (!context.auth) {
    throw new functions.https.HttpsError('unauthenticated', 'Must be authenticated');
  }
  
  const userId = context.auth.uid;
  
  // Get global channel
  const globalChannel = await db.collection('chat_channels')
    .where('type', '==', 'global')
    .limit(1)
    .get();
  
  // Get user's memberships
  const membershipsSnapshot = await db.collection('chat_members')
    .where('userId', '==', userId)
    .get();
  
  const channelIds = membershipsSnapshot.docs.map(doc => doc.data().channelId);
  
  // Add global channel if not already included
  if (!globalChannel.empty && !channelIds.includes(globalChannel.docs[0].id)) {
    channelIds.unshift(globalChannel.docs[0].id);
  }
  
  // Get channel details
  const channels: Array<ChatChannel & { unreadCount: number }> = [];
  
  for (const channelId of channelIds) {
    const channelDoc = await db.collection('chat_channels').doc(channelId).get();
    if (channelDoc.exists) {
      const channel = channelDoc.data() as ChatChannel;
      const membership = membershipsSnapshot.docs.find(d => d.data().channelId === channelId);
      
      channels.push({
        ...channel,
        unreadCount: membership?.data().unreadCount || 0
      });
    }
  }
  
  // Sort by last message time
  channels.sort((a, b) => {
    const aTime = a.lastMessageAt?.toMillis() || 0;
    const bTime = b.lastMessageAt?.toMillis() || 0;
    return bTime - aTime;
  });
  
  return { channels };
});

// ============================================================================
// Initialize Global Chat (Admin)
// ============================================================================

export const initializeGlobalChat = functions.https.onCall(async (data, context) => {
  // Check if global chat exists
  const existing = await db.collection('chat_channels')
    .where('type', '==', 'global')
    .limit(1)
    .get();
  
  if (!existing.empty) {
    return { channelId: existing.docs[0].id, existing: true };
  }
  
  const channelRef = db.collection('chat_channels').doc('global');
  await channelRef.set({
    id: 'global',
    type: 'global',
    name: 'Global Chat',
    description: 'Chat with players worldwide',
    isActive: true,
    isModerated: true,
    slowModeSeconds: 3,
    memberCount: 0,
    messageCount: 0,
    createdAt: admin.firestore.Timestamp.now()
  });
  
  return { channelId: 'global', existing: false };
});
