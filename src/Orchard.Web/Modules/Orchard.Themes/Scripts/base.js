﻿jQuery.fn.extend({
    helpfullyFocus: function() {
        var _this = $(this);
        var firstError = _this.find(".input-validation-error").first();
        return firstError.size() === 1
            ? firstError.focus()
            : _this.find("input:text").first().focus();
    },
    toggleWhatYouControl: function() {
        var _this = $(this);
        var _controllees = $("[data-controllerid=" + _this.attr("id") + "]");
        var _controlleesAreHidden = _controllees.is(":hidden");
        if (_this.is(":checked") && _controlleesAreHidden) {
            _controllees.hide(); // <- unhook this when the following comment applies
            $(_controllees.show()[0]).find("input").focus(); // <- aaaand a slideDown there...eventually
        } else if (!(_this.is(":checked") && _controlleesAreHidden)) {
            //_controllees.slideUp(200); <- hook this back up when chrome behaves, or when I care less
            _controllees.hide()
        }
    }
});
(function() {
    $("[data-controllerid]").each(function() {
        var controller = $("#" + $(this).attr("data-controllerid"));
        if (controller.data("isControlling")) {
            return;
        }
        controller.data("isControlling", 1);
        if (!controller.is(":checked")) {
            $("[data-controllerid=" + controller.attr("id") + "]").hide();
        }
        if (controller.is(":checkbox")) {
            controller.click($(this).toggleWhatYouControl);
        } else if (controller.is(":radio")) {
            $("[name=" + controller.attr("name") + "]").click(function() { $("[name=" + $(this).attr("name") + "]").each($(this).toggleWhatYouControl); });
        }
    });
})();