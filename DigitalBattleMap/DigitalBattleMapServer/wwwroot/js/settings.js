$(document).ready(function() {
    $("[charactersInput]").tagify({
        originalInputValueFormat: valuesArr => valuesArr.map(item => item.value).join(','),
        pattern: /^[a-zA-Z]*$/
    });
});