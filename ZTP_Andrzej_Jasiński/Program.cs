namespace ZTP_Andrzej_Jasiński;

using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime;
using System.Threading.Channels;
using System.Threading.Tasks;

// Program powinien działać w pętli for przetwarzając odpowiednio dużo obiektów (np. zdjęć).
// Mogą być inne obiekty i inne obliczenia na nich, niż zdjęcia.
// W przypadku zdjęć pamiętać, że bitmapa trafia do pamięci niezarządzalnej, trzeba ją usuwać przez Dispose()
// oraz wykorzystać odpowiadającą jej tablicę bitów w pamięci zarządzalnej.

public class PhotoSimulator : IDisposable
{
    public byte[] Data { get; private set; }
    private bool _disposed = false;

    public PhotoSimulator(int size)
    {
        Data = new byte[size];
        // Symulacja danych - przy 1MB i 5000 iteracji alokacja zajmie chwilę
        if (size < 2000) new Random().NextBytes(Data); 
    }

    public void ProcessScalar()
    {
        for (int i = 0; i < Data.Length; i++)
            Data[i] = (byte)(Data[i] + 10);
    }

    public void ProcessSIMD()
    {
        int vectorSize = Vector<byte>.Count;
        int i = 0;
        var vectorOffset = new Vector<byte>(10);
        for (; i <= Data.Length - vectorSize; i += vectorSize)
        {
            var vector = new Vector<byte>(Data, i);
            var result = Vector.Add(vector, vectorOffset);
            result.CopyTo(Data, i);
        }
        for (; i < Data.Length; i++) Data[i] = (byte)(Data[i] + 10);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            Data = null;
            _disposed = true;
        }
    }
}

public class Program
{
    // KONFIGURACJA GŁÓWNA
    const int PHOTO_SIZE = 1_000_000; // 1 MB (LOH)
    const int ITERATIONS = 3000;      

    static void Main(string[] args)
    {
        Console.WriteLine($"=== START ZBIORCZEGO TESTU ===");
        Console.WriteLine($"Data: {DateTime.Now}");
        Console.WriteLine($"System: {Environment.OSVersion}");
        Console.WriteLine($"Liczba rdzeni: {Environment.ProcessorCount}");
        
        // 1. Przetestować wpływ poszczególnych parametrów Garbage Collectora na szybkość działania programu i
        // zajętość pamięci oraz na alokację pamięci na stertach poszczególnych generacji (dot Memory)
        // https://learn.microsoft.com/en-us/dotnet/core/runtime-config/garbage-collector

        // parametry do optymalizacji:
        // - server mode / workstation mode
        // - Large object heap threshold (aby to sprawdzić trzeba mieć w programie na stercie zarządzalnej obiekty o takiej wielkości, by raz znalazły się pod, a raz nad tym thresholdem)
        // - Heap hard limit lub Heap hard limit percent (aby to sprawdzić trzeba mieć w programie na stercie zarządzalnej obiekty o takiej wielkości, by raz znalazły się pod, a raz nad tym thresholdem)
        // - High memory percent (aby to sprawdzić trzeba mieć w programie na stercie zarządzalnej obiekty o takiej wielkości, by raz znalazły się pod,a raz nad tym thresholdem)
        // - Conserve memory
        // - opcjonalnie inne parametry.
        
        Console.WriteLine($"GC Server Mode: {GCSettings.IsServerGC}"); // Wynik ustawień z runtimeconfig.json
        Console.WriteLine($"LOH Threshold Check: Obiekt {PHOTO_SIZE} B > 85000 B? Tak.");
        Console.WriteLine("--------------------------------------------------\n");

        // 2. Przetestować wykorzystanie instrukcji SIMD (vector w najprostszym przypadku, opcjonalnie innych)
        // - wpływ na szybkość działania programu.
        Console.WriteLine(">>> REALIZACJA ZADANIA 2 (SIMD vs Scalar) <<<");
        RunBenchmark("ZAD 2: Scalar (Single Thread)", parallel: false, simd: false);
        RunBenchmark("ZAD 2: SIMD (Single Thread)", parallel: false, simd: true);

        // 3. Przetestować wykorzystanie wielowątkowości (Task lub Thread, ilość wątków w zależności od
        // liczby rdzeni i liczby i złożoności zadań) oraz ustawień Process Affinity
        // i Thread Affinity w systemach jedno i wieloprocesorowych (na razie tylko w jednoprocesorowych)
        // i ich wpływ na czas działania programu i wykorzystanie pamięci w zależności od wielkości zadań.
        Console.WriteLine(">>> REALIZACJA ZADANIA 3 (Multithreading & Affinity) <<<");
        RunBenchmark("ZAD 3: Multi-Thread (SIMD)", parallel: true, simd: true);
        
        // Test Affinity (Powiązanie z rdzeniami)
        RunAffinityTest();

        // 4. Na przyszłość: systemy rozproszone (Rabbit MQ, opcjonalnie  inne),
        // przetestować skalowanie czasu wykonania w zależności od wielkości zadań.
        Console.WriteLine(">>> REALIZACJA ZADANIA 4 (Distributed/Queue Simulation) <<<");
        RunDistributedSimulation().Wait();

        // 5. Na przyszłość: obliczenia na kartach GPU z wykorzystaniem bezpośrednio CUDA Toolkit (w C++)
        // jako biblioteki do programu w C# oraz z wykorzystaniem dowolnej biblioteki do C# korzystającej z CUDA.
        // Przetestować skalowanie czasu wykonania w zależności od wielkości i złożoności zadań.
        Console.WriteLine(">>> REALIZACJA ZADANIA 5 (GPU/CUDA Simulation) <<<");
        RunGpuSimulation();

        // 6. Z każdego etapu zrobić sprawozdanie dołączając wyniki pomiarów i printscreeny z programów diagnostycznych oraz wnioski.
    }

