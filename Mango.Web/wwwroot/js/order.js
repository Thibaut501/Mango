var dataTable;

$(document).ready(function () {
    ensureDataTables(function () {
        if ($('#tblData').length) {
            loadDataTable();
        }
    });
});

function ensureDataTables(callback) {
    try {
        if ($ && $.fn && $.fn.DataTable) {
            return callback && callback();
        }
    } catch (_) { }

    // Try to load DataTables JS dynamically (fallback if CDN/script was blocked)
    var head = document.getElementsByTagName('head')[0] || document.documentElement;

    // Ensure CSS too (idempotent)
    var cssId = 'dt-css-1134';
    if (!document.getElementById(cssId)) {
        var link = document.createElement('link');
        link.id = cssId;
        link.rel = 'stylesheet';
        link.href = 'https://cdn.datatables.net/1.13.4/css/jquery.dataTables.min.css';
        head.appendChild(link);
    }

    var script = document.createElement('script');
    script.src = 'https://cdn.datatables.net/1.13.4/js/jquery.dataTables.min.js';
    script.async = true;
    script.onload = function () { try { callback && callback(); } catch (e) { /* no-op */ } };
    script.onerror = function () { if (window.toastr) toastr.error('Impossible de charger DataTables.'); };
    head.appendChild(script);
}

function loadDataTable() {
    var url = window.location.search;
    if (url.includes("approved")) {
        url = "/Order/GetAll?status=approved";
    }
    else if (url.includes("readyforpickup")) {
        url = "/Order/GetAll?status=readyforpickup";
    }
    else if (url.includes("cancelled")) {
        url = "/Order/GetAll?status=cancelled";
    }
    else {
        url = "/Order/GetAll?status=all";
    }

    if (!$.fn || !$.fn.DataTable) {
        if (window.toastr) toastr.error('DataTables n\'est pas disponible.');
        return;
    }

    dataTable = $('#tblData').DataTable({
        "ajax": {
            "url": url,
            "type": "GET",
            "datatype": "json"
        },
        "columns": [
            { "data": "orderHeaderId", "width": "10%" },
            { "data": "email", "width": "20%" },
            { "data": "name", "width": "15%" },
            { "data": "phone", "width": "15%" },
            { "data": "status", "width": "10%" },
            {
                "data": "orderTotal",
                "render": function (data) {
                    return 'Rs ' + parseFloat(data).toFixed(2);
                },
                "width": "10%"
            },
            {
                "data": "orderHeaderId",
                "render": function (data) {
                    return `
                        <div class="text-center">
                            <a href="/Order/OrderDetail?orderId=${data}" class="btn btn-success btn-sm text-white">
                                <i class="bi bi-pencil-square"></i> View
                            </a>
                        </div>
                    `;
                },
                "width": "10%"
            }
        ]
    });
}
