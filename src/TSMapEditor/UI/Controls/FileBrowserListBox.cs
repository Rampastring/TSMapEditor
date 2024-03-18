using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.IO;

namespace TSMapEditor.UI.Controls
{
    public class FileSelectionEventArgs
    {
        public FileSelectionEventArgs(string filePath)
        {
            FilePath = filePath;
        }

        public string FilePath { get; }
    }

    public class FileBrowserListBox : EditorListBox
    {
        private const string DirectoryPrefix = "<DIR> ";

        public FileBrowserListBox(WindowManager windowManager) : base(windowManager)
        {
            SelectedIndexChanged += FileBrowserListBox_SelectedIndexChanged;
            DoubleLeftClick += FileBrowserListBox_DoubleLeftClick;
        }

        public event EventHandler<FileSelectionEventArgs> FileSelected;
        public event EventHandler FileDoubleLeftClick;

        private string directoryPath;
        public string DirectoryPath
        {
            get => directoryPath;
            set
            {
                if (value != null && !value.EndsWith(Path.DirectorySeparatorChar.ToString()))
                    value += Path.DirectorySeparatorChar;

                directoryPath = value;
                ListFiles();
            }
        }

        private bool IsShowingRoot => DirectoryPath == null;

        private void FileBrowserListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (IsShowingRoot)
                return;

            if (SelectedItem == null)
                return;

            if (SelectedItem.Tag != null)
                return;

            if (!DirectoryPath.EndsWith(Path.DirectorySeparatorChar.ToString()))
                directoryPath += Path.DirectorySeparatorChar;

            FileSelected?.Invoke(this, new FileSelectionEventArgs(DirectoryPath + SelectedItem.Text));
        }

        private void FileBrowserListBox_DoubleLeftClick(object sender, EventArgs e)
        {
            if (SelectedItem == null)
                return;

            if (IsShowingRoot)
            {
                // Special case - select a drive
                DirectoryPath = SelectedItem.Text;
                return;
            }

            if (SelectedIndex == 0)
            {
                // Special case -- go up a directory
                DirectoryPath = Path.GetDirectoryName(DirectoryPath.TrimEnd('/', '\\'));
                return;
            }

            if (!directoryPath.EndsWith(Path.DirectorySeparatorChar.ToString()))
                directoryPath += Path.DirectorySeparatorChar;

            if (SelectedItem.Tag != null)
            {
                // Browse to next directory
                DirectoryPath = DirectoryPath + SelectedItem.Text.Substring(DirectoryPrefix.Length) + Path.DirectorySeparatorChar;
                return;
            }

            FileSelected?.Invoke(this, new FileSelectionEventArgs(DirectoryPath + SelectedItem.Text));
            FileDoubleLeftClick?.Invoke(this, EventArgs.Empty);
        }

        private void ListFiles()
        {
            ViewTop = 0;
            SelectedIndex = -1;
            Clear();

            if (IsShowingRoot)
            {
                var drives = Directory.GetLogicalDrives();
                foreach (var drive in drives)
                    AddItem(drive);

                return;
            }

            if (string.IsNullOrWhiteSpace(DirectoryPath) || !Directory.Exists(DirectoryPath))
            {
                return;
            }

            AddItem(new XNAListBoxItem(".. <Directory Up>", Color.Gray) { Tag = new object() });

            var directories = Directory.GetDirectories(DirectoryPath);
            foreach (string dir in directories)
            {
                string dirName = dir;
                dirName = dirName.Substring(dirName.LastIndexOf(Path.DirectorySeparatorChar) + 1);
                dirName = dirName.Substring(dirName.LastIndexOf(Path.AltDirectorySeparatorChar) + 1);
                AddItem(new XNAListBoxItem(DirectoryPrefix + dirName, Color.LightGray) { Tag = new object() }); // Yay for wasting memory
            }

            var files = Directory.GetFiles(DirectoryPath);
            foreach (string file in files)
            {
                if (!Path.GetExtension(file).Equals(".map", StringComparison.InvariantCultureIgnoreCase) &&
                    !Path.GetExtension(file).Equals(".mpr", StringComparison.InvariantCultureIgnoreCase) &&
                    !Path.GetExtension(file).Equals(".yrm", StringComparison.InvariantCultureIgnoreCase))
                    continue;

                AddItem(Path.GetFileName(file));
            }
        }
    }
}
