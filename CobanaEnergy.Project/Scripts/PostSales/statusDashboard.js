
$(document).ready(function () {
    let table;
    let selectedEmails = [];
    let selectedType = "";
    let emailSubject = "";
    let emailBody = "";

    const selectConfigs = {
        '#contractstatus': 'Select Contract Status',
        '#paymentStatusAcc': 'Select Payment Status'
    };

    for (const [selector, placeholder] of Object.entries(selectConfigs)) {
        $(selector).select2({
            placeholder: placeholder,
            allowClear: true,
            width: '100%'
        });
    }



    function parseDateString(dateStr) {
        if (!dateStr) return null;
        const iso = dateStr.match(/^(\d{4})-(\d{2})-(\d{2})/);
        if (iso) return new Date(iso[1], iso[2] - 1, iso[3]);
        const f = new Date(dateStr);
        return isNaN(f.getTime()) ? null : f;
    }

    function formatDateForInput(dateStr) {
        if (!dateStr) return '-'; // null, undefined, empty string

        const d = parseDateString(dateStr);
        if (!(d instanceof Date) || isNaN(d)) return '-'; // invalid date
        const mm = String(d.getMonth() + 1).padStart(2, '0');
        const dd = String(d.getDate()).padStart(2, '0');

        return `${d.getFullYear()}-${mm}-${dd}`;
    }

    function initTable() {
        table = $('#statusDashboardTable').DataTable({
            serverSide: true,
            processing: true,
            autoWidth: false, 
            fixedColumns: {
                leftColumns: 3
            },
            ajax: {
                url: '/StatusDashboard/GetPostSalesContracts',
                type: 'POST',
                data: function (d) {
                    return $.extend({}, d, {
                        Supplier: $('#supplierFilter').val(),
                        ContractStatus: $('#contractstatus').val(),
                        PaymentStatus: $('#paymentStatusAcc').val(),
                        DateFrom: $('#startDateFilter').val(),
                        DateTo: $('#endDateFilter').val(),
                        Department: $('#departmentFilter').val(),
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
                },
                dataSrc: function (json) {
                    console.log("Returned rows:", json.data[0]); // check first row
                    return json.data;
                }
            },
            columns: [
                {
                    data: null, orderable: false, render: function (row) {
                        const url = `/StatusDashboard/Edit/${encodeURIComponent(row.EId)}?type=${encodeURIComponent(row.ContractType)}`;
                        return `<a class="btn edit-btn btn-sm btn-default" href="${url}" target="_blank">Edit</a>`;
                    }
                },
                {
                    data: null, orderable: false, render: function (row) {
                        return `
                         <div class="text-center">
                         <button class="btn btn-sm btn-primary save-row" data-eid="${row.EId}" disabled>
                          <span class="btn-text">Save</span>
                          <span class="spinner-border spinner-border-sm d-none" role="status" aria-hidden="true"></span>
                          </button>
                         </div>
                     `;
                    }

                },
                {
                    data: null, orderable: false, render: function (row) {
                        return `
                        <div class="d-flex flex-column gap-2">
                            <button class="btn btn-sm btn-primary send-email" 
                                data-eid="${row.EId}" 
                                data-emails="${row.EmailList || ''}" 
                                data-type="Supplier" data-subject="${row.EmailSubject || ''}" data-body="${row.EmailBody || ''}">
                                 <i class="bi bi-envelope-fill me-1"></i> Supplier
                            </button>
                            <button class="btn btn-sm btn-secondary send-email" 
                                data-eid="${row.EId}" 
                                data-emails="${row.AgentEmail || ''}" 
                                data-type="Agent" data-subject="${row.EmailSubject || ''}" data-body="${row.EmailBody || ''}">
                               <i class="bi bi-person-fill me-1"></i> Agent
                            </button>
                             </div>
                          `;
                    }

                },
                { data: 'Agent' },
                { data: 'Collaboration' },
                { data: 'CobanaSalesType' },
                { data: 'MPXN' },
                { data: 'SupplierName' },
                { data: 'BusinessName', className: 'wrap-text' },
                { data: 'InputDate', render: d => d ? formatDateForInput(d) : '-' },
                { data: 'PostCode' },
                // Email editable cell
                {
                    data: 'Email', className: 'email-column', render: function (d, t, row) {
                        const safe = d === null ? '' : d;
                        return `<input type="text" class="form-control form-control-sm editable-input email" data-eid="${row.EId}" data-field="Email" value="${escapeHtml(d)}" />`;
                    }
                },
                // StartDate editable
                {
                    data: 'StartDate', render: function (d, t, row) {
                        return `<input type="date" class="form-control form-control-sm editable-input startdate" data-eid="${row.EId}" data-field="StartDate" value="${formatDateForInput(d)}" />`;
                    }
                },
                // CED editable
                {
                    data: 'CED', render: function (d, t, row) {
                        return `<input type="date" class="form-control form-control-sm editable-input ced" data-eid="${row.EId}" data-field="CED" value="${formatDateForInput(d)}" />`;
                    }
                },
                // COT editable
                {
                    data: 'COTDate', render: function (d, t, row) {
                        return `<input type="date" class="form-control form-control-sm editable-input cot" data-eid="${row.EId}" data-field="COTDate" value="${formatDateForInput(d)}" />`;
                    }
                },
                // Contract Status editable select
                {
                    data: 'ContractStatus', render: function (d, t, row) {
                        const options = AccountDropdownOptions.contractStatus;
                        let html = `<select class="form-select form-select-sm contract-status" data-eid="${row.EId}" data-field="ContractStatus">`;
                        html += `<option value="">${d ?? '-'}</option>`;
                        options.forEach(o => {
                            const sel = (o === d) ? 'selected' : '';
                            html += `<option value="${o}" ${sel}>${o}</option>`;
                        });
                        html += `</select>`;
                        return html;
                    }
                },
                // Objection Date editable
                {
                    data: 'ObjectionDate', render: function (d, t, row) {
                        return `<input type="date" class="form-control form-control-sm editable-input objectiondate" data-eid="${row.EId}" data-field="ObjectionDate" value="${formatDateForInput(d)}" />`;
                    }
                },
                { data: 'ObjectionCount' },
                // QueryType editable
                {
                    data: 'QueryType', className: 'querytype-column', title: 'Query Type', render: function (data, type, row) {
                        if (type === 'display') {
                            return `<select class="form-select query-type-dropdown w-100" data-supplier="${row.SupplierName}"  data-selected="${data || ''}" data-id="${row.SupplierName}">
                                <option value="${data}">${data}</option>
                            </select>`;
                        }
                        return data;
                    }
                },
                { data: 'Duration', visible: false },
                { data: 'ContractType', visible: false },
                { data: 'EmailList', visible: false },
                { data: 'ContractId', visible: false }
            ],
            pageLength: 25,
            order: [[9, 'desc']],
            drawCallback: function () {
                // attach events for inline edits
                GetQueryDropDownData();
            }
        });
    }

    $('#statusDashboardTable').on('change keyup', '.editable, .editable-input, .contract-status, .query-type-dropdown', function () {
        const $row = $(this).closest('tr');
        $row.find('.save-row').prop('disabled', false); // sirf usi row ka save enable karo
    });

    $('#statusDashboardTable').on('click', '.save-row', function () {
        const $btn = $(this);
        const $row = $btn.closest('tr');
        const eid = $btn.data('eid');
        const rowDataTable = table.row($row).data();

        // Collect row data
        const rowData = {
            EId: eid,
            Email: $row.find('.email').val(),
            ContractId: rowDataTable.ContractId,
            StartDate: $row.find('.startdate').val(),
            CED: $row.find('.ced').val(),
            COTDate: $row.find('.cot').val(),
            ContractStatus: $row.find('.contract-status').val(),
            ObjectionDate: $row.find('.objectiondate').val(),
            ContractType : rowDataTable.ContractType,
            Duration: rowDataTable.Duration,
            QueryType: $row.find('.query-type-dropdown').val()
        };

        // Disable all save buttons while this one is processing
        $('#statusDashboardTable .save-row').prop('disabled', true);

        $btn.prop('disabled', true);
        $btn.find('.btn-text').text('');
        $btn.find('.spinner-border').removeClass('d-none');

        $.ajax({
            url: '/StatusDashboard/UpdatePostSalesRow',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(rowData),
            success: function (res) {
                if (res.success) {
                    showToastSuccess("Row updated successfully!");
                    table.ajax.reload(null, false);
                } else {
                    showToastError("Failed to update row.");
                }
            },
            error: function () {
                showToastError("Server error occurred.");
            },
            complete: function () {
                // After request finishes, re-enable all Save buttons except this row (disable after save)
                $('#statusDashboardTable .save-row').prop('disabled', true);
                // Optional: only this row's save reset, others stay enabled if edited
                $btn.prop('disabled', true);
                $btn.find('.btn-text').text('Save');
                $btn.find('.spinner-border').addClass('d-none');
            }
        });
    });


    function GetQueryDropDownData(){
        $('.query-type-dropdown').each(function () {
            const $dropdown = $(this);
            const supplier = $dropdown.data('supplier');
            let selectedVal = ($dropdown.data('selected') || "").trim();

            $.ajax({
                url: '/StatusDashboard/GetQueryTypes',
                type: 'GET',
                data: { supplier: supplier },
                success: function (options) {
                    $dropdown.empty();
                    if (!selectedVal || selectedVal === "-") {
                        $dropdown.append(`<option value="" selected>Select Query Type</option>`);
                    }

                    options.forEach(opt => {
                        const optTrim = (opt || "").trim();
                        const selected = (optTrim.toLowerCase() === selectedVal.toLowerCase()) ? "selected" : "";
                        $dropdown.append(`<option value="${optTrim}" ${selected}>${optTrim}</option>`);
                    });

                    // fallback: agar selectedVal backend ke options mai nahi hai
                    if (selectedVal && selectedVal !== "-" && !$dropdown.val()) {
                        $dropdown.append(`<option value="${selectedVal}" selected>${selectedVal}</option>`);
                    }
                }
            });
        });
    }


    function escapeHtml(text) {
        if (!text) return '';
        return text.replace(/[&<>"'\/]/g, function (s) {
            return ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;', '/': '&#x2F;' })[s];
        });
    }

    // Init flows

    $(function () {

        populateDropdown("departmentFilter", DropdownOptions.department, $('#departmentFilter').data('current'));
        populateDropdown("contractstatus", AccountDropdownOptions.contractStatus, $('#contractstatus').data('current'));
        populateDropdown("paymentStatusAcc", AccountDropdownOptions.paymentStatus, $('#paymentStatusAcc').data('current'));
        
        initTable();
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

    // auto refresh on filters change
    $('#contractStatusFilter, #supplierFilter, #startDateFilter, #endDateFilter').on('change', function () {
        table.ajax.reload();
    });



    $(document).on("click", ".send-email", function () {
        const emails = $(this).data("emails");
        selectedType = ($(this).data("type") || "").toLowerCase();
        emailSubject = ($(this).data("subject") || "").toLowerCase();
        emailBody = ($(this).data("body") || "").toLowerCase();

        selectedEmails = (emails && emails !== '-')
            ? (Array.isArray(emails) ? emails : emails.split(","))
            : [];
        const $container = $("#emailListContainer");
        $container.empty();

        if (selectedEmails.length === 0) {
            $container.append(`<tr><td colspan="2" class="text-muted text-center">No emails found</td></tr>`);
        } else {
            selectedEmails.forEach((e, i) => {
                $container.append(`
                <tr>
                    <td>${e.trim()}</td>
                    <td class="text-center">
                        <button class="btn btn-sm btn-outline-primary copy-single" data-email="${e.trim()}">
                            <i class="bi bi-clipboard"></i>
                        </button>
                    </td>
                </tr>
            `);
            });
        }

        $("#emailModal").modal("show");
    });

    // Copy All
    $(document).on("click", "#copyAllBtn", function () {
        if (selectedEmails.length) {
            navigator.clipboard.writeText(selectedEmails.join(";"));
            showToastSuccess("All emails copied!");
        }
    });

    // Copy single
    $(document).on("click", ".copy-single", function () {
        const email = $(this).data("email");
        navigator.clipboard.writeText(email);
        showToastSuccess(`Copied: ${email}`);
    });

    // Send via Outlook
    $(document).on("click", "#sendEmailBtn", function () {
        if (!selectedEmails.length) {
            showToastError("No emails to send!");
            return;
        }
        const to = selectedEmails.join(";");
        window.location.href = `mailto:${to}?subject=${emailSubject}&body=${emailBody}`;
    });

});

