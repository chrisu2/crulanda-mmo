# -*- coding: utf-8 -*-
"""Build "Crulanda: everything built so far" (a PDF) from history_content.md.

Run (from anywhere):
    python make_history_pdf.py [--content history_content.md] [--out <pdf>]
                               [--img-dir <folder>] [--captures <folder>]

Needs reportlab and Pillow. The content file is a small Markdown dialect:

    @key: value            front matter (title, subtitle, dates, cover, covernote, footer)
    # Heading              chapter: starts a page, goes in the contents
    ## Heading             section: goes in the contents
    ### Heading / ####     smaller headings, not in the contents
    plain lines            a paragraph (a blank line ends it)
    - item                 bullet ("  - item" is a nested bullet)
    > words                the owner's words: set apart, in italics
    | a | b |              a table; the first row is the header and repeats on every page
    {table: widths=20,50,30; size=7.5}   optional line before a table (widths in percent)
    {tiles} ... {/tiles}   a grid of big numbers: one "number | label" per line
    ![caption](world-captures/name.png)  a picture, taken from the captures folder
    {pagebreak}            a new page
    inline: **bold**, *italic*, `code`, and status words in square brackets such as [PUBLISHED]

Pictures are copied, downscaled to at most 1400 px wide JPEG, into --img-dir (a cache).
If the cache already holds a picture, the captures folder is not needed for it.
"""
import argparse
import os
import re
import sys
import tempfile

from reportlab.lib import colors
from reportlab.lib.enums import TA_CENTER, TA_LEFT
from reportlab.lib.pagesizes import letter
from reportlab.lib.styles import ParagraphStyle
from reportlab.lib.units import inch
from reportlab.pdfbase.pdfmetrics import stringWidth
from reportlab.platypus import (BaseDocTemplate, CondPageBreak, Frame, Image, KeepTogether,
                                LongTable, NextPageTemplate, PageBreak, PageTemplate, Paragraph,
                                Spacer, Table, TableStyle)
from reportlab.platypus.tableofcontents import TableOfContents

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.dirname(os.path.dirname(HERE))
DEFAULT_CONTENT = os.path.join(HERE, "history_content.md")
DEFAULT_OUT = os.path.join(REPO, "New Unity Project", "Docs", "Crulanda-Everything-Built-So-Far.pdf")
DEFAULT_CAPTURES = r"C:\Users\chris\Documents\Codex\2026-09-28\hel\work"

PAGE_W, PAGE_H = letter
MARGIN_X = 0.85 * inch
MARGIN_TOP = 0.95 * inch
MARGIN_BOTTOM = 0.85 * inch
FRAME_W = PAGE_W - 2 * MARGIN_X

INK = colors.HexColor("#1f2428")
MUTED = colors.HexColor("#5b6670")
ACCENT = colors.HexColor("#2f5d50")      # deep green
ACCENT2 = colors.HexColor("#8a5a1c")     # warm brown
RULE = colors.HexColor("#c9d1d6")
HEAD_BG = colors.HexColor("#e6eeea")
ROW_ALT = colors.HexColor("#f6f8f7")
QUOTE_BG = colors.HexColor("#f7f3ea")

BODY_FONT = "Helvetica"
BOLD_FONT = "Helvetica-Bold"
ITAL_FONT = "Helvetica-Oblique"
MONO_FONT = "Courier"

# ---------------------------------------------------------------- text helpers
_REPLACE = {
    u"\u2014": "-", u"\u2013": "-", u"\u2012": "-", u"\u2010": "-", u"\u2011": "-",
    u"\u2018": "'", u"\u2019": "'", u"\u201c": '"', u"\u201d": '"',
    u"\u2026": "...", u"\u2192": "->", u"\u2190": "<-", u"\u00d7": "x", u"\u2265": ">=",
    u"\u2264": "<=", u"\u00a0": " ", u"\u2022": "-", u"\u00b7": "-", u"\ufeff": "",
    u"\u2212": "-", u"\u2248": "about ",
}


