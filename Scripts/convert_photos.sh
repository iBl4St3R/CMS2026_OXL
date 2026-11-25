#!/usr/bin/env bash
# Konwersja paczki zdjęć JPG -> WebP (ImageMagick), poza grą (rebuild.md 2.4).
# Jedna rozdzielczość (ta z fotografa, 1280x720), bez wariantów low/high.
# Odtwarza układ folderów CarPhotos/{carId}/{cfg}/{kolor}/{stan}/ w katalogu wyjściowym.
# Pomija pliki, które już mają aktualny .webp (można przerwać i puścić ponownie).
# Użycie: bash Scripts/convert_photos.sh <wejście CarPhotos> <wyjście CarPhotos> [jakość=80] [wątki=8]
set -euo pipefail
IN="${1:?input CarPhotos dir}"; OUT="${2:?output dir}"; Q="${3:-80}"; P="${4:-8}"
command -v magick >/dev/null || { echo "magick not found"; exit 1; }
export IN OUT Q
cd "$IN"
find . -type f -iname '*.jpg' -print0 | xargs -0 -n 16 -P "$P" bash -c '
for f in "$@"; do
  o="$OUT/${f%.*}.webp"
  [ -f "$o" ] && [ "$o" -nt "$f" ] && continue
  mkdir -p "$(dirname "$o")"
  magick "$f" -strip -quality "$Q" -define webp:method=6 "$o.tmp.webp" && mv -f "$o.tmp.webp" "$o" || echo "FAIL $f"
done' _
n_in=$(find . -type f -iname '*.jpg' | wc -l); n_out=$(find "$OUT" -type f -name '*.webp' | wc -l)
echo "jpg: $n_in, webp: $n_out"; du -sh "$IN" "$OUT"
