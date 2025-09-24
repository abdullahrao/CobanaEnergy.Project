window.FilterModule = (function () {

    function init(tableInstance) {
        setupDepartmentCascade(tableInstance);
    }

    function setupDepartmentCascade(table) {
        // hide all initially
        $("#brokerage-area, #brokerage-staff-area, #subbrokerage-area, #inhouse-closer-area, #leadgen-area, #referral-area, #introducer-area, #subintroducer-area").hide();

        $('#department').on('change', function () {
            const val = $(this).val();
            $("#brokerage-area, #brokerage-staff-area, #subbrokerage-area, #inhouse-closer-area, #leadgen-area, #referral-area, #introducer-area, #subintroducer-area").hide();

            if (val === 'Brokers') {
                $('#brokerage-area').show();
                $.getJSON(`/Common/Sectors?sectorType=Brokerage`).done(data => fillDropdown("#brokerageFilter", data));
            }
            else if (val === 'In House') {
                $('#inhouse-closer-area').show();
                $.getJSON(`/Common/Closers`).done(data => fillDropdown("#closerFilter", data));
            }
            else if (val === 'Introducers') {
                $('#introducer-area').show();
                $.getJSON(`/Common/Introducers`).done(data => fillDropdown("#introducerFilter", data));
            }

            if (table) table.ajax.reload();
        });

        $('#brokerageFilter').on('change', function () {
            const id = $(this).val();
            $('#brokerageStaffFilter, #subBrokerageFilter').empty().append('<option value="">All</option>');
            if (id) {
                $('#brokerage-staff-area, #subbrokerage-area').show();
                $.getJSON(`/Common/BrokerageStaff?brokerageId=${id}`).done(data => fillDropdown("#brokerageStaffFilter", data));
                $.getJSON(`/Common/SubBrokerages?brokerageId=${id}`).done(data => fillDropdown("#subBrokerageFilter", data));
            } else {
                $('#brokerage-staff-area, #subbrokerage-area').hide();
            }
            if (table) table.ajax.reload();
        });

        $('#brokerageStaffFilter, #subBrokerageFilter, #supplierFilter, #contractstatus,#paymentStatusAcc, #startDateFilter, #endDateFilter').on('change', () => {
            if (table) table.ajax.reload();
        });

        $('#closerFilter').on('change', function () {
            const id = $(this).val();
            $('#leadGenFilter, #referralFilter').empty().append('<option value="">All</option>');
            if (id) {
                $('#leadgen-area, #referral-area').show();
                $.getJSON(`/Common/LeadGenerators?closerId=${id}`).done(data => fillDropdown("#leadGenFilter", data));
                $.getJSON(`/Common/ReferralPartners?closerId=${id}`).done(data => fillDropdown("#referralFilter", data));
            } else {
                $('#leadgen-area, #referral-area').hide();
            }
            if (table) table.ajax.reload();
        });

        $('#leadGenFilter, #referralFilter').on('change', () => {
            if (table) table.ajax.reload();
        });

        $('#introducerFilter').on('change', function () {
            const id = $(this).val();
            $('#subIntroducerFilter').empty().append('<option value="">All</option>');
            if (id) {
                $('#subintroducer-area').show();
                $.getJSON(`/Common/SubIntroducers?introducerId=${id}`).done(data => fillDropdown("#subIntroducerFilter", data));
            } else {
                $('#subintroducer-area').hide();
            }
            if (table) table.ajax.reload();
        });

        $('#subIntroducerFilter').on('change', () => {
            if (table) table.ajax.reload();
        });
    }

    function fillDropdown(selector, data) {
        var $ddl = $(selector);
        $ddl.empty().append('<option value="">All</option>');
        if (!data || !data.Data || !Array.isArray(data.Data)) {
            return;
        }
        $.each(data.Data, function (i, item) {
            $ddl.append('<option value="' + item.Id + '">' + item.Name + '</option>');
        });
    }

    return {
        init: init
    };

})();
