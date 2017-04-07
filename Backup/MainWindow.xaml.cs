using Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Backup
{
    /// <summary>Interaction logic for MainWindow.xaml</summary>
    public partial class MainWindow : INotifyPropertyChanged
    {
        private readonly List<DriveInfo> _driveList = new List<DriveInfo>();
        private DriveInfo _selectedSourceDrive, _selectedDestinationDrive;
        private string _destinationDriveInfo, _sourceDriveInfo, _status;

        #region Properties

        /// <summary>The drive selected as the source drive.</summary>
        public DriveInfo SelectedSourceDrive
        {
            get { return _selectedSourceDrive; }
            set { _selectedSourceDrive = value; OnPropertyChanged("SelectedSourceDrive"); OnPropertyChanged("SourceDriveInfo"); }
        }

        /// <summary>The drive selected as the destination drive.</summary>
        public DriveInfo SelectedDestinationDrive
        {
            get { return _selectedDestinationDrive; }
            set { _selectedDestinationDrive = value; OnPropertyChanged("SelectedDestinationDrive"); OnPropertyChanged("DestinationDriveInfo"); }
        }

        /// <summary>The information about the drive selected as the destination drive.</summary>
        public string DestinationDriveInfo
        {
            get { return _destinationDriveInfo; }
            set { _destinationDriveInfo = value; OnPropertyChanged("DestinationDriveInfo"); }
        }

        /// <summary>The information about the drive selected as the source drive.</summary>
        public string SourceDriveInfo
        {
            get { return _sourceDriveInfo; }
            set { _sourceDriveInfo = value; OnPropertyChanged("SourceDriveInfo"); }
        }

        /// <summary>The status of the current backup operation.</summary>
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

        /// <summary>Load all drives connected to the computer which aren't a CD.</summary>
        private void LoadDrives()
        {
            _driveList.Clear();
            try
            {
                foreach (DriveInfo drive in DriveInfo.GetDrives())
                {
                    if (drive.DriveType != DriveType.CDRom)
                        _driveList.Add(drive);
                }
            }
            catch (Exception ex)
            {
                new Notification(ex.Message, "Backup", NotificationButtons.OK, this).ShowDialog();
            }

            cmbSource.ItemsSource = _driveList;
            cmbDestination.ItemsSource = _driveList;
            cmbSource.Items.Refresh();
            cmbDestination.Items.Refresh();
            cmbSource.SelectedIndex = 0;
            cmbDestination.SelectedIndex = 0;
        }

        /// <summary>Toggles several controls' IsEnabled Property.</summary>
        /// <param name="toggle">Determines the IsEnabled Property of the controls</param>
        private void ToggleControls(bool toggle)
        {
            btnBackup.IsEnabled = toggle;
            btnRefresh.IsEnabled = toggle;
            cmbSource.IsEnabled = toggle;
            cmbDestination.IsEnabled = toggle;
        }

        /// <summary>Sets the text for the TextBox of a selected drive.</summary>
        /// <param name="selectedDrive">Drive the user selected</param>
        /// <returns>Information about the selected Drive</returns>
        private string SetDriveInformationText(DriveInfo selectedDrive)
        {
            return "Drive Letter: " + selectedDrive.Name +
                               "\nDrive Label: " + selectedDrive.VolumeLabel +
                               "\nDrive Type: " + selectedDrive.DriveType +
                               "\nDrive Format: " + selectedDrive.DriveFormat +
                               "\nTotal Capacity: " + $"{selectedDrive.TotalSize:0,0}" +
                               "\nTotal Free Space: " + $"{selectedDrive.TotalFreeSpace:0,0}" +
                               "\nAvailable Free Space: " + $"{selectedDrive.AvailableFreeSpace:0,0}";
        }

        #region Backup Methods

        /// <summary>Backup one hard drive to another.</summary>
        private async void Backup()
        {
            DriveInfo sourceDrive = _driveList[cmbSource.SelectedIndex];
            DriveInfo destDrive = _driveList[cmbDestination.SelectedIndex];

            string source = sourceDrive.Name;
            string dest = destDrive.Name;
            string whatToCopy = "";
            if (chkMirror.IsChecked != null && chkMirror.IsChecked.Value)
                whatToCopy += "/MIR";
            if (chkPurge.IsChecked != null && chkPurge.IsChecked.Value)
                whatToCopy += whatToCopy.Length > 0 ? " /PURGE" : "/PURGE";
            if (chkCopyE.IsChecked != null && chkCopyE.IsChecked.Value)
                whatToCopy += whatToCopy.Length > 0 ? " /E" : "/E";
            if (chkCopyS.IsChecked != null && chkCopyS.IsChecked.Value)
                whatToCopy += whatToCopy.Length > 0 ? " /S" : "/S";

            string log = "";
            if (chkLog.IsChecked != null && chkLog.IsChecked.Value)
                log = chkLogPlus.IsChecked != null && chkLogPlus.IsChecked.Value ? "/LOG+:" + txtLogLocation.Text : "/LOG:" + txtLogLocation.Text;

            string options = "";
            if (chkRetryCount.IsChecked != null && chkRetryCount.IsChecked.Value)
                options += "/R:" + txtRetryCount.Text;
            if (chkRetryWait.IsChecked != null && chkRetryWait.IsChecked.Value)
                options += " /W:" + txtRetryWait.Text;

            if (txtCustomCommand.Text.Length > 0)
                options += " " + txtCustomCommand.Text;

            options += " " + log;

            string exclude_dir = "";
            if (chkExcludeDirectories.IsChecked != null && chkExcludeDirectories.IsChecked.Value)
            {
                exclude_dir += "/XD";

                if (txtXD1.Text.Length > 0)
                    exclude_dir += " \"" + source + txtXD1.Text + "\"";
                if (txtXD2.Text.Length > 0)
                    exclude_dir += " \"" + source + txtXD2.Text + "\"";
                if (txtXD3.Text.Length > 0)
                    exclude_dir += " \"" + source + txtXD3.Text + "\"";
                if (txtXD4.Text.Length > 0)
                    exclude_dir += " \"" + source + txtXD4.Text + "\"";
                if (txtXD5.Text.Length > 0)
                    exclude_dir += " \"" + source + txtXD5.Text + "\"";
            }

            string exclude_files = "";
            if (chkExcludeFiles.IsChecked != null && chkExcludeFiles.IsChecked.Value)
            {
                exclude_files += "/XF";

                if (txtXF1.Text.Length > 0)
                    exclude_files += " \"" + source + txtXF1.Text + "\"";
                if (txtXF2.Text.Length > 0)
                    exclude_files += " \"" + source + txtXF2.Text + "\"";
                if (txtXF3.Text.Length > 0)
                    exclude_files += " \"" + source + txtXF3.Text + "\"";
                if (txtXF4.Text.Length > 0)
                    exclude_files += " \"" + source + txtXF4.Text + "\"";
                if (txtXF5.Text.Length > 0)
                    exclude_files += " \"" + source + txtXF5.Text + "\"";
            }

            string robo = source + " " + dest + " " + whatToCopy + " " + options + " " + exclude_dir + " " + exclude_files;

            if (!Directory.Exists(txtLogLocation.Text.Substring(0, txtLogLocation.Text.LastIndexOf("\\") + 1)))
                Directory.CreateDirectory(txtLogLocation.Text.Substring(0, txtLogLocation.Text.LastIndexOf("\\") + 1));

            try
            {
                Status = "Backing Up...";
                ToggleControls(false);
                Process proc = new Process
                {
                    StartInfo =
                    {
                        RedirectStandardOutput = true,
                        FileName = "Robocopy.exe",
                        Arguments = robo,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    },
                    EnableRaisingEvents = true
                };
                proc.Start();
                await proc.WaitForExitAsync();

                BackupComplete();
            }
            catch (Exception ex)
            {
                new Notification(ex.Message, "Backup", NotificationButtons.OK, this).ShowDialog();
            }
        }

        /// <summary>Re-enable buttons when backup is complete.</summary>
        private void BackupComplete()
        {
            ToggleControls(true);
            SourceDriveInfo = SetDriveInformationText(SelectedSourceDrive);
            DestinationDriveInfo = SetDriveInformationText(SelectedDestinationDrive);
            Status = "Backup Complete!";
        }

        #endregion Backup Methods

        #region Button Click Methods

        private void btnBackup_Click(object sender, RoutedEventArgs e)
        {
            DriveInfo sourceDrive = _driveList[cmbSource.SelectedIndex];
            DriveInfo destDrive = _driveList[cmbDestination.SelectedIndex];

            if (cmbDestination.SelectedIndex >= 0 && cmbSource.SelectedIndex >= 0)
            {
                if (sourceDrive != destDrive)
                {
                    if (sourceDrive.TotalSize - sourceDrive.TotalFreeSpace <= destDrive.TotalSize)
                        Backup();
                    else
                        new Notification("Destination drive is too small to backup this drive. Please select another.", "Backup", NotificationButtons.OK, this).ShowDialog();
                }
                else
                    new Notification("Source drive and destination drive must be different.", "Backup", NotificationButtons.OK, this).ShowDialog();
            }
            else
                new Notification("Please select both a source and destination drive.", "Backup", NotificationButtons.OK, this).ShowDialog();
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadDrives();
            Status = "";
        }

        #endregion Button Click Methods

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
                SelectedSourceDrive = (DriveInfo)cmbSource.SelectedValue;
                SourceDriveInfo = SetDriveInformationText(SelectedSourceDrive);
                btnBackup.IsEnabled = cmbDestination.SelectedIndex >= 0;
            }
            else
                SourceDriveInfo = "";

            Status = "";
        }

        private void cmbDestination_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbDestination.SelectedIndex >= 0)
            {
                SelectedDestinationDrive = (DriveInfo)cmbDestination.SelectedValue;
                DestinationDriveInfo = SetDriveInformationText(SelectedDestinationDrive);
                btnBackup.IsEnabled = cmbSource.SelectedIndex >= 0;
            }
            else
                DestinationDriveInfo = "";

            Status = "";
        }

        private void txtRetry_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            Functions.PreviewKeyDown(e, KeyType.Integers);
        }

        private void textBox_GotFocus(object sender, RoutedEventArgs e)
        {
            Functions.TextBoxGotFocus(sender);
        }

        #endregion Window-Manipulation Methods
    }
}