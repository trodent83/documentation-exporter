"""MkDocs build hooks for GitHub Alerts and out-of-docs source-link rewriting."""

from __future__ import annotations

import os
import re
from pathlib import Path

_ALERT_HEADER_RE = re.compile(
    r"^\s*>\s*\[!(NOTE|TIP|IMPORTANT|WARNING|CAUTION)\]\s*$",
    re.IGNORECASE,
)
_BLOCKQUOTE_LINE_RE = re.compile(r"^\s*>\s?(.*)$")
_FENCE_RE = re.compile(r"^\s*(`{3,}|~{3,})")
_INLINE_CODE_OR_LINK_RE = re.compile(
    r"(`+[^`]*`+)|(\[[^\]]*\]\(([^)\s]+)\))"
)

_ALERT_MAP = {
    "NOTE": "!!! note",
    "TIP": "!!! tip",
    "IMPORTANT": '!!! info "Important"',
    "WARNING": "!!! warning",
    "CAUTION": '!!! danger "Caution"',
}

_GITHUB_REPO_URL = "https://github.com/trodent83/documentation-exporter"


def _rewrite_line_links(
    line: str,
    page_dir: Path,
    docs_dir: Path,
    repo_root: Path,
    git_ref: str,
) -> str:
    """Rewrites relative links pointing outside docs/ to GitHub blob/tree URLs."""

    def _replace(match: re.Match[str]) -> str:
        if match.group(1) is not None:
            return match.group(1)

        full_link = match.group(2)
        target = match.group(3)
        if target.startswith(("http://", "https://", "mailto:", "#", "/")):
            return full_link

        if "#" in target:
            path_part, anchor = target.split("#", 1)
            anchor_suffix = f"#{anchor}"
        else:
            path_part = target
            anchor_suffix = ""

        if not path_part:
            return full_link

        try:
            resolved = (page_dir / path_part).resolve()
        except OSError:
            return full_link

        try:
            resolved.relative_to(docs_dir)
            return full_link
        except ValueError:
            pass

        try:
            rel_repo = resolved.relative_to(repo_root).as_posix()
        except ValueError:
            return full_link

        if not resolved.exists():
            return full_link

        kind = "tree" if resolved.is_dir() else "blob"
        github_url = f"{_GITHUB_REPO_URL}/{kind}/{git_ref}/{rel_repo}{anchor_suffix}"
        return full_link.replace(f"({target})", f"({github_url})", 1)

    return _INLINE_CODE_OR_LINK_RE.sub(_replace, line)


def on_page_markdown(markdown: str, page, config, files) -> str:
    """Transforms GitHub Alerts and rewrites out-of-docs links during MkDocs build."""
    docs_dir = Path(config["docs_dir"]).resolve()
    repo_root = docs_dir.parent
    page_dir = (docs_dir / page.file.src_path).resolve().parent
    git_ref = os.environ.get("DOCS_GIT_REF", "").strip() or "main"

    lines = markdown.splitlines()
    output: list[str] = []
    index = 0
    fence_char: str | None = None
    fence_len = 0

    while index < len(lines):
        line = lines[index]
        fence_match = _FENCE_RE.match(line)
        if fence_match:
            token = fence_match.group(1)
            if fence_char is None:
                fence_char = token[0]
                fence_len = len(token)
            elif token[0] == fence_char and len(token) >= fence_len:
                fence_char = None
                fence_len = 0
            output.append(line)
            index += 1
            continue

        if fence_char is not None:
            output.append(line)
            index += 1
            continue

        alert_match = _ALERT_HEADER_RE.match(line)
        if alert_match:
            alert_kind = alert_match.group(1).upper()
            output.append(_ALERT_MAP[alert_kind])
            index += 1
            while index < len(lines):
                bq_match = _BLOCKQUOTE_LINE_RE.match(lines[index])
                if not bq_match:
                    break
                inner = _rewrite_line_links(
                    bq_match.group(1), page_dir, docs_dir, repo_root, git_ref
                )
                output.append(f"    {inner}" if inner else "")
                index += 1
            continue

        output.append(
            _rewrite_line_links(line, page_dir, docs_dir, repo_root, git_ref)
        )
        index += 1

    trailing_newline = "\n" if markdown.endswith("\n") else ""
    return "\n".join(output) + trailing_newline