def clean(text):
    """Replace characters the built-in fonts cannot draw."""
    out = []
    for ch in text:
        if ch in _REPLACE:
            out.append(_REPLACE[ch])
        elif ord(ch) < 256:
            out.append(ch)
        else:
            sys.stderr.write("warning: character %r replaced with ?\n" % ch)
            out.append("?")
    return "".join(out)


STATUS_COLOURS = {
    "PUBLISHED": "#2f6b3a", "BUILT, NOT YET PUBLISHED": "#9a5b00", "PLANNED": "#7a3b8f",
    "IN PROGRESS": "#9a5b00", "OPEN": "#a23b2a", "KEEP": "#2f6b3a", "DECLINED": "#5b6670",
    "DONE": "#2f6b3a", "NOT BUILT": "#7a3b8f",
}
_STATUS_RE = re.compile(r"\[(%s)\]" % "|".join(re.escape(k) for k in
                                                sorted(STATUS_COLOURS, key=len, reverse=True)))


def _status_tag(m):
    word = m.group(1)
    return '<font color="%s"><b>%s</b></font>' % (STATUS_COLOURS[word], word)


def inline(text):
    """Markdown-ish inline markup to reportlab's paragraph markup."""
    text = clean(text)
    text = text.replace("&", "&amp;").replace("<", "&lt;").replace(">", "&gt;")
    parts = re.split(r"(`[^`]+`)", text)   # code first, so a * inside code stays put
    for i, p in enumerate(parts):
        if p.startswith("`") and p.endswith("`") and len(p) > 2:
            parts[i] = '<font face="%s" size="-1">%s</font>' % (MONO_FONT, p[1:-1])
        else:
            p = re.sub(r"\*\*(.+?)\*\*", r"<b>\1</b>", p)
            p = re.sub(r"(?<![\w*])\*(?!\s)(.+?)(?<!\s)\*(?![\w*])", r"<i>\1</i>", p)
            parts[i] = _STATUS_RE.sub(_status_tag, p)
    return "".join(parts)


# ---------------------------------------------------------------- styles
def make_styles():
    s = {}
    s["body"] = ParagraphStyle("body", fontName=BODY_FONT, fontSize=10, leading=14, textColor=INK,
                               spaceAfter=6, alignment=TA_LEFT)
    s["bullet"] = ParagraphStyle("bullet", parent=s["body"], leftIndent=14, bulletIndent=3,
                                 spaceAfter=3)
    s["bullet2"] = ParagraphStyle("bullet2", parent=s["body"], leftIndent=28, bulletIndent=17,
                                  spaceAfter=2, fontSize=9.5, leading=13)
    s["h1"] = ParagraphStyle("h1", fontName=BOLD_FONT, fontSize=22, leading=26, textColor=ACCENT,
                             spaceAfter=10, spaceBefore=0)
    s["h2"] = ParagraphStyle("h2", fontName=BOLD_FONT, fontSize=14.5, leading=18, textColor=INK,
                             spaceBefore=14, spaceAfter=6, keepWithNext=1)
    s["h3"] = ParagraphStyle("h3", fontName=BOLD_FONT, fontSize=11.5, leading=15, textColor=ACCENT2,
                             spaceBefore=10, spaceAfter=4, keepWithNext=1)
    s["h4"] = ParagraphStyle("h4", fontName=BOLD_FONT, fontSize=10, leading=13, textColor=INK,
                             spaceBefore=6, spaceAfter=3, keepWithNext=1)
    s["quote"] = ParagraphStyle("quote", fontName=ITAL_FONT, fontSize=10, leading=14,
                                textColor=colors.HexColor("#3a2f1a"))
    s["caption"] = ParagraphStyle("caption", fontName=ITAL_FONT, fontSize=8.5, leading=11,
                                  textColor=MUTED, alignment=TA_CENTER, spaceBefore=3, spaceAfter=8)
    s["toc1"] = ParagraphStyle("toc1", fontName=BOLD_FONT, fontSize=10.5, leading=15, textColor=INK,
                               spaceBefore=6)
    s["toc2"] = ParagraphStyle("toc2", fontName=BODY_FONT, fontSize=9.5, leading=13, textColor=INK,
                               leftIndent=14)
    s["tile_num"] = ParagraphStyle("tile_num", fontName=BOLD_FONT, fontSize=19, leading=22,
                                   textColor=ACCENT, alignment=TA_CENTER)
    s["tile_lab"] = ParagraphStyle("tile_lab", fontName=BODY_FONT, fontSize=8.5, leading=11,
                                   textColor=INK, alignment=TA_CENTER)
    s["cover_title"] = ParagraphStyle("cover_title", fontName=BOLD_FONT, fontSize=30, leading=35,
                                      textColor=ACCENT, alignment=TA_CENTER)
    s["cover_sub"] = ParagraphStyle("cover_sub", fontName=BODY_FONT, fontSize=13, leading=18,
                                    textColor=INK, alignment=TA_CENTER)
    s["cover_note"] = ParagraphStyle("cover_note", fontName=ITAL_FONT, fontSize=9.5, leading=13,
                                     textColor=MUTED, alignment=TA_CENTER)
    return s


