using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace HelpMuslimWebApp.Api.Utils
{
    // Traduit les exceptions métier des builders en réponses HTTP
    //   ArgumentException          → 400 (donnée invalide)
    //   UnauthorizedAccessException → 403 (pas propriétaire / pas la permission)
    //   KeyNotFoundException       → 404
    //   InvalidOperationException  → 409 (règle métier : offre fermée, référentiel utilisé…)
    public class ApiExceptionFilter : IExceptionFilter
    {
        private readonly ILogger<ApiExceptionFilter> _logger;

        public ApiExceptionFilter(ILogger<ApiExceptionFilter> logger)
        {
            _logger = logger;
        }

        public void OnException(ExceptionContext context)
        {
            var (status, message) = context.Exception switch
            {
                ArgumentException e => (StatusCodes.Status400BadRequest, e.Message),
                UnauthorizedAccessException => (StatusCodes.Status403Forbidden, "Vous n'avez pas les droits pour cette action."),
                KeyNotFoundException e => (StatusCodes.Status404NotFound, e.Message),
                InvalidOperationException e => (StatusCodes.Status409Conflict, e.Message),
                _ => (0, string.Empty),
            };

            if (status == 0)
            {
                _logger.LogError(context.Exception, "Erreur non gérée sur {Path}", context.HttpContext.Request.Path);
                return;
            }

            context.Result = new ObjectResult(new { message }) { StatusCode = status };
            context.ExceptionHandled = true;
        }
    }
}
