using Data;
using Microsoft.EntityFrameworkCore;

namespace Gateway.Api.Services;

public class ProjectAccessService : IProjectAccessService
{
    private readonly ApplicationDbContext _db;

    public ProjectAccessService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<bool> CanAccessProjectAsync(int currentUserId, int projectId, CancellationToken cancellationToken = default)
    {
        // Versión inicial: acceso basado en propietario legacy (UserId).
        var project = await _db.Projects
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);

        if (project == null)
        {
            return false;
        }

        return project.UserId == currentUserId;
    }
}

