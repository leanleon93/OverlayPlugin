﻿using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using RainbowMage.OverlayPlugin.MemoryProcessors.InCombat;
using RainbowMage.OverlayPlugin.NetworkProcessors;
using static RainbowMage.OverlayPlugin.MemoryProcessors.InCombat.LineInCombat;

namespace RainbowMage.OverlayPlugin
{
    class OverlayHider : IDisposable
    {
        private bool gameActive = true;
        private bool inCutscene = false;
        private bool inCombat = false;
        private IPluginConfig config;
        private ILogger logger;
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Usage",
            "CA2213:Disposable fields should be disposed",
            Justification = "main is disposed of by TinyIoCContainer")]
        private PluginMain main;
        private FFXIVRepository repository;
        private int ffxivPid = -1;
        private Timer focusTimer;
        private bool _disposed;

        public OverlayHider(TinyIoCContainer container)
        {
            this.config = container.Resolve<IPluginConfig>();
            this.logger = container.Resolve<ILogger>();
            this.main = container.Resolve<PluginMain>();
            this.repository = container.Resolve<FFXIVRepository>();

            container.Resolve<NativeMethods>().ActiveWindowChanged += ActiveWindowChangedHandler;
            container.Resolve<NetworkParser>().OnOnlineStatusChanged += OnlineStatusChanged;
            LineInCombat lineInCombat;
            if (container.TryResolve(out lineInCombat))
            {
                lineInCombat.OnInCombatChanged += CombatStatusChanged;
            }

            try
            {
                repository.RegisterProcessChangedHandler(UpdateFFXIVProcess);
            }
            catch (Exception ex)
            {
                logger.Log(LogLevel.Error, "Failed to register process watcher for FFXIV; this is only an issue if you're playing FFXIV. As a consequence, OverlayPlugin won't be able to hide overlays if you're not in-game.");
                logger.Log(LogLevel.Error, "Details: " + ex.ToString());
            }

            focusTimer = new Timer();
            focusTimer.Tick += (o, e) => ActiveWindowChangedHandler(this, IntPtr.Zero);
            focusTimer.Interval = 10000;  // 10 seconds
            focusTimer.Start();
        }

        private void UpdateFFXIVProcess(Process p)
        {
            if (p != null)
            {
                ffxivPid = p.Id;
            }
            else
            {
                ffxivPid = -1;
            }
        }

        public void UpdateOverlays()
        {
            if (!config.HideOverlaysWhenNotActive)
                gameActive = true;

            if (!config.HideOverlayDuringCutscene)
                inCutscene = false;

            try
            {
                foreach (var overlay in main.Overlays)
                {
                    if (overlay.Config.IsVisible)
                    {
                        overlay.Visible = gameActive && !inCutscene && (!overlay.Config.HideOutOfCombat || inCombat);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Log(LogLevel.Error, $"OverlayHider: Failed to update overlays: {ex}");
            }
        }

        private void ActiveWindowChangedHandler(object sender, IntPtr changedWindow)
        {
            if (!config.HideOverlaysWhenNotActive) return;
            try
            {
                try
                {
                    NativeMethods.GetWindowThreadProcessId(NativeMethods.GetForegroundWindow(), out uint pid);

                    if (pid == 0)
                        return;

                    if (ffxivPid != -1)
                    {
                        gameActive = pid == ffxivPid || pid == Process.GetCurrentProcess().Id;
                    }
                    else
                    {
                        var exePath = Process.GetProcessById((int)pid).MainModule.FileName;
                        var fileName = Path.GetFileName(exePath.ToString());
                        gameActive = (fileName.Equals("BNSR.exe", StringComparison.OrdinalIgnoreCase) ||
                                        exePath.ToString() == Process.GetCurrentProcess().MainModule.FileName);
                    }
                }
                catch (System.ComponentModel.Win32Exception ex)
                {
                    // Ignore access denied errors. Those usually happen if the foreground window is running with
                    // admin permissions but we are not.
                    if (ex.ErrorCode == -2147467259)  // 0x80004005
                    {
                        gameActive = false;
                    }
                    else
                    {
                        logger.Log(LogLevel.Error, "XivWindowWatcher: {0}", ex.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Log(LogLevel.Error, "XivWindowWatcher: {0}", ex.ToString());
            }

            UpdateOverlays();
        }

        private void OnlineStatusChanged(object sender, OnlineStatusChangedArgs e)
        {
            if (!config.HideOverlayDuringCutscene || e.Target != repository.GetPlayerID()) return;

            inCutscene = e.Status == 15;
            UpdateOverlays();
        }

        private void CombatStatusChanged(object sender, InCombatArgs args)
        {
            inCombat = args.InGameCombat;

            if (args.InGameCombatChanged)
            {
                UpdateOverlays();
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    focusTimer?.Stop();
                    focusTimer?.Dispose();
                    focusTimer = null;
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
