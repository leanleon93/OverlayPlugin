﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.Serialization;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace RainbowMage.OverlayPlugin
{
    public abstract class OverlayConfigBase : IOverlayConfig
    {
        public event EventHandler<VisibleStateChangedEventArgs> VisibleChanged;
        public event EventHandler DisabledChanged;
        public event EventHandler<ThruStateChangedEventArgs> ClickThruChanged;
        public event EventHandler<UrlChangedEventArgs> UrlChanged;
        public event EventHandler<MaxFrameRateChangedEventArgs> MaxFrameRateChanged;
        public event EventHandler GlobalHotkeyChanged;
        public event EventHandler<LockStateChangedEventArgs> LockChanged;
        public event EventHandler LogConsoleMessagesChanged;
        public event EventHandler HideOutOfCombatChanged;

        public string Name { get; set; }

        public Guid Uuid { get; set; }

        private bool isVisible;
        public bool IsVisible
        {
            get
            {
                return this.isVisible;
            }
            set
            {
                if (this.isVisible != value)
                {
                    this.isVisible = value;
                    VisibleChanged?.Invoke(this, new VisibleStateChangedEventArgs(this.isVisible));
                }
            }
        }

        private bool disabled;
        public bool Disabled
        {
            get
            {
                return disabled;
            }
            set
            {
                if (disabled != value)
                {
                    disabled = value;
                    DisabledChanged?.Invoke(this, new EventArgs());
                }
            }
        }

        private bool isClickThru;
        public bool IsClickThru
        {
            get
            {
                return this.isClickThru;
            }
            set
            {
                if (this.isClickThru != value)
                {
                    this.isClickThru = value;
                    ClickThruChanged?.Invoke(this, new ThruStateChangedEventArgs(this.isClickThru));
                }
            }
        }

        public Point Position { get; set; }
        public Size Size { get; set; }

        private string url;
        public string Url
        {
            get
            {
                return this.url;
            }
            set
            {
                if (this.url != value)
                {
                    this.url = value;
                    UrlChanged?.Invoke(this, new UrlChangedEventArgs(this.url));
                }
            }
        }

        private int maxFrameRate;
        public int MaxFrameRate
        {
            get
            {
                return this.maxFrameRate;
            }
            set
            {
                if (this.maxFrameRate != value)
                {
                    this.maxFrameRate = value;
                    MaxFrameRateChanged?.Invoke(this, new MaxFrameRateChangedEventArgs(this.maxFrameRate));
                }
            }
        }

        private bool globalHotkeyEnabled;
        [Obsolete("Use the GlobalHotkeys list instead", true)]
        public bool GlobalHotkeyEnabled
        {
            get
            {
                return globalHotkeyEnabled;
            }
            set
            {
                globalHotkeyEnabled = value;
            }
        }

        private Keys globalHotkey;
        [Obsolete("Use the GlobalHotkeys list instead", true)]
        public Keys GlobalHotkey
        {
            get
            {
                return globalHotkey;
            }
            set
            {
                globalHotkey = value;
            }
        }

        private Keys globalHotkeyModifiers;
        [Obsolete("Use the GlobalHotkeys list instead", true)]
        public Keys GlobalHotkeyModifiers
        {
            get
            {
                return globalHotkeyModifiers;
            }
            set
            {
                globalHotkeyModifiers = value;
            }
        }

        private GlobalHotkeyType globalHotkeyType;
        [Obsolete("Use the GlobalHotkeys list instead", true)]
        public GlobalHotkeyType GlobalHotkeyType
        {
            get
            {
                return globalHotkeyType;
            }
            set
            {
                globalHotkeyType = value;
            }
        }

        public List<GlobalHotkey> GlobalHotkeys;

        private bool isLocked;
        public bool IsLocked
        {
            get
            {
                return this.isLocked;
            }
            set
            {
                if (this.isLocked != value)
                {
                    this.isLocked = value;
                    LockChanged?.Invoke(this, new LockStateChangedEventArgs(this.isLocked));
                }
            }
        }

        private bool logConsoleMessages = true;
        public bool LogConsoleMessages
        {
            get
            {
                return this.logConsoleMessages;
            }
            set
            {
                if (this.logConsoleMessages != value)
                {
                    this.logConsoleMessages = value;
                    LogConsoleMessagesChanged?.Invoke(this, new EventArgs());
                }
            }
        }

        private bool hideOutOfCombat = false;
        public bool HideOutOfCombat
        {
            get
            {
                return this.hideOutOfCombat;
            }
            set
            {
                if (this.hideOutOfCombat != value)
                {
                    this.hideOutOfCombat = value;
                    HideOutOfCombatChanged?.Invoke(this, new EventArgs());
                }
            }
        }

        protected OverlayConfigBase(string name)
        {
            this.Name = name;
            this.Uuid = Guid.NewGuid();
            this.IsVisible = true;
            this.IsClickThru = false;
            this.Position = new Point(20, 20);
            this.Size = new Size(300, 300);
            this.Url = "";
            this.MaxFrameRate = 30;
            this.GlobalHotkeys = new List<GlobalHotkey>();
            this.logConsoleMessages = true;
        }

        [OnDeserialized]
        public void ConvertOldSettings(StreamingContext ctx)
        {
            if (globalHotkey != Keys.None)
            {
                GlobalHotkeys.Add(new GlobalHotkey
                {
                    Enabled = globalHotkeyEnabled,
                    Modifiers = globalHotkeyModifiers,
                    Key = globalHotkey,
                    Type = globalHotkeyType
                });

                globalHotkeyEnabled = false;
                globalHotkeyModifiers = Keys.None;
                globalHotkey = Keys.None;
            }

            if (Uuid == Guid.Empty)
            {
                Uuid = Guid.NewGuid();
            }
        }

        public void TriggerGlobalHotkeyChanged()
        {
            GlobalHotkeyChanged?.Invoke(null, new EventArgs());
        }

        [JsonIgnore]
        public abstract Type OverlayType { get; }
    }

    public enum GlobalHotkeyType
    {
        ToggleVisible,
        ToggleClickthru,
        ToggleLock,
        ToogleEnabled,
    }

    public class GlobalHotkey
    {
        public bool Enabled;
        public Keys Key;
        public Keys Modifiers;
        public GlobalHotkeyType Type;
    }
}
