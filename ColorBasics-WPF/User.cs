using System.Collections.Generic;

namespace Microsoft.Kinect.SmartRoom
{
    using System.Diagnostics;

    public class Users
    {

        public List<User> UsersList { get; set; }
    }

    public class User
    {

        public string Name { get; set; }
        public string Program { get; set; }
        public string ProgramPath { get; set; }
        public string Audio { get; set; }
        public string AudioPath { get; set; }
        public string Video { get; set; }
        public string VideoPath { get; set; }

        public void RunProgram()
        {
            StartProcess(Program,ProgramPath);
        }

        public void StartVideo()
        {
            StartProcess(Video, VideoPath);
        }

        public void StartMusic()
        {
            StartProcess(Audio, AudioPath);
        }

        private static void StartProcess(string program, string args)
        {
            if (!string.IsNullOrEmpty(program))
            {
                Process.Start($"\"{program}\"", $"\"{args}\"");
            }
        }
    }
}