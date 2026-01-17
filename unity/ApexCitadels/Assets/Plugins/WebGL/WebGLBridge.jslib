mergeInto(LibraryManager.library, {
    
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
    }
});