def cell_styles(size):
    lead = size * 1.25
    td = ParagraphStyle("td%s" % size, fontName=BODY_FONT, fontSize=size, leading=lead, textColor=INK)
    th = ParagraphStyle("th%s" % size, fontName=BOLD_FONT, fontSize=size, leading=lead, textColor=INK)
    return td, th


# ---------------------------------------------------------------- pictures
def prepare_image(ref, img_dir, captures):
    """Return the path of the cached JPEG for a picture reference, making it if needed."""
    base = os.path.splitext(os.path.basename(ref))[0]
    prefix = os.path.dirname(ref).replace("\\", "/").strip("/").replace("/", "-")
    name = (prefix + "-" if prefix else "") + base + ".jpg"
    cached = os.path.join(img_dir, name)
    if os.path.exists(cached):
        return cached
    src = ref if os.path.isabs(ref) else os.path.join(captures, ref)
    if not os.path.exists(src):
        return None
    from PIL import Image as PILImage
    if not os.path.isdir(img_dir):
        os.makedirs(img_dir)
    im = PILImage.open(src).convert("RGB")
    if im.width > 1400:
        im = im.resize((1400, int(im.height * 1400 / im.width)), PILImage.LANCZOS)
    im.save(cached, "JPEG", quality=84, optimize=True)
    return cached


def picture(path, caption, styles, max_w=FRAME_W, max_h=4.3 * inch):
    from PIL import Image as PILImage
    with PILImage.open(path) as im:
        w, h = im.size
    scale = min(max_w / float(w), max_h / float(h))
    img = Image(path, width=w * scale, height=h * scale)
    img.hAlign = "CENTER"
    parts = [img]
    if caption:
        parts.append(Paragraph(inline(caption), styles["caption"]))
    return KeepTogether(parts)


# ---------------------------------------------------------------- tables
def split_row(line):
    line = line.strip()
    if line.startswith("|"):
        line = line[1:]
    if line.endswith("|") and not line.endswith("\\|"):
        line = line[:-1]
    cells = re.split(r"(?<!\\)\|", line)
    return [c.strip().replace("\\|", "|") for c in cells]


