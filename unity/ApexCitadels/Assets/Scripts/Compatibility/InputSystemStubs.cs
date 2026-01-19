// ============================================================================
// InputSystemStubs.cs - Compatibility stubs for when Input System package is not installed
// Remove this file once the Input System package is properly installed in Unity
// ============================================================================

#if !ENABLE_INPUT_SYSTEM
using System;
using UnityEngine;

namespace UnityEngine.InputSystem
{
    /// <summary>
    /// Stub for InputAction when Input System package is not available
    /// </summary>
    public class InputAction
    {
        public string name;
        public bool enabled;
        
        public event Action<CallbackContext> performed;
        public event Action<CallbackContext> canceled;
        public event Action<CallbackContext> started;
        
        public InputAction(string name = "", string binding = "")
        {
            this.name = name;
        }
        
        public void Enable() { enabled = true; }
        public void Disable() { enabled = false; }
        
        public T ReadValue<T>() where T : struct
        {
            if (typeof(T) == typeof(Vector2))
                return (T)(object)Vector2.zero;
            if (typeof(T) == typeof(float))
                return (T)(object)0f;
            return default;
        }
        
        public bool WasPressedThisFrame() => false;
        public bool WasReleasedThisFrame() => false;
        public bool IsPressed() => false;
        
        public CompositeSyntax AddCompositeBinding(string composite)
        {
            return new CompositeSyntax();
        }
        
        public class CompositeSyntax
        {
            public CompositeSyntax With(string part, string binding) => this;
        }
        
        public struct CallbackContext
        {
            public T ReadValue<T>() where T : struct
            {
                if (typeof(T) == typeof(Vector2))
                    return (T)(object)Vector2.zero;
                if (typeof(T) == typeof(float))
                    return (T)(object)0f;
                return default;
            }
            
            public bool performed => false;
            public bool canceled => false;
            public bool started => false;
        }
    }
    
    /// <summary>
    /// Stub for InputActionMap
    /// </summary>
    public class InputActionMap
    {
        public string name;
        
        public InputActionMap(string name = "")
        {
            this.name = name;
        }
        
        public InputAction AddAction(string name, string binding = "", InputActionType type = InputActionType.Value)
        {
            return new InputAction(name, binding);
        }
        
        public void Enable() { }
        public void Disable() { }
    }
    
    public enum InputActionType
    {
        Value,
        Button,
        PassThrough
    }
}
#endif
