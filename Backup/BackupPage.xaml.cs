using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
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
using Extensions;
using Extensions.Enums;

namespace Backup
{
    /// <summary>
    /// Interaction logic for BackupPage.xaml
    /// </summary>
    public partial class BackupPage : INotifyPropertyChanged
    {
        private readonly List<DriveInfo> _driveList = new List<DriveInfo>();
        private DriveInfo _selectedSourceDrive, _selectedDestinationDrive;
        private string _destinationDriveInfo, _sourceDriveInfo, _status;

        internal MainWindow Window { get; set; }

        #region Properties

        /// <summary>The drive selected as the source drive.</summary>
        public DriveInfo SelectedSourceDrive
        {
            get => _selectedSourceDrive;
            set { _selectedSourceDrive = value; OnPropertyChanged("SelectedSourceDrive"); OnPropertyChanged("SourceDriveInfo"); }
        }

        /// <summary>The drive selected as the destination drive.</summary>
        public DriveInfo SelectedDestinationDrive
        {
            get => _selectedDestinationDrive;
            set { _selectedDestinationDrive = value; OnPropertyChanged("SelectedDestinationDrive"); OnPropertyChanged("DestinationDriveInfo"); }
        }

        /// <summary>The information about the drive selected as the destination drive.</summary>
        public string DestinationDriveInfo
        {
            get => _destinationDriveInfo;
            set { _destinationDriveInfo = value; OnPropertyChanged("DestinationDriveInfo"); }
        }

        /// <summary>The information about the drive selected as the source drive.</summary>
        public string SourceDriveInfo
        {
            get => _sourceDriveInfo;
            set { _sourceDriveInfo = value; OnPropertyChanged("SourceDriveInfo"); }
        }

        /// <summary>The status of the current backup operation.</summary>
        public string Status
        {
            get => _status;
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
                new Notification(ex.Message, "Backup", NotificationButton.OK, Window).ShowDialog();
            }

            CmbSource.ItemsSource = _driveList;
            CmbDestination.ItemsSource = _driveList;
            CmbSource.Items.Refresh();
            CmbDestination.Items.Refresh();
            CmbSource.SelectedIndex = 0;
            CmbDestination.SelectedIndex = 0;
        }

        /// <summary>Toggles several controls' IsEnabled Property.</summary>
        /// <param name="toggle">Determines the IsEnabled Property of the controls</param>
        private void ToggleControls(bool toggle)
        {
            BtnBackup.IsEnabled = toggle;
            BtnRefresh.IsEnabled = toggle;
            CmbSource.IsEnabled = toggle;
            CmbDestination.IsEnabled = toggle;
        }

        /// <summary>Sets the text for the TextBox of a selected drive.</summary>
        /// <param name="selectedDrive">Drive the user selected</param>
        /// <returns>Information about the selected Drive</returns>
        private static string SetDriveInformationText(DriveInfo selectedDrive) => "Drive Letter: " + selectedDrive.Name +
                   "\nDrive Label: " + selectedDrive.VolumeLabel +
                   "\nDrive Type: " + selectedDrive.DriveType +
                   "\nDrive Format: " + selectedDrive.DriveFormat +
                   "\nTotal Capacity: " + $"{selectedDrive.TotalSize:0,0}" +
                   "\nTotal Free Space: " + $"{selectedDrive.TotalFreeSpace:0,0}" +
                   "\nAvailable Free Space: " + $"{selectedDrive.AvailableFreeSpace:0,0}";

        #region Backup Methods

