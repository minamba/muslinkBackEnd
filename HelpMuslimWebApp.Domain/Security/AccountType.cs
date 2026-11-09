namespace HelpMuslimWebApp.Domain.Security
{
    // Type de compte, transmis dans le token via le claim "account_type".
    public enum AccountType
    {
        Candidat,
        Recruteur,
        Administration,
    }

    public static class AccountTypeClaim
    {
        public const string ClaimType = "account_type";
    }
}
