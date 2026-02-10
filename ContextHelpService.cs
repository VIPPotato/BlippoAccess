using NobleRobot;
using UnityEngine.EventSystems;

namespace BlippoAccess
{
    /// <summary>
    /// Builds concise control help text for the current screen context.
    /// </summary>
    internal static class ContextHelpService
    {
        /// <summary>
        /// Builds a context-aware controls hint.
        /// </summary>
        public static string BuildAnnouncement()
        {
            if (GameManager.instance == null)
            {
                return Loc.Get("help_general");
            }

            switch (GameManager.currentSystemScreen)
            {
                case SystemScreen.Type.PROGRAM_GUIDE:
                    return Loc.Get("help_program_guide");
                case SystemScreen.Type.BROADCAST_DISPLAY:
                    return Loc.Get("help_broadcast");
                case SystemScreen.Type.CONTROL_MENU:
                    return BuildControlMenuHelp();
                case SystemScreen.Type.MESSAGES:
                    return BuildMessagesHelp();
                case SystemScreen.Type.FEMTOFAX:
                    return Loc.Get("help_femtofax");
                case SystemScreen.Type.TUNER_CALIBRATION:
                    return Loc.Get("help_tuner_calibration");
                case SystemScreen.Type.PACKETTE_LOAD:
                    return Loc.Get("help_packette_load");
                case SystemScreen.Type.CREDITS:
                    return Loc.Get("help_credits");
                case SystemScreen.Type.SIGNAL_LOSS:
                    return Loc.Get("help_signal_loss");
                default:
                    return Loc.Get("help_general");
            }
        }

        private static string BuildMessagesHelp()
        {
            if (Bookshelf.instance == null || !Bookshelf.instance.systemScreens.ContainsKey(SystemScreen.Type.MESSAGES))
            {
                return Loc.Get("help_messages_list");
            }

            var messages = Bookshelf.instance.systemScreens[SystemScreen.Type.MESSAGES] as Messages;
            if (messages == null || messages.currentSubmenu != messages.messageSubmenu)
            {
                return Loc.Get("help_messages_list");
            }

            return Loc.Get("help_messages_content");
        }

        private static string BuildControlMenuHelp()
        {
            if (Bookshelf.instance == null || !Bookshelf.instance.systemScreens.ContainsKey(SystemScreen.Type.CONTROL_MENU))
            {
                return Loc.Get("help_control_menu");
            }

            var menu = Bookshelf.instance.systemScreens[SystemScreen.Type.CONTROL_MENU] as ControlMenu;
            if (menu == null || menu.currentSubmenu == null)
            {
                return Loc.Get("help_control_menu");
            }

            var selectedObject = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            if (FactoryResetHandler.IsFactoryResetContext(menu.currentSubmenu, selectedObject))
            {
                return Loc.Get("help_control_menu_factory_reset");
            }

            if (DataManagerContentHandler.IsDataSubmenuContext(menu.currentSubmenu))
            {
                return Loc.Get("help_control_menu_data_manager");
            }

            return Loc.Get("help_control_menu");
        }
    }
}
