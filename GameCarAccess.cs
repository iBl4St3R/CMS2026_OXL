// GameCarAccess.cs
// Dostęp do stanu auta w CMS 2027, którego interop nie daje wprost: części karoserii i CarInfoData (przebieg).
// Publiczne, bo korzysta z tego też skrypt REPL Scripts/probe_carstate.cs (test G6, G7).

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Il2CppCMS.Core.Car;
using Il2CppCMS.Core.Car.Body;
using Il2CppInterop.Runtime;

namespace CMS2026_OXL
{
    public static class GameCarAccess
    {
        // ── Karoseria ─────────────────────────────────────────────────────────────
        // 2027: CarLoader nie trzyma listy blach, a test G1 (2026-11-25) pokazał 0 CarBodyPart pod obiektem auta.
        // Dlatego najpierw dzieci auta, potem cała scena z filtrem po CarPartBase.CarLoader (część wie, do którego auta należy).
        public static List<CarBodyPart> GetBodyParts(CarLoader cl, out string source)
        {
            var result = new List<CarBodyPart>();
            source = "none";
            if (cl == null) return result;
            try
            {
                var children = cl.gameObject.GetComponentsInChildren<CarBodyPart>(true);
                for (int i = 0; i < children.Length; i++) if (children[i] != null) result.Add(children[i]);
                if (result.Count > 0) { source = "children"; return result; }
            }
            catch (Exception ex) { OXLLog.Msg($"[OXL:WEAR] body parts (children) failed: {ex.Message}"); }
            try
            {
                var all = UnityEngine.Object.FindObjectsOfType(Il2CppType.Of<CarBodyPart>(), true);
                for (int i = 0; i < all.Length; i++)
                {
                    var p = all[i]?.TryCast<CarBodyPart>();
                    if (p == null) continue;
                    CarLoader owner = null;
                    try { owner = p.CarLoader; } catch { }
                    if (owner != null && owner.Pointer == cl.Pointer) result.Add(p);
                }
                source = "scene";
            }
            catch (Exception ex) { OXLLog.Msg($"[OXL:WEAR] body parts (scene) failed: {ex.Message}"); }
            return result;
        }

        // ── CarInfoData (przebieg) ───────────────────────────────────────────────
        // Interop zwraca CarInfoData jako ref, ale przez il2cpp_runtime_invoke dostaje opakowaną KOPIĘ (G1: identyczne
        // "dane" dla każdego auta = nagłówek obiektu). Zapis w kopię nic nie zmienia. Wołamy więc natywny getter
        // bezpośrednio (pierwsze pole MethodInfo to wskaźnik na kod), który zwraca prawdziwy adres struktury.
        // Układ CarInfoDataNetwork ze zrzutu: BuyPrice @0 (uint), Mileage @4 (uint), Year @8 (ushort), CarFrom @12.
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr ByRefGetter(IntPtr self, IntPtr methodInfo);

        private static ByRefGetter _getter;
        private static IntPtr _getterMethodInfo;
        private static bool _getterResolved;

        private static bool TryGetCarInfoPtr(CarLoader cl, out IntPtr ptr)
        {
            ptr = IntPtr.Zero;
            if (cl == null) return false;
            try
            {
                if (!_getterResolved)
                {
                    _getterResolved = true;
                    var f = typeof(CarLoader).GetField("NativeMethodInfoPtr_get_CarInfoData_Public_get_byref_CarInfoDataNetwork_0", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                    _getterMethodInfo = f != null ? (IntPtr)f.GetValue(null) : IntPtr.Zero;
                    IntPtr code = _getterMethodInfo != IntPtr.Zero ? Marshal.ReadIntPtr(_getterMethodInfo) : IntPtr.Zero;
                    if (code != IntPtr.Zero) _getter = Marshal.GetDelegateForFunctionPointer<ByRefGetter>(code);
                    if (_getter == null) OXLPlugin.Log.Warning("[OXL] CarInfoData native getter not found; mileage will not be set.");
                }
                if (_getter == null) return false;
                // Najpierw bezpieczne wywołanie przez interop: jeśli auto nie jest gotowe w sieci, wyjątek łapie runtime_invoke,
                // a nie natywny kod (bezpośrednie wywołanie przy błędzie wywróciłoby grę).
                var _ = cl.CarInfoData;
                ptr = _getter(cl.Pointer, _getterMethodInfo);
                return ptr != IntPtr.Zero;
            }
            catch (Exception ex) { OXLLog.Msg($"[OXL] CarInfoData pointer failed: {ex.Message}"); return false; }
        }

        public static bool TryReadCarInfo(CarLoader cl, out uint mileage, out ushort year)
        {
            mileage = 0; year = 0;
            if (!TryGetCarInfoPtr(cl, out var p)) return false;
            mileage = (uint)Marshal.ReadInt32(p, 4);
            year = (ushort)Marshal.ReadInt16(p, 8);
            return true;
        }

        public static bool TrySetMileage(CarLoader cl, uint mileage)
        {
            if (!TryGetCarInfoPtr(cl, out var p)) return false;
            Marshal.WriteInt32(p, 4, unchecked((int)mileage));
            return true;
        }
    }
}
