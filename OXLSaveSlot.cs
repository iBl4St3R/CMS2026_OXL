using System;
using System.IO;

namespace CMS2026_OXL
{
    /// <summary>
    /// Ogłoszenia OXL osobno dla każdego zapisu gry (ZADANIE pkt 3, site.md 17). Pliki (listings.dat, items.dat) leżą w kopii roboczej zapisu
    /// BlastCoreOS (BlastCoreProfileStorage.DataRoot = Profiles/Profile{slot}/Session/OXL): zapis gry kopiuje ją do Saved, wyjście bez zapisu
    /// ją porzuca, jak pulpity i bank BlastCoreOS. Nowa kariera = świeże ogłoszenia.
    /// Dopóki slot nie jest znany (pierwsze klatki garażu, menu), OXL nie czyta i nie pisze ogłoszeń: inaczej trafiłyby do poprzedniego zapisu.
    /// Ustawienia (CMS2026_OXL.cfg) i wiedza o grze (engine_parts.txt) zostają wspólne.
    /// </summary>
    public static class OXLSaveSlot
    {
        public static bool Ready { get; private set; }
        public static int Slot { get; private set; } = -1;
        private static string _dir;

        /// <summary>Ścieżka pliku ogłoszeń bieżącego zapisu; null, gdy slot jeszcze nieznany (wtedy nie czytamy i nie zapisujemy).</summary>
        public static string PathOf(string file) => Ready ? Path.Combine(_dir, file) : null;

        /// <summary>Wejście do garażu albo wyjście z niego: slot mógł się zmienić, czekamy na BlastCoreOS.</summary>
        public static void Reset() => Ready = false;

        /// <summary>Co klatkę w garażu, póki slot nieznany. True raz, w chwili gdy BlastCoreOS przygotował kopię roboczą zapisu: wtedy wczytać ogłoszenia.</summary>
        public static bool TryActivate()
        {
            if (Ready) return false;
            try
            {
                if (!BlastCoreOS.BlastCoreProfileStorage.IsReady) return false;
                _dir = Path.Combine(BlastCoreOS.BlastCoreProfileStorage.DataRoot, "OXL");
                Directory.CreateDirectory(_dir);
                Slot = BlastCoreOS.BlastCoreProfileStorage.Slot;
                Ready = true;
                OXLPlugin.Log.Msg($"[OXL:SAVE] listings of game save slot {Slot}: {_dir}");
                return true;
            }
            catch (Exception ex) { OXLPlugin.Log.Warning($"[OXL:SAVE] save slot folder failed: {ex.Message}"); return false; }
        }
    }
}
