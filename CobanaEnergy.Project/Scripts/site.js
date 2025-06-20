// site.js
// Automatically include anti-forgery token in all Ajax requests
$(document).ready(function () {
    var token = $('input[name="__RequestVerificationToken"]').val();
    if (token) {
        $.ajaxSetup({
            headers: {
                'RequestVerificationToken': token
            }
        });
    }
});
