// GameTimeProvider.cs
using System;
using System.Reflection;
using UnityEngine;

namespace CMS2026_OXL
{
    public static class GameTimeProvider
    {
        private static Type _tmType;
        private static object _tmInstance;
        private static PropertyInfo _totalSimProp;
        private static bool _resolved;
        private static bool _failed;

        // ── Fallback state ─────────────────────────────────────────────────
        private static double _lastKnown = 0.0;
        private static float _lastRealtime = 0f;
        private const double FallbackMultiplier = 15.0;

        // ─────────────────────────────────────────────────────────────────
        //  PUBLIC API
        // ─────────────────────────────────────────────────────────────────

        /// <summary>Skumulowany czas gry w sekundach (z mnożnikiem 15×).</summary>
        public static double TotalGameSeconds
        {
            get
            {
                if (_failed) return Fallback();
                if (!_resolved) TryResolve();
                if (_failed) return Fallback();

                try
                {
                    double val = (double)_totalSimProp.GetValue(_tmInstance);
                    _lastKnown = val;
                    _lastRealtime = Time.realtimeSinceStartup;
                    return val;
                }
                catch
                {
                    _failed = true;
                    return Fallback();
                }
            }
        }

        public static double TotalGameHours => TotalGameSeconds / 3600.0;
        public static bool IsAvailable => _resolved && !_failed;

        /// <summary>Wywoływane przy przeładowaniu sceny.</summary>
        public static void Reset()
        {
            _resolved = false;
            _failed = false;
            _tmInstance = null;
            _totalSimProp = null;
        }

        // ─────────────────────────────────────────────────────────────────
        //  PRIVATE
        // ─────────────────────────────────────────────────────────────────

        private static double Fallback()
        {
            // Szacuj na podstawie Unity realtime + domyślny mnożnik 15×
            float elapsed = Time.realtimeSinceStartup - _lastRealtime;
            return _lastKnown + elapsed * FallbackMultiplier;
        }

        private static void TryResolve()
        {
            _resolved = true;
            try
            {
                _tmType = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a =>
                    {
                        try { return a.GetTypes(); }
                        catch { return Type.EmptyTypes; }
                    })
                    .FirstOrDefault(t =>
                        t.FullName == "Il2CppCMS.Core.TimeManagement.TimeManager");

                if (_tmType == null)
                {
                    OXLPlugin.Log.Msg("[OXL:TIME] TimeManager type not found — using fallback.");
                    _failed = true;
                    return;
                }

                var il2T = Il2CppInterop.Runtime.Il2CppType.From(_tmType);
                var objs = UnityEngine.Object.FindObjectsOfType(il2T, true);

                if (objs == null || objs.Length == 0)
                {
                    OXLPlugin.Log.Msg("[OXL:TIME] TimeManager instance not found — using fallback.");
                    _failed = true;
                    return;
                }

                _tmInstance = Activator.CreateInstance(_tmType,
                    new object[] { objs[0].Pointer });

                _totalSimProp = _tmType.GetProperty("totalSimulatedSeconds",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                if (_totalSimProp == null)
                {
                    OXLPlugin.Log.Msg("[OXL:TIME] totalSimulatedSeconds property not found — using fallback.");
                    _failed = true;
                    return;
                }

                // Weryfikacja
                _lastKnown = (double)_totalSimProp.GetValue(_tmInstance);
                _lastRealtime = Time.realtimeSinceStartup;

                OXLPlugin.Log.Msg(
                    $"[OXL:TIME] TimeManager OK — " +
                    $"totalSim={_lastKnown:F1}s ({_lastKnown / 3600.0:F2}h game)");
            }
            catch (Exception ex)
            {
                OXLPlugin.Log.Msg($"[OXL:TIME] Resolve failed: {ex.Message} — using fallback.");
                _failed = true;
            }
        }
    }
}