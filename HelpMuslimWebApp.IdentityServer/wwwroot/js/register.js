// Inscription : critères du mot de passe cochés en direct et contrôle de la confirmation
(function () {
  var form = document.querySelector('[data-register-form]');
  if (!form) return;

  var password = form.querySelector('[data-password-rules-input]');
  var confirm = form.querySelector('[data-password-confirm]');
  var rules = form.querySelector('[data-password-rules]');
  var match = form.querySelector('[data-password-match]');

  // Mêmes règles que l'IdentityServer (Identity : 8 caractères, majuscule, minuscule, chiffre, caractère spécial)
  var tests = {
    length: function (v) { return v.length >= 8; },
    upper: function (v) { return /[A-Z]/.test(v); },
    lower: function (v) { return /[a-z]/.test(v); },
    digit: function (v) { return /[0-9]/.test(v); },
    special: function (v) { return /[^A-Za-z0-9]/.test(v); }
  };

  function update() {
    var value = password.value;
    rules.querySelectorAll('[data-rule]').forEach(function (item) {
      item.classList.toggle('is-ok', tests[item.dataset.rule](value));
    });

    var same = confirm.value.length > 0 && confirm.value === value;
    match.hidden = !same;
    confirm.closest('.input').classList.toggle('is-ok', same);
  }

  password.addEventListener('input', update);
  confirm.addEventListener('input', update);
  update();
})();
