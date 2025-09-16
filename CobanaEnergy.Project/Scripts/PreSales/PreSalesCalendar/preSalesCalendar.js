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
                render: function (data, type, row) {
                    if (type === 'display') {
                        let editUrl = '';
                        if (row.Type === 'Electric') {
                            editUrl = `/Electric/ElectricEdit?eid=${data}`;
                        } else if (row.Type === 'Gas') {
                            editUrl = `/Gas/EditGas?eid=${data}`;
                        }
                        return `<a href="${editUrl}" class="btn btn-sm btn-primary">Edit</a>`;
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
});
