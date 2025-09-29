$(document).ready(function () {
    let table;
    let allFollowUpDates = [];
    let selectedDate = "";
    let allFollowUpDatesTyped = [];

    table = $('#preSalesCalendarTable').DataTable({
        responsive: true,
        processing: true,
        serverSide: true,
        paging: true,
        ordering: true,
        searching: true,
        order: [],
        autoWidth: false,
        ajax: {
            url: '/PreSalesCalendar/GetPreSalesCalendarContracts',
            type: 'POST',
            data: function (d) {
                d.SelectedDate = selectedDate;
            }
        },
        language: {
            emptyTable: "No data available in table",
            zeroRecords: "No matching pre-sales calendar entries found"
        },
        columns: [
            {
                data: 'EId',
                orderable: false,
                searchable: false,
                render: function (data, type, row) {
                    if (type === 'display') {
                        return `<a class="btn btn-sm edit-btn edit-contract-btn" target="_blank" title="Edit" data-eid="${data}" data-type="${row.Type}" ><i class='fas fa-pencil-alt me-1'></i>Edit</a>`;
                    }
                    return data;
                }
            },
            { data: 'Agent' },
            { data: 'MPAN' },
            { data: 'MPRN' },
            { data: 'PostCode' },
            { data: 'BusinessName' },
            { data: 'Supplier' }
        ]
    });

    loadFollowUpDatesAndInitCalendar();

    // Event handler for edit buttons
    $(document).on('click', '.edit-contract-btn', function() {
        const eId = $(this).data('eid');
        const contractType = $(this).data('type');
        openEditContractNotesPopup(eId, contractType);
    });

    // Event handler for form submission
    $(document).on('submit', '#editContractNotesForm', function(e) {
        e.preventDefault();
        updateContractNotes();
    });

    function openEditContractNotesPopup(eId, contractType) {
        // First, try to lock the contract
        $.ajax({
            url: '/PreSales/LockContract',
            type: 'POST',
            data: { eid: eId },
            success: function(lockResponse) {
                if (lockResponse.success) {
                    // Lock acquired, now load the popup content
                    loadContractNotesPopup(eId, contractType);
                } else {
                    // Lock failed, show error message
                    showToastError(lockResponse.message || 'Failed to lock contract');
                }
            },
            error: function() {
                showToastError('Error occurred while locking contract.');
            }
        });
    }

    function loadContractNotesPopup(eId, contractType) {
        $.ajax({
            url: '/PreSalesCalendar/GetContractNotes',
            type: 'POST',
            data: {
                eId: eId,
                contractType: contractType,
                __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val()
            },
            success: function(res) {
                if (res.success) {
                    // Populate the modal with data
                    $('#eid').val(res.Data.EId);
                    $('#contractType').val(res.Data.ContractType);
                    $('#contractNotes').val(res.Data.ContractNotes);
                    $('#preSalesFollowUpDate').val(res.Data.PreSalesFollowUpDate);
                    
                    if (res.Data.IsDualContract) {
                        $('.dual-contract-message').removeClass('d-none');
                    } else {
                        $('.dual-contract-message').addClass('d-none');
                    }
                
                    // Show the modal
                    $('#editContractNotesModal').modal('show');
                } else {
                    // Failed to load data, unlock the contract
                    unlockContract(eId);
                    showToastError(res.message || 'Failed to load contract data.');
                }
            },
            error: function() {
                // Failed to load data, unlock the contract
                unlockContract(eId);
                showToastError('Failed to load contract data.');
            }
        });
    }

    function updateContractNotes() {
        const formData = {
            EId: $('#eid').val(),
            ContractType: $('#contractType').val(),
            ContractNotes: $('#contractNotes').val(),
            PreSalesFollowUpDate: $('#preSalesFollowUpDate').val(),
            __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val()
        };

        $.ajax({
            url: '/PreSalesCalendar/UpdateContractNotes',
            type: 'POST',
            data: formData,
            success: function(res) {
                if (res.success) {
                    showToastSuccess('Contract notes and follow-up date updated successfully.');
                    $('#editContractNotesModal').modal('hide');
                    // Refresh the calendar to reflect the new follow-up date
                    refreshCalendar();
                    table.ajax.reload(); // Refresh the table
                } else {
                    showToastError(res.message || 'Failed to update contract notes.');
                }
            },
            error: function() {
                showToastError('Failed to update contract notes.');
            }
        });
    }

    function loadFollowUpDatesAndInitCalendar() {
        $.ajax({
            url: '/PreSalesCalendar/GetPreSalesFollowUpDates',
            type: 'POST',
            data: {
                __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val()
            },
            success: function (res) {
                if (res.success && Array.isArray(res.Data)) {
                    allFollowUpDates = res.Data.filter(d => /^\d{4}-\d{2}-\d{2}$/.test(d));
                    if (allFollowUpDates.length === 0) {
                        const today = moment().format("YYYY-MM-DD");
                        allFollowUpDates = [today];
                    }
                    initFlatpickr();
                } else {
                    showToastError("No pre-sales follow-up dates found.");
                }
            },
            error: function () {
                showToastError("Failed to load pre-sales follow-up dates.");
            }
        });
    }

    function initFlatpickr() {
        flatpickr("#calendarFlatContainer", {
            inline: true,
            dateFormat: "Y-m-d",
            defaultDate: null,
            enable: allFollowUpDates,
            disableMobile: true,
            locale: {
                firstDayOfWeek: 1
            },
            onChange: function (selectedDates, dateStr) {
                selectedDate = dateStr;
                table.ajax.reload();
            },
            onDayCreate: function (_, __, ___, dayElem) {
                const day = dayElem.dateObj?.getDate();
                const month = (dayElem.dateObj?.getMonth() + 1).toString().padStart(2, '0');
                const year = dayElem.dateObj?.getFullYear();
                const formattedDate = `${year}-${month}-${day.toString().padStart(2, '0')}`;

                if (allFollowUpDates.includes(formattedDate)) {
                    dayElem.classList.add("has-followup");
                }
            }
        });
    }

    function refreshCalendar() {
        // Reload the follow-up dates and reinitialize the calendar
        loadFollowUpDatesAndInitCalendar();
    }

    // Unlock contract function
    function unlockContract(eId) {
        if (!eId) return;
        
        $.ajax({
            url: '/PreSales/UnlockContract',
            type: 'POST',
            data: { eid: eId },
            success: function(response) {
                console.log('Contract unlocked:', response.message);
            },
            error: function() {
                console.log('Failed to unlock contract');
            }
        });
    }

    // Unlock contract when modal is closed
    $(document).on('hidden.bs.modal', '#editContractNotesModal', function() {
        const eId = $('#eid').val();
        if (eId) {
            unlockContract(eId);
        }
    });
});
