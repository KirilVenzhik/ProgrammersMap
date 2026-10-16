const STORAGE_KEY = "prog-checklist-v2";
const OWNER_KEY = "prog-checklist-owner"; // userId whose data the local progress mirrors
const MERGED_PREFIX = "prog-checklist-merged:"; // + userId; set once local progress was merged into the account

const rows = [...document.querySelectorAll("li[data-key]")];
const checkboxes = [...document.querySelectorAll('input[type="checkbox"][data-key]')];
const mapCells = new Map(
  [...document.querySelectorAll("#map span[data-key]")].map((el) => [el.dataset.key, el])
);

const userId = metaContent("user-id");
const csrfToken = metaContent("csrf-token");
// Signed-in users sync with the server (ADR-14); guests never touch the network.
let syncEnabled = Boolean(userId && csrfToken);

// Set by the server once after the account was deleted: the local copy belonged to that account.
if (metaContent("clear-progress") === "1") clearLocalProgress();

let done = loadProgress();
if (syncEnabled && storageGet(OWNER_KEY) !== null && storageGet(OWNER_KEY) !== userId) {
  // Left by another user (e.g. cookie expired without logout): not ours, do not merge it.
  done = new Set();
  saveProgress();
}

function metaContent(name) {
  return document.querySelector(`meta[name="${name}"]`)?.content ?? "";
}

function storageGet(key) {
  try {
    return localStorage.getItem(key);
  } catch {
    return null;
  }
}

function storageSet(key, value) {
  try {
    localStorage.setItem(key, value);
  } catch {
    // Storage unavailable: ignore.
  }
}

function storageRemove(key) {
  try {
    localStorage.removeItem(key);
  } catch {
    // Storage unavailable: ignore.
  }
}

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
  const key = cb.dataset.key;
  if (cb.checked) done.add(key);
  else done.delete(key);
  saveProgress();
  renderProgress();
  recordOp({ type: cb.checked ? "add" : "remove", key });
  syncTopic(key, cb.checked);
}

// Server sync (ADR-14)
//
// Protocol:
// - The initial request (POST merge on the first sign-in on this browser, GET afterwards) is the
//   head of `queue`; every PUT/DELETE waits for it, so a stale merge body can never resurrect a
//   key removed by a later click. Clicks made meanwhile are recorded (`initialOps`) and replayed
//   on top of the server set when it arrives.
// - After that, mutations are sent one at a time in click order.
// - The merged flag means "the server has everything this browser has". It is removed only when a
//   WRITE did not reach the server (network error, 5xx, 401, or a local-only write after sync was
//   disabled), so the next page load merges local progress again (union). A failed GET or merge
//   never removes it.
// - 401 stops syncing for the rest of the page.

const MERGED_KEY = `${MERGED_PREFIX}${userId}`;
let initialOps = null; // clicks made while the initial request is in flight (null = not in flight)
let warnedBadRequest = false;
let queue = Promise.resolve();

function recordOp(op) {
  if (initialOps) initialOps.push(op);
}

function applyOp(set, op) {
  if (op.type === "add") set.add(op.key);
  else if (op.type === "remove") set.delete(op.key);
  else set.clear();
}

// A write that did not reach the server: next load must merge again. A guest page with an owner
// means the session expired without logout, so the owner's next sign-in must merge these clicks.
function markWriteLost() {
  const owner = userId || storageGet(OWNER_KEY);
  if (owner) storageRemove(`${MERGED_PREFIX}${owner}`);
}

// Sends a request. Returns the Response, or null when it failed or sync is stopped
// (failure bookkeeping is done here). Never throws.
async function send(method, url, body) {
  const isWrite = method !== "GET" && url !== "/api/progress/merge";
  if (!syncEnabled) {
    if (isWrite) markWriteLost();
    return null;
  }
  const headers = { RequestVerificationToken: csrfToken };
  if (body !== undefined) headers["Content-Type"] = "application/json";
  try {
    const response = await fetch(url, {
      method,
      headers,
      credentials: "same-origin",
      body: body === undefined ? undefined : JSON.stringify(body),
    });
    if (response.status === 401) {
      syncEnabled = false; // session expired: act as a guest for the rest of the page
      if (isWrite) markWriteLost();
      return null;
    }
    if (response.status >= 500) {
      if (isWrite) markWriteLost();
      return null;
    }
    return response;
  } catch {
    if (isWrite) markWriteLost(); // network error
    return null;
  }
}

function enqueue(method, url) {
  queue = queue
    .then(() => send(method, url))
    .then((response) => {
      if (response && (response.status === 400 || response.status === 404) && !warnedBadRequest) {
        warnedBadRequest = true;
        console.warn(`Progress sync: ${method} ${url} rejected with ${response.status}`);
      }
    })
    .catch(() => {});
}

function syncTopic(key, checked) {
  if (!userId) return markWriteLost(); // guest: local-only write
  const [section, topic] = key.split("/").map(encodeURIComponent);
  enqueue(checked ? "PUT" : "DELETE", `/api/progress/${section}/${topic}`);
}

function syncReset() {
  if (!userId) return markWriteLost(); // guest: local-only write
  enqueue("DELETE", "/api/progress");
}

// Loads the server set: merge on the first sign-in on this browser, plain GET afterwards.
async function initialSync() {
  if (!syncEnabled) return;
  const needsMerge = storageGet(MERGED_KEY) === null;
  initialOps = [];
  try {
    const response = needsMerge
      ? await send("POST", "/api/progress/merge", { keys: [...done] })
      : await send("GET", "/api/progress");
    if (!response) return;
    if (!response.ok) {
      console.warn(`Progress sync: initial request rejected with ${response.status}`);
      return;
    }

    let keys;
    try {
      keys = (await response.json()).keys;
    } catch {
      return;
    }
    if (!Array.isArray(keys)) return;

    done = new Set(keys.filter((k) => typeof k === "string"));
    for (const op of initialOps) applyOp(done, op); // clicks that happened while we waited win
    saveProgress();
    storageSet(OWNER_KEY, userId);
    if (needsMerge) storageSet(MERGED_KEY, "1");
    renderProgress();
  } finally {
    initialOps = null; // on failure local state (with the clicks already applied) stays as is
  }
}

// Local data belongs to the signed-in user: wipe it on logout (the form is submitted as usual).
function onLogoutSubmit(event) {
  if (!(event.target instanceof HTMLFormElement) || !event.target.matches("form[data-logout]")) return;
  clearLocalProgress();
}

function clearLocalProgress() {
  storageRemove(STORAGE_KEY);
  storageRemove(OWNER_KEY);
  try {
    const merged = [];
    for (let i = 0; i < localStorage.length; i++) {
      const key = localStorage.key(i);
      if (key && key.startsWith(MERGED_PREFIX)) merged.push(key);
    }
    for (const key of merged) localStorage.removeItem(key);
  } catch {
    // Storage unavailable: nothing to clear.
  }
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
  const groupTitle = row.closest(".group")?.querySelector("h2, h3")?.textContent ?? "";
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
      const message = syncEnabled
        ? "Сбросить весь прогресс? Он будет удалён и из аккаунта."
        : "Сбросить весь прогресс?";
      if (!confirm(message)) return;
      done = new Set();
      saveProgress();
      renderProgress();
      recordOp({ type: "clear" });
      syncReset();
    });
  }
}

document.addEventListener("change", onToggle);
window.addEventListener("storage", onStorage);
document.addEventListener("submit", onLogoutSubmit);
if (toolbar) initToolbar();
renderProgress();
queue = initialSync().catch(() => {});
