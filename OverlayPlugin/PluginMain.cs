﻿using Advanced_Combat_Tracker;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Windows.Forms;
using System.Reflection;
using RainbowMage.HtmlRenderer;
using System;
using System.Drawing;
using System.IO;
using System.ComponentModel;
using System.Diagnostics;

namespace RainbowMage.OverlayPlugin
{
    public class PluginMain : IActPluginV1
    {
        TabPage tabPage;
        Label label;
        ControlPanel controlPanel;

        string pluginDirectory;

        internal PluginConfig Config { get; private set; }
        internal MiniParseOverlay MiniParseOverlay { get; private set; }
        internal SpellTimerOverlay SpellTimerOverlay { get; private set; }
        internal BindingList<LogEntry> Logs { get; private set; }

        public PluginMain()
        {
            this.Logs = new BindingList<LogEntry>();

        }

        public void InitPlugin(TabPage pluginScreenSpace, Label pluginStatusText)
        {
            try
            {
                this.tabPage = pluginScreenSpace;
                this.label = pluginStatusText;

#if DEBUG
                Log(LogLevel.Debug, "##################################");
                Log(LogLevel.Debug, "           DEBUG BUILD");
                Log(LogLevel.Debug, "##################################");
#endif
                this.pluginDirectory = GetPluginDirectory();
                Log(LogLevel.Info, "InitPlugin: PluginDirectory = {0}", this.pluginDirectory);

                // プラグインの配置してあるフォルダを検索するカスタムリゾルバーでアセンブリを解決する
                AppDomain.CurrentDomain.AssemblyResolve += CustomAssemblyResolve;

                // コンフィグ系読み込み
                LoadConfig();
                this.controlPanel = new ControlPanel(this, this.Config);
                this.controlPanel.Dock = DockStyle.Fill;
                this.tabPage.Controls.Add(this.controlPanel);

                // ACT 終了時に CEF をシャットダウン（ゾンビ化防止）
                Application.ApplicationExit += (o, e) =>
                {
                    try { Renderer.Shutdown(); }
                    catch { }
                };

                // オーバーレイ初期化
                this.MiniParseOverlay = new OverlayPlugin.MiniParseOverlay(this.Config.MiniParseOverlay);
                this.MiniParseOverlay.OnLog += (o, e) => Log(e.Level, e.Message);
                this.MiniParseOverlay.Start();
                this.SpellTimerOverlay = new OverlayPlugin.SpellTimerOverlay(this.Config.SpellTimerOverlay);
                this.SpellTimerOverlay.OnLog += (o, e) => Log(e.Level, e.Message);
                this.SpellTimerOverlay.Start();

                // ショートカットキー設定
                ActGlobals.oFormActMain.KeyPreview = true;
                ActGlobals.oFormActMain.KeyDown += oFormActMain_KeyDown;

                Log(LogLevel.Info, "InitPlugin: Initialized.");
                this.label.Text = "Initialized.";
            }
            catch (Exception e)
            {
                Log(LogLevel.Error, "InitPlugin: {0}", e.ToString());

                throw;
            }
        }

        public void DeInitPlugin()
        {
            SaveConfig();
            this.MiniParseOverlay.Dispose();
            this.SpellTimerOverlay.Dispose();
            ActGlobals.oFormActMain.KeyDown -= oFormActMain_KeyDown;

            AppDomain.CurrentDomain.AssemblyResolve -= CustomAssemblyResolve;

            Log(LogLevel.Info, "DeInitPlugin: Finalized.");
            this.label.Text = "Finalized.";
        }

        static readonly Dictionary<string, string> assemblyTable = new Dictionary<string, string>
        {
            { "HtmlRenderer,", "HtmlRenderer.dll" },
            { "Xilium.CefGlue,", "Xilium.CefGlue.dll" }
        };

