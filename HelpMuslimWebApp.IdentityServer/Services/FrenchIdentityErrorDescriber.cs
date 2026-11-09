using Microsoft.AspNetCore.Identity;

namespace HelpMuslimWebApp.IdentityServer.Services
{
    public class FrenchIdentityErrorDescriber : IdentityErrorDescriber
    {
        public override IdentityError DefaultError() => new() { Code = nameof(DefaultError), Description = "Une erreur inconnue est survenue." };
        public override IdentityError DuplicateEmail(string email) => new() { Code = nameof(DuplicateEmail), Description = $"L'adresse email '{email}' est déjà utilisée." };
        public override IdentityError DuplicateUserName(string userName) => new() { Code = nameof(DuplicateUserName), Description = $"L'identifiant '{userName}' est déjà utilisé." };
        public override IdentityError InvalidEmail(string? email) => new() { Code = nameof(InvalidEmail), Description = $"L'adresse email '{email}' est invalide." };
        public override IdentityError InvalidUserName(string? userName) => new() { Code = nameof(InvalidUserName), Description = $"L'identifiant '{userName}' est invalide." };
        public override IdentityError DuplicateRoleName(string role) => new() { Code = nameof(DuplicateRoleName), Description = $"Le rôle '{role}' existe déjà." };
        public override IdentityError InvalidRoleName(string? role) => new() { Code = nameof(InvalidRoleName), Description = $"Le nom de rôle '{role}' est invalide." };
        public override IdentityError PasswordMismatch() => new() { Code = nameof(PasswordMismatch), Description = "Mot de passe incorrect." };
        public override IdentityError PasswordTooShort(int length) => new() { Code = nameof(PasswordTooShort), Description = $"Le mot de passe doit contenir au moins {length} caractères." };
        public override IdentityError PasswordRequiresNonAlphanumeric() => new() { Code = nameof(PasswordRequiresNonAlphanumeric), Description = "Le mot de passe doit contenir au moins un caractère spécial." };
        public override IdentityError PasswordRequiresDigit() => new() { Code = nameof(PasswordRequiresDigit), Description = "Le mot de passe doit contenir au moins un chiffre." };
        public override IdentityError PasswordRequiresLower() => new() { Code = nameof(PasswordRequiresLower), Description = "Le mot de passe doit contenir au moins une minuscule." };
        public override IdentityError PasswordRequiresUpper() => new() { Code = nameof(PasswordRequiresUpper), Description = "Le mot de passe doit contenir au moins une majuscule." };
        public override IdentityError PasswordRequiresUniqueChars(int uniqueChars) => new() { Code = nameof(PasswordRequiresUniqueChars), Description = $"Le mot de passe doit contenir au moins {uniqueChars} caractères différents." };
        public override IdentityError UserAlreadyHasPassword() => new() { Code = nameof(UserAlreadyHasPassword), Description = "Cet utilisateur a déjà un mot de passe." };
        public override IdentityError UserAlreadyInRole(string role) => new() { Code = nameof(UserAlreadyInRole), Description = $"L'utilisateur a déjà le rôle '{role}'." };
        public override IdentityError UserNotInRole(string role) => new() { Code = nameof(UserNotInRole), Description = $"L'utilisateur n'a pas le rôle '{role}'." };
        public override IdentityError LoginAlreadyAssociated() => new() { Code = nameof(LoginAlreadyAssociated), Description = "Ce compte externe est déjà associé à un utilisateur." };
        public override IdentityError InvalidToken() => new() { Code = nameof(InvalidToken), Description = "Jeton invalide." };
        public override IdentityError ConcurrencyFailure() => new() { Code = nameof(ConcurrencyFailure), Description = "Les données ont été modifiées entre-temps, veuillez réessayer." };
    }
}
