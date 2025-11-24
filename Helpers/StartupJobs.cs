namespace Pidar.Helpers;

using Microsoft.EntityFrameworkCore;
using Pidar.Data;

public class StartupJobs
{
    public static async Task RunAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PidarDbContext>();

        // Ensure DB connection is valid
        if (!await db.Database.CanConnectAsync())
            return;

        // Load all datasets ordered by DatasetId (stable order)
        var all = await db.Datasets
            .OrderBy(x => x.DatasetId)
            .ToListAsync();

        if (all.Count == 0)
            return;

        int next = 1;
        bool changed = false;

        foreach (var ds in all)
        {
            if (ds.DisplayId != next)
            {
                ds.DisplayId = next;
                changed = true;
            }
            next++;
        }

        // Save only if any row was modified
        if (changed)
            await db.SaveChangesAsync();
    }
}
