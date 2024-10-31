using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace PikaNoteAPI.Infrastructure.Services.Security;

public interface ISecurityService
{
    public Task ConfigureAccessToken(string token, IEnumerable<Claim> claims);
    public Task<Dictionary<Guid, bool>?> HasNotesAccess(string token, Dictionary<Guid, Guid> guids);
}