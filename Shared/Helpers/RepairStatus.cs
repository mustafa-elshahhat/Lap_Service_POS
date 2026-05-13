using System.Collections.Generic;

namespace AlJohary.ServiceHub.Shared.Helpers
{
    public static class RepairStatus
    {
        public const string Received     = "received";
        public const string Inspection   = "inspection";
        public const string WaitingParts = "waiting_parts";
        public const string InProgress   = "in_progress";
        public const string Completed    = "completed";
        public const string Delivered    = "delivered";
        public const string Cancelled    = "cancelled";

        public static readonly string[] ForwardFlow = { Received, Inspection, WaitingParts, InProgress, Completed, Delivered };

        public static bool IsFinal(string status) =>
            status == Delivered || status == Cancelled;

        public static bool CanTransitionTo(string current, string next)
        {
            if (current == Cancelled || current == Delivered) return false;
            if (next == Cancelled) return true;
            int ci = System.Array.IndexOf(ForwardFlow, current);
            int ni = System.Array.IndexOf(ForwardFlow, next);
            return ci >= 0 && ni > ci;
        }

        public static string ToArabic(string status) => status switch
        {
            Received     => "مستلم",
            Inspection   => "قيد الفحص",
            WaitingParts => "انتظار قطع",
            InProgress   => "جاري الإصلاح",
            Completed    => "مكتمل",
            Delivered    => "تم التسليم",
            Cancelled    => "ملغي",
            _            => status
        };

        public static List<string> GetAll() => new List<string>
            { Received, Inspection, WaitingParts, InProgress, Completed, Delivered, Cancelled };

        public static List<string> GetActive() => new List<string>
            { Received, Inspection, WaitingParts, InProgress, Completed };

        public static string GetColorKey(string status) => status switch
        {
            Received     => "Primary",
            Inspection   => "Info",
            WaitingParts => "Warning",
            InProgress   => "Warning",
            Completed    => "Success",
            Delivered    => "Success",
            Cancelled    => "Danger",
            _            => "Data"
        };
    }
}
