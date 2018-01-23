using System;
using System.IO;
using System.Text;

namespace GettingOverItMP.Updater
{
    public class ScriptGenerator
    {
        private enum Platform
        {
            Windows,
            Unix
        }

        private Platform platform;
        private StringBuilder builder = new StringBuilder();

        public ScriptGenerator()
        {
            var platformId = Environment.OSVersion.Platform;

            switch (platformId)
            {
                case PlatformID.MacOSX:
                case PlatformID.Unix:
                    platformId = PlatformID.Unix;
                    break;
                default:
                    platform = Platform.Windows;
                    break;
            }

            if (platform == Platform.Windows)
            {
                builder.AppendLine("@echo off");
                builder.AppendLine("cls");
            }
        }

        public ScriptGenerator DeleteFile(string filePath)
        {
            filePath = Path.GetFullPath(filePath);

            switch (platform)
            {
                default: throw new NotImplementedException();
                case Platform.Windows:
                    builder.AppendLine($"del /F /Q \"{filePath}\"");
                    break;
                case Platform.Unix:
                    builder.AppendLine($"rm -f \"{filePath}\"");
                    break;
            }

            return this;
        }

        public ScriptGenerator MoveFile(string filePath, string newFilePath)
        {
            filePath = Path.GetFullPath(filePath);
            newFilePath = Path.GetFullPath(newFilePath);

            switch (platform)
            {
                default: throw new NotImplementedException();
                case Platform.Windows:
                    builder.AppendLine($"move /Y \"{filePath}\" \"{newFilePath}\"");
                    break;
                case Platform.Unix:
                    builder.AppendLine($"mv -f \"{filePath}\" \"{newFilePath}\"");
                    break;
            }

            return this;
        }

        public string Generate()
        {
            return builder.ToString();
        }

        public string GetFileName(string fileName)
        {
            switch (platform)
            {
                case Platform.Windows:
                    return fileName + ".bat";
                case Platform.Unix:
                    return fileName + ".sh";
            }

            throw new NotImplementedException();
        }

        public ScriptGenerator SleepSeconds(uint seconds)
        {
            switch (platform)
            {
                default: throw new NotImplementedException();
                case Platform.Windows:
                    builder.AppendLine($"ping 127.0.0.1 -n {seconds + 1} > nul");
                    break;
                case Platform.Unix:
                    builder.AppendLine($"sleep({seconds})");
                    break;
            }

            return this;
        }

        public ScriptGenerator WriteLine(string message)
        {
            switch (platform)
            {
                default: throw new NotImplementedException();
                case Platform.Windows:
                case Platform.Unix:
                    builder.AppendLine($"echo {message}");
                    break;
            }

            return this;
        }

        public ScriptGenerator LaunchFile(string filePath, string arguments)
        {
            filePath = Path.GetFullPath(filePath);

            if (!string.IsNullOrEmpty(arguments))
                arguments = " " + arguments;

            switch (platform)
            {
                default: throw new NotImplementedException();
                case Platform.Windows:
                case Platform.Unix:
                    builder.AppendLine($"\"{filePath}\"{arguments}");
                    break;
            }

            return this;
        }
    }
}
