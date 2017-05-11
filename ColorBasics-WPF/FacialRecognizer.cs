using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Kinect.SmartRoom.Properties;
using SkyBiometry.Client.FC;

namespace Microsoft.Kinect.SmartRoom
{
    public class FacialRecognizer
    {

        private FCClient _Client;
        private string _ApiKey = "f3e2fd74dbb741c48f980c124a9a945d";
        private string _ApiSecret = "847b5c0a60244082bdf213f3f5041e7d";
        private string _Namespace = "Kinect";

        public FacialRecognizer()
        {

            Auth();
        }

        

        public FacialRecognizer(string apiKey, string apiSecret) : this()
        {
            
            _ApiKey = apiKey;
            _ApiSecret = apiSecret;
        }

        public async Task<FCResult> Auth()
        {
            
            _Client = new FCClient(_ApiKey, _ApiSecret);
            FCResult result = await _Client.Account.AuthenticateAsync();
            return result;
        }

        public IEnumerable<string> Recognize(string path)
        {
            IEnumerable<string> users = new List<string>() { "all@" + _Namespace };
            Stream pic = File.OpenRead(path);
            FCResult faceRecognition =
                _Client.Faces.RecognizeAsync(users, null, new[] { pic }, _Namespace, Detector.Default,
                        Attributes.Default, null).Result;
            var recognizedNames = new List<string>();
            if (faceRecognition.Photos.Any() && faceRecognition.Photos[0].Tags.Any())
            {
                recognizedNames.AddRange(
                    from tag in faceRecognition.Photos[0].Tags
                    select tag.Matches.FirstOrDefault() into match
                    where match != null && match.Confidence > 50
                    select match.UserId.Split('@')[0]);
            }
            return recognizedNames;
        }
    }
}