def build_table(rows, opts, styles):
    size = float(opts.get("size", 8.5))
    ncol = max(len(r) for r in rows)
    rows = [r + [""] * (ncol - len(r)) for r in rows]
    total_w = FRAME_W
    pad = 4
    if "widths" in opts:
        pct = [float(x) for x in opts["widths"].split(",")]
        if len(pct) != ncol:
            raise ValueError("table has %d columns but %d widths: %r" % (ncol, len(pct), rows[0]))
        tot = sum(pct)
        widths = [total_w * p / tot for p in pct]
    else:
        # share the width by how much text each column carries,
        # and never make a column narrower than its longest word
        mins, weights = [], []
        for c in range(ncol):
            longest_word = 0.0
            lens = []
            for ri, r in enumerate(rows):
                txt = re.sub(r"[*`]", "", clean(r[c]))
                lens.append(len(txt))
                font = BOLD_FONT if ri == 0 else BODY_FONT
                for wd in re.split(r"[\s/]+", txt):
                    longest_word = max(longest_word, stringWidth(wd, font, size))
            mins.append(min(longest_word + 2 * pad + 2, total_w * 0.5))
            body = lens[1:] or lens
            avg = sum(body) / float(len(body))
            weights.append(max(3.0, min(70.0, 0.55 * avg + 0.45 * min(max(lens), 90))))
        if sum(mins) >= total_w:
            widths = [m * total_w / sum(mins) for m in mins]
        else:
            wsum = sum(weights)
            want = [max(m, total_w * w / wsum) for m, w in zip(mins, weights)]
            scale = total_w / sum(want)
            widths = [max(m, wv * scale) for m, wv in zip(mins, want)]
            over = sum(widths) - total_w
            if over > 0.5:
                flex = [wv - m for wv, m in zip(widths, mins)]
                fs = sum(flex) or 1.0
                widths = [wv - over * f / fs for wv, f in zip(widths, flex)]
    td, th = cell_styles(size)
    data = []
    for ri, r in enumerate(rows):
        st = th if ri == 0 else td
        data.append([Paragraph(inline(c), st) for c in r])
        # a single word wider than its column would run into the next one: say so
        for ci, c in enumerate(r):
            font = BOLD_FONT if ri == 0 else BODY_FONT
            for wd in re.split(r"[\s]+", re.sub(r"[*`]", "", clean(c))):
                if stringWidth(wd, font, size) > widths[ci] - 2 * pad + 0.5:
                    sys.stderr.write("warning: %r is wider than its column (table headed %r)\n"
                                     % (wd, rows[0][0]))
    t = LongTable(data, colWidths=widths, repeatRows=1, splitByRow=1)
    t.setStyle(TableStyle([
        ("BACKGROUND", (0, 0), (-1, 0), HEAD_BG),
        ("LINEBELOW", (0, 0), (-1, 0), 0.8, ACCENT),
        ("LINEBELOW", (0, 1), (-1, -1), 0.25, RULE),
        ("VALIGN", (0, 0), (-1, -1), "TOP"),
        ("LEFTPADDING", (0, 0), (-1, -1), pad), ("RIGHTPADDING", (0, 0), (-1, -1), pad),
        ("TOPPADDING", (0, 0), (-1, -1), 2.5), ("BOTTOMPADDING", (0, 0), (-1, -1), 3),
        ("ROWBACKGROUNDS", (0, 1), (-1, -1), [colors.white, ROW_ALT]),
    ]))
    t.hAlign = "LEFT"
    return t


def build_tiles(lines, styles, cols=4):
    cells = []
    for ln in lines:
        if "|" not in ln:
            continue
        num, lab = [x.strip() for x in ln.split("|", 1)]
        cells.append([Paragraph(inline(num), styles["tile_num"]),
                      Paragraph(inline(lab), styles["tile_lab"])])
    rows = []
    for i in range(0, len(cells), cols):
        row = cells[i:i + cols]
        while len(row) < cols:
            row.append("")
        rows.append(row)
    w = FRAME_W / cols
    t = Table(rows, colWidths=[w] * cols)
    t.setStyle(TableStyle([
        ("BOX", (0, 0), (-1, -1), 0.5, RULE), ("INNERGRID", (0, 0), (-1, -1), 0.5, RULE),
        ("BACKGROUND", (0, 0), (-1, -1), colors.HexColor("#fafbfa")),
        ("VALIGN", (0, 0), (-1, -1), "MIDDLE"),
        ("TOPPADDING", (0, 0), (-1, -1), 7), ("BOTTOMPADDING", (0, 0), (-1, -1), 7),
    ]))
    return t


