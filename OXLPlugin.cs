using CMS2026UITKFramework;
using MelonLoader;
using System;
using System.Linq;

[assembly: MelonInfo(typeof(CMS2026_OXL.OXLPlugin),
    "CMS2026_OXL", "0.1.0", "Blaster")]
[assembly: MelonGame("Red Dot Games", "Car Mechanic Simulator 2026 Demo")]
[assembly: MelonGame("Red Dot Games", "Car Mechanic Simulator 2026")]

namespace CMS2026_OXL
{
    public class OXLPlugin : MelonMod
    {
        internal static MelonLogger.Instance Log => Melon<OXLPlugin>.Logger;

        private OXLPanel _panel;

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            if (!sceneName.ToLower().Contains("garage")) return;
            if (!FrameworkAPI.IsReady)
            {
                Log.Warning("[OXL] UITK Framework not ready.");
                return;
            }

            _panel = new OXLPanel();
            _panel.Build();

            TryRegisterConsole();
        }

        public override void OnUpdate()
        {
            // FIX CS0656: operator ?. zastąpiony jawnym if — unika NullableAttribute
            if (UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.F10))
            {
                if (_panel != null) _panel.Open();
            }
        }

        private void TryRegisterConsole()
        {
            try
            {
                var apiType = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => { try { return a.GetTypes(); } catch { return Type.EmptyTypes; } })
                    .FirstOrDefault(t => t.FullName == "CMS2026SimpleConsole.ConsoleAPI");

                if (apiType == null) return;

                apiType.GetMethod("RegisterMod")?.Invoke(null, new object[]
                {
                    "CMS2026_OXL",
                    "OXL — Online eX-Owner Lies",
                    "Blaster",
                    "In-game car auction marketplace for CMS 2026",
                    "https://github.com/iBl4St3R/CMS2026-OXL",
                    null, null
                });

                apiType.GetMethod("RegisterCommand")?.Invoke(null, new object[]
                {
                    "oxl_open",
                    "Toggle the OXL auction panel, 2nd time close the panel",
                    (Action<string[]>)(_ => { if (_panel != null) _panel.Toggle(); })
                });



                Log.Msg("[OXL] Registered in SimpleConsole.");
            }
            catch (Exception ex)
            {
                Log.Warning($"[OXL] Console registration failed: {ex.Message}");
            }
        }
    }




    /// <summary>
    /// Routes log messages to SimpleConsole if available, otherwise MelonLoader.
    /// </summary>
    internal static class OXLLog
    {
        private static readonly MelonLogger.Instance _log =
            new MelonLogger.Instance("CMS2026_OXL");

        private static System.Reflection.MethodInfo _consolePrint;
        private static bool _resolved;

        private static void TryResolve()
        {
            if (_resolved) return;
            _resolved = true;
            try
            {
                var t = System.AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => {
                        try { return a.GetTypes(); }
                        catch { return System.Type.EmptyTypes; }
                    })
                    .FirstOrDefault(x => x.FullName == "CMS2026SimpleConsole.ConsoleAPI");

                _consolePrint = t?.GetMethod("Print",
                    new[] { typeof(string), typeof(string) });

                if (_consolePrint == null)
                    _consolePrint = t?.GetMethod("Print",
                        new[] { typeof(string) });
            }
            catch { }
        }

        public static void Msg(string msg)
        {
            try
            {
                TryResolve();
                if (_consolePrint != null)
                {
                    var pars = _consolePrint.GetParameters();
                    if (pars.Length == 2)
                        _consolePrint.Invoke(null, new object[] { msg, "OXL" });
                    else
                        _consolePrint.Invoke(null, new object[] { msg });
                    return;
                }
            }
            catch { }
            _log.Msg(msg);
        }

        public static void Warn(string msg) => _log.Warning(msg);
        public static void Error(string msg) => _log.Error(msg);
    }
}