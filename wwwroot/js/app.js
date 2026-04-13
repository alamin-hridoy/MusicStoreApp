const state = {
  locale: "en-US",
  seed: "58933423",
  likes: 3.7,
  view: "table",
  tablePage: 1,
  tablePageSize: 10,
  galleryPage: 1,
  galleryPageSize: 12,
  galleryHasMore: true,
  galleryLoading: false
};

const elements = {
  localeSelect: document.querySelector("#localeSelect"),
  seedInput: document.querySelector("#seedInput"),
  randomSeedButton: document.querySelector("#randomSeedButton"),
  likesInput: document.querySelector("#likesInput"),
  likesValue: document.querySelector("#likesValue"),
  statusLine: document.querySelector("#statusLine"),
  tableBody: document.querySelector("#tableBody"),
  pageIndicator: document.querySelector("#pageIndicator"),
  prevPageButton: document.querySelector("#prevPageButton"),
  nextPageButton: document.querySelector("#nextPageButton"),
  tableView: document.querySelector("#tableView"),
  galleryView: document.querySelector("#galleryView"),
  tableViewButton: document.querySelector("#tableViewButton"),
  galleryViewButton: document.querySelector("#galleryViewButton"),
  galleryGrid: document.querySelector("#galleryGrid"),
  gallerySentinel: document.querySelector("#gallerySentinel"),
  tableDetailTemplate: document.querySelector("#tableDetailTemplate")
};

async function fetchJson(url) {
  const response = await fetch(url);
  if (!response.ok) {
    throw new Error(`Request failed: ${response.status}`);
  }
  return response.json();
}

async function loadLocales() {
  const data = await fetchJson("/api/locales");
  elements.localeSelect.innerHTML = data.locales
    .map(locale => `<option value="${locale.code}">${locale.name}</option>`)
    .join("");
  elements.localeSelect.value = state.locale;
}

function currentParams(page, pageSize) {
  return new URLSearchParams({
    locale: state.locale,
    seed: state.seed,
    likes: state.likes.toFixed(1),
    page: String(page),
    pageSize: String(pageSize)
  });
}

function setStatus(text) {
  elements.statusLine.textContent = text;
}

function formatDuration(totalSeconds) {
  const minutes = Math.floor(totalSeconds / 60);
  const seconds = String(totalSeconds % 60).padStart(2, "0");
  return `${minutes}:${seconds}`;
}

function fillDetail(detailElement, record) {
  detailElement.querySelector(".cover-art").src = record.coverUrl;
  detailElement.querySelector(".cover-art").alt = `${record.title} cover`;
  detailElement.querySelector(".detail-kicker").textContent = `${record.genre} • ${record.likes} likes`;
  detailElement.querySelector(".duration-pill").textContent = formatDuration(record.durationSeconds);
  detailElement.querySelector("h3").textContent = record.title;
  detailElement.querySelector(".detail-meta").textContent =
    `${record.artist} • ${record.album} • ${record.label}, ${record.releaseYear} • ${formatDuration(record.durationSeconds)}`;
  detailElement.querySelector("audio").src = record.audioUrl;
  detailElement.querySelector(".detail-review").textContent = record.review;
}

function renderTable(response) {
  const rows = [];

  for (const record of response.records) {
    rows.push(`
      <tr class="summary-row" data-song-index="${record.index}">
        <td class="expand-cell"><button type="button" class="expand-toggle" aria-label="Toggle details" aria-expanded="false">+</button></td>
        <td class="number-cell">${record.index}</td>
        <td class="song-cell"><strong>${record.title}</strong></td>
        <td class="artist-cell">${record.artist}</td>
        <td class="album-cell">${record.album}</td>
        <td class="genre-cell">${record.genre}</td>
        <td class="likes-cell"><span class="likes-badge">${record.likes}</span></td>
      </tr>
      <tr class="detail-row hidden" data-detail-index="${record.index}">
        <td colspan="7"></td>
      </tr>
    `);
  }

  elements.tableBody.innerHTML = rows.join("");

  for (const row of elements.tableBody.querySelectorAll(".summary-row")) {
    row.addEventListener("click", event => {
      const button = row.querySelector(".expand-toggle");
      if (event.target instanceof HTMLElement && event.target.tagName === "BUTTON") {
        event.stopPropagation();
      }

      const index = Number(row.dataset.songIndex);
      const detailRow = elements.tableBody.querySelector(`[data-detail-index="${index}"]`);
      const record = response.records.find(item => item.index === index);
      if (!detailRow || !record) {
        return;
      }

      const isHidden = detailRow.classList.contains("hidden");
      elements.tableBody.querySelectorAll(".detail-row").forEach(item => item.classList.add("hidden"));
      elements.tableBody.querySelectorAll(".summary-row").forEach(item => item.classList.remove("is-open"));
      elements.tableBody.querySelectorAll(".expand-toggle").forEach(item => {
        item.textContent = "+";
        item.setAttribute("aria-expanded", "false");
      });

      if (isHidden) {
        const detailMarkup = elements.tableDetailTemplate.content.firstElementChild.cloneNode(true);
        fillDetail(detailMarkup, record);
        detailRow.firstElementChild.replaceChildren(detailMarkup);
        detailRow.classList.remove("hidden");
        row.classList.add("is-open");
        button.textContent = "–";
        button.setAttribute("aria-expanded", "true");
      }
    });
  }

  elements.pageIndicator.textContent = response.totalPages
    ? `Page ${response.page} of ${response.totalPages}`
    : `Page ${response.page}`;
  elements.prevPageButton.disabled = response.page <= 1;
  elements.nextPageButton.disabled = response.hasMore === false;
}

