// Stub classes to allow compilation without Firebase SDK
// Remove this file after importing Firebase SDK

#if !FIREBASE_ENABLED

namespace Firebase
{
    public enum DependencyStatus { Available }
    
    public class FirebaseApp
    {
        public static System.Threading.Tasks.Task<DependencyStatus> CheckAndFixDependenciesAsync()
        {
            return System.Threading.Tasks.Task.FromResult(DependencyStatus.Available);
        }
        public static FirebaseApp DefaultInstance => null;
    }
}

namespace Firebase.Auth
{
    public class FirebaseAuth
    {
        public static FirebaseAuth DefaultInstance => new FirebaseAuth();
        public FirebaseUser CurrentUser => null;
        public System.Threading.Tasks.Task<FirebaseUser> SignInAnonymouslyAsync() 
            => System.Threading.Tasks.Task.FromResult<FirebaseUser>(null);
        public System.Threading.Tasks.Task<FirebaseUser> SignInWithEmailAndPasswordAsync(string email, string password)
            => System.Threading.Tasks.Task.FromResult<FirebaseUser>(null);
        public System.Threading.Tasks.Task<FirebaseUser> CreateUserWithEmailAndPasswordAsync(string email, string password)
            => System.Threading.Tasks.Task.FromResult<FirebaseUser>(null);
        public void SignOut() { }
        public event System.EventHandler<System.EventArgs> StateChanged;
    }
    
    public class FirebaseUser
    {
        public string UserId => "stub_user";
        public string DisplayName => "Stub User";
        public string Email => "stub@example.com";
        public bool IsAnonymous => true;
    }
}

namespace Firebase.Extensions
{
    public static class TaskExtension
    {
        public static void ContinueWithOnMainThread<T>(this System.Threading.Tasks.Task<T> task, System.Action<System.Threading.Tasks.Task<T>> callback)
        {
            task.ContinueWith(t => callback(t));
        }
    }
}

namespace Firebase.Firestore
{
    public class FirebaseFirestore
    {
        public static FirebaseFirestore DefaultInstance => new FirebaseFirestore();
        public CollectionReference Collection(string path) => new CollectionReference();
    }
    
    public class CollectionReference : Query
    {
        public DocumentReference Document(string id) => new DocumentReference();
        public DocumentReference Document() => new DocumentReference();
    }
    
    public class DocumentReference
    {
        public string Id => System.Guid.NewGuid().ToString();
        public System.Threading.Tasks.Task SetAsync(object data) => System.Threading.Tasks.Task.CompletedTask;
        public System.Threading.Tasks.Task<DocumentSnapshot> GetSnapshotAsync() 
            => System.Threading.Tasks.Task.FromResult(new DocumentSnapshot());
        public System.Threading.Tasks.Task DeleteAsync() => System.Threading.Tasks.Task.CompletedTask;
        public System.Threading.Tasks.Task UpdateAsync(string field, object value) => System.Threading.Tasks.Task.CompletedTask;
    }
    
    public class Query
    {
        public Query WhereEqualTo(string field, object value) => this;
        public Query WhereLessThan(string field, object value) => this;
        public Query WhereGreaterThan(string field, object value) => this;
        public Query OrderBy(string field) => this;
        public Query Limit(int limit) => this;
        public System.Threading.Tasks.Task<QuerySnapshot> GetSnapshotAsync()
            => System.Threading.Tasks.Task.FromResult(new QuerySnapshot());
    }
    
    public class QuerySnapshot
    {
        public DocumentSnapshot[] Documents => new DocumentSnapshot[0];
        public int Count => 0;
    }
    
    public class DocumentSnapshot
    {
        public bool Exists => false;
        public string Id => "";
        public T ConvertTo<T>() where T : new() => new T();
        public T GetValue<T>(string field) => default(T);
        public bool TryGetValue<T>(string field, out T value)
        {
            value = default(T);
            return false;
        }
    }
    
    [System.AttributeUsage(System.AttributeTargets.Property)]
    public class FirestorePropertyAttribute : System.Attribute 
    {
        public FirestorePropertyAttribute(string name = null) { }
    }
    
    [System.AttributeUsage(System.AttributeTargets.Class)]
    public class FirestoreDataAttribute : System.Attribute { }

    public enum ServerTimestampBehavior { None, Estimate, Previous }
    public static class FieldValue
    {
        public static object ServerTimestamp => System.DateTime.UtcNow;
        public static object Delete => null;
        public static object ArrayUnion(params object[] values) => values;
        public static object ArrayRemove(params object[] values) => values;
        public static object Increment(long value) => value;
    }

    public class Timestamp
    {
        public System.DateTime ToDateTime() => System.DateTime.UtcNow;
        public static Timestamp FromDateTime(System.DateTime dt) => new Timestamp();
    }
}

namespace Firebase.Functions
{
    public class FirebaseFunctions
    {
        public static FirebaseFunctions DefaultInstance => new FirebaseFunctions();
        public HttpsCallableReference GetHttpsCallable(string name) => new HttpsCallableReference();
    }

    public class HttpsCallableReference
    {
        public System.Threading.Tasks.Task<HttpsCallableResult> CallAsync(object data = null)
            => System.Threading.Tasks.Task.FromResult(new HttpsCallableResult());
    }

    public class HttpsCallableResult
    {
        public object Data => "{}";
    }
}

namespace Firebase.Analytics
{
    public static class FirebaseAnalytics
    {
        public static string ParameterLevel => "level";
        public static string ParameterValue => "value";
        public static void LogEvent(string name, params Parameter[] parameters) { }
        public static void SetUserId(string userId) { }
        public static void SetUserProperty(string name, string value) { }
    }

    public class Parameter
    {
        public Parameter(string name, string value) { }
        public Parameter(string name, long value) { }
        public Parameter(string name, double value) { }
    }
}

namespace Firebase.Crashlytics
{
    public static class Crashlytics
    {
        public static bool IsCrashlyticsCollectionEnabled { get; set; }
        public static void Log(string message) { }
        public static void SetCustomKey(string key, string value) { }
        public static void SetCustomKey(string key, int value) { }
        public static void SetCustomKey(string key, float value) { }
        public static void SetCustomKey(string key, bool value) { }
        public static void SetUserId(string userId) { }
        public static void RecordException(System.Exception ex) { }
    }
}

// Note: FirebaseManager stub removed - the real FirebaseManager in Backend/ handles both
// FIREBASE_ENABLED and non-FIREBASE_ENABLED cases internally

#endif
