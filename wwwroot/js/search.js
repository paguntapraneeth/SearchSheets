const $ = id => document.getElementById(id);

async function searchByPhone() {
    const phone = $('searchInput').value.trim();

    if (!phone) {
        $('metaText').textContent = 'Enter phone number';
        return;
    }

    $('metaText').textContent = 'Searching...';

    try {
        const res = await fetch(`/api/search/phone?phone=${phone}`);
        const data = await res.json();

        renderResults(data);
    } catch {
        $('metaText').textContent = 'Error occurred';
    }
}

function renderResults(data) {
    const container = $('resultsContainer');
    const tbody = $('resultsBody');

    container.style.display = 'block';

    if (!data || data.length === 0) {
        $('metaText').textContent = 'No results';

        tbody.innerHTML = `
            <tr>
                <td colspan="5" class="empty-row">
                    No records found
                </td>
            </tr>
        `;
        return;
    }

    $('metaText').textContent = `${data.length} result(s)`;

    let rows = '';

    data.forEach(item => {
        rows += `
            <tr>
                <td><strong>${item.name}</strong></td>
                <td>${item.phone}</td>
                <td>${item.th}</td>
                <td>${item.ro}</td>
                <td>₹${item.am}</td>
            </tr>
        `;
    });

    tbody.innerHTML = rows;
}

// Events
document.addEventListener('DOMContentLoaded', () => {
    $('searchBtn').addEventListener('click', searchByPhone);

    $('searchInput').addEventListener('keydown', e => {
        if (e.key === 'Enter') searchByPhone();
    });

    $('syncBtn').addEventListener('click', async () => {
        $('metaText').textContent = 'Syncing...';
        await fetch('/api/sync', { method: 'POST' });
        $('metaText').textContent = 'Sync completed';
    });
});