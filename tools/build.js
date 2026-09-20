// Build script: doc book/manifest.json + book/<part>/chXX-*.md, sinh HTML vao dist/.
// Chuong nao chua co file .md thi bo qua (cho phep viet sach dan dan, moi chuong commit rieng).
'use strict';

const fs = require('fs');
const path = require('path');
const MarkdownIt = require('markdown-it');
const hljs = require('highlight.js');

const ROOT = path.join(__dirname, '..');
const BOOK_DIR = path.join(ROOT, 'book');
const CODE_DIR = path.join(ROOT, 'code');
const DIST_DIR = path.join(ROOT, 'dist');

const manifest = JSON.parse(fs.readFileSync(path.join(BOOK_DIR, 'manifest.json'), 'utf8'));

const md = new MarkdownIt({
  html: true,
  linkify: true,
  typographer: false,
  highlight(str, lang) {
    if (lang === 'mermaid') {
      return `<pre class="mermaid">${md.utils.escapeHtml(str)}</pre>`;
    }
    if (lang && hljs.getLanguage(lang)) {
      try {
        return `<pre class="hljs"><code>${hljs.highlight(str, { language: lang }).value}</code></pre>`;
      } catch (e) {
        // fall through to default escaping
      }
    }
    return `<pre class="hljs"><code>${md.utils.escapeHtml(str)}</code></pre>`;
  },
});

// Flatten manifest thanh danh sach chuong theo thu tu, chi giu chuong da co file.
function slugFile(part, chapter) {
  return path.join(BOOK_DIR, part.dir, `ch${String(chapter.num).padStart(2, '0')}-${chapter.slug}.md`);
}

const allChapters = [];
for (const part of manifest.parts) {
  for (const chapter of part.chapters) {
    const file = slugFile(part, chapter);
    allChapters.push({ part, chapter, file, exists: fs.existsSync(file) });
  }
}

const written = allChapters.filter((c) => c.exists);
const missing = allChapters.filter((c) => !c.exists);

if (written.length === 0) {
  console.error('Chua co chuong nao duoc viet (khong tim thay file .md nao khop manifest).');
  process.exit(1);
}

fs.mkdirSync(DIST_DIR, { recursive: true });
fs.copyFileSync(path.join(__dirname, 'style.css'), path.join(DIST_DIR, 'style.css'));

// Sao chep toan bo code/ vao dist/code/ - cac chuong link toi code mau bang duong
// dan tuong doi "../../code/..." (dung khi doc file .md tren GitHub, tu book/<part>/
// len 2 cap toi goc repo). Trong dist/ (cau truc PHANG, moi file HTML nam ngang
// hang nhau), duong dan do duoc VIET LAI thanh "code/..." ngay ben duoi (xem ham
// fixCodeLinks) - nen can co dist/code/ ton tai thi link moi khong bi gay.
fs.rmSync(path.join(DIST_DIR, 'code'), { recursive: true, force: true });
fs.cpSync(CODE_DIR, path.join(DIST_DIR, 'code'), { recursive: true });

function fixCodeLinks(html) {
  return html.replace(/(href=")\.\.\/\.\.\/code\//g, '$1code/');
}

function chapterHref(entry) {
  return `ch${String(entry.chapter.num).padStart(2, '0')}-${entry.chapter.slug}.html`;
}

function renderSidebar(activeNum) {
  let html = '<nav class="sidebar"><div class="sidebar-title">' + manifest.title + '</div><ul class="part-list">';
  for (const part of manifest.parts) {
    html += `<li class="part"><div class="part-title">${part.title}</div><ul class="chapter-list">`;
    for (const chapter of part.chapters) {
      const entry = allChapters.find((c) => c.chapter === chapter);
      const cls = ['chapter-link'];
      if (!entry.exists) cls.push('pending');
      if (chapter.num === activeNum) cls.push('active');
      if (entry.exists) {
        html += `<li><a class="${cls.join(' ')}" href="${chapterHref(entry)}">${chapter.num}. ${chapter.title}</a></li>`;
      } else {
        html += `<li><span class="${cls.join(' ')}">${chapter.num}. ${chapter.title}</span></li>`;
      }
    }
    html += '</ul></li>';
  }
  html += '</ul></nav>';
  return html;
}

function renderPage({ title, bodyHtml, activeNum, prev, next }) {
  const prevLink = prev ? `<a class="nav-prev" href="${chapterHref(prev)}">&larr; ${prev.chapter.num}. ${prev.chapter.title}</a>` : '<span></span>';
  const nextLink = next ? `<a class="nav-next" href="${chapterHref(next)}">${next.chapter.num}. ${next.chapter.title} &rarr;</a>` : '<span></span>';
  return `<!doctype html>
<html lang="vi">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>${title} — ${manifest.title}</title>
<link rel="stylesheet" href="style.css">
</head>
<body>
<div class="layout">
${renderSidebar(activeNum)}
<main class="content">
<article>
${bodyHtml}
</article>
<div class="chapter-nav">${prevLink}${nextLink}</div>
</main>
</div>
<script src="https://cdnjs.cloudflare.com/ajax/libs/mermaid/10.9.1/mermaid.min.js"></script>
<script>if (window.mermaid) { mermaid.initialize({ startOnLoad: true, theme: 'neutral' }); }</script>
</body>
</html>
`;
}

for (let i = 0; i < written.length; i++) {
  const entry = written[i];
  const src = fs.readFileSync(entry.file, 'utf8');
  const bodyHtml = fixCodeLinks(md.render(src));
  const titleMatch = src.match(/^#\s+(.+)$/m);
  const title = titleMatch ? titleMatch[1] : entry.chapter.title;
  const prev = i > 0 ? written[i - 1] : null;
  const next = i < written.length - 1 ? written[i + 1] : null;
  const html = renderPage({ title, bodyHtml, activeNum: entry.chapter.num, prev, next });
  fs.writeFileSync(path.join(DIST_DIR, chapterHref(entry)), html, 'utf8');
}

// Trang muc luc (index.html)
let indexBody = `<h1>${manifest.title}</h1><p class="subtitle">${manifest.subtitle}</p>`;
if (written.length > 0) {
  indexBody += `<p><a class="start-link" href="${chapterHref(written[0])}">Bắt đầu đọc &rarr;</a></p>`;
}
const indexHtml = `<!doctype html>
<html lang="vi">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>${manifest.title}</title>
<link rel="stylesheet" href="style.css">
</head>
<body>
<div class="layout">
${renderSidebar(null)}
<main class="content"><article>${indexBody}</article></main>
</div>
</body>
</html>
`;
fs.writeFileSync(path.join(DIST_DIR, 'index.html'), indexHtml, 'utf8');

console.log(`Da build ${written.length} chuong vao dist/. Con ${missing.length} chuong chua viet.`);
if (missing.length > 0 && missing.length <= 10) {
  console.log('Chua viet:', missing.map((c) => `${c.chapter.num}. ${c.chapter.title}`).join(', '));
}
