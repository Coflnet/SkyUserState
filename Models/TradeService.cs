using System.Collections.Generic;
using System.Threading.Tasks;
using Cassandra.Data.Linq;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using Coflnet.Sky.PlayerState.Services;

namespace Coflnet.Sky.PlayerState.Models;

public class TradeService
{
    private ITransactionService transactionService;
    private IItemsService itemService;
    public async Task<IEnumerable<Transaction>> GetTradeTransactions(string itemTag, Guid itemId, DateTime end)
    {
        var items = await itemService.FindItems(new ItemIdSearch[] { new() { Tag = itemTag, Uuid = itemId } });
        var intItemId = items.FirstOrDefault()?.Id ?? throw new ValidationException("Item not found");
        var transfers = await transactionService.GetItemTransactions(intItemId, 1);
        var user = transfers.FirstOrDefault()?.PlayerUuid ?? throw new ValidationException("No transfers for item found");
        var tradeTransactions = await transactionService.GetTransactions(user, TimeSpan.FromSeconds(30), transfers.First().TimeStamp + TimeSpan.FromSeconds(1));

        return tradeTransactions;
    }
}
#nullable restore