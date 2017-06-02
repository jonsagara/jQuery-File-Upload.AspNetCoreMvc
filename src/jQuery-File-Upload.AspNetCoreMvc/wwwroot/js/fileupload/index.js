// Write your Javascript code.


var $form = null;
$(function () {

    $form = $('#fileupload').fileupload({
        dataType: 'json'
    });

});

//$('#fileupload').addClass('fileupload-processing');