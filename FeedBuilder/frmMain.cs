using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using System.Xml;
using System.Linq;
using FeedBuilder.Properties;
using System.Text.RegularExpressions;
using SettingsProviderNet;

namespace FeedBuilder
{
	public partial class frmMain : Form
	{
		public frmMain()
		{
			InitializeComponent();
		}

		#region " Private constants/variables"

        private const string DialogFilter = "Feed configuration files (*.settings)|*.settings|All files (*.*)|*.*";
		private const string DefaultFileName = "FeedBuilderSetting.settings";
		private OpenFileDialog _openDialog;

		#endregion

		private ArgumentsParser _argParser;

		#region " Properties"

        protected FeedBuilderSetting FeedSetting { get; set; }
        protected ISettingsProvider SettingsProvider { get; set; }
		public string FileName { get; set; }
		public bool ShowGui { get; set; }

		#endregion

		#region " Loading/Initialization/Lifetime"

		private void frmMain_Load(Object sender, EventArgs e)
		{
			Visible = false;
			string[] args = Environment.GetCommandLineArgs();
			// The first arg is the path to ourself
			//If args.Count >= 2 Then
			//    If File.Exists(args(1)) Then
			//        Dim p As New FeedBuilderSettingsProvider()
			//        p.LoadFrom(args(1))
			//        Me.FileName = args(1)
			//    End If
			//End If
            ISettingsStorage SettingStorage = new PortableStorage();
			// The first arg is the path to ourself
			_argParser = new ArgumentsParser(args);
            if (_argParser.HasArgs)
            {
                FileName = _argParser.FileName;
                if (!string.IsNullOrEmpty(FileName))
                {
                    if (File.Exists(FileName))
                    {
                        SettingStorage = new FileStorage(FileName);
                    }
                    else
                    {
                        _argParser.ShowGui = true;
                        _argParser.Build = false;
                        FileName = _argParser.FileName;
                        UpdateTitle();
                    }
                }
            }

            SettingsProvider = new SettingsProvider(SettingStorage);
            FeedSetting = SettingsProvider.GetSettings<FeedBuilderSetting>();

            InitializeFormSettings();

            if (_argParser.HasArgs)
            {
                if (_argParser.ShowGui) Show();
                if (_argParser.Build) Build();
                if (!_argParser.ShowGui) Close();
            }
		}

		private void InitializeFormSettings()
		{
            if (!string.IsNullOrEmpty(FeedSetting.OutputFolder) && Directory.Exists(FeedSetting.OutputFolder)) txtOutputFolder.Text = FeedSetting.OutputFolder;
            if (!string.IsNullOrEmpty(FeedSetting.FeedXML)) txtFeedXML.Text = FeedSetting.FeedXML;
            if (!string.IsNullOrEmpty(FeedSetting.BaseURL)) txtBaseURL.Text = FeedSetting.BaseURL;

            chkVersion.Checked = FeedSetting.CompareVersion;
            chkSize.Checked = FeedSetting.CompareSize;
            chkDate.Checked = FeedSetting.CompareDate;
            chkHash.Checked = FeedSetting.CompareHash;

            chkIgnoreSymbols.Checked = FeedSetting.IgnoreDebugSymbols;
            chkIgnoreVsHost.Checked = FeedSetting.IgnoreVsHosting;
            chkCopyFiles.Checked = FeedSetting.CopyFiles;
            chkCleanUp.Checked = FeedSetting.CleanUp;

			ReadFiles();
			UpdateTitle();
		}

		private void UpdateTitle()
		{
			if (string.IsNullOrEmpty(FileName)) Text = "Feed Builder";
			else Text = "Feed Builder - " + FileName;
		}

