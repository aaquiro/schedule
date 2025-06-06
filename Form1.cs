using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;

namespace ChildUsageEnforcer
{
    public partial class Form1 : Form
    {
        private NotifyIcon trayIcon;
        private ContextMenuStrip trayMenu;
        private System.Timers.Timer backgroundTimer;
        private ScheduleConfig schedule;
        private Button button1;
        private List<string> blockedProcesses = new List<string>();

        public Form1()
        {
            InitializeComponent();

            _ = InitializeAsync();

            InitializeTrayIcon();

            this.WindowState = FormWindowState.Minimized;
            this.ShowInTaskbar = false;
            this.Visible = false;
            Log($"Form1-loaded");
        }

        private async Task InitializeAsync()
        {
            StartBackgroundTask();
        }
        private void InitializeTrayIcon()
        {
            trayMenu = new ContextMenuStrip();
            trayMenu.Items.Add("Exit", null, OnExit);

            trayIcon = new NotifyIcon
            {
                Text = "SysInfo",
                Icon = SystemIcons.Application,
                ContextMenuStrip = trayMenu,
                Visible = true
            };
        }

        private async Task LoadScheduleAsync()
        {
            string url = "https://raw.githubusercontent.com/aaquiro/schedule/refs/heads/main/schedule.json";

            try
            {
                using HttpClient client = new HttpClient();
                string json = await client.GetStringAsync(url);
                schedule = JsonSerializer.Deserialize<ScheduleConfig>(json);
                Log($"LoadScheduleAsync-loaded");
            }
            catch (Exception ex)
            {
                Log($"Failed to load schedule from URL: {ex.Message}");
                schedule = new ScheduleConfig { AllowedTimeRanges = new List<TimeRange>() };
            }
        }


        private async Task LoadBlockedProcessesAsync()
        {
            string url = "https://raw.githubusercontent.com/aaquiro/schedule/refs/heads/main/blocked_apps.json";

            try
            {
                using HttpClient client = new HttpClient();
                string json = await client.GetStringAsync(url);
                blockedProcesses = JsonSerializer.Deserialize<List<string>>(json);
                Log($"LoadBlockedProcessesAsync-loaded");
            }
            catch (Exception ex)
            {
                Log($"Failed to load blocked apps from URL: {ex.Message}");
                blockedProcesses = new List<string>(); // fallback empty list
            }
        }

        private async Task StartBackgroundTask()
        {
            await LoadScheduleAsync();
            await LoadBlockedProcessesAsync();

            backgroundTimer = new System.Timers.Timer(60000);
            backgroundTimer.Elapsed += (s, e) =>
            {
                if (!IsPlayTime(DateTime.Now))
                {
                    KillBlockedApps();
                }
            };
            backgroundTimer.Start();
        }

        private bool IsPlayTime(DateTime now)
        {
            if (schedule == null) return false;

            TimeSpan current = now.TimeOfDay;

            foreach (var slot in schedule.AllowedTimeRanges)
            {
                if (TimeSpan.TryParse(slot.Start, out var start) &&
                    TimeSpan.TryParse(slot.End, out var end))
                {
                    if (current >= start && current < end)
                        return true;
                }
            }

            return false;
        }

        private void KillBlockedApps()
        {

            var allProcesses = Process.GetProcesses();

            foreach (var proc in allProcesses)
            {
                try
                {
                    foreach (var blockedName in blockedProcesses)
                    {
                        if (proc.ProcessName.IndexOf(blockedName, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            proc.Kill();
                            Log($"Killed process: {proc.ProcessName}");
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to kill {proc.ProcessName}: {ex.Message}");
                }
            }

            _ = RunOptionalLauncherOncePerDayAsync();
        }
        private void Log(string message)
        {
            try
            {
                string logDir = @"C:\Temp";
                if (!Directory.Exists(logDir))
                    Directory.CreateDirectory(logDir);

                string dateStamp = DateTime.Now.ToString("yyyy-MM-dd");
                string logFile = Path.Combine(logDir, $"log_{dateStamp}.txt");

                string logEntry = $"{DateTime.Now:HH:mm:ss} - {message}{Environment.NewLine}";
                File.AppendAllText(logFile, logEntry);
            }
            catch
            {
                // Silent fail to avoid crashes from logging
            }
        }
        public class LauncherConfig
        {
            public string Path { get; set; }
        }
      
        private async Task RunOptionalLauncherOncePerDayAsync()
        {
            string logPath = @"C:\Temp\launcher.last";
            string today = DateTime.Now.ToString("yyyy-MM-dd");

            try
            {
                if (File.Exists(logPath))
                {
                    string lastRunDate = File.ReadAllText(logPath).Trim();
                    if (lastRunDate == today)
                    {
                        Log("Launcher already run today.");
                        return; // Already launched today
                    }
                }

                string url = "https://raw.githubusercontent.com/aaquiro/schedule/refs/heads/main/launcher.json";

                using HttpClient client = new HttpClient();
                string json = await client.GetStringAsync(url);

                var launcher = JsonSerializer.Deserialize<LauncherConfig>(json);

                if (!string.IsNullOrWhiteSpace(launcher?.Path) && File.Exists(launcher.Path))
                {
                    Process.Start(launcher.Path);
                    Log($"Launched: {launcher.Path}");

                    File.WriteAllText(logPath, today); // Record that it ran today
                }
                else
                {
                    Log($"Launcher path invalid or file missing: {launcher?.Path}");
                }
            }
            catch (Exception ex)
            {
                Log($"Failed to launch from remote JSON: {ex.Message}");
            }
        }
        private void OnExit(object sender, EventArgs e)
        {
            trayIcon.Visible = false;
            backgroundTimer?.Stop();
            Application.Exit();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            trayIcon.Visible = false;
            base.OnFormClosing(e);
        }

        private void InitializeComponent()
        {
            this.button1 = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(139, 48);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(94, 29);
            this.button1.TabIndex = 0;
            this.button1.Text = "button1";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // Form1
            // 
            this.ClientSize = new System.Drawing.Size(282, 253);
            this.Controls.Add(this.button1);
            this.Name = "Form1";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.ResumeLayout(false);

        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {

        }
    }
}