        /// <summary>Backup one hard drive to another.</summary>
        private async void Backup()
        {
            DriveInfo sourceDrive = _driveList[CmbSource.SelectedIndex];
            DriveInfo destDrive = _driveList[CmbDestination.SelectedIndex];

            string source = sourceDrive.Name;
            string dest = destDrive.Name;
            string whatToCopy = "";
            if (ChkMirror.IsChecked != null && ChkMirror.IsChecked.Value)
                whatToCopy += "/MIR";
            if (ChkPurge.IsChecked != null && ChkPurge.IsChecked.Value)
                whatToCopy += whatToCopy.Length > 0 ? " /PURGE" : "/PURGE";
            if (ChkCopyE.IsChecked != null && ChkCopyE.IsChecked.Value)
                whatToCopy += whatToCopy.Length > 0 ? " /E" : "/E";
            if (ChkCopyS.IsChecked != null && ChkCopyS.IsChecked.Value)
                whatToCopy += whatToCopy.Length > 0 ? " /S" : "/S";

            string log = "";
            if (ChkLog.IsChecked != null && ChkLog.IsChecked.Value)
                log = ChkLogPlus.IsChecked != null && ChkLogPlus.IsChecked.Value ? "/LOG+:" + TxtLogLocation.Text : "/LOG:" + TxtLogLocation.Text;

            string options = "";
            if (ChkRetryCount.IsChecked != null && ChkRetryCount.IsChecked.Value)
                options += "/R:" + TxtRetryCount.Text;
            if (ChkRetryWait.IsChecked != null && ChkRetryWait.IsChecked.Value)
                options += " /W:" + TxtRetryWait.Text;

            if (TxtCustomCommand.Text.Length > 0)
                options += " " + TxtCustomCommand.Text;

            options += " " + log;

            string exclude_dir = "";
            if (ChkExcludeDirectories.IsChecked != null && ChkExcludeDirectories.IsChecked.Value)
            {
                exclude_dir += "/XD";

                if (TxtXd1.Text.Length > 0)
                    exclude_dir += " \"" + source + TxtXd1.Text + "\"";
                if (TxtXd2.Text.Length > 0)
                    exclude_dir += " \"" + source + TxtXd2.Text + "\"";
                if (TxtXd3.Text.Length > 0)
                    exclude_dir += " \"" + source + TxtXd3.Text + "\"";
                if (TxtXd4.Text.Length > 0)
                    exclude_dir += " \"" + source + TxtXd4.Text + "\"";
                if (TxtXd5.Text.Length > 0)
                    exclude_dir += " \"" + source + TxtXd5.Text + "\"";
            }

            string exclude_files = "";
            if (ChkExcludeFiles.IsChecked != null && ChkExcludeFiles.IsChecked.Value)
            {
                exclude_files += "/XF";

                if (TxtXf1.Text.Length > 0)
                    exclude_files += " \"" + source + TxtXf1.Text + "\"";
                if (TxtXf2.Text.Length > 0)
                    exclude_files += " \"" + source + TxtXf2.Text + "\"";
                if (TxtXf3.Text.Length > 0)
                    exclude_files += " \"" + source + TxtXf3.Text + "\"";
                if (TxtXf4.Text.Length > 0)
                    exclude_files += " \"" + source + TxtXf4.Text + "\"";
                if (TxtXf5.Text.Length > 0)
                    exclude_files += " \"" + source + TxtXf5.Text + "\"";
            }

            string robo = source + " " + dest + " " + whatToCopy + " " + options + " " + exclude_dir + " " + exclude_files;

            if (!Directory.Exists(TxtLogLocation.Text.Substring(0, TxtLogLocation.Text.LastIndexOf("\\") + 1)))
                Directory.CreateDirectory(TxtLogLocation.Text.Substring(0, TxtLogLocation.Text.LastIndexOf("\\") + 1));

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
                new Notification(ex.Message, "Backup", NotificationButton.OK, Window).ShowDialog();
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
            DriveInfo sourceDrive = _driveList[CmbSource.SelectedIndex];
            DriveInfo destDrive = _driveList[CmbDestination.SelectedIndex];

            if (CmbDestination.SelectedIndex >= 0 && CmbSource.SelectedIndex >= 0)
            {
                if (sourceDrive != destDrive)
                {
                    if (sourceDrive.TotalSize - sourceDrive.TotalFreeSpace <= destDrive.TotalSize)
                        Backup();
                    else
                        new Notification("Destination drive is too small to backup this drive. Please select another.", "Backup", NotificationButton.OK, Window).ShowDialog();
                }
                else
                    new Notification("Source drive and destination drive must be different.", "Backup", NotificationButton.OK, Window).ShowDialog();
            }
            else
                new Notification("Please select both a source and destination drive.", "Backup", NotificationButton.OK, Window).ShowDialog();
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadDrives();
            Status = "";
        }

        #endregion Button Click Methods

        #region Window-Manipulation Methods

        private void cmbSource_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbSource.SelectedIndex >= 0)
            {
                SelectedSourceDrive = (DriveInfo)CmbSource.SelectedValue;
                SourceDriveInfo = SetDriveInformationText(SelectedSourceDrive);
                BtnBackup.IsEnabled = CmbDestination.SelectedIndex >= 0;
            }
            else
                SourceDriveInfo = "";

            Status = "";
        }

        private void cmbDestination_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbDestination.SelectedIndex >= 0)
            {
                SelectedDestinationDrive = (DriveInfo)CmbDestination.SelectedValue;
                DestinationDriveInfo = SetDriveInformationText(SelectedDestinationDrive);
                BtnBackup.IsEnabled = CmbSource.SelectedIndex >= 0;
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

        public BackupPage()
        {
            InitializeComponent();
            DataContext = this;
            LoadDrives();
        }
    }
}