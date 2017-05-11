using System;

namespace Microsoft.Kinect.SmartRoom
{

    public delegate void UserRecognizedEventHandler(object sender, UserRecognizedEventArgs e);

    public class UserRecognizedEventArgs : EventArgs
    {
        public UserRecognizedEventArgs(string name, DateTime ts)
        {
            Name = name;
            TimeStamp = ts;
        }

        public string Name { get; set; }
        public DateTime TimeStamp { get; set; }
    }
}