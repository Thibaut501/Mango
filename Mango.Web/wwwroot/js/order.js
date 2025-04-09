$(document).ready(function () {
    $('#tblData').DataTable({
        "ajax": {
            "url": "/Order/GetAll",
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
                data: 'orderHeaderId',
                "render": function (data) {
                    return `<div class="w-75 btn-group" role="group">
                    <a href="/order/orderDetail?orderId=${data}" class="btn btn-primary mx-2"><i class="bi bi-pencil-square"></i></a>
                    </div>`
                },
                "width": "10%"
            }
        ]
    });
});