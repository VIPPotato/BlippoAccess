using System.Globalization;

namespace BlippoAccess
{
    /// <summary>
    /// Builds concise signal diagnostics announcements.
    /// </summary>
    internal static class SignalStatusService
    {
        /// <summary>
        /// Attempts to build current signal diagnostics text.
        /// </summary>
        /// <param name="announcement">Diagnostics announcement when available.</param>
        /// <returns>True when viewer signal data is available; otherwise false.</returns>
        public static bool TryBuildAnnouncement(out string announcement)
        {
            announcement = string.Empty;
            if (ViewerData_v1.current == null)
            {
                return false;
            }

            var eVrp = ((int)ViewerData_v1.current.eVRP).ToString("#,##0", CultureInfo.InvariantCulture);
            var eArcp = ((int)ViewerData_v1.current.eARCP).ToString("#,##0", CultureInfo.InvariantCulture);
            var modalState = Loc.Get(GetSignalModalStateKey(SignalLoss.currentTypeUp));

            announcement = Loc.Get("signal_status_summary", modalState, eVrp, eArcp);
            return true;
        }

        private static string GetSignalModalStateKey(SignalLoss.ModalType modalType)
        {
            switch (modalType)
            {
                case SignalLoss.ModalType.WARNING:
                    return "signal_status_modal_warning";
                case SignalLoss.ModalType.PROMPT:
                    return "signal_status_modal_prompt";
                case SignalLoss.ModalType.FORCE:
                    return "signal_status_modal_force";
                default:
                    return "signal_status_modal_none";
            }
        }
    }
}
