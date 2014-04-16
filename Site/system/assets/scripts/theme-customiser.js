(function(jQuery){

	var $variable = $('#less-variable');
	var $value = $('#less-value');
	var vars = {};

	$('#update-theme').click(function(){

		vars[$variable.val()] = $value.val();

		less.modifyVars(vars);

		$('#less-css').html(
			$('#less\\:assets-less-main').html()
		);

	});

})($);