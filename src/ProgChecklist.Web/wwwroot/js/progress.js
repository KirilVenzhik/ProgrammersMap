const STORAGE_KEY = "prog-checklist-v2";

const rows = [...document.querySelectorAll("li[data-key]")];
const checkboxes = [...document.querySelectorAll('input[type="checkbox"][data-key]')];
const mapCells = new Map(
  [...document.querySelectorAll("#map span[data-key]")].map((el) => [el.dataset.key, el])
);

let done = loadProgress();

function loadProgress() {
  try {
    const parsed = JSON.parse(localStorage.getItem(STORAGE_KEY));
    if (Array.isArray(parsed)) {
      return new Set(parsed.filter((k) => typeof k === "string"));
    }
  } catch {
    // Missing, corrupt or inaccessible storage: start empty.
  }
  return new Set();
}

function saveProgress() {
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify([...done]));
  } catch {
    // Storage unavailable: progress stays in memory for this page.
  }
}

function renderProgress() {
  for (const row of rows) {
    row.classList.toggle("done", done.has(row.dataset.key));
  }
  for (const cb of checkboxes) cb.checked = done.has(cb.dataset.key);
  for (const [key, cell] of mapCells) {
    cell.classList.toggle("on", done.has(key));
  }
  renderStats();
  applyFilters();
}

function renderStats() {
  const total = rows.length;
  const count = rows.filter((row) => done.has(row.dataset.key)).length;
  setText("#pct", `${total ? Math.round((count / total) * 100) : 0}%`);
  setText("#count", `${count} из ${total} тем`);

  const slugs = new Set();
  for (const attr of ["sc", "nc", "nr"]) {
    for (const el of document.querySelectorAll(`[data-${attr}]`)) slugs.add(el.dataset[attr]);
  }
  for (const slug of slugs) {
    const sectionRows = rows.filter((row) => row.dataset.key.startsWith(`${slug}/`));
    const k = sectionRows.filter((row) => done.has(row.dataset.key)).length;
    const t = sectionRows.length;
    for (const el of document.querySelectorAll(`[data-sc="${CSS.escape(slug)}"]`)) el.textContent = `${k} / ${t}`;
    for (const el of document.querySelectorAll(`[data-nc="${CSS.escape(slug)}"]`)) el.textContent = `${k} из ${t}`;
    for (const el of document.querySelectorAll(`[data-nr="${CSS.escape(slug)}"]`)) {
      el.style.width = `${t ? (k / t) * 100 : 0}%`;
    }
  }
}

function setText(selector, text) {
  const el = document.querySelector(selector);
  if (el) el.textContent = text;
}

function onToggle(event) {
  const cb = event.target;
  if (!(cb instanceof HTMLInputElement) || !cb.matches('input[type="checkbox"][data-key]')) return;
  if (cb.checked) done.add(cb.dataset.key);
  else done.delete(cb.dataset.key);
  saveProgress();
  renderProgress();
}

function onStorage(event) {
  if (event.key !== STORAGE_KEY && event.key !== null) return;
  done = loadProgress();
  renderProgress();
}

// Filters (toolbar is optional and rendered hidden for progressive enhancement)

const toolbar = document.getElementById("toolbar");
const filters = { query: "", level: "all", hideDone: false };

function matchesFilters(row) {
  if (filters.level !== "all" && row.dataset.level !== filters.level) return false;
  if (filters.hideDone && done.has(row.dataset.key)) return false;
  if (!filters.query) return true;
  const title = row.querySelector("a.txt")?.textContent ?? "";
  const groupTitle = row.closest(".group")?.querySelector("h3")?.textContent ?? "";
  return (
    title.toLowerCase().includes(filters.query) || groupTitle.toLowerCase().includes(filters.query)
  );
}

function applyFilters() {
  if (!toolbar) return;
  for (const row of rows) row.hidden = !matchesFilters(row);
  for (const group of document.querySelectorAll(".group")) {
    group.hidden = !group.querySelector("li:not([hidden])");
  }
  let anyVisible = false;
  for (const sec of document.querySelectorAll("section.sec")) {
    sec.hidden = !sec.querySelector(".group:not([hidden])");
    if (!sec.hidden) anyVisible = true;
  }
  const empty = document.getElementById("empty");
  if (empty) empty.hidden = anyVisible;
}

function initToolbar() {
  toolbar.hidden = false;

  const search = document.getElementById("q");
  if (search) {
    search.addEventListener("input", () => {
      filters.query = search.value.trim().toLowerCase();
      applyFilters();
    });
  }

  const levelButtons = [...toolbar.querySelectorAll(".seg button[data-level]")];
  for (const button of levelButtons) {
    button.addEventListener("click", () => {
      filters.level = button.dataset.level;
      for (const other of levelButtons) {
        other.setAttribute("aria-pressed", String(other === button));
      }
      applyFilters();
    });
  }

  const hide = document.getElementById("hide");
  if (hide) {
    hide.addEventListener("change", () => {
      filters.hideDone = hide.checked;
      applyFilters();
    });
  }

  const reset = document.getElementById("reset");
  if (reset) {
    reset.addEventListener("click", () => {
      if (!confirm("Сбросить весь прогресс?")) return;
      done = new Set();
      saveProgress();
      renderProgress();
    });
  }
}

document.addEventListener("change", onToggle);
window.addEventListener("storage", onStorage);
if (toolbar) initToolbar();
renderProgress();
