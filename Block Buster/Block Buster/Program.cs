using System;
using System.IO;

namespace Block_Buster
{
#if WINDOWS || XBOX
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.UnhandledException += new UnhandledExceptionEventHandler(ApplicationThreadException);
            using (Main game = new Main())
            {
                game.Run();
            }
        }
        static void ApplicationThreadException(object sender, UnhandledExceptionEventArgs args)
        {
            try
            {
                Exception exception = (Exception)args.ExceptionObject;
                //  using (FileStream stream = new FileStream(Path.Combine(Environment.CurrentDirectory, "crash.txt"), FileMode.Create, FileAccess.Write, FileShare.None))
                using (FileStream stream = new FileStream("crash.txt", FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    using (BinaryWriter writer = new BinaryWriter(stream))
                    {
                        writer.Write(exception.ToString());
                    }
                }
            }
            catch { }
        }
    }
#endif
}

