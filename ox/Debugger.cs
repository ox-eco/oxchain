using System;

namespace OX
{
    public static class Debugger
    {
        public static bool Running { get; set; }
        public static int Level { get; set; } = 1;
        public static void Log(string source, string message, int level = 1)
        {
            if (Running && Level.Contains(level))
            {
                //Plugin.Log(source, level, $"{DateTime.Now.ToString("HH:mm:ss")}:{message}");
                Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}]({source}):{message}");
            }
        }
    }
}