		private void SaveFormSettings()
		{

            if (!string.IsNullOrEmpty(txtOutputFolder.Text.Trim()) && Directory.Exists(txtOutputFolder.Text.Trim())) FeedSetting.OutputFolder = txtOutputFolder.Text.Trim();
// ReSharper disable AssignNullToNotNullAttribute
            if (!string.IsNullOrEmpty(txtFeedXML.Text.Trim()) && Directory.Exists(Path.GetDirectoryName(txtFeedXML.Text.Trim()))) FeedSetting.FeedXML = txtFeedXML.Text.Trim();
// ReSharper restore AssignNullToNotNullAttribute
            if (!string.IsNullOrEmpty(txtBaseURL.Text.Trim())) FeedSetting.BaseURL = txtBaseURL.Text.Trim();

            FeedSetting.CompareVersion = chkVersion.Checked;
            FeedSetting.CompareSize = chkSize.Checked;
            FeedSetting.CompareDate = chkDate.Checked;
            FeedSetting.CompareHash = chkHash.Checked;

            FeedSetting.IgnoreDebugSymbols = chkIgnoreSymbols.Checked;
            FeedSetting.IgnoreVsHosting = chkIgnoreVsHost.Checked;
            FeedSetting.CopyFiles = chkCopyFiles.Checked;
            FeedSetting.CleanUp = chkCleanUp.Checked;

            FeedSetting.IncludeFiles.Clear();
			foreach (ListViewItem thisItem in lstFiles.Items) {
                if (thisItem.Checked) FeedSetting.IncludeFiles.Add(thisItem.Text);
			}
		}

		private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
		{
            SaveFormSettings();
            SettingsProvider.SaveSettings<FeedBuilderSetting>(FeedSetting);
		}

		#endregion

		#region " Commands Events"

		private void cmdBuild_Click(Object sender, EventArgs e)
		{
			Build();
		}

		private void btnOpenOutputs_Click(object sender, EventArgs e)
		{
			OpenOutputsFolder();
		}

		private void btnNew_Click(Object sender, EventArgs e)
        {
            SettingsProvider = new SettingsProvider(new PortableStorage());
            FeedSetting = SettingsProvider.ResetToDefaults<FeedBuilderSetting>();
			InitializeFormSettings();
		}

		private void btnOpen_Click(Object sender, EventArgs e)
		{
			OpenFileDialog dlg;
			if (_openDialog == null) {
				dlg = new OpenFileDialog {
					CheckFileExists = true,
					FileName = string.IsNullOrEmpty(FileName) ? DefaultFileName : FileName
				};
				_openDialog = dlg;
			} else dlg = _openDialog;
			dlg.Filter = DialogFilter;
			if (dlg.ShowDialog() != DialogResult.OK) return;

            FileName = dlg.FileName;

            SettingsProvider = new SettingsProvider(new FileStorage(FileName));
            FeedSetting = SettingsProvider.GetSettings<FeedBuilderSetting>();

			InitializeFormSettings();
		}

		private void btnSave_Click(Object sender, EventArgs e)
		{
			Save(false);
		}

		private void btnSaveAs_Click(Object sender, EventArgs e)
		{
			Save(true);
		}

		private void btnRefresh_Click(Object sender, EventArgs e)
		{
			ReadFiles();
		}

		#endregion

		#region " Options Events"

		private void cmdOutputFolder_Click(Object sender, EventArgs e)
		{
			fbdOutputFolder.SelectedPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
			if (fbdOutputFolder.ShowDialog(this) != DialogResult.OK) return;
			txtOutputFolder.Text = fbdOutputFolder.SelectedPath;
			ReadFiles();
		}

		private void cmdFeedXML_Click(Object sender, EventArgs e)
		{
			sfdFeedXML.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
			if (sfdFeedXML.ShowDialog(this) == DialogResult.OK) txtFeedXML.Text = sfdFeedXML.FileName;
		}

		private void chkIgnoreSymbols_CheckedChanged(object sender, EventArgs e)
		{
			ReadFiles();
		}

		private void chkCopyFiles_CheckedChanged(Object sender, EventArgs e)
		{
			chkCleanUp.Enabled = chkCopyFiles.Checked;
			if (!chkCopyFiles.Checked) chkCleanUp.Checked = false;
		}

		#endregion

		#region " Helper Methods "

