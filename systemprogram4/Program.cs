using System;
using System.Threading;

namespace systemprogram4
{
    internal class Program
    {
        static Semaphore semaphore = new Semaphore(3, 3);
        static CancellationTokenSource cts = new CancellationTokenSource();
        static CancellationToken token;
        static object lockObj = new object();

        class FileInfo
        {
            public string? Id;
            public string? Name;
            public double TotalSize;
            public string Status = "Waiting";
            public int Progress = 0;
            public double CurrentSize = 0;
        }

        static FileInfo[] files = new FileInfo[]
        {
            new FileInfo { Id = "#1", Name = "mp4_sample_file_25MB.mp4", TotalSize = 25.0 },
            new FileInfo { Id = "#2", Name = "mp4_sample_file_50MB.mp4", TotalSize = 50.0 },
            new FileInfo { Id = "#3", Name = "mp4_sample_file_100MB.mp4", TotalSize = 99.2 },
            new FileInfo { Id = "#4", Name = "mp4_sample_file_200MB.mp4", TotalSize = 200.0 }
        };

        static int activeCount = 0;
        static int waitingCount = 0;
        static int completedCount = 0;

        static void DownloadFile(object state)
        {
            var file = (FileInfo)state;

            try
            {
                token.ThrowIfCancellationRequested();

                lock (lockObj)
                {
                    waitingCount++;
                }

                semaphore.WaitOne();

                if (token.IsCancellationRequested) return;

                lock (lockObj)
                {
                    waitingCount--;
                    activeCount++;
                    file.Status = "Downloading";
                }

                int steps = 20;
                for (int i = 1; i <= steps; i++)
                {
                    if (token.IsCancellationRequested) return;
                    Thread.Sleep(200);

                    lock (lockObj)
                    {
                        file.Progress = (i * 100) / steps;
                        file.CurrentSize = Math.Round((file.TotalSize * file.Progress) / 100.0, 1);
                    }
                }

                lock (lockObj)
                {
                    activeCount--;
                    completedCount++;
                    file.Status = "Completed";
                    file.Progress = 100;
                    file.CurrentSize = file.TotalSize;
                }
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                try { semaphore.Release(); } catch { }
            }
        }

        static void DrawInterface()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("╔═════════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                 MULTI-THREAD VIDEO DOWNLOADER                       ║");
            Console.WriteLine("║                 Semaphore: MAXIMUM 3                                ║");
            Console.WriteLine("╚═════════════════════════════════════════════════════════════════════╝");

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("┌─────────────────────────────────────────────────────────────────────┐");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("  1-3  → Downloading");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("  4+   → Waiting");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("  ESC  → Cancel ALL");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("└─────────────────────────────────────────────────────────────────────┘\n");

            lock (lockObj)
            {
                foreach (var file in files)
                {
                    int totalBars = 20;
                    int filled = (file.Progress * totalBars) / 100;
                    string bar = new string('█', filled) + new string('░', totalBars - filled);

                    if (file.Status == "Downloading")
                    {
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.Write($"{file.Id,-4} Downloading   [{bar}]");
                        Console.ResetColor();
                        Console.WriteLine($"   {file.Progress,3}%  {file.CurrentSize:0.1} MB/{file.TotalSize:0.1} MB");
                    }
                    else if (file.Status == "Waiting")
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.Write($"{file.Id,-4} Waiting       [{bar}]");
                        Console.ResetColor();
                        Console.WriteLine($"   {file.Progress,3}%");
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write($"{file.Id,-4} Completed   [{bar}]");
                        Console.ResetColor();
                        Console.WriteLine($"   100%  {file.TotalSize:0.1} MB/{file.TotalSize:0.1} MB");
                    }

                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine($"     {file.Name}");
                    Console.ResetColor();
                    Console.WriteLine();
                }
            }

            Console.WriteLine("───────────────────────────────────────────────────────────────────────");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"Aktiv: {activeCount} | Gozleyen: {waitingCount} | Tamamlanan: {completedCount}");
            Console.ResetColor();
        }

        static void Main(string[] args)
        {
            token = cts.Token;
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            foreach (var file in files)
            {
                ThreadPool.QueueUserWorkItem(DownloadFile, file);
            }

            Thread monitor = new Thread(() =>
            {
                while (!token.IsCancellationRequested && completedCount < files.Length)
                {
                    DrawInterface();
                    Thread.Sleep(100);
                }
                DrawInterface();
            });
            monitor.Start();

            while (true)
            {
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true);
                    if (key.Key == ConsoleKey.Escape)
                    {
                        cts.Cancel();
                        break;
                    }
                }

                if (completedCount == files.Length)
                {
                    Thread.Sleep(1000);
                    break;
                }

                Thread.Sleep(50);
            }

            Console.WriteLine("\nButun emeliyyatlar bitdi. Cixis ucun duymeye basin.");
            Console.ReadLine();
        }
    }
}