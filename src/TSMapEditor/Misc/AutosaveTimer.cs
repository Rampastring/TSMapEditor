using Rampastring.Tools;
using System;
using System.IO;
using TSMapEditor.Models;
using TSMapEditor.Settings;

namespace TSMapEditor.Misc
{
    public class AutosaveTimer
    {
        public AutosaveTimer(Map map) 
        {
            this.map = map;
            AutoSaveTime = TimeSpan.FromSeconds(UserSettings.Instance.AutoSaveInterval);
        }

        private readonly Map map;

        public TimeSpan AutoSaveTime { get; set; }

        private void DoSave()
        {
            map.AutoSave(Path.Combine(Path.GetDirectoryName(Environment.ProcessPath), "autosave.map"));
        }

        public string Update(TimeSpan elapsedTime)
        {
            AutoSaveTime -= elapsedTime;

            if (AutoSaveTime.TotalMilliseconds <= 0)
            {
                AutoSaveTime = TimeSpan.FromSeconds(UserSettings.Instance.AutoSaveInterval);

                try
                {
                    DoSave();
                }
                catch (Exception ex)
                {
                    if (ex is UnauthorizedAccessException || ex is IOException)
                    {
                        Logger.Log("Failed to auto-save map. Returned error message: " + ex.Message);
                        return ex.Message;
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            return null;
        }
    }
}