		private void Build()
		{
			Console.WriteLine("Building NAppUpdater feed '{0}'", txtBaseURL.Text.Trim());
			if (string.IsNullOrEmpty(txtFeedXML.Text)) {
				const string msg = "The feed file location needs to be defined.\n" + "The outputs cannot be generated without this.";
				if (_argParser.ShowGui) MessageBox.Show(msg);
				Console.WriteLine(msg);
				return;
			}
			// If the target folder doesn't exist, create a path to it
			string dest = txtFeedXML.Text.Trim();
			var destDir = Directory.GetParent(new FileInfo(dest).FullName);
			if (!Directory.Exists(destDir.FullName)) Directory.CreateDirectory(destDir.FullName);

			XmlDocument doc = new XmlDocument();
			XmlDeclaration dec = doc.CreateXmlDeclaration("1.0", "utf-8", null);

			doc.AppendChild(dec);
			XmlElement feed = doc.CreateElement("Feed");
			if (!string.IsNullOrEmpty(txtBaseURL.Text.Trim())) feed.SetAttribute("BaseUrl", txtBaseURL.Text.Trim());
			doc.AppendChild(feed);

			XmlElement tasks = doc.CreateElement("Tasks");

			Console.WriteLine("Processing feed items");
			int itemsCopied = 0;
			int itemsCleaned = 0;
			int itemsSkipped = 0;
			int itemsFailed = 0;
			int itemsMissingConditions = 0;
			foreach (ListViewItem thisItem in lstFiles.Items) {
				string destFile = "";
				string folder = "";
				string filename = "";
				try {
					folder = Path.GetDirectoryName(txtFeedXML.Text.Trim());
					filename = thisItem.Text;
					if (folder != null) destFile = Path.Combine(folder, filename);
				} catch {}
				if (destFile == "" || folder == "" || filename == "") {
					string msg = string.Format("The file could not be pathed:\nFolder:'{0}'\nFile:{1}", folder, filename);
					if (_argParser.ShowGui) MessageBox.Show(msg);
					Console.WriteLine(msg);
					continue;
				}

				if (thisItem.Checked) {
					var fileInfoEx = (FileInfoEx)thisItem.Tag;
					XmlElement task = doc.CreateElement("FileUpdateTask");
					task.SetAttribute("localPath", fileInfoEx.RelativeName);

					// generate FileUpdateTask metadata items
					task.SetAttribute("lastModified", fileInfoEx.FileInfo.LastWriteTime.ToFileTime().ToString(CultureInfo.InvariantCulture));
					task.SetAttribute("fileSize", fileInfoEx.FileInfo.Length.ToString(CultureInfo.InvariantCulture));
					if (!string.IsNullOrEmpty(fileInfoEx.FileVersion)) task.SetAttribute("version", fileInfoEx.FileVersion);

					XmlElement conds = doc.CreateElement("Conditions");
					XmlElement cond;
					bool hasFirstCondition = false;

                    //File Exists
                    cond = doc.CreateElement("FileExistsCondition");
                    cond.SetAttribute("type", "or");
                    conds.AppendChild(cond);
                    

					//Version
					if (chkVersion.Checked && !string.IsNullOrEmpty(fileInfoEx.FileVersion)) {
						cond = doc.CreateElement("FileVersionCondition");
						cond.SetAttribute("what", "below");
						cond.SetAttribute("version", fileInfoEx.FileVersion);
						conds.AppendChild(cond);
						hasFirstCondition = true;
					}

					//Size
					if (chkSize.Checked) {
						cond = doc.CreateElement("FileSizeCondition");
						cond.SetAttribute("type", hasFirstCondition ? "or-not" : "not");
						cond.SetAttribute("what", "is");
						cond.SetAttribute("size", fileInfoEx.FileInfo.Length.ToString(CultureInfo.InvariantCulture));
						conds.AppendChild(cond);
					}

					//Date
					if (chkDate.Checked) {
						cond = doc.CreateElement("FileDateCondition");
						if (hasFirstCondition) cond.SetAttribute("type", "or");
						cond.SetAttribute("what", "older");
						// local timestamp, not UTC
						cond.SetAttribute("timestamp", fileInfoEx.FileInfo.LastWriteTime.ToFileTime().ToString(CultureInfo.InvariantCulture));
						conds.AppendChild(cond);
					}

					//Hash
					if (chkHash.Checked) {
						cond = doc.CreateElement("FileChecksumCondition");
						cond.SetAttribute("type", hasFirstCondition ? "or-not" : "not");
						cond.SetAttribute("checksumType", "sha256");
						cond.SetAttribute("checksum", fileInfoEx.Hash);
						conds.AppendChild(cond);
					}

					if (conds.ChildNodes.Count == 0) itemsMissingConditions++;
					task.AppendChild(conds);
					tasks.AppendChild(task);

					if (chkCopyFiles.Checked) {
						if (CopyFile(fileInfoEx.FileInfo.FullName, destFile)) itemsCopied++;
						else itemsFailed++;
					}
				} else {
					try {
						if (chkCleanUp.Checked & File.Exists(destFile)) {
							File.Delete(destFile);
							itemsCleaned += 1;
						} else itemsSkipped += 1;
					} catch (IOException) {
						itemsFailed += 1;
					}
				}
			}
			feed.AppendChild(tasks);
			doc.Save(txtFeedXML.Text.Trim());

			// open the outputs folder if we're running from the GUI or 
			// we have an explicit command line option to do so
			if (!_argParser.HasArgs || _argParser.OpenOutputsFolder) OpenOutputsFolder();
			Console.WriteLine("Done building feed.");
			if (itemsCopied > 0) Console.WriteLine("{0,5} items copied", itemsCopied);
			if (itemsCleaned > 0) Console.WriteLine("{0,5} items cleaned", itemsCleaned);
			if (itemsSkipped > 0) Console.WriteLine("{0,5} items skipped", itemsSkipped);
			if (itemsFailed > 0) Console.WriteLine("{0,5} items failed", itemsFailed);
			if (itemsMissingConditions > 0) Console.WriteLine("{0,5} items without any conditions", itemsMissingConditions);
		}

