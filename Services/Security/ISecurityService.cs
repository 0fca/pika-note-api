using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PikaNoteAPI.Services.Security;

public interface ISecurityService
{
    public Task VerifyRemoteClientWithClientId(string clientId);
    public Task<Dictionary<Guid, bool>?> HasNotesAccess(Dictionary<Guid, Guid> guids);
}