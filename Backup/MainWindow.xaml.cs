using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace Backup
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private List<DriveInfo> driveList = new List<DriveInfo>();
        private string nl = Environment.NewLine;
        private DriveInfo _selectedSourceDrive;
        private DriveInfo _selectedDestinationDrive;
        private string _destinationDriveInfo = "";
        private string _sourceDriveInfo = "";
        private string _status = "";

        #region Properties

        public DriveInfo SelectedSourceDrive
        {
            get { return _selectedSourceDrive; }
            set { _selectedSourceDrive = value; OnPropertyChanged("SelectedSourceDrive"); OnPropertyChanged("SourceDriveInfo"); }
        }

        public DriveInfo SelectedDestinationDrive
        {
            get { return _selectedDestinationDrive; }
            set { _selectedDestinationDrive = value; OnPropertyChanged("SelectedDestinationDrive"); OnPropertyChanged("DestinationDriveInfo"); }
        }

        public string DestinationDriveInfo
        {
            get { return _destinationDriveInfo; }
            set { _destinationDriveInfo = value; OnPropertyChanged("DestinationDriveInfo"); }
        }

        public string SourceDriveInfo
        {
            get { return _sourceDriveInfo; }
            set { _sourceDriveInfo = value; OnPropertyChanged("SourceDriveInfo"); }
        }

        public string Status
        {
            get { return _status; }
            set { _status = value; OnPropertyChanged("Status"); }
        }

        #endregion Properties

        #region Data-Binding

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }

        #endregion Data-Binding

        /// <summary>
        /// Load all drives connected to the computer which aren't a CD.
        /// </summary>
        private void LoadDrives()
        {
            driveList.Clear();
            try
            {
                foreach (DriveInfo drive in DriveInfo.GetDrives())
                {
                    if (drive.DriveType != DriveType.CDRom)
                        driveList.Add(drive);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Backup", MessageBoxButton.OK);
            }

            cmbSource.ItemsSource = driveList;
            cmbDestination.ItemsSource = driveList;
            cmbSource.Items.Refresh();
            cmbDestination.Items.Refresh();
            cmbSource.SelectedIndex = 0;
            cmbDestination.SelectedIndex = 0;
        }

        #region Backup Methods

        /// <summary>
        /// Backup one hard drive to another.
        /// </summary>
        private async void Backup()
        {
            DriveInfo sourceDrive, destDrive;
            sourceDrive = driveList[cmbSource.SelectedIndex];
            destDrive = driveList[cmbDestination.SelectedIndex];

            string source, dest, log, whatToCopy, exclude_files, options, exclude_dir, robo;
            source = sourceDrive.Name;
            dest = destDrive.Name;
            whatToCopy = "/MIR";
            log = txtLog.Text + "\\copylog.txt";
            options = "/R:1 /W:1 /LOG:" + log;
            exclude_dir = "/XD " + source + "System Volume Information " + source + "$RECYCLE.BIN " + dest + "Save";
            exclude_files = "/XF " + source + "Icon.ico";
            robo = source + " " + dest + " " + whatToCopy + " " + options + " " + exclude_dir + " " + exclude_files;

            if (!Directory.Exists(txtLog.Text))
                Directory.CreateDirectory(txtLog.Text);

            try
            {
                Status = "Backing Up...";
                btnBackup.IsEnabled = false;
                btnRefresh.IsEnabled = false;
                cmbSource.IsEnabled = false;
                cmbDestination.IsEnabled = false;

                Process proc = new Process();
                proc.StartInfo.RedirectStandardOutput = true;
                proc.StartInfo.FileName = "Robocopy.exe";
                proc.StartInfo.Arguments = robo;
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.CreateNoWindow = true;
                proc.EnableRaisingEvents = true;
                proc.Start();
                await proc.WaitForExitAsync();

                BackupComplete();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Backup", MessageBoxButton.OK);
            }
        }

        /// <summary>
        /// Re-enable buttons when backup is complete.
        /// </summary>
        private void BackupComplete()
        {
            btnBackup.IsEnabled = true;
            btnRefresh.IsEnabled = true;
            cmbSource.IsEnabled = true;
            cmbDestination.IsEnabled = true;
            Status = "Backup Complete!";
        }

        #endregion Backup Methods

        #region Button Click Methods

        private void btnBackup_Click(object sender, RoutedEventArgs e)
        {
            DriveInfo sourceDrive, destDrive;
            sourceDrive = driveList[cmbSource.SelectedIndex];
            destDrive = driveList[cmbDestination.SelectedIndex];

            if (cmbDestination.SelectedIndex >= 0 && cmbSource.SelectedIndex >= 0 && txtLog.Text.Length > 0)
            {
                if (sourceDrive != destDrive)
                {
                    if (sourceDrive.TotalSize - sourceDrive.TotalFreeSpace <= destDrive.TotalSize)
                    {
                        Backup();
                    }
                    else
                        MessageBox.Show("Destination drive is too small to backup this drive. Please select another.", "Backup", MessageBoxButton.OK);
                }
                else
                    MessageBox.Show("Source drive and destination drive must be different.", "Backup", MessageBoxButton.OK);
            }
            else { MessageBox.Show("Please select both a source and destination drive and type a valid log location.", "Backup", MessageBoxButton.OK); }
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadDrives();
            Status = "";
        }

        #endregion Button Click Methods

        /// <summary>
        /// Sets the text for the TextBox of a selected drive.
        /// </summary>
        /// <param name="selectedDrive">Drive the user selected</param>
        /// <returns>Information about the selected Drive</returns>
        private string SetDriveInformationText(DriveInfo selectedDrive)
        {
            return "Drive Letter: " + selectedDrive.Name + nl +
                               "Drive Label: " + selectedDrive.VolumeLabel + nl +
                               "Drive Type: " + selectedDrive.DriveType + nl +
                               "Drive Format: " + selectedDrive.DriveFormat + nl +
                               "Total Capacity: " + string.Format("{0:0,0}", selectedDrive.TotalSize) + nl +
                               "Total Free Space: " + string.Format("{0:0,0}", selectedDrive.TotalFreeSpace) + nl +
                               "Available Free Space: " + string.Format("{0:0,0}", selectedDrive.AvailableFreeSpace);
        }

        #region Window-Manipulation Methods

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            LoadDrives();
        }

        private void cmbSource_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbSource.SelectedIndex >= 0)
            {
                SourceDriveInfo = SetDriveInformationText((DriveInfo)cmbSource.SelectedValue);
                if (cmbDestination.SelectedIndex >= 0)
                    btnBackup.IsEnabled = true;
                else
                    btnBackup.IsEnabled = false;
            }
            else
                SourceDriveInfo = "";

            Status = "";
        }

        private void cmbDestination_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbDestination.SelectedIndex >= 0)
            {
                DestinationDriveInfo = SetDriveInformationText((DriveInfo)cmbDestination.SelectedValue);
                if (cmbSource.SelectedIndex >= 0)
                    btnBackup.IsEnabled = true;
                else
                    btnBackup.IsEnabled = false;
            }
            else
                DestinationDriveInfo = "";

            Status = "";
        }

        #endregion Window-Manipulation Methods
    }
}