		private bool CopyFile(string sourceFile, string destFile)
		{
			// If the target folder doesn't exist, create the path to it
			var fi = new FileInfo(destFile);
			var d = Directory.GetParent(fi.FullName);
			if (!Directory.Exists(d.FullName)) CreateDirectoryPath(d.FullName);

			// Copy with delayed retry
			int retries = 3;
			while (retries > 0) {
				try {
					if (File.Exists(destFile)) File.Delete(destFile);
					File.Copy(sourceFile, destFile);
					retries = 0; // success
					return true;
				} catch (IOException) {
					// Failed... let's try sleeping a bit (slow disk maybe)
					if (retries-- > 0) Thread.Sleep(200);
				} catch (UnauthorizedAccessException) {
					// same handling as IOException
					if (retries-- > 0) Thread.Sleep(200);
				}
			}
			return false;
		}

		private void CreateDirectoryPath(string directoryPath)
		{
			// Create the folder/path if it doesn't exist, with delayed retry
			int retries = 3;
			while (retries > 0 && !Directory.Exists(directoryPath)) {
				Directory.CreateDirectory(directoryPath);
				if (retries-- < 3) Thread.Sleep(200);
			}
		}

		private void OpenOutputsFolder()
		{
			string dir = Path.GetDirectoryName(txtFeedXML.Text.Trim());
			if (dir == null) return;
			CreateDirectoryPath(dir);
			Process process = new Process {
				StartInfo = {
					UseShellExecute = true,
					FileName = dir
				}
			};
			process.Start();
		}

		private int GetImageIndex(string ext)
		{
			switch (ext.Trim('.')) {
				case "bmp":
					return 1;
				case "dll":
					return 2;
				case "doc":
				case "docx":
					return 3;
				case "exe":
					return 4;
				case "htm":
				case "html":
					return 5;
				case "jpg":
				case "jpeg":
					return 6;
				case "pdf":
					return 7;
				case "png":
					return 8;
				case "txt":
					return 9;
				case "wav":
				case "mp3":
					return 10;
				case "wmv":
					return 11;
				case "xls":
				case "xlsx":
					return 12;
				case "zip":
					return 13;
				default:
					return 0;
			}
		}

