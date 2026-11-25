#!/usr/bin/env bash
# Sprawdza, czy skrypt REPL (w tym repo) kompiluje się w kopii szablonu konsoli (te same usingi, ciało Execute()).
# Użycie (Git Bash, z roota repo): bash Scripts/_check.sh Scripts/probe_x.cs
# Harness powstaje w %TEMP%/cms2026_oxl_replcheck, nie w repo i nie w folderze gry. Nic nie kopiuje do Mods.
# Szim NullableAttribute na końcu to obejście dla nowszego Roslyna z SDK; w grze nie jest potrzebny.
# Il2CppFusion.Runtime: w 2027 CarLoader dziedziczy po NetworkBehaviour; bez tej referencji każdy skrypt z autem daje CS0012.
set -euo pipefail

SCRIPT="${1:?podaj ścieżkę do skryptu .cs}"
GAME='C:\Program Files (x86)\Steam\steamapps\common\Car Mechanic Simulator 2026 Demo'
DIR="${TEMP:-/tmp}/cms2026_oxl_replcheck"
# Skrypt korzystający z nowego kodu OXL (jeszcze niezbudowanego w VS): OXL_DLL=<ścieżka Windows do DLL z _build_check.sh>.
OXLDLL="${OXL_DLL:-\$(M)\\CMS2026_OXL.dll}"
mkdir -p "$DIR"

cat > "$DIR/replcheck.csproj" <<EOF
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup><TargetFramework>net6.0</TargetFramework><LangVersion>10</LangVersion><Nullable>disable</Nullable><EnableDefaultCompileItems>false</EnableDefaultCompileItems><NoWarn>CS0618;CS8321;CS0168;CS0219</NoWarn></PropertyGroup>
  <PropertyGroup><I>$GAME\MelonLoader\Il2CppAssemblies</I><N>$GAME\MelonLoader\net6</N><M>$GAME\Mods</M></PropertyGroup>
  <ItemGroup><Compile Include="Wrapped.cs" /></ItemGroup>
  <ItemGroup>
    <Reference Include="\$(I)\Il2Cppmscorlib.dll" /><Reference Include="\$(I)\UnityEngine.CoreModule.dll" /><Reference Include="\$(I)\Assembly-CSharp.dll" />
    <Reference Include="\$(I)\UnityEngine.UI.dll" /><Reference Include="\$(I)\UnityEngine.UIModule.dll" /><Reference Include="\$(I)\UnityEngine.PhysicsModule.dll" />
    <Reference Include="\$(I)\UnityEngine.InputLegacyModule.dll" /><Reference Include="\$(I)\UnityEngine.UIElementsModule.dll" /><Reference Include="\$(I)\UnityEngine.TextRenderingModule.dll" />
    <Reference Include="\$(I)\Il2CppFusion.Runtime.dll" /><Reference Include="\$(I)\Il2CppSystem.dll" /><Reference Include="\$(I)\UnityEngine.ImageConversionModule.dll" />
    <Reference Include="\$(N)\MelonLoader.dll" /><Reference Include="\$(N)\Il2CppInterop.Runtime.dll" /><Reference Include="\$(N)\Il2CppInterop.Common.dll" />
    <Reference Include="$OXLDLL" /><Reference Include="\$(M)\_CMS2026_UITK_Framework.dll" /><Reference Include="\$(M)\BlastCoreOS.dll" />
  </ItemGroup>
</Project>
EOF

{
  printf 'using System;\nusing System.Linq;\nusing UnityEngine;\nusing UnityEngine.SceneManagement;\nusing Il2CppCMS.Player;\nusing Il2CppCMS.Scenes.Loader;\nusing Il2CppCMS.Player.Controller;\nusing Il2CppCMS.Core.Car;\nusing Il2CppCMS.SaveSystem.Containers.Car;\nusing Il2CppInterop.Runtime;\nusing UnityEngine.UI;\nusing System.Runtime.InteropServices;\n'
  printf 'public static class __Repl {\n public static Action<string> Print; public static Action<string> Log;\n public static void Execute() {\n'
  cat "$SCRIPT"
  printf '\n }\n}\n'
  printf 'namespace System.Runtime.CompilerServices { internal sealed class NullableAttribute : Attribute { public NullableAttribute(byte b){} public NullableAttribute(byte[] b){} } internal sealed class NullableContextAttribute : Attribute { public NullableContextAttribute(byte b){} } }\n'
} > "$DIR/Wrapped.cs"

# Numery linii w błędach: linia skryptu = linia w Wrapped.cs minus 15.
cd "$DIR" && dotnet build -nologo -v q 2>&1 | grep -E "error|Kompil|Build succeeded|FAILED" | sed 's/ \[.*csproj\]//' | sort -u
