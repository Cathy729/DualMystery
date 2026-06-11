using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DualMystery
{
    public static class PhoneManager
    {
        private static bool isRinging;
        private static bool isCallActive;
        private static string caller;
        private static string callee;
        private static DateTime ringStartTime;

        public static bool IsRinging => isRinging;
        public static string Caller => caller;
        public static string Callee => callee;

        public static event Action<string, string> OnCallRequest;
        public static event Action OnCallEstablished;
        public static event Action OnCallEnded;
        public static event Action<string> OnRingTimeout;

        public static void RequestCall(string from)
        {
            if (!isRinging)
            {
                caller = from;
                callee = (from == "A" ? "B" : "A");
                isRinging = true;
                ringStartTime = DateTime.Now;
                OnCallRequest?.Invoke(caller, callee);
            }
        }

        public static void AcceptCall(string by)
        {
            if (isRinging && by == callee)
            {
                isRinging = false;
                isCallActive = true;
                OnCallEstablished?.Invoke();
                EndRing(true);
            }
        }

        public static void DeclineCall(string by)
        {
            if (isRinging && by == callee)
            {
                EndRing(false);
            }
        }

        public static void TimeoutCall()
        {
            if (isRinging && (DateTime.Now - ringStartTime).TotalSeconds >= 3)
            {
                OnRingTimeout?.Invoke(caller);
                EndRing(false);
            }
        }

        public static void HangUp(string by)
        {
            if (!isRinging && !isCallActive) return;
            isCallActive = false;
            isRinging = false;
            OnCallEnded?.Invoke();
        }

        private static void EndRing(bool success)
        {
            isRinging = false;
            caller = null;
            callee = null;
        }
    }
}
