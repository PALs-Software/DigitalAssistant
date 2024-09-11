function SelectSlideByNumber(id, slideNumber) {
    $("#" + id).carousel(slideNumber);
}

function SelectNextSlide(id) {
    $("#" + id).carousel("next");
}

function SelectPreviousSlide(id) {
    $("#" + id).carousel("prev");
}