    // --- Metody pomocnicze realizujące logikę testów ---

    static void RunBenchmark(string testName, bool parallel, bool simd)
    {
        PrepareEnvironment();
        Console.WriteLine($"[TEST] {testName}...");
        
        long startMem = GC.GetTotalMemory(true);
        int g0 = GC.CollectionCount(0), g1 = GC.CollectionCount(1), g2 = GC.CollectionCount(2);

        Stopwatch sw = Stopwatch.StartNew();

        if (parallel)
        {
            Parallel.For(0, ITERATIONS, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, i =>
            {
                using (var p = new PhotoSimulator(PHOTO_SIZE))
                {
                    if (simd) p.ProcessSIMD(); else p.ProcessScalar();
                }
            });
        }
        else
        {
            for (int i = 0; i < ITERATIONS; i++)
            {
                using (var p = new PhotoSimulator(PHOTO_SIZE))
                {
                    if (simd) p.ProcessSIMD(); else p.ProcessScalar();
                }
            }
        }

        sw.Stop();
        PrintResults(sw.ElapsedMilliseconds, startMem, GC.GetTotalMemory(false), g0, g1, g2);
    }

    static void RunAffinityTest()
    {
        PrepareEnvironment();
        Console.WriteLine($"[TEST] ZAD 3: Affinity Limit (1 Core) + MultiThread...");

        IntPtr originalAffinity = IntPtr.Zero;
        bool affinitySupported = true;

        try
        {
            Process proc = Process.GetCurrentProcess();
            originalAffinity = proc.ProcessorAffinity;
            proc.ProcessorAffinity = (IntPtr)1; // Tylko 1 rdzeń
        }
        catch (PlatformNotSupportedException)
        {
            Console.WriteLine("   ! SKIP: Zmiana Affinity nie jest wspierana na tym systemie (macOS/Linux).");
            affinitySupported = false;
        }
        catch (Exception) { affinitySupported = false; }

        if (affinitySupported)
        {
            long startMem = GC.GetTotalMemory(true);
            int g0 = GC.CollectionCount(0), g1 = GC.CollectionCount(1), g2 = GC.CollectionCount(2);
            
            Stopwatch sw = Stopwatch.StartNew();
            Parallel.For(0, ITERATIONS, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, i =>
            {
                using (var p = new PhotoSimulator(PHOTO_SIZE)) p.ProcessSIMD();
            });
            sw.Stop();
            PrintResults(sw.ElapsedMilliseconds, startMem, GC.GetTotalMemory(false), g0, g1, g2);
            try { Process.GetCurrentProcess().ProcessorAffinity = originalAffinity; } catch { }
        }
    }

    static async Task RunDistributedSimulation()
    {
        PrepareEnvironment();
        Console.WriteLine($"[TEST] ZAD 4: Distributed Queue Simulation (Channels)...");
        
        long startMem = GC.GetTotalMemory(true);
        Stopwatch sw = Stopwatch.StartNew();

        var channel = Channel.CreateBounded<int>(new BoundedChannelOptions(100) { FullMode = BoundedChannelFullMode.Wait });

        var producer = Task.Run(async () =>
        {
            for (int i = 0; i < ITERATIONS; i++) await channel.Writer.WriteAsync(i);
            channel.Writer.Complete();
        });

        int workerCount = Environment.ProcessorCount;
        Task[] workers = new Task[workerCount];
        for (int i = 0; i < workerCount; i++)
        {
            workers[i] = Task.Run(async () =>
            {
                while (await channel.Reader.WaitToReadAsync())
                {
                    while (channel.Reader.TryRead(out int item))
                    {
                        using (var p = new PhotoSimulator(PHOTO_SIZE)) p.ProcessSIMD();
                    }
                }
            });
        }

        await producer;
        await Task.WhenAll(workers);
        sw.Stop();
        PrintResults(sw.ElapsedMilliseconds, startMem, GC.GetTotalMemory(false), 0,0,0);
    }

    static void RunGpuSimulation()
    {
        PrepareEnvironment();
        Console.WriteLine($"[TEST] ZAD 5: GPU Simulation (PCIe transfer + CUDA Kernel)...");
        Stopwatch sw = Stopwatch.StartNew();

        for (int i = 0; i < ITERATIONS; i++)
        {
            double pcieLatency = 0.15; 
            Thread.Sleep(TimeSpan.FromMilliseconds(pcieLatency)); 
        }

        sw.Stop();
        Console.WriteLine($"   -> Czas: {sw.ElapsedMilliseconds} ms (Symulacja narzutu PCIe)\n");
    }

    static void PrepareEnvironment()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }

    static void PrintResults(long timeMs, long startMem, long endMem, int g0, int g1, int g2)
    {
        Console.WriteLine($"   -> Czas: {timeMs} ms");
        Console.WriteLine($"   -> Pamięć (Delta): {(endMem - startMem) / 1024.0 / 1024.0:F2} MB");
        if (g0 > 0 || g1 > 0 || g2 > 0)
        {
            Console.WriteLine($"   -> GC (Gen0/1/2): {GC.CollectionCount(0)-g0} / {GC.CollectionCount(1)-g1} / {GC.CollectionCount(2)-g2}");
        }
        Console.WriteLine("");
    }
}