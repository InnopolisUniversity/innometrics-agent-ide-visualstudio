using System;
using System.IO;
using System.Text;

namespace InnometricsVSTracker
{
    class ConfigFile
    { 
        internal string Token { get; set; }
        internal string Url { get; set; }
        internal string Username { get; set; }
        internal string Password { get; set; }

        private readonly string _configFilepath;

        internal ConfigFile()
        {
            _configFilepath = GetConfigFilePath();
            Read();
        }

        internal void Read()
        {
            var ret = new StringBuilder(2083);

            Token = NativeMethods.GetPrivateProfileString("settings", "token", "", ret, 2083, _configFilepath) > 0
                ? ret.ToString()
                : string.Empty;

            Url = NativeMethods.GetPrivateProfileString("settings", "url", "", ret, 2083, _configFilepath) > 0
                ? ret.ToString()
                : string.Empty;

            Username = NativeMethods.GetPrivateProfileString("settings", "username", "", ret, 2083, _configFilepath) > 0
                ? ret.ToString()
                : string.Empty;

            Password = NativeMethods.GetPrivateProfileString("settings", "password", "", ret, 2083, _configFilepath) > 0
                ? ret.ToString()
                : string.Empty;
        }

        internal void Save()
        {
            if (!string.IsNullOrEmpty(Token))
                NativeMethods.WritePrivateProfileString("settings", "token", Token, _configFilepath);

            NativeMethods.WritePrivateProfileString("settings", "url", Url.Trim(), _configFilepath);
            NativeMethods.WritePrivateProfileString("settings", "username", Username.Trim(), _configFilepath);
            NativeMethods.WritePrivateProfileString("settings", "password", Password, _configFilepath);
        }

        static string GetConfigFilePath()
        {
            var homeFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(homeFolder, ".innometrics.cfg");
        }
    }
}
