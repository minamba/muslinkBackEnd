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
    [Route("professions")]
    public class ProfessionController : Controller
    {
        private readonly IProfessionViewModelBuilder _builder;

        public ProfessionController(IProfessionViewModelBuilder builder)
        {
            _builder = builder ?? throw new ArgumentNullException(nameof(builder));
        }

        [HttpGet]
        [AllowAnonymous]
        [SwaggerResponse(StatusCodes.Status200OK, "Métiers (filtrables par domaine)", typeof(IEnumerable<ProfessionViewModel>))]
        public async Task<IActionResult> GetProfessions([FromQuery] int? domainId) => Ok(await _builder.GetProfessionsAsync(domainId));

        [HttpGet("{id:int}")]
        [AllowAnonymous]
        [SwaggerResponse(StatusCodes.Status200OK, "Métier", typeof(ProfessionViewModel))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Métier introuvable")]
        public async Task<IActionResult> GetProfession(int id)
        {
            var result = await _builder.GetProfessionByIdAsync(id);
            return result == null ? NotFound() : Ok(result);
        }

        [HttpPost]
        [Authorize(Policy = Permissions.Referentials.Create)]
        [SwaggerResponse(StatusCodes.Status200OK, "Métier créé", typeof(ProfessionViewModel))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Domaine inexistant")]
        [SwaggerResponse(StatusCodes.Status409Conflict, "Métier déjà existant dans ce domaine")]
        public async Task<IActionResult> AddProfession([FromBody] ProfessionRequest model) => Ok(await _builder.AddProfessionAsync(model));

        [HttpPut]
        [Authorize(Policy = Permissions.Referentials.Edit)]
        [SwaggerResponse(StatusCodes.Status200OK, "Métier modifié", typeof(ProfessionViewModel))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Métier introuvable")]
        public async Task<IActionResult> UpdateProfession([FromBody] ProfessionRequest model)
        {
            var result = await _builder.UpdateProfessionAsync(model);
            return result == null ? NotFound() : Ok(result);
        }

        [HttpDelete("{id:int}")]
        [Authorize(Policy = Permissions.Referentials.Delete)]
        [SwaggerResponse(StatusCodes.Status200OK, "Métier supprimé")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Métier introuvable")]
        [SwaggerResponse(StatusCodes.Status409Conflict, "Métier utilisé par des offres")]
        public async Task<IActionResult> DeleteProfession(int id) => await _builder.DeleteProfessionAsync(id) ? Ok() : NotFound();
    }
}
