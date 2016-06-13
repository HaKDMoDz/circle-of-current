using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.IO;
using System.Windows.Forms;
using System.Reflection;
using Ini;
using System.Text.RegularExpressions;

namespace AVRProjectIDE
{
    public class EnviroSettings
    {
        public string AssemblyTitle
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyTitleAttribute), false);
                if (attributes.Length > 0)
                {
                    AssemblyTitleAttribute titleAttribute = (AssemblyTitleAttribute)attributes[0];
                    if (titleAttribute.Title != "")
                    {
                        return titleAttribute.Title;
                    }
                }
                return System.IO.Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().CodeBase);
            }
        }

        private string recentFilePath;
        private string settingsFilePath;

        private Dictionary<int, string> recentFileLookupList = new Dictionary<int, string>();
        private List<string> recentFileList = new List<string>();

        public List<string> RecentFileList
        {
            get { return recentFileList; }
            set { recentFileList = value; }
        }

        private IniFile myIni;

        public EnviroSettings()
        {
            //recentFilePath = Program.CleanFilePath(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)) + @"\" + AssemblyTitle + @"\recent.txt";
            //settingsFilePath = Program.CleanFilePath(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)) + @"\" + AssemblyTitle + @"\settings.ini";
            recentFilePath = @"recent.txt";
            settingsFilePath = @"settings.ini";
            myIni = new IniFile(settingsFilePath);

            if (LoadRecentList() == false)
            {
                MessageBox.Show("Error Loading Environment Settings");
            }
        }

        public string FilePathFromListBoxIndex(int index)
        {
            string result = "";
            if (recentFileLookupList.TryGetValue(index, out result))
            {
                return result;
            }
            else
                return "";
        }

        public void AddFileAsMostRecent(string filePath)
        {
            filePath = Program.CleanFilePath(filePath);
            while (recentFileList.Contains(filePath))
            {
                recentFileList.Remove(filePath);
            }
            recentFileList.Insert(0, filePath);
        }

        public void FillListBox(ListBox toFill)
        {
            recentFileLookupList.Clear();
            toFill.Items.Clear();
            foreach(string filePath in recentFileList)
            {
                string[] folders = filePath.Split('\\');
                string displayName = "";
                if (folders.Length - 1 >= 0)
                {
                    displayName = @"\" + folders[folders.Length - 1];
                }
                if (folders.Length - 2 >= 0)
                {
                    displayName = @"\" + folders[folders.Length - 2] + displayName;
                }
                else
                {
                    displayName = folders[0] + displayName;
                }
                if (folders.Length - 3 >= 1)
                {
                    displayName = folders[0] + @"\~~~\" + folders[folders.Length - 3] + displayName;
                }
                else
                {
                    displayName = folders[0] + displayName;
                }
                int index = toFill.Items.Add(displayName);
                recentFileLookupList.Add(index, filePath);
            }
        }

        public bool SaveRecentList()
        {
            StreamWriter writer = null;
            bool success = true;
            try
            {
                writer = new StreamWriter(recentFilePath);
                foreach (string file in recentFileList)
                {
                    string f = Program.CleanFilePath(file);
                    if (File.Exists(f))
                    {
                        writer.WriteLine(f);
                    }
                }
            }
            catch
            {
                success = false;
            }
            try
            {
                writer.Close();
            }
            catch
            {
                success = false;
            }
            
            return success;
        }

        public bool LoadRecentList()
        {
            bool success = true;
            string rFP = Program.CleanFilePath(recentFilePath);
            recentFileList.Clear();
            if (File.Exists(rFP))
            {
                StreamReader reader = null;
                try
                {
                    reader = new StreamReader(rFP);

                    int cnt = 0;
                    string line = reader.ReadLine();
                    while (line != null)
                    {
                        line = Program.CleanFilePath(line);
                        if (File.Exists(line) && line.EndsWith(".avrproj"))
                        {
                            recentFileList.Add(line);
                            cnt++;
                        }
                        line = reader.ReadLine();
                        if (cnt > 10)
                            break;
                    }
                }
                catch
                {
                    success = false;
                }
                try
                {
                    reader.Close();
                }
                catch
                {
                    success = false;
                }
            }
            return success;
        }
    }
}
