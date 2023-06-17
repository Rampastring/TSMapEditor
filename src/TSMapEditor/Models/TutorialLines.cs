using Rampastring.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TSMapEditor.Extensions;

namespace TSMapEditor.Models
{
    public struct TutorialLine
    {
        public TutorialLine(int id, string text)
        {
            ID = id;
            Text = text;
        }

        public int ID;
        public string Text;
    }

    public class TutorialLines
    {
        public TutorialLines(string iniPath, Action<Action> modifyEventCallback)
        {
            this.iniPath = iniPath;
            this.modifyEventCallback = modifyEventCallback;
            SetUpFSW();
            Read();
        }

        private readonly string iniPath;
        private readonly Action<Action> modifyEventCallback;

        private readonly object locker = new object();

        private bool callbackAdded = false;

        private FileSystemWatcher fsw;

        private void Fsw_Changed(object sender, FileSystemEventArgs e)
        {
            lock (locker)
            {
                if (callbackAdded)
                    return;

                Logger.Log("Tutorial INI has been modified, adding callback to reload it.");

                AddCallback();
            }
        }

        private void AddCallback()
        {
            callbackAdded = true;
            modifyEventCallback(HandleFSW);
        }

        private void HandleFSW()
        {
            lock (locker)
            {
                callbackAdded = false;

                tutorialLines.Clear();
                Read();
            }
        }

        private void SetUpFSW()
        {
            string directoryPath = Path.GetDirectoryName(iniPath);
            if (!Directory.Exists(directoryPath))
            {
                Logger.Log($"Directory in path specified for Tutorial.ini ({directoryPath}) does not exist! Skipping file system watcher setup.");
                return;
            }

            fsw = new FileSystemWatcher(Path.GetDirectoryName(iniPath));
            fsw.Filter = Path.GetFileName(iniPath);
            fsw.EnableRaisingEvents = true;
            fsw.Changed += Fsw_Changed;
        }

        public void ShutdownFSW()
        {
            if (fsw == null)
                return;

            fsw.EnableRaisingEvents = false;
            fsw.Changed -= Fsw_Changed;
            fsw.Dispose();
            fsw = null;
        }

        private Dictionary<int, string> tutorialLines = new Dictionary<int, string>();

        public List<TutorialLine> GetLines() => tutorialLines.Select(tl => new TutorialLine(tl.Key, tl.Value)).OrderBy(tl => tl.ID).ToList();

        /// <summary>
        /// Fetches a tutorial text line with the given ID.
        /// If the text line doesn't exist, returns an empty string.
        /// </summary>
        public string GetStringByIdOrEmptyString(int id)
        {
            if (tutorialLines.TryGetValue(id, out string value))
                return value;

            return string.Empty;
        }

        private void Read()
        {
            const string TutorialSectionName = "Tutorial";

            if (!File.Exists(iniPath))
            {
                Logger.Log("File for tutorial lines does not exist! Tried to read from: " + iniPath);
                return;
            }

            Logger.Log("Reading tutorial lines from " + iniPath);

            IniFile tutorialIni;

            try
            {
                tutorialIni = new IniFileEx(iniPath);
            }
            catch (IOException ex)
            {
                Logger.Log(nameof(TutorialLines) + ": failed to read tutorial lines: " + ex.Message + ". Re-adding callback.");
                AddCallback();
                return;
            }

            var keys = tutorialIni.GetSectionKeys(TutorialSectionName);
            if (keys == null)
                return;

            foreach (string key in keys)
            {
                int id = Conversions.IntFromString(key, -1);

                if (id > -1)
                {
                    tutorialLines.Add(id, tutorialIni.GetStringValue(TutorialSectionName, key, string.Empty));
                }
            }
        }
    }
}
