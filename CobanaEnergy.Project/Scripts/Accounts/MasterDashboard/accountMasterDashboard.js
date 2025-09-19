$(document).ready(function () {
    let table;
    let allFollowUpDates = [];
    let selectedDate = "";


    const selectConfigs = {
        '#contractstatus': 'Select Contract Status',
        '#supplierFilter': 'All Suppliers',
        '#department': 'Select Department',
        '#brokerageFilter': 'Select Brokerage',
        '#brokerageStaffFilter': 'Select Staff',
        '#subBrokerageFilter': 'Select Sub Brokerage',
        '#closerFilter': 'Select Closer',
        '#leadGenFilter': 'Select Lead Generator',
        '#introducerFilter': 'Select Introducer',
        '#subIntroducerFilter': 'Select Sub Introducer',
        '#referralFilter' : 'Select Referral'
    };

    for (const [selector, placeholder] of Object.entries(selectConfigs)) {
        $(selector).select2({
            placeholder: placeholder,
            allowClear: true,
            width: '100%'
        });
    }


    function dataTableInit() {
        table = $('#accountMasterTable').DataTable({
            serverSide: true,
            processing: true,
            ajax: {
                url: '/AccountMasterDashboard/GetContracts',
                type: 'POST',
                data: function (d) {
                    return $.extend({}, d, {
                        Supplier: $('#supplierFilter').val(),
                        ContractStatus: $('#contractstatus').val(),
                        DateFrom: $('#startDateFilter').val(),
                        DateTo: $('#endDateFilter').val(),
                        Department: $('#department').val(),
                        BrokerageId: $('#brokerageFilter').val(),
                        StaffId: $('#brokerageStaffFilter').val(),
                        SubBrokerageId: $('#subBrokerageFilter').val(),
                        CloserId: $('#closerFilter').val(),
                        LeadGeneratorId: $('#leadGenFilter').val(),
                        ReferralPartnerId: $('#referralFilter').val(),
                        IntroducerId: $('#introducerFilter').val(),
                        SubIntroducerId: $('#subIntroducerFilter').val()
                    });
                }
            },
            pageLength: 25,
            order: [[6, 'desc']],
            dom: 'Bfrtip',
            buttons: [
                {
                    extend: 'excelHtml5',
                    text: '<i class="fas fa-file-excel me-2"></i> Export Excel',
                    className: 'btn btn-success btn-sm dt-btn',
                    title: 'AccountMaster_Export'
                },
                {
                    extend: 'pdfHtml5',
                    text: '<i class="fas fa-file-pdf me-2"></i> Export PDF',
                    className: 'btn btn-danger btn-sm dt-btn',
                    title: 'AccountMaster_Export',
                    orientation: 'landscape',
                    pageSize: 'A4'
                }
            ],
            columns: [
                {
                    data: null, orderable: false, render: function (data, type, row) {
                        const mp = encodeURIComponent(row.MPXN || '');
                        const supplierId = row.SupplierId || '';
                        const id = row.EId;
                        const controller = row.Controller;
                        const action = row.Action;

                        const url = `/${controller}/${action}/${id}?supplierId=${supplierId}&type=${mp}`;
                        return `<a class="btn btn-sm edit-btn" href="${url}" target="_blank">Edit</a>`;
                    }
                },
                { data: 'SupplierName' },
                { data: 'BusinessName' },
                { data: 'MPXN' },
                { data: 'InputEAC' },
                { data: 'SupplierEAC' },
                { data: 'InputDate', render: d => d ? new Date(d).toLocaleDateString() : '-' },
                { data: 'StartDate', render: d => d ? new Date(d).toLocaleDateString() : '-' },
                { data: 'CED', render: d => d ? new Date(d).toLocaleDateString() : '-' },
                { data: 'COTDate', render: d => d ? new Date(d).toLocaleDateString() : '-' },
                { data: 'ContractStatus' },
                { data: 'PaymentStatus' },
                { data: 'PreviousInvoiceNumbers' },
                {
                    data: 'AccountsNotes',
                    render: function (data, type, row) {
                        if (!data) return '-';
                        return `<span class="truncate-cell">
                           ${data.length > 50 ? data.substring(0, 50) + '…' : data}
                         </span>`;
                    }

                },
                { data: 'CommissionForecast', render: d => d ?? '-' },
                { data: 'CobanaDueCommission', render: d => d ?? '-' },
                { data: 'CobanaPaidCommission', render: d => d ?? '-' },
                { data: 'CobanaReconciliation', render: d => d ?? '-' }
            ],
            drawCallback: function () {
                $('#accountMasterTable tbody tr td').each(function () {
                    if ($(this).text().trim() === '') $(this).text('-');
                });
            }
        });
    }

    $(function () {
        populateDropdown("department", DropdownOptions.department, $('#department').data('current'));
        populateDropdown("contractstatus", AccountDropdownOptions.contractStatus, $('#contractstatus').data('current'));
        dataTableInit();

        FilterModule.init(table);
       
    });

    function populateDropdown(id, values, current) {
        const $el = $('#' + id);
        if (!$el.length) return;

        let placeholder = "Select " + id.replace(/([A-Z])/g, ' $1').trim();
        $el.empty().append(`<option value="">${placeholder}</option>`);
        if (Array.isArray(values)) {
            values.forEach(v => {
                const selected = v === current ? 'selected' : '';
                $el.append(`<option value="${v}" ${selected}>${v}</option>`);
            });
        }
    }

});
