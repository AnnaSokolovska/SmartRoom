using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Speech.Synthesis;
using System.Threading;
using Microsoft.Samples.Kinect.ColorBasics.Properties;

namespace Microsoft.Kinect.SmartRoom
{
    using System.Collections.Generic;

    public class UserEventController
    {
        private ConcurrentDictionary<string, DateTime> _UsedUsers = new ConcurrentDictionary<string, DateTime>();
        private TimeSpan _UserTimeoutTimeSpan;
        private Timer _UserCleaner;
        private Users _Users;
        private SpeechSynthesizer _SpeechSynthesizer;

        public UserEventController()
        {
            _SpeechSynthesizer = new SpeechSynthesizer();
            _UserTimeoutTimeSpan = Settings.Default.UserResendTimeout;
            _UserCleaner = new Timer(CleanUsedUser, null, 1000, 2000);
            string jsonFile = File.ReadAllText(Settings.Default.UsersJSonPath);
            _Users = new System.Web.Script.Serialization.JavaScriptSerializer().Deserialize<Users>(jsonFile);
            _Users.UsersList[1].RunProgram();
            GreetingGenerator.LoadGreetings(Settings.Default.GreetingCSVPath);
        }

        public void OnUserRecognized(object sender, UserRecognizedEventArgs e)
        {
            if (!_UsedUsers.ContainsKey(e.Name))
            {
                Debug.WriteLine("Event fired for" + e.Name);
                if (IsUserKnown(e.Name))
                {
                    _SpeechSynthesizer.SpeakAsync(GreetingGenerator.GetGreeting() + e.Name);
                    _UsedUsers.TryAdd(e.Name, e.TimeStamp);
                }
            }
            else
            {
                Debug.WriteLine("Already used" + e.Name);
                //update timestamp
                DateTime dt;
                _UsedUsers.TryGetValue(e.Name, out dt);
                _UsedUsers.TryUpdate(e.Name, DateTime.Now, dt);
            }   
        }

        public bool IsUserKnown(string name)
        {
            return _Users.UsersList.Any(x=>string.Equals(name, x.Name, StringComparison.InvariantCultureIgnoreCase));
        }

        private void CleanUsedUser(object stateInfo)
        {
            IEnumerable<KeyValuePair<string, DateTime>> timeouted = _UsedUsers.Where(x => DateTime.Now - x.Value > _UserTimeoutTimeSpan);
            foreach (KeyValuePair<string, DateTime> item in timeouted)
            {
                DateTime ts;
                _UsedUsers.TryRemove(item.Key, out ts);
            }
        }
    }
}