        private Assembly CustomAssemblyResolve(object sender, ResolveEventArgs e)
        {
            Log(LogLevel.Debug, "AssemblyResolve: Resolving assembly for '{0}'...", e.Name);

            Assembly result = null;
            foreach (var pair in assemblyTable)
            {
                if (e.Name.StartsWith(pair.Key))
                {
                    string asmPath = "";
                    try
                    {
                        asmPath = Path.Combine(pluginDirectory, pair.Value);
                        result = Assembly.LoadFile(asmPath);
                        Log(LogLevel.Debug, "AssemblyResolve: => Found assembly in {0}.", asmPath);
                        break;
                    }
                    catch (FileNotFoundException ex)
                    {
                        var message = string.Format(
                            "必要なアセンブリ {0} が存在しません。",
                            asmPath
                            );
                        Log(LogLevel.Error, "AssemblyResolve: => {0}", message);
                        Log(LogLevel.Error, "AssemblyResolve: => {0}", ex);
                        MessageBox.Show(message, "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    catch (FileLoadException ex)
                    {
                        var message = string.Format(
                            "必要なアセンブリ {0} は存在しますが、読み込めません。",
                            asmPath
                            );
                        Log(LogLevel.Error, "AssemblyResolve: => {0}", message);
                        Log(LogLevel.Error, "AssemblyResolve: => {0}", ex);
                        MessageBox.Show(message, "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    catch (NotSupportedException ex)
                    {
                        var message = string.Format(
                            "アセンブリ {0} がネットワーク上にあるか、またはブロックされている可能性があります。",
                            asmPath
                            );
                        Log(LogLevel.Error, "AssemblyResolve: => {0}", message);
                        Log(LogLevel.Error, "AssemblyResolve: => {0}", ex);
                        MessageBox.Show(message, "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    catch (Exception ex)
                    {
                        var message = string.Format(
                            "アセンブリ {0}の読み込み時に例外が発生しました。\n{0}",
                            asmPath
                            );
                        Log(LogLevel.Error, "AssemblyResolve: => {0}", ex);
                        MessageBox.Show(message, "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }

            if (result == null)
            {
                Log(LogLevel.Debug, "AssemblyResolve: => Not found in plugin directory.");
            }

            return result;
        }



        void oFormActMain_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.M)
            {
                // ミニパース表示非表示
                this.Config.MiniParseOverlay.IsVisible = !this.Config.MiniParseOverlay.IsVisible;
                ActGlobals.oFormActMain.Activate();
            }
            else if (e.Control && e.KeyCode == Keys.S)
            {
                // スペルタイマー表示非表示
                this.Config.SpellTimerOverlay.IsVisible = !this.Config.SpellTimerOverlay.IsVisible;
                ActGlobals.oFormActMain.Activate();
            }
        }

        private void LoadConfig()
        {
            try
            {
                Config = PluginConfig.LoadXml(GetConfigPath());
            }
            catch (Exception e)
            {
                Log(LogLevel.Warning, "LoadConfig: {0}", e);
                Log(LogLevel.Info, "LoadConfig: Creating new configuration.");
                Config = new PluginConfig();
            }
            finally
            {
                if (string.IsNullOrWhiteSpace(Config.MiniParseOverlay.Url))
                {
                    Config.MiniParseOverlay.Url =
                        new Uri(Path.Combine(pluginDirectory, "resources", "miniparse.html")).ToString();
                }
                if (string.IsNullOrWhiteSpace(Config.SpellTimerOverlay.Url))
                {
                    Config.SpellTimerOverlay.Url = 
                        new Uri(Path.Combine(pluginDirectory, "resources", "spelltimer.html")).ToString();
                }
            }
        }

        private void SaveConfig()
        {
            try
            {
                Config.MiniParseOverlay.Position = this.MiniParseOverlay.Overlay.Location;
                Config.MiniParseOverlay.Size = this.MiniParseOverlay.Overlay.Size;
                Config.SpellTimerOverlay.Position = this.SpellTimerOverlay.Overlay.Location;
                Config.SpellTimerOverlay.Size = this.SpellTimerOverlay.Overlay.Size;

                Config.SaveXml(GetConfigPath());
            }
            catch (Exception e)
            {
                Log(LogLevel.Error, "SaveConfig: {0}", e);
            }
        }

        private static string GetConfigPath()
        {
            var path = System.IO.Path.Combine(
                ActGlobals.oFormActMain.AppDataFolder.FullName,
                "Config",
                "RainbowMage.OverlayPlugin.config.xml");

            return path;
        }

        private string GetPluginDirectory()
        {
            var plugin = ActGlobals.oFormActMain.ActPlugins.Where(x => x.pluginObj == this).FirstOrDefault();
            if (plugin != null)
            {
                return System.IO.Path.GetDirectoryName(plugin.pluginFile.FullName);
            }
            else
            {
                throw new Exception();
            }
        }

        internal void Log(LogLevel level, string message)
        {
#if !DEBUG
            if (level == LogLevel.Trace || level == LogLevel.Debug)
            {
                return;
            }
#endif

            this.Logs.Add(new LogEntry(level, DateTime.Now, message));
        }

        internal void Log(LogLevel level, string format, params object[] args)
        {
            Log(level, string.Format(format, args));
        }
    }

    internal class LogEntry
    {
        public string Message { get; set; }
        public LogLevel Level { get; set; }
        public DateTime Time { get; set; }

        public LogEntry(LogLevel level, DateTime time, string message)
        {
            this.Message = message;
            this.Level = level;
            this.Time = time;
        }
    }

    public enum LogLevel
    {
        Trace,
        Debug,
        Info,
        Warning,
        Error
    }
}
