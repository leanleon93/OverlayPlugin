﻿using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace RainbowMage.OverlayPlugin
{
    public sealed class KeyboardHook : NativeWindow, IDisposable
    {
        private Dictionary<int, HotKeyInfo> _hotkeys = new Dictionary<int, HotKeyInfo>();
        private ILogger _logger;

        /// <summary>
        /// Overridden to get the notifications.
        /// </summary>
        /// <param name="m"></param>
        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            // check if we got a hot key pressed.
            if (m.Msg == NativeMethods.WM_HOTKEY && _hotkeys.TryGetValue((int)m.LParam, out HotKeyInfo info))
            {
                foreach (var cb in info.Callbacks)
                {
                    cb();
                }
            }
        }

        public KeyboardHook(TinyIoCContainer container)
        {
            // create the handle for the window.
            this.CreateHandle(new CreateParams());

            _logger = container.Resolve<ILogger>();
        }

        /// <summary>
        /// Registers a hot key in the system.
        /// </summary>
        /// <param name="modifier">The modifiers that are associated with the hot key.</param>
        /// <param name="key">The key itself that is associated with the hot key.</param>
        public void RegisterHotKey(ModifierKeys modifier, Keys key, Action callback)
        {
            var lookupKey = (int)modifier | ((int)key << 16);
            if (!_hotkeys.ContainsKey(lookupKey))
            {
                _hotkeys[lookupKey] = new HotKeyInfo();

                // register the hot key.
                if (!NativeMethods.RegisterHotKey(Handle, _hotkeys[lookupKey].Id, (uint)modifier, (uint)key))
                {
                    _hotkeys.Remove(lookupKey);
                    throw new InvalidOperationException("Couldn’t register the hot key.");
                }
            }

            _hotkeys[lookupKey].Callbacks.Add(callback);
        }

        public void UnregisterHotKey(ModifierKeys modifier, Keys key, Action callback)
        {
            var lookupKey = (int)modifier | ((int)key << 16);
            if (_hotkeys.TryGetValue(lookupKey, out HotKeyInfo info))
            {
                info.Callbacks.Remove(callback);

                if (info.Callbacks.Count < 1)
                {
                    if (NativeMethods.UnregisterHotKey(Handle, info.Id))
                    {
                        _hotkeys.Remove(lookupKey);
                    }
                }
            }
        }

        public void UnregisterHotKey(Action callback)
        {
            var toRemove = new List<int>();

            foreach (var pair in _hotkeys)
            {
                if (pair.Value.Callbacks.Contains(callback))
                {
                    pair.Value.Callbacks.Remove(callback);
                    if (pair.Value.Callbacks.Count < 1)
                    {
                        if (NativeMethods.UnregisterHotKey(Handle, pair.Value.Id))
                        {
                            toRemove.Add(pair.Key);
                        }
                    }
                }
            }

            foreach (var key in toRemove)
            {
                _hotkeys.Remove(key);
            }
        }

        public void DisableHotKeys()
        {
            foreach (var pair in _hotkeys)
            {
                if (!NativeMethods.UnregisterHotKey(Handle, pair.Value.Id))
                {
                    _logger.Log(LogLevel.Error, Resources.UnregisterHotkeyError, pair.Key);
                }
            }
        }

        public void EnableHotKeys()
        {
            foreach (var pair in _hotkeys)
            {
                uint modifier = (uint)pair.Key & 0xFFFF;
                uint key = (uint)(pair.Key >> 16) & 0xFFFF;

                if (!NativeMethods.RegisterHotKey(Handle, pair.Value.Id, modifier, key))
                {
                    _logger.Log(LogLevel.Error, Resources.RegisterHotkeyError, modifier, key);
                }
            }
        }

        #region IDisposable Members

        public void Dispose()
        {
            // unregister all the registered hot keys.
            foreach (var info in _hotkeys)
            {
                NativeMethods.UnregisterHotKey(Handle, info.Value.Id);
            }

            // dispose the native window.
            this.DestroyHandle();
        }

        #endregion

        private class HotKeyInfo
        {
            public List<Action> Callbacks { get; private set; }
            public int Id { get; private set; }

            private static int _idCounter = 0;

            public HotKeyInfo()
            {
                Callbacks = new List<Action>();
                Id = _idCounter++;
            }
        }
    }

    /// <summary>
    /// The enumeration of possible modifiers.
    /// </summary>
    [Flags]
    public enum ModifierKeys : uint
    {
        Alt = 1,
        Control = 2,
        Shift = 4,
        Win = 8
    }
}