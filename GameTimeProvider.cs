// GameTimeProvider.cs
using System;
using System.Reflection;
using UnityEngine;

namespace CMS2026_OXL
{
    public static class GameTimeProvider
    {
        private static Type _tmType;
        private static Il2CppSystem.Type _tmIl2Type;   // ← Il2CppSystem.Type, nie Il2CppInterop
        private static MethodInfo _getDayMethod;
        private static MethodInfo _getHourMethod;
        private static MethodInfo _getMinuteMethod;
        private static bool _typeResolved;
        private static bool _failed;

        private static double _lastKnown = 0.0;
        private static float _lastRealtime = 0f;
        private const double FallbackMultiplier = 15.0;

        private static double _lastMinuteVal = -1.0;
        private static float _lastMinuteRealtime = 0f;

        private static bool _isReadingFromTM = false;
        public static bool IsReadingFromTM => _typeResolved && !_failed && _isReadingFromTM;



        public static double TotalGameSeconds
        {
            get
            {
                if (_failed) return Fallback();
                if (!_typeResolved) ResolveType();
                if (_failed) return Fallback();

                try
                {
                    var objs = UnityEngine.Object.FindObjectsOfType(_tmIl2Type, true);


                    if (objs == null || objs.Length == 0)
                    {
                        _isReadingFromTM = false; 
                        return Fallback();
                    }

                    var inst = Activator.CreateInstance(_tmType, new object[] { objs[0].Pointer });

                    int day = (int)_getDayMethod.Invoke(inst, null);
                    int hour = (int)_getHourMethod.Invoke(inst, null);
                    int minute = (int)_getMinuteMethod.Invoke(inst, null);

                    double minuteVal = (day - 1) * 86400.0 + hour * 3600.0 + minute * 60.0;

                    // Wykryj zmianę minuty — zapamiętaj kiedy nastąpiła
                    if (minuteVal != _lastMinuteVal)
                    {
                        _lastMinuteVal = minuteVal;
                        _lastMinuteRealtime = Time.realtimeSinceStartup;
                    }

                    // Interpoluj sekundy między tickami minuty
                    float sinceMinuteTick = Time.realtimeSinceStartup - _lastMinuteRealtime;
                    double subMinute = Math.Min(sinceMinuteTick * FallbackMultiplier, 59.0);

                    double val = minuteVal + subMinute;

                    _isReadingFromTM = true;
                    _lastKnown = val;
                    _lastRealtime = Time.realtimeSinceStartup;
                    return val;
                }
                catch
                {
                    return Fallback();
                }
            }
        }

        public static double TotalGameHours => TotalGameSeconds / 3600.0;
        public static bool IsAvailable => _typeResolved && !_failed;

        public static void Reset()
        {
            _lastKnown = 0.0;
            _lastRealtime = 0f;
            _lastMinuteVal = -1.0;
            _lastMinuteRealtime = 0f;

            _isReadingFromTM = false;
        }

        private static double Fallback()
        {
            float elapsed = Time.realtimeSinceStartup - _lastRealtime;
            return _lastKnown + elapsed * FallbackMultiplier;
        }

        private static void ResolveType()
        {
            _typeResolved = true;
            try
            {
                _tmType = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => { try { return a.GetTypes(); } catch { return Type.EmptyTypes; } })
                    .FirstOrDefault(t => t.FullName == "Il2CppCMS.Core.TimeManagement.TimeManager");

                if (_tmType == null) { _failed = true; return; }

                // ← Il2CppInterop.Runtime.Il2CppType.From zwraca Il2CppSystem.Type
                _tmIl2Type = Il2CppInterop.Runtime.Il2CppType.From(_tmType);

                var flags = BindingFlags.Public | BindingFlags.Instance;
                _getDayMethod = _tmType.GetMethod("GetCurrentDay", flags);
                _getHourMethod = _tmType.GetMethod("GetCurrentHour", flags);
                _getMinuteMethod = _tmType.GetMethod("GetCurrentMinute", flags);

                if (_getDayMethod == null || _getHourMethod == null || _getMinuteMethod == null)
                {
                    _failed = true; return;
                }

                OXLPlugin.Log.Msg("[OXL:TIME] Type resolved — fresh instance per tick");
            }
            catch (Exception ex)
            {
                OXLPlugin.Log.Msg($"[OXL:TIME] ResolveType failed: {ex.Message}");
                _failed = true;
            }
        }
    }
}