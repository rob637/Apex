using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using ApexCitadels.Core;

#if FIREBASE_ENABLED
using Firebase;
using Firebase.Firestore;
using Firebase.Extensions;
#endif

namespace ApexCitadels.Demo
{
    /// <summary>
    /// Simple demo to test Firebase connection and Firestore read/write
    /// </summary>
    public class FirebaseTestDemo : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private Button testWriteButton;
        [SerializeField] private Button testReadButton;

        private bool _firebaseReady = false;

#if FIREBASE_ENABLED
        private FirebaseFirestore _db;
#endif

        private void Start()
        {
            SetStatus("Initializing Firebase...");
            
            if (testWriteButton != null)
                testWriteButton.onClick.AddListener(OnTestWriteClick);
            if (testReadButton != null)
                testReadButton.onClick.AddListener(OnTestReadClick);

            InitializeFirebase();
        }

        private void InitializeFirebase()
        {
#if FIREBASE_ENABLED
            FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
            {
                if (task.Result == DependencyStatus.Available)
                {
                    _db = FirebaseFirestore.DefaultInstance;
                    _firebaseReady = true;
                    SetStatus("[OK] Firebase Ready!\n\nTap 'Write Test' to save data\nTap 'Read Test' to load data");
                    ApexLogger.Log("Firebase initialized successfully!", LogCategory.General);
                }
                else
                {
                    SetStatus($"[X] Firebase Error: {task.Result}");
                    ApexLogger.LogError($"Could not resolve Firebase dependencies: {task.Result}", LogCategory.General);
                }
            });
#else
            SetStatus("Firebase not enabled.\n\nAdd FIREBASE_ENABLED to\nPlayer Settings -> Scripting Define Symbols");
            ApexLogger.LogWarning("Firebase is not enabled. Add FIREBASE_ENABLED to Scripting Define Symbols.", ApexLogger.LogCategory.General);
#endif
        }

        private void OnTestWriteClick()
        {
#if FIREBASE_ENABLED
            if (!_firebaseReady)
            {
                SetStatus("Firebase not ready yet...");
                return;
            }

            SetStatus("Writing test data...");

            var testData = new Dictionary<string, object>
            {
                { "message", "Hello from Apex Citadels!" },
                { "timestamp", FieldValue.ServerTimestamp },
                { "testNumber", Random.Range(1, 1000) },
                { "platform", Application.platform.ToString() }
            };

            _db.Collection("test").Document("unity-test").SetAsync(testData).ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted && !task.IsFaulted)
                {
                    SetStatus("[OK] Write successful!\n\nData saved to Firestore.\nCheck Firebase Console to see it.\n\nTap 'Read Test' to read it back.");
                    ApexLogger.Log("Test data written to Firestore!", LogCategory.General);
                }
                else
                {
                    SetStatus($"[X] Write failed:\n{task.Exception?.Message}");
                    ApexLogger.LogError($"Failed to write test data: {task.Exception}", LogCategory.General);
                }
            });
#else
            SetStatus("Firebase not enabled.");
#endif
        }

        private void OnTestReadClick()
        {
#if FIREBASE_ENABLED
            if (!_firebaseReady)
            {
                SetStatus("Firebase not ready yet...");
                return;
            }

            SetStatus("Reading test data...");

            _db.Collection("test").Document("unity-test").GetSnapshotAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted && !task.IsFaulted)
                {
                    var snapshot = task.Result;
                    if (snapshot.Exists)
                    {
                        var data = snapshot.ToDictionary();
                        string message = data.ContainsKey("message") ? data["message"].ToString() : "N/A";
                        string testNum = data.ContainsKey("testNumber") ? data["testNumber"].ToString() : "N/A";
                        
                        SetStatus($"[OK] Read successful!\n\nMessage: {message}\nTest #: {testNum}\n\n Firebase is working!");
                        ApexLogger.Log($"Read from Firestore: {message}", LogCategory.General);
                    }
                    else
                    {
                        SetStatus("No data found.\nTap 'Write Test' first.");
                    }
                }
                else
                {
                    SetStatus($"[X] Read failed:\n{task.Exception?.Message}");
                    ApexLogger.LogError($"Failed to read test data: {task.Exception}", LogCategory.General);
                }
            });
#else
            SetStatus("Firebase not enabled.");
#endif
        }

        private void SetStatus(string message)
        {
            if (statusText != null)
                statusText.text = message;
            ApexLogger.Log(message, ApexLogger.LogCategory.General);
        }
    }
}
