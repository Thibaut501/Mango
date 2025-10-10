// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

// Global site script
(function(){
  try {
    if (typeof toastr !== 'undefined') {
      toastr.options = toastr.options || {};
      toastr.options.closeButton = true;
      toastr.options.progressBar = true;
      toastr.options.timeOut = 3000;
      toastr.options.extendedTimeOut = 1500;
      toastr.options.positionClass = 'toast-top-right';
      toastr.options.newestOnTop = true;
    }
    var successEl = document.querySelector('[data-temp-success]');
    var errorEl = document.querySelector('[data-temp-error]');
    if (typeof toastr !== 'undefined') {
      if (successEl) {
        var msg = successEl.getAttribute('data-temp-success');
        if (msg) toastr.success(msg);
      }
      if (errorEl) {
        var emsg = errorEl.getAttribute('data-temp-error');
        if (emsg) toastr.error(emsg);
      }
    }
  } catch (e) { /* no-op */ }
})();