def build_quote(text, styles):
    t = Table([[Paragraph(inline(text), styles["quote"])]], colWidths=[FRAME_W - 12])
    t.setStyle(TableStyle([
        ("LINEBEFORE", (0, 0), (0, -1), 2.2, ACCENT2),
        ("BACKGROUND", (0, 0), (-1, -1), QUOTE_BG),
        ("LEFTPADDING", (0, 0), (-1, -1), 9), ("RIGHTPADDING", (0, 0), (-1, -1), 8),
        ("TOPPADDING", (0, 0), (-1, -1), 4), ("BOTTOMPADDING", (0, 0), (-1, -1), 5),
    ]))
    t.hAlign = "LEFT"
    return t


# ---------------------------------------------------------------- headings that feed the contents
class Heading(Paragraph):
    def __init__(self, text, style, level, key):
        Paragraph.__init__(self, text, style)
        self.toc_level = level
        self.toc_key = key
        plain = re.sub(r"<[^>]+>", "", text)
        self.plain = plain.replace("&lt;", "<").replace("&gt;", ">").replace("&amp;", "&")


class Doc(BaseDocTemplate):
    def __init__(self, filename, meta, **kw):
        BaseDocTemplate.__init__(self, filename, **kw)
        self.meta = meta
        self.chapter = ""
        frame = Frame(MARGIN_X, MARGIN_BOTTOM, FRAME_W, PAGE_H - MARGIN_TOP - MARGIN_BOTTOM,
                      leftPadding=0, rightPadding=0, topPadding=0, bottomPadding=0, id="main")
        self.addPageTemplates([
            PageTemplate(id="cover", frames=[frame], onPage=self.draw_cover_page),
            PageTemplate(id="body", frames=[frame], onPageEnd=self.draw_page),
        ])

    def beforeDocument(self):
        self.chapter = ""   # each pass of multiBuild starts afresh

    def draw_cover_page(self, canv, doc):
        canv.saveState()
        canv.setFillColor(ACCENT)
        canv.rect(0, PAGE_H - 0.42 * inch, PAGE_W, 0.42 * inch, stroke=0, fill=1)
        canv.rect(0, 0, PAGE_W, 0.28 * inch, stroke=0, fill=1)
        canv.restoreState()

    def draw_page(self, canv, doc):
        canv.saveState()
        canv.setFont(BODY_FONT, 8)
        canv.setFillColor(MUTED)
        y = PAGE_H - 0.58 * inch
        canv.drawString(MARGIN_X, y, clean(self.meta.get("title", "")))
        if self.chapter:
            canv.drawRightString(PAGE_W - MARGIN_X, y, clean(self.chapter))
        canv.setStrokeColor(RULE)
        canv.setLineWidth(0.5)
        canv.line(MARGIN_X, y - 5, PAGE_W - MARGIN_X, y - 5)
        canv.line(MARGIN_X, 0.62 * inch, PAGE_W - MARGIN_X, 0.62 * inch)
        canv.drawCentredString(PAGE_W / 2.0, 0.44 * inch, "Page %d" % doc.page)
        foot = self.meta.get("footer", "")
        if foot:
            canv.drawString(MARGIN_X, 0.44 * inch, clean(foot))
        canv.restoreState()

    def afterFlowable(self, flowable):
        if isinstance(flowable, Heading):
            if flowable.toc_level == 0:
                self.chapter = flowable.plain
            self.canv.bookmarkPage(flowable.toc_key)
            self.canv.addOutlineEntry(flowable.plain, flowable.toc_key, level=flowable.toc_level,
                                      closed=(flowable.toc_level == 0))
            self.notify("TOCEntry", (flowable.toc_level, flowable.plain, self.page, flowable.toc_key))


