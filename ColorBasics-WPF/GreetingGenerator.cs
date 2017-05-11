using System;
using System.IO;

namespace Microsoft.Kinect.SmartRoom
{
    public class GreetingGenerator
    {
        public static string[] Greetings;
        public static void LoadGreetings(string path)
        {
            Greetings = File.ReadAllText(path).Split(';');
        }

        public static string GetGreeting()
        {
            Random random = new Random();
            int randomNumber = random.Next(0, Greetings.Length);
            return Greetings[randomNumber] + " ";
        }
    }
}