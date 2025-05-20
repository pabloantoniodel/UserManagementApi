// Controllers/CompaniesController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UserManagementApi.Data;
using UserManagementApi.Models;
// using Microsoft.AspNetCore.Authorization; // Si implementas JWT y roles

// [Authorize(Roles = "Administrador")] // Proteger este controlador para Admins
[Route("api/[controller]")]
[ApiController]
public class CompaniesController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CompaniesController> _logger;

    public CompaniesController(ApplicationDbContext context, ILogger<CompaniesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: api/Companies
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Company>>> GetCompanies()
    {
        return await _context.Companies.OrderBy(c => c.Name).ToListAsync();
    }

    // GET: api/Companies/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Company>> GetCompany(int id)
    {
        var company = await _context.Companies.FindAsync(id);
        if (company == null)
        {
            return NotFound(new { message = $"Empresa con ID {id} no encontrada." });
        }
        return company;
    }

    // POST: api/Companies
    [HttpPost]
    public async Task<ActionResult<Company>> CreateCompany(Company company) // Podrías crear un CompanyCreateDto
    {
        
        if (await _context.Companies.AnyAsync(c => c.Name == company.Name))
        {
             // Podrías permitir nombres duplicados si el código es diferente
            return Conflict(new { message = $"El nombre de empresa '{company.Name}' ya existe." });
        }

        company.CreatedAt = DateTime.UtcNow;
        _context.Companies.Add(company);
        await _context.SaveChangesAsync();
        _logger.LogInformation($"Empresa '{company.Name}' creada con ID {company.Id}");

        return CreatedAtAction(nameof(GetCompany), new { id = company.Id }, company);
    }

    // PUT: api/Companies/5
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCompany(int id, Company company) // Podrías crear CompanyUpdateDto
    {
        if (id != company.Id)
        {
            return BadRequest(new { message = "El ID de la ruta no coincide con el ID del cuerpo." });
        }

        
         // if (await _context.Companies.AnyAsync(c => c.Name == company.Name && c.Id != id)) { ... }


        _context.Entry(company).State = EntityState.Modified;
        // Asegurarse de que CreatedAt no se modifique
        _context.Entry(company).Property(x => x.CreatedAt).IsModified = false;


        try
        {
            await _context.SaveChangesAsync();
            _logger.LogInformation($"Empresa con ID {id} actualizada.");
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!CompanyExists(id))
            {
                return NotFound();
            }
            else { throw; }
        }
        return NoContent();
    }

    // DELETE: api/Companies/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCompany(int id)
    {
        var company = await _context.Companies.Include(c => c.Users).FirstOrDefaultAsync(c => c.Id == id);
        if (company == null)
        {
            return NotFound(new { message = $"Empresa con ID {id} no encontrada." });
        }

        // Lógica de borrado: OnDelete(DeleteBehavior.SetNull) en User->Company
        // ya manejará poner CompanyId a null en los usuarios asociados.
        // Si usaste Restrict, aquí deberías verificar si company.Users.Any() y prohibir el borrado.

        _context.Companies.Remove(company);
        await _context.SaveChangesAsync();
        _logger.LogInformation($"Empresa con ID {id} eliminada.");

        return NoContent();
    }

    private bool CompanyExists(int id)
    {
        return _context.Companies.Any(e => e.Id == id);
    }
}