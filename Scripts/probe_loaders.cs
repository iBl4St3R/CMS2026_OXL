// Badanie kupna auta (SpawnFailed 2026-11-28): stan wszystkich CarLoaderów i które miejsce gra daje na nowe auto. Tylko odczyt.
var places = Il2CppCMS.Garage.CarLoaderPlaces.Get();
var free = places.GetPlaceForLoadCar();
Print($"[probe] placeForLoad={(free == null ? "null" : free.gameObject.name)}");
var all = UnityEngine.Object.FindObjectsOfType<Il2CppCMS.Core.Car.CarLoader>();
foreach (var cl in all)
{
    string s = $"[probe] {cl.gameObject.name} carId='{cl.CarID}' done={cl.done} loaded={cl.IsCarLoaded()} free={(free != null && free.Pointer == cl.Pointer)}";
    Print(s);
}
