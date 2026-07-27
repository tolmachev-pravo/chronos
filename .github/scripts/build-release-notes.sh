#!/usr/bin/env bash
#
# Builds the body of draft release notes for a tag, following .github/RELEASE_TEMPLATE.md.
# The sections are left as placeholders on purpose: the final text is written by a human,
# the script only prepares the skeleton, the compare link and raw material to write from.
#
# Usage: .github/scripts/build-release-notes.sh v1.6.0
#
set -euo pipefail

tag="${1:?usage: build-release-notes.sh <tag>}"
repository_url="https://github.com/tolmachev-pravo/pet-jira-copilot"

previous_tag="$(git describe --tags --abbrev=0 --match 'v*' "${tag}^" 2>/dev/null || true)"

cat <<'NOTES'
<1–2 предложения о главном в релизе — от пользы для пользователя.>

### ✨ Новые возможности
- **<Название>** — <что это даёт пользователю> (#<PR/issue>).

### 🐛 Исправления
- <Что починили> (#<PR/issue>).

### 🧰 Прочее
- <Рефакторинг, документация, инфраструктура.>
NOTES

if [ -n "$previous_tag" ]; then
    printf '\n**Полный список изменений:** %s/compare/%s...%s\n' "$repository_url" "$previous_tag" "$tag"
fi

printf '\n<!--\nЧерновой материал — удалите этот комментарий перед публикацией.\n\n'
if [ -n "$previous_tag" ]; then
    printf 'Влито в master после %s:\n' "$previous_tag"
    git log --first-parent --pretty=format:'  %s' "${previous_tag}..${tag}"
else
    printf 'Предыдущий тег не найден, вся история до %s:\n' "$tag"
    git log --first-parent --pretty=format:'  %s' "$tag"
fi
printf '\n-->\n'
