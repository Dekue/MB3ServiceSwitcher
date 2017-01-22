using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.ComponentModel;
using System.ServiceProcess;
using System.Threading;
using Microsoft.Win32;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace MB3ServiceSwitcher {
    /// <summary>
    /// Stops MB3Service and keeps track on its status.
    /// </summary>
    public partial class MainWindow : Window {
        public static MainWindow main;
        public ServiceController sc = new ServiceController("MB3Service");

        public MainWindow() {
            InitializeComponent();
        }

        private void OnContentRendered(object sender, EventArgs e) {
            textBoxAntiRansom.Text = Properties.Settings.Default.pathAntiRansom;
            textBoxDishonored.Text = Properties.Settings.Default.pathDishonored;
            Thread t1 = new Thread(new ThreadStart(labelStatusUpdater));
            t1.IsBackground = true;
            t1.Start();
        }

        private void OnClosing(object sender, CancelEventArgs e) {
            // TODO: restart Anti-Ransomware if not already running
        }

        public void labelStatusUpdater() {
            while (true) {
                try {
                    switch (sc.Status) {
                        case ServiceControllerStatus.Running:
                            changeLabelText("running");
                            mb3ServiceStatus.Dispatcher.Invoke(new Action(delegate () {
                                buttonStop.IsEnabled = true;
                                buttonStart.IsEnabled = false;
                            }));
                            WaitForStatusChange(sc, ServiceControllerStatus.Running);
                            break;
                        case ServiceControllerStatus.Stopped:
                            changeLabelText("stopped");
                            mb3ServiceStatus.Dispatcher.Invoke(new Action(delegate () {
                                buttonStop.IsEnabled = false;
                                buttonStart.IsEnabled = true;
                            }));
                            WaitForStatusChange(sc, ServiceControllerStatus.Stopped);
                            break;
                        case ServiceControllerStatus.Paused:
                            changeLabelText("paused");
                            WaitForStatusChange(sc, ServiceControllerStatus.Paused);
                            break;
                        case ServiceControllerStatus.StopPending:
                            changeLabelText("stopping");
                            mb3ServiceStatus.Dispatcher.Invoke(new Action(delegate () {
                                buttonStop.IsEnabled = false;
                                buttonStart.IsEnabled = false;
                            }));
                            WaitForStatusChange(sc, ServiceControllerStatus.StopPending);
                            break;
                        case ServiceControllerStatus.StartPending:
                            changeLabelText("starting");
                            mb3ServiceStatus.Dispatcher.Invoke(new Action(delegate () {
                                buttonStop.IsEnabled = false;
                                buttonStart.IsEnabled = false;
                            }));
                            WaitForStatusChange(sc, ServiceControllerStatus.StartPending);
                            break;
                        default:
                            changeLabelText("unknown");
                            break;
                    }
                }
                catch {
                    changeLabelText("not installed");
                    mb3ServiceStatus.Dispatcher.Invoke(new Action(delegate () {
                        buttonStop.IsEnabled = false;
                        buttonStart.IsEnabled = false;
                    }));
                    break;
                }
            }
        }

        public void changeLabelText(string s) {
            mb3ServiceStatus.Dispatcher.Invoke(new Action(delegate () {
                mb3ServiceStatus.Content = s;
            }));
        }

        public void WaitForStatusChange(ServiceController sc, ServiceControllerStatus desiredStatus) {
            DateTime utcNow = DateTime.UtcNow;
            sc.Refresh();
            while (sc.Status == desiredStatus) {
                Thread.Sleep(500);
                sc.Refresh();
            }
        }

        private void buttonStop_Click(object sender, RoutedEventArgs e) {
            if (sc.Status.Equals(ServiceControllerStatus.Running))
                sc.Stop();
            WaitForStatusChange(sc, ServiceControllerStatus.StartPending);
        }

        private void buttonStart_Click(object sender, RoutedEventArgs e) {
            if (sc.Status.Equals(ServiceControllerStatus.Stopped))
                sc.Start();
            WaitForStatusChange(sc, ServiceControllerStatus.StopPending);
        }

        private void buttonAntiRansom_Click(object sender, RoutedEventArgs e) {
            OpenFileDialog ofd = new OpenFileDialog();
            if (ofd.ShowDialog() == true)
                textBoxAntiRansom.Text = ofd.FileName;
        }

        private void buttonDishonored_Click(object sender, RoutedEventArgs e) {
            OpenFileDialog ofd = new OpenFileDialog();
            if (ofd.ShowDialog() == true)
                textBoxDishonored.Text = ofd.FileName;
        }

        private void buttonSave_Click(object sender, RoutedEventArgs e) {
            Properties.Settings.Default.pathAntiRansom = textBoxAntiRansom.Text;
            Properties.Settings.Default.pathDishonored = textBoxDishonored.Text;
            Properties.Settings.Default.Save();
        }

        private void buttonAutoStartGame_Click(object sender, RoutedEventArgs e) {

            Thread t2 = new Thread(new ThreadStart(startGame));
            t2.IsBackground = true;
            t2.Start();
            buttonAutoStartGame.IsEnabled = false;
        }
        private void startGame() { 

            // kill Anti-Ransomware process
            try {
                foreach (Process proc in Process.GetProcessesByName("Malwarebytes Anti-Ransomware")) {
                    proc.Kill();
                }
            }
            catch (Exception ex) {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            // kill Service
            try {
                if (sc.Status.Equals(ServiceControllerStatus.Running)) {
                    sc.Stop();
                    sc.WaitForStatus(ServiceControllerStatus.Stopped);
                }
            }
            catch (Exception ex) {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            // get process name
            Regex regex = new Regex(@"\\[a-zA-Z0-9]*\.exe");
            string mMatch = "";
            textBoxDishonored.Dispatcher.Invoke(new Action(delegate () {
                mMatch = textBoxDishonored.Text;
            }));
            Match match = regex.Match(mMatch);

            if (match.Success) {
                string processName = match.Value.Substring(1, match.Value.Length - 5);

                bool processStartedCorrectly = true;
                int processCounter = 0;
                try {
                    Process.Start(mMatch);
                }
                catch (Exception ex) {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    processStartedCorrectly = false;
                }

                if (processStartedCorrectly == true) {
                    // wait until game is opened
                    while (processCounter == 0) {
                        processCounter = 0;
                        foreach (Process proc in Process.GetProcessesByName(processName)) {
                            processCounter++;
                        }
                        Thread.Sleep(250);
                    }
                    // wait until game is closed again
                    while (processCounter > 0) {
                        processCounter = 0;
                        foreach (Process proc in Process.GetProcessesByName(processName)) {
                            processCounter++;
                        }
                        Thread.Sleep(250);
                    }
                    /* Windows 10: Process could geta renewal in "Apps"
                     * (not background processes), wait 10 seconds
                     * for the renewal and start a new while loop.
                     * If the program was really terminated, there
                     * only is the small 10 second delay.
                     */
                    Thread.Sleep(10000);
                    do {
                        processCounter = 0;
                        foreach (Process proc in Process.GetProcessesByName(processName)) {
                            processCounter++;
                        }
                        Thread.Sleep(250);
                    } while (processCounter > 0);
                }
            }
            // restart service
            try {
                sc.Start();
                sc.WaitForStatus(ServiceControllerStatus.Running);
            }
            catch {
            }
            // restart Anti-Ransomware process
            try {
                string antiRansomPath = "";
                textBoxAntiRansom.Dispatcher.Invoke(new Action(delegate () {
                    antiRansomPath = textBoxAntiRansom.Text;
                }));
                Process.Start(antiRansomPath);
            }
            catch (Exception ex){
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            buttonAutoStartGame.Dispatcher.Invoke(new Action(delegate () {
                buttonAutoStartGame.IsEnabled = true;
            }));
        }
    }
}