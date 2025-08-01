﻿$(document).ready(async function () {
    if (!$('#editDualForm').length) return;
    // $('#electricCommsType, #gasCommsType').prop('disabled', true);

    const token = $('input[name="__RequestVerificationToken"]').val();
    if (token) {
        $.ajaxSetup({ headers: { 'RequestVerificationToken': token } });
    }

    function populateDropdown(id, values, currentValue) {
        const $select = $('#' + id);
        $select.empty().append(`<option value="">Select ${id}</option>`);
        values.forEach(v => {
            const selected = v === currentValue ? 'selected' : '';
            $select.append(`<option value="${v}" ${selected}>${v}</option>`);
        });
    }

    populateDropdown("department", DropdownOptions.department, $('#department').data('current'));
    populateDropdown("source", DropdownOptions.source, $('#source').data('current'));
    populateDropdown("electricSalesType", DropdownOptions.salesType, $('#electricSalesType').data('current'));
    populateDropdown("electricSalesStatus", DropdownOptions.salesTypeStatus, $('#electricSalesStatus').data('current'));
    populateDropdown("electricCommsType", DropdownOptions.supplierCommsType, $('#electricCommsType').data('current'));
    populateDropdown("electricPreSalesStatus", DropdownOptions.preSalesStatus, $('#electricPreSalesStatus').data('current'));

    populateDropdown("gasSalesType", DropdownOptions.salesType, $('#gasSalesType').data('current'));
    populateDropdown("gasSalesStatus", DropdownOptions.salesTypeStatus, $('#gasSalesStatus').data('current'));
    populateDropdown("gasCommsType", DropdownOptions.supplierCommsType, $('#gasCommsType').data('current'));
    populateDropdown("gasPreSalesStatus", DropdownOptions.preSalesStatus, $('#gasPreSalesStatus').data('current'));

    const electricCurrentSupplier = $('#electricSupplier').data('current');
    const electricCurrentProduct = $('#electricProduct').data('current');

    const gasCurrentSupplier = $('#gasSupplier').data('current');
    const gasCurrentProduct = $('#gasProduct').data('current');

    handleCommsType('#electricProduct', '#electricCommsType');
    handleCommsType('#gasProduct', '#gasCommsType');

    $('#electricProduct').on('change', function () {
        handleCommsType('#electricProduct', '#electricCommsType');
    });

    $('#gasProduct').on('change', function () {
        handleCommsType('#gasProduct', '#gasCommsType');
    });

    function loadSnapshotProducts(supplierId, targetId, selectedId, products) {
        const $target = $(targetId);
        const $comms = (targetId === '#electricProduct') ? $('#electricCommsType') : $('#gasCommsType');

        if (!products || !products.length) {
            $target.empty().append('<option value="">Select Product</option>').prop('disabled', false);
            $comms.empty().append('<option value="">Select Supplier Comms Type</option>').prop('disabled', false);
            return;
        }

        $target.prop('disabled', false).empty().append('<option value="">Select Product</option>');
        products.forEach(p => {
            const comms = p.SupplierCommsType ?? '';
            $target.append(`<option value="${p.Id}" data-comms="${comms}" ${p.Id == selectedId ? 'selected' : ''}>${p.ProductName}</option>`);
        });

        handleCommsType(targetId, $comms);
    }

    function handleCommsType(productSelector, commsSelector) {
        const selectedOption = $(productSelector).find('option:selected');
        const commsType = selectedOption.data('comms') ?? '';
        const $comms = $(commsSelector);
        const current = $comms.data('current');

        $comms.empty().append('<option value="">Select Supplier Comms Type</option>');

        DropdownOptions.supplierCommsType.forEach(v => {
            const selected = commsType ? v === commsType : v === current ? 'selected' : '';
            $comms.append(`<option value="${v}" ${selected}>${v}</option>`);
        });

        if (commsType) {
            $comms.val(commsType).addClass('highlight-temp');
            setTimeout(() => {
                $comms.removeClass('highlight-temp');
            }, 1000);
        }

        $comms.prop('disabled', true);
    }

    $('.form-control, .form-select').on('input change', function () {
        if (this.checkValidity()) {
            $(this).removeClass('is-invalid');
        }
    });

    $('#editDualForm').on('submit', async function (e) {
        e.preventDefault();
        let hasInvalid = false;

        $(this).find('.form-control, .form-select').each(function () {
            if (!this.checkValidity()) {
                $(this).addClass('is-invalid');
                hasInvalid = true;
            }
        });

        if (hasInvalid) {
            const $first = $(this).find(':invalid').first();
            $first.focus();
            showToastWarning("Please fix the highlighted fields.");
            return;
        }

        const model = getDualModel();

        const $btn = $(this).find('button[type="submit"]');
        $btn.prop('disabled', true).text('Updating...');

        const $electricUplift = $('#electricUplift');
        const $gasUplift = $('#gasUplift');

        const isElectricValid = await validateUpliftAgainstSupplierLimitElectric($('#electricUplift'), $('#electricSupplier'), $('#eid').val());
        if (!isElectricValid) {
            $electricUplift.focus();
            $btn.prop('disabled', false).text('Update Dual Contract');
            return;
        }

        const isGasValid = await validateUpliftAgainstSupplierLimitGas($('#gasUplift'), $('#gasSupplier'), $('#eid').val());
        if (!isGasValid) {
            $gasUplift.focus();
            $btn.prop('disabled', false).text('Update Dual Contract');
            return;
        }

        $.ajax({
            url: '/Dual/EditDual',
            method: 'POST',
            data: JSON.stringify(model),
            contentType: 'application/json',
            success: function (res) {
                if (res.success) {
                    showToastSuccess("Dual contract updated successfully!");
                    const d = res.Data;

                    $('#department').val(d.Department);
                    $('#agent').val(d.Agent);
                    $('#source').val(d.Source);
                    $('#introducer').val(d.Introducer);
                    $('#subIntroducer').val(d.SubIntroducer);
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
                    $('#inputDateElectric').val(d.ElectricInputDate);
                    $('#inputDateGas').val(d.GasInputDate);
                    $('#emProcessor').val(d.EMProcessor);
                    $('#sortCode').val(d.SortCode);
                    $('#accountNumber').val(d.AccountNumber);

                    $('#mpan').val(d.MPAN);
                    $('#mprn').val(d.MPRN);

                    $('#electricCurrentSupplier').val(d.ElectricCurrentSupplier);
                    $('#electricSalesType').val(d.ElectricSalesType);
                    $('#electricSalesStatus').val(d.ElectricSalesTypeStatus);
                    $('#electricDuration').val(d.ElectricDuration);
                    $('#electricUplift').val(d.ElectricUplift);
                    $('#electricInputEAC').val(d.ElectricInputEAC);
                    $('#electricStandingCharge').val(d.ElectricStandingCharge);
                    $('#dayRate').val(d.ElectricDayRate);
                    $('#nightRate').val(d.ElectricNightRate);
                    $('#eveWeekendRate').val(d.ElectricEveWeekendRate);
                    $('#electricOtherRate').val(d.ElectricOtherRate);
                    $('#electricSupplier').val(d.ElectricSupplierId);
                    $('#electricProduct').val(d.ElectricProductId);
                    $('#electricCommsType').val(d.ElectricSupplierCommsType);
                    $('#electricPreSalesStatus').val(d.ElectricPreSalesStatus);

                    $('#gasCurrentSupplier').val(d.GasCurrentSupplier);
                    $('#gasSalesType').val(d.GasSalesType);
                    $('#gasSalesStatus').val(d.GasSalesTypeStatus);
                    $('#gasDuration').val(d.GasDuration);
                    $('#gasUplift').val(d.GasUplift);
                    // $('#gasInputAQ').val(d.GasInputAQ);
                    $('#gasStandingCharge').val(d.GasStandingCharge);
                    $('#gasUnitRate').val(d.GasUnitRate);
                    $('#gasSupplier').val(d.GasSupplierId);
                    $('#gasProduct').val(d.GasProductId);
                    $('#gasCommsType').val(d.GasSupplierCommsType);
                    $('#gasPreSalesStatus').val(d.GasPreSalesStatus);

                    $('#contractChecked').prop('checked', d.ContractChecked);
                    $('#contractAudited').prop('checked', d.ContractAudited);
                    $('#terminated').prop('checked', d.Terminated);
                    $('#topLine').val(d.TopLine);
                    $('#electricInitialStartDate').val(d.ElectricInitialStartDate);
                    $('#gasInitialStartDate').val(d.GasInitialStartDate);
                    $('#gasInputEAC').val(d.GasInputEAC);
                    $('#gasOtherRate').val(d.GasOtherRate);
                    $('#contractNotes').val(d.ContractNotes);

                    $('#emProcessor').val(d.EMProcessor);

                    loadLogs(d.EId);
                }
                else {
                    showToastError(res.message);
                }
            },
            error: function () {
                showToastError("Unexpected error occurred.");
            },
            complete: function () {
                $btn.prop('disabled', false).text('Update Dual Contract');
            }
        });
    });

    function getDualModel() {
        return {
            EId: $('#eid').val(),
            Department: $('#department').val(),
            Agent: $('#agent').val(),
            Source: $('#source').val(),
            Introducer: $('#introducer').val(),
            SubIntroducer: $('#subIntroducer').val(),
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
            ElectricInputDate: $('#inputDateElectric').val(),
            GasInputDate: $('#inputDateGas').val(),
            ElectricInitialStartDate: $('#electricInitialStartDate').val(),
            GasInitialStartDate: $('#gasInitialStartDate').val(),
            ElectricDuration: $('#electricDuration').val(),
            GasDuration: $('#gasDuration').val(),
            ElectricUplift: $('#electricUplift').val(),
            GasUplift: $('#gasUplift').val(),
            ElectricInputEAC: $('#electricInputEAC').val(),
            GasInputEAC: $('#gasInputEAC').val(),
            DayRate: $('#dayRate').val(),
            NightRate: $('#nightRate').val(),
            EveWeekendRate: $('#eveWeekendRate').val(),
            ElectricOtherRate: $('#electricOtherRate').val(),
            GasUnitRate: $('#gasUnitRate').val(),
            GasOtherRate: $('#gasOtherRate').val(),
            ElectricStandingCharge: $('#electricStandingCharge').val(),
            GasStandingCharge: $('#gasStandingCharge').val(),
            SortCode: $('#sortCode').val(),
            AccountNumber: $('#accountNumber').val(),
            MPAN: $('#mpan').val(),
            MPRN: $('#mprn').val(),
            ElectricSupplierId: $('#electricSupplier').val(),
            ElectricProductId: $('#electricProduct').val(),
            GasSupplierId: $('#gasSupplier').val(),
            GasProductId: $('#gasProduct').val(),
            ElectricSalesType: $('#electricSalesType').val(),
            ElectricSalesTypeStatus: $('#electricSalesStatus').val(),
            ElectricSupplierCommsType: $('#electricCommsType').val(),
            ElectricPreSalesStatus: $('#electricPreSalesStatus').val(),
            ElectricCurrentSupplier: $('#electricCurrentSupplier').val(),
            GasCurrentSupplier: $('#gasCurrentSupplier').val(),
            ElectricDayRate: $('#dayRate').val(),
            ElectricNightRate: $('#nightRate').val(),
            ElectricEveWeekendRate: $('#eveWeekendRate').val(),
            TopLine: $('#topLine').val(),
            GasSalesType: $('#gasSalesType').val(),
            GasSalesTypeStatus: $('#gasSalesStatus').val(),
            GasSupplierCommsType: $('#gasCommsType').val(),
            GasPreSalesStatus: $('#gasPreSalesStatus').val(),
            ContractChecked: $('#contractChecked').is(':checked'),
            ContractAudited: $('#contractAudited').is(':checked'),
            Terminated: $('#terminated').is(':checked'),
            EMProcessor: $('#emProcessor').val(),
            ContractNotes: $('#contractNotes').val()
        };
    }
    function loadLogs(eid) {

        const $logContainer = $('#dualLogsContainer');
        $logContainer.html('<div class="text-muted">Loading logs...</div>');

        $.get(`/Dual/GetLogsForDualContract?eid=${eid}`, function (res) {
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

    const eid = $('#eid').val();
    if (eid) loadLogs(eid);

    $('#mpan').on('input', function () {
        const val = $(this).val().trim();
        if (/^\d{13}$/.test(val)) {
            $('#mpanLoader').show();
            $.get(`/Electric/CheckDuplicateMpan?mpan=${val}`, function (res) {
                $('#mpanLoader').hide();
                if (res.success && res.Data) {
                    const d = res.Data;
                    $('#duplicateDualMpanModalEdit tbody').html(`<tr><td>${d.Agent}</td><td>${d.BusinessName}</td><td>${d.CustomerName}</td><td>${d.InputDate}</td><td>${d.PreSalesStatus}</td><td>${d.Duration}</td></tr>`);
                    $('#duplicateDualMpanModalEdit').modal('show');
                }
            }).fail(() => $('#mpanLoader').hide());
        }
    });

    $('#mprn').on('input', function () {
        const val = $(this).val().trim();
        if (/^\d{6,10}$/.test(val)) {
            $('#mprnLoader').show();
            $.get(`/Gas/CheckDuplicateMprn?mprn=${val}`, function (res) {
                $('#mprnLoader').hide();
                if (res.success && res.Data) {
                    const d = res.Data;
                    $('#duplicateDualMprnModalEdit tbody').html(`<tr><td>${d.Agent}</td><td>${d.BusinessName}</td><td>${d.CustomerName}</td><td>${d.InputDate}</td><td>${d.PreSalesStatus}</td><td>${d.Duration}</td></tr>`);
                    $('#duplicateDualMprnModalEdit').modal('show');
                }
            }).fail(() => $('#mprnLoader').hide());
        }
    });

    $('#accountNumber').on('input', function () {
        const acc = $(this).val().trim();
        if (/^\d{8}$/.test(acc)) {
            $('#accountLoader').show();
            $.get(`/CheckDuplicateAccount/CheckDuplicateAccountUnified?account=${acc}`, function (res) {
                $('#accountLoader').hide();
                if (res.success && res.Data?.length > 0) {
                    const tbody = $('#duplicateAccountModalDualEdit tbody');
                    tbody.empty();
                    res.Data.forEach(r => {
                        tbody.append(`<tr><td>${r.Agent}</td><td>${r.BusinessName}</td><td>${r.CustomerName}</td><td>${r.InputDate}</td><td>${r.PreSalesStatus}</td><td>${r.Duration}</td><td>${r.SortCode}</td><td>${r.AccountNumber}</td></tr>`);
                    });
                    $('#duplicateAccountModalDualEdit').modal('show');
                }
            }).fail(() => $('#accountLoader').hide());
        }
    });

    $('#electricUplift').on('blur', async function () {
        await validateUpliftAgainstSupplierLimitElectric($('#electricUplift'), $('#electricSupplier'), $('#eid').val());
    });

    $('#gasUplift').on('blur', async function () {
        await validateUpliftAgainstSupplierLimitGas($('#gasUplift'), $('#gasSupplier'), $('#eid').val());
    });

});