# ---------------------------------------------------------------- the content parser
def parse(content_path, img_dir, captures, styles):
    with open(content_path, "r", encoding="utf-8") as f:
        lines = f.read().split("\n")
    meta, story, outline = {}, [], []
    i, n = 0, len(lines)
    while i < n and (lines[i].startswith("@") or not lines[i].strip()):
        if lines[i].startswith("@"):
            k, v = lines[i][1:].split(":", 1)
            meta[k.strip()] = v.strip()
        i += 1
    table_opts = {}
    para = []
    key_count = [0]
    first_h1 = [True]

    def flush_para():
        if para:
            story.append(Paragraph(inline(" ".join(para)), styles["body"]))
            del para[:]

    def new_key():
        key_count[0] += 1
        return "h%04d" % key_count[0]

    while i < n:
        line = lines[i].rstrip()
        stripped = line.strip()
        if not stripped:
            flush_para()
            i += 1
            continue
        if stripped.startswith("<!--"):
            while i < n and "-->" not in lines[i]:
                i += 1
            i += 1
            continue
        if stripped == "{pagebreak}":
            flush_para()
            story.append(PageBreak())
            i += 1
            continue
        m = re.match(r"^\{space:\s*([\d.]+)\}$", stripped)
        if m:
            flush_para()
            story.append(Spacer(1, float(m.group(1))))
            i += 1
            continue
        m = re.match(r"^\{table:(.*)\}$", stripped)
        if m:
            flush_para()
            table_opts = {}
            for part in m.group(1).split(";"):
                if "=" in part:
                    k, v = part.split("=", 1)
                    table_opts[k.strip()] = v.strip()
            i += 1
            continue
        if stripped == "{tiles}":
            flush_para()
            block = []
            i += 1
            while i < n and lines[i].strip() != "{/tiles}":
                block.append(lines[i])
                i += 1
            i += 1
            story.append(build_tiles(block, styles))
            story.append(Spacer(1, 8))
            continue
        m = re.match(r"^(#{1,4})\s+(.*)$", stripped)
        if m:
            flush_para()
            level = len(m.group(1))
            text = inline(m.group(2))
            if level == 1:
                if not first_h1[0]:
                    story.append(PageBreak())
                first_h1[0] = False
                story.append(Heading(text, styles["h1"], 0, new_key()))
                outline.append(clean(m.group(2)))
            elif level == 2:
                story.append(CondPageBreak(1.4 * inch))
                story.append(Heading(text, styles["h2"], 1, new_key()))
                outline.append("    " + clean(m.group(2)))
            elif level == 3:
                story.append(CondPageBreak(1.1 * inch))
                story.append(Paragraph(text, styles["h3"]))
            else:
                story.append(CondPageBreak(0.9 * inch))
                story.append(Paragraph(text, styles["h4"]))
            i += 1
            continue
        m = re.match(r"^!\[(.*?)\]\((.*?)\)$", stripped)
        if m:
            flush_para()
            path = prepare_image(m.group(2), img_dir, captures)
            if path:
                story.append(picture(path, m.group(1), styles))
            else:
                sys.stderr.write("warning: picture not found: %s\n" % m.group(2))
                story.append(Paragraph(inline("(picture not found: %s) %s" % (m.group(2), m.group(1))),
                                       styles["caption"]))
            i += 1
            continue
        if stripped.startswith("|"):
            flush_para()
            rows = []
            while i < n and lines[i].strip().startswith("|"):
                r = split_row(lines[i])
                if not all(re.match(r"^:?-{2,}:?$", c) for c in r):
                    rows.append(r)
                i += 1
            story.append(build_table(rows, table_opts, styles))
            story.append(Spacer(1, 8))
            table_opts = {}
            continue
        if stripped.startswith(">"):
            flush_para()
            q = []
            while i < n and lines[i].strip().startswith(">"):
                q.append(lines[i].strip()[1:].strip())
                i += 1
            story.append(build_quote(" ".join(q), styles))
            story.append(Spacer(1, 6))
            continue
        m = re.match(r"^(\s*)[-*]\s+(.*)$", line)
        if m:
            flush_para()
            nested = len(m.group(1)) >= 2
            text = m.group(2)
            i += 1
            # continuation lines: indented and not a new bullet
            while (i < n and lines[i].strip() and re.match(r"^\s{2,}\S", lines[i])
                   and not re.match(r"^\s*[-*]\s+", lines[i])):
                text += " " + lines[i].strip()
                i += 1
            st = styles["bullet2"] if nested else styles["bullet"]
            story.append(Paragraph(inline(text), st, bulletText="-" if nested else u"\u2022"))
            continue
        para.append(stripped)
        i += 1
    flush_para()
    return meta, story, outline