function renderGalleryCards(records, append) {
  const html = records.map(record => `
    <article>
      <img src="${record.coverUrl}" alt="${record.title} cover">
      <h3>${record.title}</h3>
      <p class="gallery-meta">${record.artist} from <strong>${record.album}</strong></p>
      <div class="pill-row">
        <span class="pill">#${record.index}</span>
        <span class="pill">${record.genre}</span>
        <span class="pill">${record.likes} likes</span>
      </div>
      <p class="gallery-review">${record.review}</p>
      <audio controls preload="none" src="${record.audioUrl}"></audio>
    </article>
  `).join("");

  if (append) {
    elements.galleryGrid.insertAdjacentHTML("beforeend", html);
  } else {
    elements.galleryGrid.innerHTML = html;
  }
}

async function loadTablePage() {
  setStatus("Loading table view...");
  const response = await fetchJson(`/api/songs?${currentParams(state.tablePage, state.tablePageSize)}`);
  renderTable(response);
  setStatus(`Showing seeded songs for ${elements.localeSelect.selectedOptions[0]?.textContent ?? state.locale}, page ${response.page}.`);
}

async function loadGalleryPage({ reset }) {
  if (state.galleryLoading || (!state.galleryHasMore && !reset)) {
    return;
  }

  state.galleryLoading = true;
  elements.gallerySentinel.textContent = "Loading more songs...";

  if (reset) {
    state.galleryPage = 1;
    state.galleryHasMore = true;
  }

  const response = await fetchJson(`/api/songs?${currentParams(state.galleryPage, state.galleryPageSize)}`);
  renderGalleryCards(response.records, !reset);
  state.galleryHasMore = response.hasMore;
  state.galleryPage += 1;
  state.galleryLoading = false;
  elements.gallerySentinel.textContent = response.hasMore ? "Scroll for more songs..." : "You reached the end of the generated catalog.";
}

async function refreshForParameterChange() {
  state.tablePage = 1;
  state.galleryPage = 1;
  state.galleryHasMore = true;
  window.scrollTo({ top: 0, behavior: "smooth" });
  await loadTablePage();
  if (state.view === "gallery") {
    await loadGalleryPage({ reset: true });
  } else {
    elements.galleryGrid.innerHTML = "";
  }
}

function updateViewButtons() {
  const isTable = state.view === "table";
  elements.tableView.classList.toggle("hidden", !isTable);
  elements.galleryView.classList.toggle("hidden", isTable);
  elements.tableViewButton.classList.toggle("active", isTable);
  elements.galleryViewButton.classList.toggle("active", !isTable);
  elements.tableViewButton.setAttribute("aria-selected", String(isTable));
  elements.galleryViewButton.setAttribute("aria-selected", String(!isTable));
}

function wireEvents() {
  elements.localeSelect.addEventListener("change", async event => {
    state.locale = event.target.value;
    await refreshForParameterChange();
  });

  elements.seedInput.addEventListener("input", async event => {
    state.seed = event.target.value || "0";
    await refreshForParameterChange();
  });

  elements.randomSeedButton.addEventListener("click", async () => {
    const randomSeed = BigInt.asUintN(64, crypto.getRandomValues(new BigUint64Array(1))[0]).toString();
    state.seed = randomSeed;
    elements.seedInput.value = randomSeed;
    await refreshForParameterChange();
  });

  elements.likesInput.addEventListener("input", async event => {
    state.likes = Number(event.target.value);
    elements.likesValue.textContent = state.likes.toFixed(1);
    await refreshForParameterChange();
  });

  elements.prevPageButton.addEventListener("click", async () => {
    state.tablePage = Math.max(1, state.tablePage - 1);
    await loadTablePage();
  });

  elements.nextPageButton.addEventListener("click", async () => {
    state.tablePage += 1;
    await loadTablePage();
  });

  elements.tableViewButton.addEventListener("click", async () => {
    state.view = "table";
    updateViewButtons();
    await loadTablePage();
  });

  elements.galleryViewButton.addEventListener("click", async () => {
    state.view = "gallery";
    updateViewButtons();
    if (!elements.galleryGrid.children.length) {
      await loadGalleryPage({ reset: true });
    }
  });
}

function setupInfiniteScroll() {
  const observer = new IntersectionObserver(async entries => {
    if (entries.some(entry => entry.isIntersecting) && state.view === "gallery") {
      await loadGalleryPage({ reset: false });
    }
  }, {
    rootMargin: "240px"
  });

  observer.observe(elements.gallerySentinel);
}

async function bootstrap() {
  elements.likesValue.textContent = state.likes.toFixed(1);
  await loadLocales();
  wireEvents();
  updateViewButtons();
  setupInfiniteScroll();
  await loadTablePage();
}

bootstrap().catch(error => {
  console.error(error);
  setStatus("Something went wrong while loading the showcase.");
});
