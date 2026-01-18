mergeInto(LibraryManager.library, {
    
    // =====================================================
    // Unity → JavaScript Communication
    // =====================================================

    // Notify web page that Unity game is ready
    JS_SendGameReady: function() {
        if (window.onUnityGameReady) {
            window.onUnityGameReady();
        }
        // Dispatch custom event for flexibility
        window.dispatchEvent(new CustomEvent('unityGameReady'));
        console.log('[Unity→JS] Game ready');
    },
    
    // Send territory selection to web page
    JS_SendTerritorySelected: function(territoryIdPtr) {
        var territoryId = UTF8ToString(territoryIdPtr);
        if (window.onTerritorySelected) {
            window.onTerritorySelected(territoryId);
        }
        window.dispatchEvent(new CustomEvent('unityTerritorySelected', { 
            detail: { territoryId: territoryId } 
        }));
        console.log('[Unity→JS] Territory selected:', territoryId);
    },
    
    // Send player stats to web page
    JS_SendPlayerStats: function(statsJsonPtr) {
        var statsJson = UTF8ToString(statsJsonPtr);
        var stats = JSON.parse(statsJson);
        if (window.onPlayerStats) {
            window.onPlayerStats(stats);
        }
        window.dispatchEvent(new CustomEvent('unityPlayerStats', { 
            detail: stats 
        }));
        console.log('[Unity→JS] Player stats:', stats);
    },
    
    // Send notification to browser
    JS_SendNotification: function(titlePtr, messagePtr) {
        var title = UTF8ToString(titlePtr);
        var message = UTF8ToString(messagePtr);
        
        // Try browser notification API
        if ('Notification' in window && Notification.permission === 'granted') {
            new Notification(title, { body: message });
        }
        
        // Also dispatch event for custom handling
        if (window.onUnityNotification) {
            window.onUnityNotification(title, message);
        }
        window.dispatchEvent(new CustomEvent('unityNotification', { 
            detail: { title: title, message: message } 
        }));
        console.log('[Unity→JS] Notification:', title, '-', message);
    },

    // =====================================================
    // Firebase Integration (via JS SDK in index.html)
    // =====================================================

    // Check if Firebase is ready
    JS_IsFirebaseReady: function() {
        return window.firebaseData && window.firebaseData.isReady ? 1 : 0;
    },

    // Get current user ID
    JS_GetFirebaseUserId: function() {
        var userId = '';
        if (window.firebaseGetUserId) {
            userId = window.firebaseGetUserId() || '';
        }
        var bufferSize = lengthBytesUTF8(userId) + 1;
        var buffer = _malloc(bufferSize);
        stringToUTF8(userId, buffer, bufferSize);
        return buffer;
    },

    // Async: Get all territories (returns immediately, sends result via callback)
    JS_GetAllTerritories: function(gameObjectNamePtr, callbackMethodPtr) {
        var gameObjectName = UTF8ToString(gameObjectNamePtr);
        var callbackMethod = UTF8ToString(callbackMethodPtr);
        
        console.log('[Unity→JS] Requesting all territories...');
        
        if (window.firebaseGetTerritories) {
            window.firebaseGetTerritories().then(function(jsonResult) {
                console.log('[JS→Unity] Sending territories to', gameObjectName, '.', callbackMethod);
                if (window.unityInstance) {
                    window.unityInstance.SendMessage(gameObjectName, callbackMethod, jsonResult);
                }
            }).catch(function(error) {
                console.error('[Firebase] Error getting territories:', error);
                if (window.unityInstance) {
                    window.unityInstance.SendMessage(gameObjectName, callbackMethod, '[]');
                }
            });
        } else {
            console.warn('[Firebase] firebaseGetTerritories not available yet');
            // Return empty array if Firebase not ready
            setTimeout(function() {
                if (window.unityInstance) {
                    window.unityInstance.SendMessage(gameObjectName, callbackMethod, '[]');
                }
            }, 100);
        }
    },

    // Async: Get territories in area
    JS_GetTerritoriesInArea: function(north, south, east, west, maxResults, gameObjectNamePtr, callbackMethodPtr) {
        var gameObjectName = UTF8ToString(gameObjectNamePtr);
        var callbackMethod = UTF8ToString(callbackMethodPtr);
        
        console.log('[Unity→JS] Requesting territories in area...');
        
        if (window.firebaseGetTerritoriesInArea) {
            window.firebaseGetTerritoriesInArea(north, south, east, west, maxResults).then(function(jsonResult) {
                console.log('[JS→Unity] Sending area territories');
                if (window.unityInstance) {
                    window.unityInstance.SendMessage(gameObjectName, callbackMethod, jsonResult);
                }
            }).catch(function(error) {
                console.error('[Firebase] Error:', error);
            });
        }
    },

    // Async: Get single territory
    JS_GetTerritory: function(territoryIdPtr, gameObjectNamePtr, callbackMethodPtr) {
        var territoryId = UTF8ToString(territoryIdPtr);
        var gameObjectName = UTF8ToString(gameObjectNamePtr);
        var callbackMethod = UTF8ToString(callbackMethodPtr);
        
        if (window.firebaseGetTerritory) {
            window.firebaseGetTerritory(territoryId).then(function(jsonResult) {
                if (window.unityInstance) {
                    window.unityInstance.SendMessage(gameObjectName, callbackMethod, jsonResult);
                }
            });
        }
    },

    // Get cached territories synchronously (from pre-loaded data)
    JS_GetCachedTerritories: function() {
        var json = '[]';
        if (window.firebaseData && window.firebaseData.territories) {
            json = JSON.stringify(window.firebaseData.territories);
        }
        var bufferSize = lengthBytesUTF8(json) + 1;
        var buffer = _malloc(bufferSize);
        stringToUTF8(json, buffer, bufferSize);
        return buffer;
    },

    // Subscribe to real-time territory updates
    JS_SubscribeToTerritories: function(gameObjectNamePtr, callbackMethodPtr) {
        var gameObjectName = UTF8ToString(gameObjectNamePtr);
        var callbackMethod = UTF8ToString(callbackMethodPtr);
        
        if (window.firebaseSubscribeTerritories) {
            window.firebaseSubscribeTerritories(function(changes) {
                if (window.unityInstance) {
                    window.unityInstance.SendMessage(gameObjectName, callbackMethod, JSON.stringify(changes));
                }
            });
            console.log('[Unity→JS] Subscribed to territory updates');
        }
    },

    // =====================================================
    // Auth & Storage
    // =====================================================
    
    // Get auth token from browser storage/state
    JS_GetAuthToken: function() {
        var token = '';
        
        // Try to get from Firebase auth if available
        if (window.firebaseAuth && window.firebaseAuth.currentUser) {
            // This is async in reality, but for sync call we use stored token
            token = window.cachedAuthToken || '';
        }
        
        // Or from localStorage
        if (!token) {
            token = localStorage.getItem('apex_auth_token') || '';
        }
        
        // Return as Unity string
        var bufferSize = lengthBytesUTF8(token) + 1;
        var buffer = _malloc(bufferSize);
        stringToUTF8(token, buffer, bufferSize);
        return buffer;
    },

    // =====================================================
    // Browser Integration
    // =====================================================
    
    // Request fullscreen
    JS_RequestFullscreen: function() {
        var canvas = document.querySelector('#unity-canvas');
        if (canvas) {
            if (canvas.requestFullscreen) {
                canvas.requestFullscreen();
            } else if (canvas.webkitRequestFullscreen) {
                canvas.webkitRequestFullscreen();
            } else if (canvas.mozRequestFullScreen) {
                canvas.mozRequestFullScreen();
            }
        }
    },

    // Log to browser console
    JS_Log: function(messagePtr) {
        var message = UTF8ToString(messagePtr);
        console.log('[Unity]', message);
    },

    // Log error to browser console
    JS_LogError: function(messagePtr) {
        var message = UTF8ToString(messagePtr);
        console.error('[Unity Error]', message);
    }
});