def cover(meta, img_dir, captures, styles):
    out = [Spacer(1, 0.55 * inch)]
    out.append(Paragraph(inline(meta.get("title", "")), styles["cover_title"]))
    out.append(Spacer(1, 10))
    if meta.get("subtitle"):
        out.append(Paragraph(inline(meta["subtitle"]), styles["cover_sub"]))
    if meta.get("dates"):
        out.append(Spacer(1, 4))
        out.append(Paragraph(inline(meta["dates"]), styles["cover_sub"]))
    out.append(Spacer(1, 0.3 * inch))
    if meta.get("cover"):
        path = prepare_image(meta["cover"], img_dir, captures)
        if path:
            out.append(picture(path, meta.get("covercaption", ""), styles, max_h=4.6 * inch))
    out.append(Spacer(1, 0.25 * inch))
    for k in ("covernote", "covernote2", "covernote3"):
        if meta.get(k):
            out.append(Paragraph(inline(meta[k]), styles["cover_note"]))
            out.append(Spacer(1, 5))
    return out


def build(content, out, img_dir, captures):
    styles = make_styles()
    meta, story, outline = parse(content, img_dir, captures, styles)
    toc = TableOfContents()
    toc.levelStyles = [styles["toc1"], styles["toc2"]]
    toc.dotsMinLevel = 0
    front = cover(meta, img_dir, captures, styles)
    front.append(NextPageTemplate("body"))
    front.append(PageBreak())
    front.append(Paragraph("Contents", styles["h1"]))
    front.append(toc)
    front.append(PageBreak())
    doc = Doc(out, meta, pagesize=letter, leftMargin=MARGIN_X, rightMargin=MARGIN_X,
              topMargin=MARGIN_TOP, bottomMargin=MARGIN_BOTTOM,
              title=clean(meta.get("title", "")), author=clean(meta.get("author", "")),
              subject=clean(meta.get("subtitle", "")))
    doc.multiBuild(front + story)
    return doc.page, outline


def main():
    ap = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    ap.add_argument("--content", default=DEFAULT_CONTENT)
    ap.add_argument("--out", default=DEFAULT_OUT)
    ap.add_argument("--img-dir", default=None,
                    help="where the downscaled JPEG copies live (default: tools/docs/img if it "
                         "exists, else a folder in the temp directory)")
    ap.add_argument("--captures", default=DEFAULT_CAPTURES,
                    help="folder holding world-captures and ui-captures")
    ap.add_argument("--outline", action="store_true", help="print the outline")
    a = ap.parse_args()
    img_dir = a.img_dir
    if not img_dir:
        local = os.path.join(HERE, "img")
        img_dir = local if os.path.isdir(local) else os.path.join(tempfile.gettempdir(),
                                                                  "crulanda-history-img")
    pages, outline = build(a.content, a.out, img_dir, a.captures)
    size = os.path.getsize(a.out)
    print("wrote %s" % a.out)
    print("pages: %d   size: %.1f MB" % (pages, size / 1048576.0))
    if a.outline:
        for o in outline:
            print(o)


if __name__ == "__main__":
    main()
