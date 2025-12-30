namespace ZTP_Andrzej_Jasiński;

// Program powinien działać w pętli for przetwarzając odpowiednio dużo obiektów (np. zdjęć).
// Mogą być inne obiekty i inne obliczenia na nich, niż zdjęcia.
// W przypadku zdjęć pamiętać, że bitmapa trafia do pamięci niezarządzalnej, trzeba ją usuwać przez Dispose()
// oraz wykorzystać odpowiadającą jej tablicę bitów w pamięci zarządzalnej.

public class Program
{
    static void Main(string[] args)
    {
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
    }
}

// 2. Przetestować wykorzystanie instrukcji SIMD (vector w najprostszym przypadku, opcjonalnie innych)
// - wpływ na szybkość działania programu.

// 3. Przetestować wykorzystanie wielowątkowości (Task lub Thread, ilość wątków w zależności od
// liczby rdzeni i liczby i złożoności zadań) oraz ustawień Process Affinity
// i Thread Affinity w systemach jedno i wieloprocesorowych (na razie tylko w jednoprocesorowych)
// i ich wpływ na czas działania programu i wykorzystanie pamięci w zależności od wielkości zadań.

// 4. Na przyszłość: systemy rozproszone (Rabbit MQ, opcjonalnie  inne),
// przetestować skalowanie czasu wykonania w zależności od wielkości zadań.

// 5. Na przyszłość: obliczenia na kartach GPU z wykorzystaniem bezpośrednio CUDA Toolkit (w C++)
// jako biblioteki do programu w C# oraz z wykorzystaniem dowolnej biblioteki do C# korzystającej z CUDA.
// Przetestować skalowanie czasu wykonania w zależności od wielkości i złożoności zadań.

// 6. Z każdego etapu zrobić sprawozdanie dołączając wyniki pomiarów i printscreeny z programów diagnostycznych oraz wnioski.