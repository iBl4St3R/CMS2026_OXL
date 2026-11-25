#!/usr/bin/env bash
# Sprawdza, czy mod się kompiluje względem obecnych assembly interop gry (CMS 2027), BEZ kopiowania DLL do folderu gry.
# Użycie (Git Bash, z roota repo): bash Scripts/_build_check.sh
# Bierze csproj z roota repo, usuwa target CopyToMods, kompiluje *.cs repo w %TEMP%/<repo>_buildcheck.
# Szim NullableAttribute dodawany tylko, gdy repo go nie ma (obejście dla nowszego Roslyna z SDK; w VS niepotrzebny).
set -euo pipefail

REPO="$(cd "$(dirname "$0")/.." && pwd -W)"   # pwd -W: ścieżka Windows (Git Bash)
NAME="$(basename "$REPO")"
CSPROJ="$(ls "$REPO"/*.csproj | head -1)"
DIR="${TEMP:-/tmp}/${NAME}_buildcheck"
mkdir -p "$DIR"
rm -f "$DIR"/*.csproj

sed -e '/<Target Name="CopyToMods"/,/<\/Target>/d' \
    -e '0,/<PropertyGroup>/s//<PropertyGroup><EnableDefaultCompileItems>false<\/EnableDefaultCompileItems>/' \
    -e "s#</Project>#  <ItemGroup><Compile Include=\"$REPO/**/*.cs\" Exclude=\"$REPO/bin/**;$REPO/obj/**;$REPO/Scripts/**\" /><Compile Include=\"Shim.cs\" Condition=\"Exists('Shim.cs')\" /></ItemGroup>\n</Project>#" \
    "$CSPROJ" > "$DIR/buildcheck.csproj"

rm -f "$DIR/Shim.cs"
if ! grep -rqs "class NullableAttribute" --include=*.cs "$REPO"; then
  printf 'namespace System.Runtime.CompilerServices { internal sealed class NullableAttribute : Attribute { public NullableAttribute(byte b){} public NullableAttribute(byte[] b){} } internal sealed class NullableContextAttribute : Attribute { public NullableContextAttribute(byte b){} } }\n' > "$DIR/Shim.cs"
fi

cd "$DIR" && dotnet build -nologo -v q 2>&1 | grep -E "error|Kompil|Build succeeded|FAILED" | sed 's/ \[.*csproj\]//' | sed "s#$REPO[/\\]##" | sort -u
