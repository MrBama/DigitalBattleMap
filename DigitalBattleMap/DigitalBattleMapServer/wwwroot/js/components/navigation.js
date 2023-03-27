$(document).ready(function() {
    $(".btn-direction").click(function() {
        let character = $("#character").val();
        let direction = $(this).attr('direction');
        $.ajax({
            url: "Navigation/Move",
            type: "POST",
            data: { 'character': character, 'direction': direction }
        })
    })
});