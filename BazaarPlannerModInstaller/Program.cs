using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Windows.Forms;

namespace BazaarPlannerModInstaller
{
    static class Program
    {
        private const string DOTNET_DOWNLOAD_URL = "https://download.visualstudio.microsoft.com/download/pr/f18288f6-1732-415b-b577-7fb46510479a/a98239f751a7aed31bc4aa12f348a9bf/windowsdesktop-runtime-8.0.2-win-x64.exe";

        [STAThread]
        static void Main()
        {
            string logPath = Path.Combine(Path.GetTempPath(), "BazaarPlannerInstaller.log");
            try
            {
                File.WriteAllText(logPath, $"Application starting at {DateTime.Now}\n");

                // Check if .NET runtime is installed
                if (!IsDotNetRuntimeInstalled())
                {
                    File.AppendAllText(logPath, "Required .NET runtime not found\n");
                    DialogResult result = MessageBox.Show(
                        "This application requires .NET 8.0 Desktop Runtime. Would you like to download and install it now?",
                        "Runtime Required",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (result == DialogResult.Yes)
                    {
                        if (!InstallDotNetRuntime())
                        {
                            MessageBox.Show("Failed to install .NET Runtime. Please install it manually from Microsoft's website.",
                                "Installation Failed",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
                            return;
                        }
                    }
                    else
                    {
                        return;
                    }
                }

                File.AppendAllText(logPath, "Initializing ApplicationConfiguration...\n");
                ApplicationConfiguration.Initialize();
                
                File.AppendAllText(logPath, "Creating InstallerForm...\n");
                var form = new InstallerForm();
                
                File.AppendAllText(logPath, "Running application...\n");
                Application.Run(form);
            }
            catch (Exception ex)
            {
                File.AppendAllText(logPath, $"Error occurred:\n{ex.Message}\n{ex.StackTrace}\n");
                
                MessageBox.Show(
                    $"Application failed to start:\n\n{ex.Message}\n\nStack Trace:\n{ex.StackTrace}\n\nCheck log file at: {logPath}",
                    "Startup Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private static bool IsDotNetRuntimeInstalled()
        {
            try
            {
                Process process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "dotnet",
                        Arguments = "--list-runtimes",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                // Check for any 8.0.x version
                return output.Contains("Microsoft.WindowsDesktop.App 8.0.");
            }
            catch
            {
                return false;
            }
        }

        private static bool InstallDotNetRuntime()
        {
            try
            {
                string tempFile = Path.Combine(Path.GetTempPath(), "dotnet_runtime_installer.exe");

                // Download the installer
                using (var client = new HttpClient())
                {
                    byte[] installerData = client.GetByteArrayAsync(DOTNET_DOWNLOAD_URL).Result;
                    File.WriteAllBytes(tempFile, installerData);
                }

                // Run the installer
                Process process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = tempFile,
                        Arguments = "/quiet /norestart",
                        UseShellExecute = true,
                        Verb = "runas" // Request admin privileges
                    }
                };

                process.Start();
                process.WaitForExit();

                File.Delete(tempFile);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
} 