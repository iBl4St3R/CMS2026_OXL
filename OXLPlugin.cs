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
}