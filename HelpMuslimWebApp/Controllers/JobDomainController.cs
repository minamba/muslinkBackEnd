using HelpMuslimWebApp.Api.Builders;
using HelpMuslimWebApp.Api.Request;
using HelpMuslimWebApp.Api.ViewModels;
using HelpMuslimWebApp.Domain.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace HelpMuslimWebApp.Api.Controllers
{
    [ApiController]
    [Route("domains")]
    public class JobDomainController : Controller
    {
        private readonly IJobDomainViewModelBuilder _builder;

        public JobDomainController(IJobDomainViewModelBuilder builder)
        {
            _builder = builder ?? throw new ArgumentNullException(nameof(builder));
        }

        [HttpGet]
        [AllowAnonymous]
        [SwaggerResponse(StatusCodes.Status200OK, "Domaines avec leurs métiers", typeof(IEnumerable<JobDomainViewModel>))]
        public async Task<IActionResult> GetJobDomains() => Ok(await _builder.GetJobDomainsAsync());

        [HttpGet("{id:int}")]
        [AllowAnonymous]
        [SwaggerResponse(StatusCodes.Status200OK, "Domaine", typeof(JobDomainViewModel))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Domaine introuvable")]
        public async Task<IActionResult> GetJobDomain(int id)
        {
            var result = await _builder.GetJobDomainByIdAsync(id);
            return result == null ? NotFound() : Ok(result);
        }

        [HttpPost]
        [Authorize(Policy = Permissions.Referentials.Create)]
        [SwaggerResponse(StatusCodes.Status200OK, "Domaine créé", typeof(JobDomainViewModel))]
        [SwaggerResponse(StatusCodes.Status409Conflict, "Nom déjà utilisé")]
        public async Task<IActionResult> AddJobDomain([FromBody] JobDomainRequest model) => Ok(await _builder.AddJobDomainAsync(model));

        [HttpPut]
        [Authorize(Policy = Permissions.Referentials.Edit)]
        [SwaggerResponse(StatusCodes.Status200OK, "Domaine modifié", typeof(JobDomainViewModel))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Domaine introuvable")]
        [SwaggerResponse(StatusCodes.Status409Conflict, "Nom déjà utilisé")]
        public async Task<IActionResult> UpdateJobDomain([FromBody] JobDomainRequest model)
        {
            var result = await _builder.UpdateJobDomainAsync(model);
            return result == null ? NotFound() : Ok(result);
        }

        [HttpDelete("{id:int}")]
        [Authorize(Policy = Permissions.Referentials.Delete)]
        [SwaggerResponse(StatusCodes.Status200OK, "Domaine supprimé")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Domaine introuvable")]
        [SwaggerResponse(StatusCodes.Status409Conflict, "Domaine utilisé par des offres")]
        public async Task<IActionResult> DeleteJobDomain(int id) => await _builder.DeleteJobDomainAsync(id) ? Ok() : NotFound();
    }
}
