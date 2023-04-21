$(document).ready(function() {
    $(".btn-direction").click(function() {
        let character = $("#character").val();
        let direction = $(this).attr('direction');

        // If direction is undefined, we hit te center button
        if(!direction)
            return;
        
        $.ajax({
            url: "Navigation/Move",
            type: "POST",
            data: { 'character': character, 'direction': direction }
        })
    })

    $(".btn-condition").click(function () {
        let character = $("#character").val();
        let condition = $(this).attr('condition');

        $.ajax({
            url: "Navigation/ToggleCondition",
            type: "POST",
            data: { 'character': character, 'condition': condition}
        })
    })
});