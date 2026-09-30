// Bodu overlay on the DocFX modern template: the framework selector.
//
// API pages carry the frameworks each API exists in (data-bodu-frameworks on the page title and on
// every member) and the list of documented frameworks (#bodu-framework-state, written by
// partials/bodu.frameworkState). The selector lets a reader choose a framework, much as Microsoft
// Learn's ?view= does, and recedes whatever the chosen framework lacks, with a note saying so. There
// is one page per API, not one site per framework: the choice changes how a page reads, not which
// page is served.
//
// The choice comes from ?view=net-8.0 when the URL has one, otherwise from the reader's last choice
// (localStorage, when the browser allows it), otherwise the newest framework. It is kept in the URL,
// and carried onto the API pages the reader follows a link to.

const STORAGE_KEY = 'bodu.docs.framework';

function readFrameworks() {
  const state = document.getElementById('bodu-framework-state');
  if (!state) return null;
  const list = state.dataset.boduFrameworkList
    .split(';')
    .filter(Boolean)
    .map(entry => {
      const [tfm, label, view] = entry.split('|');
      return { tfm, label, view };
    });
  return list.length > 0 ? list : null;
}

function storedView() {
  try {
    return localStorage.getItem(STORAGE_KEY);
  } catch {
    return null;
  }
}

function storeView(view) {
  try {
    localStorage.setItem(STORAGE_KEY, view);
  } catch {
    // Storage is a convenience; without it the choice still travels in the URL.
  }
}

function initialFramework(frameworks) {
  const byView = view => frameworks.find(f => f.view === view);
  return byView(new URLSearchParams(location.search).get('view')) || byView(storedView()) || frameworks[0];
}

function withView(href, framework, newest) {
  const url = new URL(href, location.href);
  if (framework === newest) url.searchParams.delete('view');
  else url.searchParams.set('view', framework.view);
  return url;
}

function apply(framework, frameworks) {
  document.documentElement.dataset.boduView = framework.tfm;

  for (const element of document.querySelectorAll('[data-bodu-frameworks]')) {
    const available = element.dataset.boduFrameworks.split(' ').filter(Boolean);
    const missing = available.length > 0 && !available.includes(framework.tfm);
    element.classList.toggle('bodu-unavailable', missing);

    // A member section or a namespace listing entry recedes as a whole; its heading carries the note.
    const heading = element.matches('section, dl') ? element.querySelector('h3, dt') || element : element;
    if (missing) heading.dataset.boduUnavailableNote = `Not in ${framework.label}`;
    else delete heading.dataset.boduUnavailableNote;
  }

  const title = document.querySelector('article h1[data-bodu-frameworks]');
  let banner = document.getElementById('bodu-framework-banner');
  if (title && title.classList.contains('bodu-unavailable')) {
    const labels = frameworks.filter(f => title.dataset.boduFrameworks.split(' ').includes(f.tfm)).map(f => f.label);
    if (!banner) {
      banner = document.createElement('div');
      banner.id = 'bodu-framework-banner';
      banner.className = 'alert alert-warning bodu-framework-banner';
      banner.setAttribute('role', 'note');
      title.insertAdjacentElement('afterend', banner);
    }
    banner.textContent = `This API is not available in ${framework.label}. It applies to ${labels.join(', ')}.`;
  } else if (banner) {
    banner.remove();
  }

  history.replaceState(history.state, '', withView(location.href, framework, frameworks[0]));
}

function picker(frameworks, selected, onChange) {
  const wrapper = document.createElement('div');
  wrapper.className = 'bodu-framework-picker d-print-none';

  const label = document.createElement('label');
  label.htmlFor = 'bodu-framework';
  label.textContent = 'Framework';

  const select = document.createElement('select');
  select.id = 'bodu-framework';
  select.className = 'form-select form-select-sm';
  for (const framework of frameworks) {
    const option = document.createElement('option');
    option.value = framework.view;
    option.textContent = framework.label;
    option.selected = framework === selected;
    select.append(option);
  }
  select.addEventListener('change', () => onChange(frameworks.find(f => f.view === select.value)));

  wrapper.append(label, select);
  return wrapper;
}

function start() {
  const frameworks = readFrameworks();
  if (!frameworks) return;

  let selected = initialFramework(frameworks);
  const choose = framework => {
    selected = framework;
    storeView(framework.view);
    apply(framework, frameworks);
  };

  const bar = document.querySelector('.actionbar') || document.querySelector('article');
  bar.append(picker(frameworks, selected, choose));
  apply(selected, frameworks);

  // The table of contents and navigation render after start(), so links are rewritten as they are
  // followed rather than up front. Only same-site pages under the API reference carry the choice.
  document.addEventListener('click', event => {
    const link = event.target instanceof Element ? event.target.closest('a[href]') : null;
    if (!link || selected === frameworks[0]) return;
    const url = new URL(link.href, location.href);
    if (url.origin !== location.origin || !/\/api\/[^/]+\.html$/.test(url.pathname)) return;
    link.href = withView(url.href, selected, frameworks[0]).href;
  }, true);
}

export default {
  iconLinks: [
    {
      icon: 'github',
      href: 'https://github.com/bslater/bodu',
      title: 'GitHub'
    }
  ],
  start
};
