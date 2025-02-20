using System.Linq;
using System.Threading.Tasks;
using Cassandra;
using Cassandra.Data.Linq;
using Cassandra.Mapping;

namespace Coflnet.Sky.PlayerState.Services;

public class ShenHistoryService : IShenStorage
{
    Table<ShenHistory> shenHistoryTable;

    public ShenHistoryService(ISession session)
    {
        var mapping = new MappingConfiguration().Define(
            new Map<ShenHistory>()
                .TableName("shen_history")
                .PartitionKey(x => x.Year)
                .ClusteringKey(x => x.Reporter)
                .Column(x => x.Reporter, cm => cm.WithName("reporter"))
                .Column(x => x.ReportTime, cm => cm.WithName("report_time"))
                .Column(x => x.Offers, cm => cm.WithName("offers"))
        );
        shenHistoryTable = new Table<ShenHistory>(session, mapping);
        shenHistoryTable.CreateIfNotExists();
    }

    public async Task Store(ShenHistory shenHistory)
    {
        await shenHistoryTable.Insert(shenHistory).ExecuteAsync();
    }

    public async Task<ShenHistory[]> Get(int year)
    {
        return (await shenHistoryTable.Where(x => x.Year == year).ExecuteAsync()).ToArray();
    }
}
