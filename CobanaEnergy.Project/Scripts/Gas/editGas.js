$(document).ready(function () {

    if (!$('#editGasForm').length) return;

    const token = $('input[name="__RequestVerificationToken"]').val();
    if (token) {
        $.ajaxSetup({ headers: { 'RequestVerificationToken': token } });
    }

    for (const id in DropdownOptions) {
        populateDropdown(id, DropdownOptions[id]);
    }

    function populateDropdown(id, values) {
        const $el = $('#' + id);
        if (!$el.length) return;
        const current = $el.data('current');
        $el.empty().append(`<option value="">Select ${id}</option>`);
        values.forEach(val => {
            const selected = val === current ? 'selected' : '';
            $el.append(`<option value="${val}" ${selected}>${val}</option>`);
        });
    }

    $.get('/Supplier/GetActiveSuppliersForDropdown', function (res) {
        const $supplier = $('#supplierSelect');
        $supplier.empty().append('<option value="">Select Supplier</option>');

        if (res.success && res.Data.length > 0) {
            res.Data.forEach(s => {
                const selected = s.Id == $supplier.data('current') ? 'selected' : '';
                $supplier.append(`<option value="${s.Id}" ${selected}>${s.Name}</option>`);
            });
        } else {
            $supplier.append('<option disabled>No suppliers found</option>');
        }

        $('#supplierSelect').trigger('change');
    });

    $('#supplierSelect').change(function () {
        const supplierId = $(this).val();
        const $product = $('#productSelect');
        $product.prop('disabled', true).empty().append('<option>Loading...</option>');
        if (!supplierId) {
            $product.empty().append('<option value="">Select Product</option>').prop('disabled', false);
            return;
        }

        $.get(`/Supplier/GetProductsBySupplier?supplierId=${supplierId}`, function (res) {
            $product.empty().append('<option value="">Select Product</option>');
            if (res.success && res.Data.length > 0) {
                res.Data.forEach(p => {
                    const selected = (p.Id == $product.data('current')) ? 'selected' : '';
                    $product.append(`<option value="${p.Id}" ${selected}>${p.ProductName}</option>`);
                });
            } else {
                $product.append('<option disabled>No products found</option>');
            }
            $product.prop('disabled', false);
        });
    });

    $('#supplierSelect').trigger('change');

    $('#editGasForm').on('submit', function (e) {
        e.preventDefault();
        let hasInvalid = false;

        $(this).find('.form-control, .form-select').each(function () {
            $(this).removeClass('is-invalid');
            if (!this.checkValidity()) {
                $(this).addClass('is-invalid');
                hasInvalid = true;
            }
        });

        if (hasInvalid) {
            const $first = $(this).find(':invalid').first();
            $first.focus();
            showToastWarning("Please fill all required fields.");
            return;
        }

        const model = {
            EId: $('#eid').val(),
            Department: $('#department').val(),
            Agent: $('#agent').val(),
            Source: $('#source').val(),
            Introducer: $('#introducer').val(),
            SubIntroducer: $('#subIntroducer').val(),
            SalesType: $('#salesType').val(),
            SalesTypeStatus: $('#salesTypeStatus').val(),
            BusinessName: $('#businessName').val(),
            CustomerName: $('#customerName').val(),
            BusinessDoorNumber: $('#businessDoorNumber').val(),
            BusinessHouseName: $('#businessHouseName').val(),
            BusinessStreet: $('#businessStreet').val(),
            BusinessTown: $('#businessTown').val(),
            BusinessCounty: $('#businessCounty').val(),
            PostCode: $('#postCode').val(),
            PhoneNumber1: $('#phoneNumber1').val(),
            PhoneNumber2: $('#phoneNumber2').val(),
            EmailAddress: $('#emailAddress').val(),
            InitialStartDate: $('#initialStartDate').val(),
            Duration: $('#duration').val(),
            Uplift: $('#uplift').val(),
            InputEAC: $('#inputEAC').val(),
            UnitRate: $('#unitRate').val(),
            OtherRate: $('#otherRate').val(),
            StandingCharge: $('#standingCharge').val(),
            SortCode: $('#sortCode').val(),
            AccountNumber: $('#accountNumber').val(),
            MPRN: $('#mprn').val(),
            CurrentSupplier: $('#currentSupplier').val(),
            SupplierId: $('#supplierSelect').val(),
            ProductId: $('#productSelect').val(),
            SupplierCommsType: $('#supplierCommsType').val(),
            PreSalesStatus: $('#preSalesStatus').val(),
            EMProcessor: $('#emProcessor').val(),
            ContractChecked: $('#contractChecked').is(':checked'),
            ContractAudited: $('#contractAudited').is(':checked'),
            Terminated: $('#terminated').is(':checked'),
            ContractNotes: $('#contractNotes').val()
        };

        const $btn = $(this).find('button[type="submit"]');
        $btn.prop('disabled', true).text('Updating...');

        $.ajax({
            url: '/Gas/EditGas',
            method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(model),
            success: function (res) {
                if (res.success) {
                    showToastSuccess("Gas contract updated successfully!");
                    const d = res.Data;

                    $('#agent').val(d.Agent);
                    $('#department').val(d.Department);
                    $('#source').val(d.Source);
                    $('#introducer').val(d.Introducer);
                    $('#subIntroducer').val(d.SubIntroducer);
                    $('#salesType').val(d.SalesType);
                    $('#salesTypeStatus').val(d.SalesTypeStatus);
                    $('#businessName').val(d.BusinessName);
                    $('#customerName').val(d.CustomerName);
                    $('#businessDoorNumber').val(d.BusinessDoorNumber);
                    $('#businessHouseName').val(d.BusinessHouseName);
                    $('#businessStreet').val(d.BusinessStreet);
                    $('#businessTown').val(d.BusinessTown);
                    $('#businessCounty').val(d.BusinessCounty);
                    $('#postCode').val(d.PostCode);
                    $('#phoneNumber1').val(d.PhoneNumber1);
                    $('#phoneNumber2').val(d.PhoneNumber2);
                    $('#emailAddress').val(d.EmailAddress);
                    $('#inputDate').val(d.InputDate);
                    $('#initialStartDate').val(d.InitialStartDate);
                    $('#duration').val(d.Duration);
                    $('#uplift').val(d.Uplift);
                    $('#inputEAC').val(d.InputEAC);
                    $('#unitRate').val(d.UnitRate);
                    $('#otherRate').val(d.OtherRate);
                    $('#standingCharge').val(d.StandingCharge);
                    $('#sortCode').val(d.SortCode);
                    $('#accountNumber').val(d.AccountNumber);
                    $('#mprn').val(d.MPRN);
                    $('#currentSupplier').val(d.CurrentSupplier);
                    $('#supplierSelect').val(d.SupplierId);
                    $('#productSelect').val(d.ProductId);
                    $('#supplierCommsType').val(d.SupplierCommsType);
                    $('#preSalesStatus').val(d.PreSalesStatus);
                    $('#contractNotes').val(d.ContractNotes);
                    $('#emProcessor').val(d.EMProcessor);

                    $('#contractChecked').prop('checked', d.ContractChecked);
                    $('#contractAudited').prop('checked', d.ContractAudited);
                    $('#terminated').prop('checked', d.Terminated);

                    loadLogs();
                } else {
                    showToastError(res.message);
                }
            },
            error: function () {
                showToastError("Unexpected error occurred.");
            },
            complete: function () {
                $btn.prop('disabled', false).text('Update Contract');
            }
        });
    });

    $(document).on('input change', '.form-control, .form-select', function () {
        if (this.checkValidity()) {
            $(this).removeClass('is-invalid');
        }
    });

    function loadLogs() {
        const eid = $('#eid').val();
        const $logContainer = $('#gasLogsContainer');
        $logContainer.html('<div class="text-muted">Loading logs...</div>');

        $.get(`/Gas/GetLogsForGasContract?eid=${eid}`, function (res) {
            if (res.success && res.Data.length > 0) {
                const html = res.Data.map(log => `
                    <div class="log-entry">
                        <div class="log-date">${log.ActionDate}</div>
                        <p class="log-field"><span class="log-label">User:</span> <span class="log-value">${log.Username}</span></p>
                        <p class="log-field"><span class="log-label">Status:</span> <span class="log-value">${log.PreSalesStatusType}</span></p>
                        <p class="log-field"><span class="log-label">Message:</span> <span class="log-value"><strong>${log.Message}</strong></span></p>
                    </div>
                `).join('');
                $logContainer.html(html);
            } else {
                $logContainer.html('<div class="text-muted">No logs available.</div>');
            }
        }).fail(() => {
            $logContainer.html('<div class="text-danger">Failed to load logs.</div>');
        });
    }

    loadLogs();

    $('#mprn').on('input', function () {
        const mprn = $(this).val().trim();
        if (/^\d{6,10}$/.test(mprn)) {
            $('#mprnLoader').show();
            $.get(`/Gas/CheckDuplicateMprn?mprn=${mprn}`, function (res) {
                $('#mprnLoader').hide();
                if (res.success && res.Data) {
                    const d = res.Data;
                    $('#duplicateMprnModalEdit tbody').html(`
                        <tr>
                            <td>${d.Agent}</td>
                            <td>${d.BusinessName}</td>
                            <td>${d.CustomerName}</td>
                            <td>${d.InputDate}</td>
                            <td>${d.PreSalesStatus}</td>
                            <td>${d.Duration}</td>
                        </tr>
                    `);
                    $('#duplicateMprnModalEdit').modal('show');
                }
            }).fail(function () {
                $('#mprnLoader').hide();
                showToastError("Error checking MPRN.");
            });
        }
    });

    $('#accountNumber').on('input', function () {
        const acc = $(this).val().trim();
        if (/^\d{8}$/.test(acc)) {
            $('#accountLoader').show();
            $.get(`/CheckDuplicateAccount/CheckDuplicateAccountUnified?account=${acc}`, function (res) {
                $('#accountLoader').hide();
                if (res.success && res.Data?.length > 0) {
                    const tbody = $('#duplicateAccountModalGasEdit tbody');
                    tbody.empty();
                    res.Data.forEach(r => {
                        tbody.append(`
                            <tr>
                                <td>${r.Agent}</td>
                                <td>${r.BusinessName}</td>
                                <td>${r.CustomerName}</td>
                                <td>${r.InputDate}</td>
                                <td>${r.PreSalesStatus}</td>
                                <td>${r.Duration}</td>
                                <td>${r.SortCode}</td>
                                <td>${r.AccountNumber}</td>
                            </tr>
                        `);
                    });
                    $('#duplicateAccountModalGasEdit').modal('show');
                }
            }).fail(function () {
                $('#accountLoader').hide();
                showToastError("Error checking account number.");
            });
        }
    });

});