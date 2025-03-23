// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
$(document).ready(function () {

    $('#toast').toast({
        delay: 2500
    }).toast('show');

    $('#toast .close').click(function () {
        $('#toast').toast('hide');
    });
});

const MAX_SIZE = 20 * 1024 * 1024;

document.querySelector('form').addEventListener('submit', function (e) {
    const fileInput = document.getElementById('fileInput');
    const file = fileInput.files[0];

    if (file && file.size > MAX_SIZE) {
        alert('O arquivo é muito grande! O tamanho máximo permitido é 5MB.');
        e.preventDefault();
    }
});