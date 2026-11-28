// Felgi i opony z OXL (2026-11-28): w paczce WheelData = 0 (opona "0/0R0", wartość 0). Jakie klucze ma [3_wheels] / [4_wheels_rear] w configach aut. Tylko odczyt.
var cars = UnityEngine.Object.FindObjectOfType<Il2Cpp.CarBundleLoader>(true)?.CarNamesData;
int shown = 0;
for (int ci = 0; cars != null && ci < cars.Count; ci++)
{
    var data = cars[ci];
    for (int i = 0; data?.CarConfigs != null && i < data.CarConfigs.Count && shown < 6; i++)
    {
        var ini = data.CarConfigs[i];
        string s = "";
        for (int k = 0; ini?.Entries != null && k < ini.Entries.Count; k++)
        {
            var en = ini.Entries[k];
            if (en.Section == "3_wheels" || en.Section == "4_wheels_rear") s += $"{en.Section}.{en.ID}={en.Value}; ";
        }
        Print($"[probe] {data.CarID}#{i}: {s}");
        shown++;
    }
}
