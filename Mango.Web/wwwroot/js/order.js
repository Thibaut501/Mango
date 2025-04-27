var dataTable;

$(document).ready(function () {
    loadDataTable();
});

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
