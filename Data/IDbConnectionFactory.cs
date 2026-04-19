using System.Data;

namespace SheetsSearchApp.Data;

public interface IDbConnectionFactory
{
    IDbConnection CreateConnection();
}
