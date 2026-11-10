using System;
using System.Collections.Generic;

namespace SpacePartyGame
{
    /// <summary>Every menu or overlay screen the game can show.</summary>
    public enum MenuId
    {
        None,
        StartMenu,
        Join,
        Pause,
        Settings,
        Winner,
        NameEntry,
        Leaderboard,
    }

    /// <summary>
    /// The open menus, newest on top. Only the top one is shown, so opening Settings from Pause hides Pause
    /// completely, and closing Settings shows Pause again.
    /// </summary>
    public sealed class MenuStack
    {
        private readonly List<MenuId> _open = new List<MenuId>();

        /// <summary>Raised after every change.</summary>
        public event Action Changed;

        /// <summary>The menu shown now, or None.</summary>
        public MenuId Top => _open.Count > 0 ? _open[_open.Count - 1] : MenuId.None;

        /// <summary>Whether any menu is open, which also means the game behind it is darkened.</summary>
        public bool IsAnyOpen => _open.Count > 0;

        /// <summary>Whether a menu is the one on show.</summary>
        public bool IsShown(MenuId menu) => menu != MenuId.None && Top == menu;

        /// <summary>Opens a menu over the current one.</summary>
        public void Push(MenuId menu)
        {
            if (menu == MenuId.None)
            {
                return;
            }

            _open.Add(menu);
            Changed?.Invoke();
        }

        /// <summary>Closes the top menu, showing the one underneath.</summary>
        public void Pop()
        {
            if (_open.Count == 0)
            {
                return;
            }

            _open.RemoveAt(_open.Count - 1);
            Changed?.Invoke();
        }

        /// <summary>Closes every menu, then opens one, or none.</summary>
        public void ReplaceWith(MenuId menu)
        {
            _open.Clear();

            if (menu != MenuId.None)
            {
                _open.Add(menu);
            }

            Changed?.Invoke();
        }
    }

    /// <summary>Which menu each stage of the game opens.</summary>
    public static class MenuRules
    {
        /// <summary>The menu a stage opens, or None while the game itself is on screen.</summary>
        public static MenuId MenuFor(GameState state)
        {
            switch (state)
            {
                case GameState.StartMenu:
                    return MenuId.StartMenu;
                case GameState.Join:
                    return MenuId.Join;
                case GameState.Paused:
                    return MenuId.Pause;
                case GameState.RoundEnd:
                    return MenuId.Winner;
                case GameState.NameEntry:
                    return MenuId.NameEntry;
                case GameState.Leaderboard:
                    return MenuId.Leaderboard;
                default:
                    return MenuId.None;
            }
        }
    }
}
