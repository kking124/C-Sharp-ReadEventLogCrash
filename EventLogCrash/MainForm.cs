using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace EventLogCrash
{
    public class MainForm : Form
    {
    private readonly Button exitButton;
    private readonly Button crashButton;
    private readonly Label startTimesLabel;
    private TextBox crashLogTextBox;

        public MainForm()
        {
            var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "";
            Text = $"EventLogCrash v{version}";
            try
            {
                this.Icon = new System.Drawing.Icon("favicon.ico");
            }
            catch { /* Ignore icon load errors */ }
            Width = 500;
            Height = 250;

            exitButton = new Button { Text = "Exit Successfully", Left = 20, Top = 20, Width = 200 };
            crashButton = new Button { Text = "Crash App", Left = 240, Top = 20, Width = 200 };
            startTimesLabel = new Label { Left = 20, Top = 60, Width = 440, Height = 40, AutoSize = true };
            crashLogTextBox = new TextBox {
                Left = 20,
                Top = 110,
                Width = 440,
                Height = 80,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                WordWrap = true,
                ReadOnly = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };

            exitButton.Click += ExitButton_Click;
            crashButton.Click += CrashButton_Click;

            Controls.Add(exitButton);
            Controls.Add(crashButton);
            Controls.Add(startTimesLabel);
            Controls.Add(crashLogTextBox);

            Load += MainForm_Load;

        // ...existing code...
    }
    // ...existing code...

        private void MainForm_Load(object sender, EventArgs e)
        {
            WriteStartEvent();
            startTimesLabel.Text = GetLastTwoStartTimes();
            crashLogTextBox.Text = "Checking for recent crashes...";
            System.Threading.Tasks.Task.Run(() => {
                var status = GetLastRunStatus();
                this.Invoke((MethodInvoker)delegate {
                    crashLogTextBox.Text = status;
                });
            });
        }

        private static string GetLastTwoStartTimes()
        {
            string appName = Application.ProductName;
            try
            {
                var eventLog = new EventLog("Application");
                var startEntries = eventLog.Entries.Cast<EventLogEntry>()
                    .Where(e => e.InstanceId == 1 && e.Source == appName)
                    .OrderByDescending(e => e.TimeGenerated)
                    .Take(2)
                    .ToList();
                if (startEntries.Count == 0)
                    return "No start events found.";
                var result = "Last 2 Start Times:\n";
                for (int i = 0; i < startEntries.Count; i++)
                {
                    result += $"{i + 1}: {startEntries[i].TimeGenerated}\n";
                }
                return result;
            }
            catch (Exception ex)
            {
                return $"Error reading start events: {ex.Message}";
            }
        }

        private void ExitButton_Click(object sender, EventArgs e)
        {
            WriteSuccessEvent();
            Application.Exit();
        }

        private void CrashButton_Click(object sender, EventArgs e)
        {
            Environment.FailFast("Intentional crash triggered by user.");
        }

        private static string GetLastRunStatus()
        {
            string exeName = System.IO.Path.GetFileName(Application.ExecutablePath);
            string appName = Application.ProductName;
            try
            {
                var eventLog = new EventLog("Application");
                // Find the two most recent start events
                var startEntries = eventLog.Entries.Cast<EventLogEntry>()
                    .Where(e => e.InstanceId == 1 && e.Source == appName)
                    .OrderByDescending(e => e.TimeGenerated)
                    .Take(2)
                    .ToList();
                // Use the second most recent start event as the reference for the previous run
                DateTime? prevStartTime = startEntries.Count == 2 ? startEntries[1].TimeGenerated : (DateTime?)null;
                DateTime? currStartTime = startEntries.Count > 0 ? startEntries[0].TimeGenerated : (DateTime?)null;

                // Find crash events between prevStartTime and currStartTime
                var crashEntries = eventLog.Entries.Cast<EventLogEntry>()
                    .Where(e => (e.InstanceId == 1000 || e.InstanceId == 1001)
                        && e.Source == "Application Error"
                        && e.Message.Contains(exeName, StringComparison.OrdinalIgnoreCase)
                        && (prevStartTime == null || e.TimeGenerated > prevStartTime)
                        && (currStartTime == null || e.TimeGenerated < currStartTime))
                    .OrderByDescending(e => e.TimeGenerated)
                    .ToList();
                var lastCrash = crashEntries.FirstOrDefault();

                // Find success events between prevStartTime and currStartTime
                var successEntries = eventLog.Entries.Cast<EventLogEntry>()
                    .Where(e => e.InstanceId == 0 && e.Source == appName
                        && (prevStartTime == null || e.TimeGenerated > prevStartTime)
                        && (currStartTime == null || e.TimeGenerated < currStartTime))
                    .OrderByDescending(e => e.TimeGenerated)
                    .ToList();
                var lastSuccess = successEntries.FirstOrDefault();

                if (lastCrash == null && lastSuccess == null)
                {
                    return "No previous run information found.";
                }
                if (lastSuccess != null && (lastCrash == null || lastSuccess.TimeGenerated > lastCrash.TimeGenerated))
                {
                    return "Application Last Exited Successfully";
                }
                if (lastCrash != null)
                {
                    var sb = new StringBuilder();
                    sb.AppendLine("Application Last Exited With Error");
                    sb.AppendLine($"Time: {lastCrash.TimeGenerated}");
                    sb.AppendLine($"Message: {lastCrash.Message}");

                    // Find all .NET Runtime events for this application between prevStartTime and currStartTime
                    var dotnetEvents = eventLog.Entries.Cast<EventLogEntry>()
                        .Where(e => e.Source == ".NET Runtime"
                            && e.TimeGenerated <= lastCrash.TimeGenerated
                            && e.TimeGenerated > (prevStartTime ?? DateTime.MinValue)
                            && e.Message.Contains(exeName, StringComparison.OrdinalIgnoreCase)
                            )
                        .OrderByDescending(e => e.TimeGenerated)
                        .ToList();
                    sb.AppendLine("----------------------");
                    sb.AppendLine($".NET Runtime Error Count: {dotnetEvents.Count}");
                    if (dotnetEvents.Count > 0)
                    {
                        sb.AppendLine(".NET Runtime Event(s):");
                        foreach (var dotnetEvent in dotnetEvents)
                        {
                            sb.AppendLine($"Event ID: {dotnetEvent.InstanceId}");
                            sb.AppendLine($"Time: {dotnetEvent.TimeGenerated}");
                            // Pretty print the .NET error message
                            var msg = dotnetEvent.Message;
                            var lines = msg.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                            foreach (var line in lines)
                            {
                                sb.AppendLine(line);
                            }
                            sb.AppendLine("----------------------");
                        }
                    }
                    else
                    {
                        sb.AppendLine("No .NET Runtime errors found for this run.");
                    }
                    return sb.ToString();
                }
                return "No previous run information found.";
            }
            catch (Exception ex)
            {
                return $"Error reading event log: {ex.Message}";
            }
        }

        private static void WriteStartEvent()
        {
            try
            {
                string appName = Application.ProductName;
                if (!EventLog.SourceExists(appName))
                {
                    EventLog.CreateEventSource(appName, "Application");
                }
                EventLog.WriteEntry(appName, "Application started.", EventLogEntryType.Information, 1);
            }
            catch
            {
                // Ignore errors writing to event log
            }
    }

        private static void WriteSuccessEvent()
        {
            try
            {
                string appName = Application.ProductName;
                if (!EventLog.SourceExists(appName))
                {
                    EventLog.CreateEventSource(appName, "Application");
                }
                EventLog.WriteEntry(appName, "Application exited successfully.", EventLogEntryType.Information, 0);
            }
            catch
            {
                // Ignore errors writing to event log
            }
        }
    }
}
