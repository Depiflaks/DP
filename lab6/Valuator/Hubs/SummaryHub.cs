using Microsoft.AspNetCore.SignalR;
using Valuator.Services;

namespace Valuator.Hubs;

public sealed class SummaryHub(IStorageService storage) : Hub
{
    public async Task Subscribe(string textId)
    {
        if (!Guid.TryParse(textId, out var id))
        {
            throw new HubException("Некорректный идентификатор текста.");
        }

        var key = id.ToString();
        await Groups.AddToGroupAsync(Context.ConnectionId, key, Context.ConnectionAborted);
        if (storage.HasRank(key))
        {
            await Clients.Caller.SendAsync("RankCalculated", key, storage.GetRank(key), Context.ConnectionAborted);
        }
    }
}
