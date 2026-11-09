// Œil des champs mot de passe : bascule entre saisie masquée et visible
document.addEventListener('click', function (event) {
  var button = event.target.closest('[data-password-toggle]');
  if (!button) return;

  // Le bouton est dans un <label> : on évite que le clic soit renvoyé vers le champ
  event.preventDefault();

  var input = button.parentElement.querySelector('input');
  var visible = input.type === 'password';
  input.type = visible ? 'text' : 'password';
  button.setAttribute('aria-pressed', String(visible));
  button.setAttribute('aria-label', visible ? 'Masquer le mot de passe' : 'Afficher le mot de passe');

  // Le curseur reste en fin de saisie
  input.focus();
  var length = input.value.length;
  try { input.setSelectionRange(length, length); } catch (e) { /* type sans sélection */ }
});
