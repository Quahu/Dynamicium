using System;

namespace Dynamicium
{
    internal sealed class Program
    {
        private static void Main(string[] args)
        {
            if (!OperatingSystem.IsWindows())
            {
                Console.WriteLine("You're on your own with examples, sorry.");
                return;
            }

            // Use some winapi methods
            using (dynamic kernel32 = new Kernel32())
            {
                kernel32.Beep(2000, 100);

                var currentThreadId = kernel32.GetCurrentThreadId<uint>();
                Console.WriteLine($"Current thread ID is {currentThreadId}.");
            }

            using (dynamic user32 = new User32())
            {
                var monitorCount = user32.GetSystemMetrics<int>(0x50);
                var (x, y) = (user32.GetSystemMetrics<int>(0), user32.GetSystemMetrics<int>(1));

                const string title = "Important Info";
                var message = $"You have {monitorCount} monitors.\n"
                    + $"Your primary monitor's resolution is {x}x{y}.";

                // Null arguments are replaced with null pointers.
                user32.MessageBoxA(null, message, title, 0x40);
            }

            // Then relax to some great OST
            using (dynamic winmm = new DynamicLibrary("winmm"))
            {
                const string fileName = "bgm001.wav"; // Huh? You don't know, pal?
                winmm.PlaySoundA(fileName, null, 0x1 | 0x8 | 0x20000);
            }

            Console.ReadLine();
        }
    }
}
