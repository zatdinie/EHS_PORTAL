function formatMyPhone(digits) {
    if (digits.startsWith('01')) {
        var part1 = digits.slice(0, 3);
        var part2 = digits.slice(3, 7);
        var part3 = digits.slice(7, 11);
    } else {
        var part1 = digits.slice(0, 2);
        var part2 = digits.slice(2, 6);
        var part3 = digits.slice(6, 10);
    }

    var result = part1;
    if (part2) result += '-' + part2;
    if (part3) result += ' ' + part3;
    return result;
}

function stripPhone (val) {
    return (val || '').replace(/\D/g,'');
}

function setPhone(inputId, val) {
    var digits = (val || '').replace(/\D/g, '');
    document.getElementById(inputId).value = formatMyPhone(digits);
}
