﻿$(document).ready(async function () {

    if (!$('#editElectricForm').length) return;
    //$('#productSelect, #supplierCommsType').prop('disabled', true); 

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

    $('#productSelect').on('change', function () {
        const selectedOption = $(this).find('option:selected');
        const commsType = selectedOption.data('comms') ?? '';
        const $commsSelect = $('#supplierCommsType');

        $commsSelect.empty().append('<option value="">Select Supplier Comms Type</option>');
        DropdownOptions.supplierCommsType.forEach(v => {
            const selected = v === commsType ? 'selected' : '';
            $commsSelect.append(`<option value="${v}" ${selected}>${v}</option>`);
        });

        if (commsType) {
            $commsSelect.val(commsType).addClass('highlight-temp');
            setTimeout(() => {
                $commsSelect.removeClass('highlight-temp');
            }, 1000);
        }

        $commsSelect.prop('disabled', true);
    });

    $('#editElectricForm').on('submit', async function (e) {
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
            DayRate: $('#dayRate').val(),
            NightRate: $('#nightRate').val(),
            EveWeekendRate: $('#eveWeekendRate').val(),
            OtherRate: $('#otherRate').val(),
            StandingCharge: $('#standingCharge').val(),
            SortCode: $('#sortCode').val(),
            AccountNumber: $('#accountNumber').val(),
            TopLine: $('#topLine').val(),
            MPAN: $('#mpan').val(),
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

        const $uplift = $('#uplift');
        const $supplier = $('#supplierSelect');
        const eid = $('#eid').val();
        const isValid = await validateUpliftAgainstSupplierLimitElectric($uplift, $supplier, eid);
        if (!isValid) {
            $uplift.focus();
            $btn.prop('disabled', false).text('Update Contract');
            return;
        }

        $.ajax({
            url: '/Electric/ElectricEdit',
            method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(model),
            success: function (res) {
                if (res.success) {
                    showToastSuccess("Electric contract updated successfully!");

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
                    $('#initialStartDate').val(d.InitialStartDate);
                    $('#duration').val(d.Duration);
                    $('#uplift').val(d.Uplift);
                    $('#inputEAC').val(d.InputEAC);
                    $('#dayRate').val(d.DayRate);
                    $('#nightRate').val(d.NightRate);
                    $('#eveWeekendRate').val(d.EveWeekendRate);
                    $('#otherRate').val(d.OtherRate);
                    $('#standingCharge').val(d.StandingCharge);
                    $('#topLine').val(d.TopLine);
                    $('#mpan').val(d.MPAN);
                    $('#currentSupplier').val(d.CurrentSupplier);
                    $('#supplierId').val(d.SupplierId);
                    $('#productId').val(d.ProductId);
                    $('#supplierCommsType').val(d.SupplierCommsType);
                    $('#preSalesStatus').val(d.PreSalesStatus);
                    $('#contractNotes').val(d.ContractNotes);
                    $('#sortCode').val(d.SortCode);
                    $('#accountNumber').val(d.AccountNumber);
                    $('#inputDate').val(d.InputDate);
                    $('#emProcessor').val(d.EMProcessor);

                    $('#contractChecked').prop('checked', d.ContractChecked);
                    $('#contractAudited').prop('checked', d.ContractAudited);
                    $('#terminated').prop('checked', d.Terminated);

                    loadLogs(); // reload
                    //setTimeout(() => location.reload());

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
        const $logContainer = $('#electricLogsContainer');

        $logContainer.html('<div class="text-muted">Loading logs...</div>');

        $.get(`/Electric/GetLogsForElectricContract?eid=${eid}`, function (res) {
            if (res.success && res.Data.length > 0) {
                const html = res.Data.map(log => `
                   <div class="log-entry">
                      <div class="log-date">${log.ActionDate}</div>

                      <p class="log-field">
                        <span class="log-label">User:</span>
                        <span class="log-value">${log.Username}</span>
                      </p>

                      <p class="log-field">
                        <span class="log-label">Status:</span>
                        <span class="log-value">${log.PreSalesStatusType}</span>
                      </p>

                      <p class="log-field">
                        <span class="log-label">Message:</span>
                        <span class="log-value"><strong>${log.Message}</strong></span>
                      </p>
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

    $('#mpan').on('input', function () {
        const mpan = $(this).val().trim();

        if (/^\d{13}$/.test(mpan)) {
            $('#mpanLoader').show();

            $.get(`/Electric/CheckDuplicateMpan?mpan=${mpan}`, function (res) {
                $('#mpanLoader').hide();

                if (res.success && res.Data) {
                    const d = res.Data;
                    $('#duplicateMpanModalEdit tbody').html(`
                    <tr>
                        <td>${d.Agent}</td>
                        <td>${d.BusinessName}</td>
                        <td>${d.CustomerName}</td>
                        <td>${d.InputDate}</td>
                        <td>${d.PreSalesStatus}</td>
                        <td>${d.Duration}</td>
                    </tr>
                `);
                    $('#duplicateMpanModalEdit').modal('show');
                }
            }).fail(function () {
                $('#mpanLoader').hide();
                showToastError("Error checking MPAN.");
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
                    const tbody = $('#duplicateAccountModalEdit tbody');
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

                    $('#duplicateAccountModalEdit').modal('show');
                }
            }).fail(function () {
                $('#accountLoader').hide();
                showToastError("Error checking account number.");
            });
        }
    });

    $('#uplift').on('blur', async function () {
        const eid = $('#eid').val();
        await validateUpliftAgainstSupplierLimitElectric($('#uplift'), $('#supplierSelect'), eid);
    });

    $('#supplierSelect').on('change', async function () {
        const eid = $('#eid').val();
        await validateUpliftAgainstSupplierLimitElectric($('#uplift'), $('#supplierSelect'), eid);
    });

});