		private void ReadFiles()
		{
			if (string.IsNullOrEmpty(txtOutputFolder.Text.Trim()) || !Directory.Exists(txtOutputFolder.Text.Trim())) return;

			lstFiles.BeginUpdate();
			lstFiles.Items.Clear();

			string outputDir = txtOutputFolder.Text.Trim();
			int outputDirLength = txtOutputFolder.Text.Trim().Length;

            //var IgnoreFilter = Settings.Default.IgnoreFiles.Cast<string>().Select<string, Regex>(f =>
            //{
            //    string regexString = Regex.Escape(f);
            //    regexString = "^" + Regex.Replace(regexString, @"\\\*", ".*");
            //    regexString = Regex.Replace(regexString, @"\\\?", ".");
            //    regexString += "$";
            //    Regex regex = new Regex(regexString, RegexOptions.Compiled | RegexOptions.IgnoreCase);
            //    return regex;
            //});
            
			FileSystemEnumerator enumerator = new FileSystemEnumerator(txtOutputFolder.Text.Trim(), "*.*", true);
			foreach (FileInfo fi in enumerator.Matches()) {
				string thisFile = fi.FullName;
				if ((IsIgnorable(thisFile))) continue;
				FileInfoEx thisInfo = new FileInfoEx(thisFile,outputDirLength);
				ListViewItem thisItem = new ListViewItem(thisInfo.RelativeName, GetImageIndex(thisInfo.FileInfo.Extension));
				thisItem.SubItems.Add(thisInfo.FileVersion);
				thisItem.SubItems.Add(thisInfo.FileInfo.Length.ToString(CultureInfo.InvariantCulture));
				thisItem.SubItems.Add(thisInfo.FileInfo.LastWriteTime.ToString(CultureInfo.InvariantCulture));
				thisItem.SubItems.Add(thisInfo.Hash);
                thisItem.Checked = FeedSetting.IncludeFiles.Contains(thisInfo.RelativeName);
				thisItem.Tag = thisInfo;
				lstFiles.Items.Add(thisItem);
			}
			lstFiles.EndUpdate();
		}

		private bool IsIgnorable(string filename)
		{
			string ext = Path.GetExtension(filename);
			if ((chkIgnoreSymbols.Checked && ext == ".pdb")) return true;
			return (chkIgnoreVsHost.Checked && filename.ToLower().Contains("vshost.exe"));
		}

		private void Save(bool forceDialog)
		{
			SaveFormSettings();
			if (forceDialog || string.IsNullOrEmpty(FileName)) {
				SaveFileDialog dlg = new SaveFileDialog {
					Filter = DialogFilter,
					FileName = DefaultFileName
				};
				DialogResult result = dlg.ShowDialog();
                if (result == DialogResult.OK)
                {
                    FileName = dlg.FileName;
                    SettingsProvider = new SettingsProvider(new FileStorage(FileName));
                }
                else
                {
                    return;
                }
            }

            SettingsProvider.SaveSettings<FeedBuilderSetting>(FeedSetting);

			UpdateTitle();
		}

		#endregion

		private void frmMain_DragEnter(object sender, DragEventArgs e)
		{
			string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
			if (files.Length == 0) return;
            e.Effect = files[0].EndsWith(".settings") ? DragDropEffects.Move : DragDropEffects.None;
		}

		private void frmMain_DragDrop(object sender, DragEventArgs e)
		{
			string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
			if (files.Length == 0) return;
			try {
                FileName = files[0];
                
                SettingsProvider = new SettingsProvider(new FileStorage(FileName));
                FeedSetting = SettingsProvider.GetSettings<FeedBuilderSetting>();

				InitializeFormSettings();
			} catch (Exception ex) {
				MessageBox.Show("The file could not be opened: \n" + ex.Message);
			}
		}
	}
}
