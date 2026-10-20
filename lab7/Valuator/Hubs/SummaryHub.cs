using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Valuator.Services;

namespace Valuator.Hubs;

[Authorize]
public sealed class SummaryHub(IStorageService storage) : Hub
{
    public async Task Subscribe(string textId)
    {
        if (!Guid.TryParse(textId, out var id))
        {
            throw new HubException("Некорректный идентификатор текста.");
        }

        var key = id.ToString();
        if (!storage.IsAuthor(key, Context.User?.FindFirstValue(ClaimTypes.NameIdentifier)))
        {
            throw new HubException("Результат недоступен.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, key, Context.ConnectionAborted);
        if (storage.HasRank(key))
        {
            await Clients.Caller.SendAsync("RankCalculated", key, storage.GetRank(key), Context.ConnectionAborted);
        }
    }
}
