// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
$(document).ready(function () {
    // Exibe o toast

    //$('#toast').toast('show');
    $('#toast').toast({
        delay: 2500
    }).toast('show');

    // Fecha o toast quando o botão de fechar for clicado
    $('#toast .close').click(function () {
        $('#toast').toast('hide');
    });
});

