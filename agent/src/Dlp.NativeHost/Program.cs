using System;

namespace Dlp.NativeHost;

class Program
{
    static void Main(string[] args)
    {
        Console.Error.WriteLine("[DLP NativeHost] Started. Waiting for messages on stdin...");

        // Native messaging loop will be implemented in Issue 10
        Console.Error.WriteLine("[DLP NativeHost] No message handler yet. Exiting.");
    }
}
