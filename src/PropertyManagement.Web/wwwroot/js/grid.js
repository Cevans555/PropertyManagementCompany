// Reusable data grid, rendered by the Grid view component:
//   <div data-grid data-grid-url="/api/applications" data-grid-columns='[...]' data-grid-filter-form="filters">
//
// The endpoint receives page, pageSize, sort and direction plus the filter form's fields, and returns
//   { items: [...], totalCount, page, pageSize }
//
// Grid state lives in the page URL (?status=Draft&sort=status&direction=asc&page=2), so a refresh or a shared link
// shows the same page, and Back/Forward step through earlier states. Cell text is always set with textContent.
(() => {
    const dateFormat = new Intl.DateTimeFormat('en-US', { month: 'short', day: 'numeric', year: 'numeric' });

    document.querySelectorAll('[data-grid]').forEach(initGrid);

    function initGrid(root) {
        const columns = JSON.parse(root.dataset.gridColumns);
        const table = root.querySelector('table');
        const body = root.querySelector('[data-grid-body]');
        const summary = root.querySelector('[data-grid-summary]');
        const pager = root.querySelector('[data-grid-pager]');
        const prev = root.querySelector('[data-grid-prev]');
        const next = root.querySelector('[data-grid-next]');
        const pageLabel = root.querySelector('[data-grid-page-label]');
        const form = root.dataset.gridFilterForm ? document.getElementById(root.dataset.gridFilterForm) : null;
        const defaults = {
            sort: root.dataset.gridSort,
            direction: root.dataset.gridDirection,
            pageSize: root.dataset.gridPageSize
        };
        const filterNames = form ? [...new Set([...form.elements].map(e => e.name).filter(Boolean))] : [];
        const sortKeys = new Set(columns.map(c => c.sortKey).filter(Boolean));

        let state = readState(new URLSearchParams(location.search));
        let totalPages = 1;
        let latestRequest = 0;

        root.querySelectorAll('th[data-grid-sort-key] button').forEach(button => {
            button.addEventListener('click', () => {
                const key = button.closest('th').dataset.gridSortKey;
                const direction = state.sort === key && state.direction === 'asc' ? 'desc' : 'asc';
                navigate({ ...state, sort: key, direction, page: 1 });
            });
        });
        prev.addEventListener('click', () => navigate({ ...state, page: state.page - 1 }));
        next.addEventListener('click', () => navigate({ ...state, page: state.page + 1 }));

        if (form) {
            form.addEventListener('submit', event => {
                event.preventDefault();
                const filters = {};
                new FormData(form).forEach((value, name) => { if (value !== '') filters[name] = value; });
                navigate({ ...state, filters, page: 1 });
            });
        }

        window.addEventListener('popstate', () => {
            state = readState(new URLSearchParams(location.search));
            syncForm();
            load();
        });

        load();

        function readState(params) {
            const filters = {};
            filterNames.forEach(name => { if (params.get(name)) filters[name] = params.get(name); });
            const sort = sortKeys.has(params.get('sort')) ? params.get('sort') : defaults.sort;
            const direction = ['asc', 'desc'].includes(params.get('direction')) ? params.get('direction') : defaults.direction;
            return {
                filters,
                sort,
                direction,
                page: positiveInt(params.get('page')) || 1,
                pageSize: positiveInt(params.get('pageSize')) || positiveInt(defaults.pageSize) || 20
            };
        }

        function toParams(s, includeDefaults) {
            const params = new URLSearchParams(s.filters);
            if (includeDefaults || s.sort !== defaults.sort || s.direction !== defaults.direction) {
                params.set('sort', s.sort);
                params.set('direction', s.direction);
            }
            if (includeDefaults || s.page !== 1) params.set('page', s.page);
            if (includeDefaults || String(s.pageSize) !== defaults.pageSize) params.set('pageSize', s.pageSize);
            return params;
        }

        function navigate(newState) {
            state = newState;
            const query = toParams(state, false).toString();
            history.pushState(null, '', location.pathname + (query ? `?${query}` : ''));
            load();
        }

        function syncForm() {
            if (!form) return;
            filterNames.forEach(name => {
                const field = form.elements[name];
                if (field) field.value = state.filters[name] ?? '';
            });
        }

        async function load() {
            const requestId = ++latestRequest;
            table.setAttribute('aria-busy', 'true');
            updateSortHeaders();

            let data;
            try {
                const response = await fetch(`${root.dataset.gridUrl}?${toParams(state, true)}`, {
                    headers: { Accept: 'application/json' }
                });
                if (requestId !== latestRequest) return;
                if (response.status === 401) {
                    location.reload(); // signed out: let the server redirect to the login page
                    return;
                }
                if (!response.ok) throw new Error(`HTTP ${response.status}`);
                data = await response.json();
            } catch {
                if (requestId !== latestRequest) return;
                showMessage('Could not load this list. Please try again.');
                summary.textContent = '';
                pager.hidden = true;
                table.setAttribute('aria-busy', 'false');
                return;
            }
            if (requestId !== latestRequest) return;

            // The server clamps a page past the end; keep the URL in step with what's shown.
            if (data.page !== state.page) {
                state = { ...state, page: data.page };
                const query = toParams(state, false).toString();
                history.replaceState(null, '', location.pathname + (query ? `?${query}` : ''));
            }
            render(data);
            table.setAttribute('aria-busy', 'false');
        }

        function render(data) {
            totalPages = Math.max(1, Math.ceil(data.totalCount / data.pageSize));

            if (data.items.length === 0) {
                showMessage(root.dataset.gridEmpty);
            } else {
                body.replaceChildren(...data.items.map(renderRow));
            }

            const first = data.totalCount === 0 ? 0 : (data.page - 1) * data.pageSize + 1;
            const last = (data.page - 1) * data.pageSize + data.items.length;
            summary.textContent = data.totalCount === 0 ? '' : `Showing ${first}–${last} of ${data.totalCount}`;

            pager.hidden = totalPages <= 1;
            pageLabel.textContent = `Page ${data.page} of ${totalPages}`;
            prev.disabled = data.page <= 1;
            next.disabled = data.page >= totalPages;
        }

        function renderRow(item) {
            const row = document.createElement('tr');
            columns.forEach(column => row.appendChild(renderCell(column, item)));
            return row;
        }

        function renderCell(column, item) {
            const cell = document.createElement('td');
            const value = item[column.key];

            if (value === null || value === undefined || value === '') {
                if (column.emptyText) cell.appendChild(muted(column.emptyText));
                return cell;
            }

            switch (column.format) {
                case 'date':
                    cell.textContent = formatDate(value);
                    break;
                case 'badge': {
                    const badge = document.createElement('span');
                    badge.className = `badge ${(column.badgeClasses || {})[value] || 'text-bg-light border'}`;
                    badge.textContent = column.secondaryKey ? item[column.secondaryKey] : value;
                    cell.appendChild(badge);
                    break;
                }
                case 'link': {
                    cell.className = 'text-end';
                    const link = document.createElement('a');
                    link.className = 'btn btn-sm btn-outline-primary';
                    link.href = value;
                    link.textContent = column.linkText || 'Open';
                    cell.appendChild(link);
                    break;
                }
                default:
                    cell.textContent = value;
                    if (column.secondaryKey && item[column.secondaryKey] != null) {
                        const line = muted(`${column.secondaryPrefix || ''}${item[column.secondaryKey]}`);
                        line.classList.add('small');
                        cell.appendChild(line);
                    }
            }
            return cell;
        }

        function updateSortHeaders() {
            root.querySelectorAll('th[data-grid-sort-key]').forEach(th => {
                const active = th.dataset.gridSortKey === state.sort;
                th.setAttribute('aria-sort', active ? (state.direction === 'asc' ? 'ascending' : 'descending') : 'none');
                th.querySelector('.grid-sort-icon').textContent = active ? (state.direction === 'asc' ? ' ▲' : ' ▼') : '';
            });
        }

        function showMessage(text) {
            const row = document.createElement('tr');
            const cell = document.createElement('td');
            cell.colSpan = columns.length;
            cell.className = 'text-center text-muted py-5';
            cell.textContent = text;
            row.appendChild(cell);
            body.replaceChildren(row);
        }
    }

    function muted(text) {
        const element = document.createElement('div');
        element.className = 'text-muted';
        element.textContent = text;
        return element;
    }

    // Dates arrive as ISO strings; format the calendar date as sent, without shifting it through the browser's time zone.
    function formatDate(value) {
        const [year, month, day] = String(value).slice(0, 10).split('-').map(Number);
        return dateFormat.format(new Date(year, month - 1, day));
    }

    function positiveInt(value) {
        const number = Number.parseInt(value, 10);
        return Number.isInteger(number) && number > 0 ? number : null;
    }
})();
