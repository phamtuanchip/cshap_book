// Build script PDF: gop toan bo chuong da viet thanh MOT trang HTML dai (bia +
// muc luc + tung chuong ngan cach bang page-break), dung Puppeteer (Chromium
// headless) de in ra dist/*.pdf. Tai su dung DUNG mot markdown-it/CSS voi build.js
// (ban HTML) de hai ban phat hanh nhat quan nhau.
'use strict';

const fs = require('fs');
const path = require('path');
const MarkdownIt = require('markdown-it');
const hljs = require('highlight.js');

const ROOT = path.join(__dirname, '..');
const BOOK_DIR = path.join(ROOT, 'book');
const DIST_DIR = path.join(ROOT, 'dist');
const GITHUB_REPO_URL = 'https://github.com/phamtuanchip/cshap_book';

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
        // fall through
      }
    }
    return `<pre class="hljs"><code>${md.utils.escapeHtml(str)}</code></pre>`;
  },
});

function slugFile(part, chapter) {
  return path.join(BOOK_DIR, part.dir, `ch${String(chapter.num).padStart(2, '0')}-${chapter.slug}.md`);
}

function chapterAnchor(chapter) {
  return `ch${String(chapter.num).padStart(2, '0')}`;
}

// PDF khong mang theo thu muc code/ nhu ban HTML (xem build.js) - link "code mau"
// duoc viet lai TRO THANG sang GitHub thay vi "../../code/..." (chi dung khi doc
// file .md tren GitHub) hay "code/..." (chi dung trong dist/ phang cua ban HTML).
function fixCodeLinksForPdf(html) {
  return html.replace(/href="\.\.\/\.\.\/code\//g, `href="${GITHUB_REPO_URL}/tree/main/code/`);
}

const allChapters = [];
for (const part of manifest.parts) {
  for (const chapter of part.chapters) {
    const file = slugFile(part, chapter);
    allChapters.push({ part, chapter, file, exists: fs.existsSync(file) });
  }
}
const written = allChapters.filter((c) => c.exists);

if (written.length === 0) {
  console.error('Chua co chuong nao duoc viet - khong the build PDF.');
  process.exit(1);
}

// -- Bia --
let bia = `
<section class="bia">
  <h1>${manifest.title}</h1>
  <p class="subtitle">${manifest.subtitle}</p>
  <p class="repo-link">${GITHUB_REPO_URL}</p>
</section>
<div class="page-break"></div>
`;

// -- Muc luc --
let mucLuc = '<section class="muc-luc"><h1>Mục lục</h1>';
for (const part of manifest.parts) {
  mucLuc += `<h3>${part.title}</h3><ul>`;
  for (const chapter of part.chapters) {
    const entry = allChapters.find((c) => c.chapter === chapter);
    if (entry.exists) {
      mucLuc += `<li><a href="#${chapterAnchor(chapter)}">${chapter.num}. ${chapter.title}</a></li>`;
    } else {
      mucLuc += `<li class="pending">${chapter.num}. ${chapter.title} (chưa viết)</li>`;
    }
  }
  mucLuc += '</ul>';
}
mucLuc += '</section><div class="page-break"></div>';

// -- Tung chuong --
let noiDungChuong = '';
for (const entry of written) {
  const src = fs.readFileSync(entry.file, 'utf8');
  const bodyHtml = fixCodeLinksForPdf(md.render(src));
  noiDungChuong += `<section class="chuong" id="${chapterAnchor(entry.chapter)}">${bodyHtml}</section><div class="page-break"></div>\n`;
}

const cssBoSung = `
  .bia { display: flex; flex-direction: column; justify-content: center; align-items: center;
         height: 90vh; text-align: center; }
  .bia h1 { font-size: 2.2rem; max-width: 80%; }
  .bia .subtitle { color: #6a737d; font-size: 1.1rem; max-width: 70%; }
  .bia .repo-link { margin-top: 2em; font-size: 0.9rem; color: #6a737d; }
  .muc-luc ul { list-style: none; padding-left: 0; margin-bottom: 1.5em; }
  .muc-luc li { padding: 3px 0; }
  .muc-luc li.pending { color: #6a737d; font-style: italic; }
  .muc-luc a { color: inherit; text-decoration: none; }
  .page-break { page-break-after: always; }
  section.chuong { padding-top: 8px; }
  body { padding: 0 40px; }
`;

const fullHtml = `<!doctype html>
<html lang="vi">
<head>
<meta charset="utf-8">
<title>${manifest.title}</title>
<link rel="stylesheet" href="${path.join(__dirname, 'style.css').replace(/\\/g, '/')}">
<style>${cssBoSung}</style>
</head>
<body>
${bia}
${mucLuc}
${noiDungChuong}
<script src="https://cdnjs.cloudflare.com/ajax/libs/mermaid/10.9.1/mermaid.min.js"></script>
<script>
  window.__mermaidDone = false;
  if (window.mermaid) {
    mermaid.initialize({ startOnLoad: true, theme: 'neutral' });
    mermaid.run().finally(() => { window.__mermaidDone = true; }).catch(() => { window.__mermaidDone = true; });
  } else {
    window.__mermaidDone = true;
  }
</script>
</body>
</html>
`;

fs.mkdirSync(DIST_DIR, { recursive: true });
const tempHtmlPath = path.join(DIST_DIR, '_pdf-source.html');
fs.writeFileSync(tempHtmlPath, fullHtml, 'utf8');

(async () => {
  const puppeteer = require('puppeteer');
  const browser = await puppeteer.launch();
  try {
    const page = await browser.newPage();
    await page.goto('file://' + tempHtmlPath.replace(/\\/g, '/'), { waitUntil: 'networkidle0' });
    // Cho Mermaid ve xong sơ đồ (toi da 5 giay) truoc khi in - sơ đồ chuyen thanh
    // <svg> tinh, khong can tuong tac trong PDF (khac ban HTML co pan/zoom)
    await page.waitForFunction('window.__mermaidDone === true', { timeout: 5000 }).catch(() => {});

    const outPdfPath = path.join(DIST_DIR, 'csharp-tu-co-ban-den-nang-cao-tap1.pdf');
    await page.pdf({
      path: outPdfPath,
      format: 'A4',
      printBackground: true,
      margin: { top: '18mm', bottom: '18mm', left: '16mm', right: '16mm' },
    });
    console.log(`Da build PDF: ${outPdfPath} (${written.length} chuong).`);
  } finally {
    await browser.close();
    fs.rmSync(tempHtmlPath, { force: true });
  }
})();
