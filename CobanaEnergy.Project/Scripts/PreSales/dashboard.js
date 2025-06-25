$(document).ready(function () {

    loadContractTable();
    loadSupplierStats();
    setInterval(loadSupplierStats, 120000);
});

function loadContractTable() {
    const $table = $('#contractTable tbody');

    $table.html('<tr><td colspan="9" class="text-center text-secondary">Loading...</td></tr>');

    $.get('/PreSales/GetAllContracts', function (res) {
        if (!res.success || !res.Data) {
            $table.html('<tr><td colspan="9" class="text-danger fw-bold text-center">No contracts found.</td></tr>');
            showToastWarning("No contract records found.");
            return;
        }

        $table.empty();
        res.Data.forEach(r => {
            let controler;
            if (r.Type == 'Electric') {
                controler = 'Electric/ElectricEdit';
            }
            if (r.Type == 'Gas') {
                controler = 'Gas/EditGas';
            }
            if (r.Type == 'Dual') {
                controler = 'Dual/EditDual';
            }

            const row = `
                            <tr>
                                <td><a href="/${controler}?eid=${r.EId}" class="btn btn-sm edit-btn" title="Edit"><i class="fas fa-pencil-alt me-1"></i> Edit</a></td>
                                <td>${r.Agent}</td>
                                <td>${r.MPAN || '-'}</td>
                                <td>${r.MPRN || '-'}</td>
                                <td>${r.BusinessName}</td>
                                <td>${r.CustomerName}</td>
                                <td>${r.InputDate ?? '-'}</td>
                                <td>${r.PreSalesStatus ?? '-'}</td>
                                <td>
                                      <span class="truncate-cell">
                                        ${(r.Notes && r.Notes.length > 50) ? r.Notes.substring(0, 50) + '…' : (r.Notes || '-')}
                                      </span>
                               </td>
                            </tr>`;
            $table.append(row);
        });

        $('#contractTable').DataTable({
            paging: true,
            searching: true,
            ordering: true,
            order: [[6, 'desc']],
            info: true,
            responsive: true,
            autoWidth: false
        });

    }).fail(function () {
        $table.html('<tr><td colspan="9" class="text-danger fw-bold text-center">Error loading contracts.</td></tr>');
        showToastError("Error loading dashboard data.");
    });
}
function loadSupplierStats() {
    const $panel = $('#supplier-stats-content');
    const now = new Date();
    const hour = now.getHours();

    if (hour < 8 || hour >= 19) {
        $panel.html('<div class="text-muted text-center w-100">Supplier stats are only visible from 08:00 to 19:00.</div>');
        return;
    }

    $panel.html('<div class="text-center text-secondary w-100">Loading supplier stats...</div>');

    $.get('/PreSales/GetSupplierContractStats', function (res) {
        if (!res.success || !res.Data || res.Data.length === 0) {
            $panel.html('<div class="text-danger text-center w-100">No stats found.</div>');
            return;
        }

        const grouped = {};
        res.Data.forEach(item => {
            if (!grouped[item.SupplierName]) grouped[item.SupplierName] = [];
            grouped[item.SupplierName].push(item);
        });

        let html = '';

        Object.keys(grouped).forEach(supplier => {
            html += `<div class="supplier-block mb-4">
                <h6 class="supplier-name">${supplier}</h6>`;
            grouped[supplier].forEach(stat => {
                html += `<div class="status-line">
                    <strong>Status: ${stat.PreSalesStatus}</strong>
                    <div class="badge-group">
                        <span class="badge bg-success">Gas: ${stat.GasCount}</span>
                        <span class="badge bg-primary">Electric: ${stat.ElectricCount}</span>
                    </div>
                 </div>`;
            });
            html += `</div>`;
        });

        $panel.html(html);
    }).fail(function () {
        $panel.html('<div class="text-danger text-center w-100">Failed to load stats.</div>');
        showToastError("Error loading supplier stats.");
    });
}