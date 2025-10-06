$(document).ready(function () {
    let table;
    let allFollowUpDates = [];
    let selectedDate = "";

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
                        PaymentStatus: $('#paymentStatusAcc').val(),
                        DateFrom: $('#startDateFilter').val(),
                        DateTo: $('#endDateFilter').val(),
                        Department: $('#department').val(),
                        BrokerageId: $('#brokerageFilter').val(),
                        StaffId: $('#brokerageStaffFilter').val(),
                        SubBrokerageId: $('#subBrokerageFilter').val(),
                        CloserId: $('#closerFilter').val(),
                        LeadGeneratorId: $('#leadGenFilter').val(),
                        ReferralPartnerId: $('#referralFilter').val(),
                        SubReferralPartnerId: $('#subReferralFilter').val(),
                        IntroducerId: $('#introducerFilter').val(),
                        SubIntroducerId: $('#subIntroducerFilter').val(),
                        Search: d.search
                    });
                }
            },
            pageLength: 25,
            order: [[6, 'desc']],
            dom:
                '<"row mb-2"<"col-sm-12 text-end"B>>' +
                '<"row mb-2"<"col-sm-6"l><"col-sm-6"f>>' +
                'rtip',
            buttons: [
                {
                    extend: 'excelHtml5',
                    text: '<i class="fas fa-file-excel me-2"></i> Export Excel',
                    className: 'btn btn-success btn-sm dt-btn',
                    title: '',
                    filename: function() {
                        let filename = 'AccountMasterDashboard';
                        
                        const contractStatus = $('#contractstatus').val();
                        const paymentStatus = $('#paymentStatusAcc').val();
                        
                        if (contractStatus) {
                            filename += `_${contractStatus}`;
                        }
                        
                        if (paymentStatus) {
                            filename += `_${paymentStatus}`;
                        }
                        
                        return filename;
                    },
                    exportOptions: {
                        columns: ':visible:not(:first-child)' 
                    }
                },
                {
                    extend: 'pdfHtml5',
                    text: '<i class="fas fa-file-pdf me-2"></i> Export PDF',
                    className: 'btn btn-danger btn-sm dt-btn',
                    title: 'AccountMaster',
                    orientation: 'landscape',
                    pageSize: 'A3',
                    filename: function() {
                        let filename = 'AccountMasterDashboard';
                        
                        const contractStatus = $('#contractstatus').val();
                        const paymentStatus = $('#paymentStatusAcc').val();
                        
                        if (contractStatus) {
                            filename += `_${contractStatus}`;
                        }
                        
                        if (paymentStatus) {
                            filename += `_${paymentStatus}`;
                        }
                        
                        return filename;
                    },
                    exportOptions: {
                        columns: ':visible:not(:first-child)'
                    },
                    customize: function (doc) {
                        doc.defaultStyle.fontSize = 5;
                        doc.styles.tableHeader.fontSize = 6;
                        
                        // More robust table width setting
                        if (doc.content && doc.content.length > 1) {
                            for (let i = 0; i < doc.content.length; i++) {
                                if (doc.content[i].table && doc.content[i].table.body && doc.content[i].table.body.length > 0) {
                                    const colCount = doc.content[i].table.body[0].length;
                                    doc.content[i].table.widths = Array(colCount).fill('*');
                                    break;
                                }
                            }
                        }
                    }
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
                {
                    data: 'InputDate',
                    render: function (d) {
                        const dateObj = parseDateString(d);
                        return dateObj ? dateObj.toLocaleDateString() : "-";
                    }
                },
                {
                    data: 'StartDate',
                    render: function (d) {
                        const dateObj = parseDateString(d);
                        return dateObj ? dateObj.toLocaleDateString() : "-";
                    }
                },
                {
                    data: 'CED',
                    render: function (d) {
                        const dateObj = parseDateString(d);
                        return dateObj ? dateObj.toLocaleDateString() : "-";
                    }
                },
                {
                    data: 'COTDate',
                    render: function (d) {
                        const dateObj = parseDateString(d);
                        return dateObj ? dateObj.toLocaleDateString() : "-";
                    }
                },
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
                { data: 'CobanaFinalReconciliation', render: d => d ?? '-' }
            ],
            drawCallback: function () {
                $('#accountMasterTable tbody tr td').each(function () {
                    if ($(this).text().trim() === '') $(this).text('-');
                });
            }
        });
    }

    function parseDateString(dateStr) {
        if (!dateStr || dateStr.trim() === "") return null;

        // yyyy-MM-dd
        const isoMatch = dateStr.match(/^(\d{4})-(\d{2})-(\d{2})$/);
        if (isoMatch) {
            const [_, y, m, d] = isoMatch;
            return new Date(y, m - 1, d);
        }

        // dd/MM/yyyy
        const ukMatch = dateStr.match(/^(\d{2})\/(\d{2})\/(\d{4})$/);
        if (ukMatch) {
            const [_, d, m, y] = ukMatch;
            return new Date(y, m - 1, d);
        }

        // fallback → try browser parser
        const fallback = new Date(dateStr);
        return isNaN(fallback.getTime()) ? null : fallback;
    }

    $(function () {
        populateDropdown("department", DropdownOptions.department, $('#department').data('current'));
        populateDropdown("contractstatus", AccountDropdownOptions.contractStatus, $('#contractstatus').data('current'));
        populateDropdown("paymentStatusAcc", AccountDropdownOptions.paymentStatus, $('#paymentStatusAcc').data('current'));

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
