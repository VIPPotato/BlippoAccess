namespace BlippoAccess
{
    /// <summary>
    /// Builds inbox summary announcements from viewer message state.
    /// </summary>
    internal static class InboxSummaryService
    {
        /// <summary>
        /// Attempts to build a messages summary announcement.
        /// </summary>
        /// <param name="announcement">Summary announcement when available.</param>
        /// <returns>True when viewer data is available; otherwise false.</returns>
        public static bool TryBuildAnnouncement(out string announcement)
        {
            announcement = string.Empty;
            if (ViewerData_v1.current == null || ViewerData_v1.current.messagesInInbox == null)
            {
                return false;
            }

            var total = ViewerData_v1.current.messagesInInbox.Count;
            if (total <= 0)
            {
                announcement = Loc.Get("messages_summary_empty");
                return true;
            }

            var unread = 0;
            foreach (var messageStatus in ViewerData_v1.current.messagesInInbox.Values)
            {
                if (messageStatus != null && !messageStatus.read)
                {
                    unread++;
                }
            }

            announcement = Loc.Get("messages_summary_counts", total, unread);
            return true;
        }
    }
}
