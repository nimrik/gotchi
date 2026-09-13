using System;
using UnityEngine;

namespace Gotchi.Persistence
{
    public struct AuthResult
    {
        public bool Success;
        public string Message;
        public string SessionToken;
    }

    public interface IAuthService
    {
        void SignUp(string email, string password, Action<AuthResult> onComplete);
        void SignIn(string email, string password, Action<AuthResult> onComplete);
    }

    // MVP stand-in for Supabase Auth. It validates the shape of the input, issues a fake session and
    // never stores the password anywhere.
    public class MockAuthService : IAuthService
    {
        public void SignUp(string email, string password, Action<AuthResult> onComplete)
        {
            string problem = Validate(email, password);
            if (problem != null) { onComplete?.Invoke(new AuthResult { Success = false, Message = problem }); return; }
            Debug.Log($"[MockAuth] Signed up {email}");
            onComplete?.Invoke(new AuthResult { Success = true, Message = "OK", SessionToken = Guid.NewGuid().ToString("N") });
        }

        public void SignIn(string email, string password, Action<AuthResult> onComplete) => SignUp(email, password, onComplete);

        private static string Validate(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email) || !email.Contains("@") || !email.Contains(".")) return "Please enter a valid email.";
            if (string.IsNullOrEmpty(password) || password.Length < 6) return "Password needs at least 6 characters.";
            return null;
        }
    }
}
