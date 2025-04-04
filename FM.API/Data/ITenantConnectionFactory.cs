using System.Data;
using System.Threading.Tasks;

namespace FM.API.Data
{
    /// <summary>
    /// Factory for creating database connections for specific tenants
    /// </summary>
    public interface ITenantConnectionFactory
    {
        /// <summary>
        /// Creates a database connection for the specified tenant
        /// </summary>
        /// <param name="tenantId">The ID of the tenant</param>
        /// <returns>An open database connection for the tenant</returns>
        Task<IDbConnection> CreateConnectionAsync(string tenantId);
    }
}